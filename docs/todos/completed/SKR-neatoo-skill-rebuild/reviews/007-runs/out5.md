I have what I need. Here is the full design, with every piece and why it sits where it does.

**Layout.** Three projects: `zCRM.Domain` (shared by both tiers, so no EF Core reference), `zCRM.Server`, `zCRM.Client`. The dormancy invariant lives once, as a rule on the `Contact` aggregate. The nightly job and the edit screen both exercise the same rule by setting the same property, so neither re-implements it. The job is a `[Remote, Execute]` command in the domain, and a hosted service on the server triggers it at 02:00.

Assumptions I made: a contact with no visits at all counts as "no visit in 180 days". The Visits table has `ContactId` and `VisitDate` columns. The job runs at 02:00 server local time.

## zCRM.Domain

### `Contacts/IContact.cs`

The public face of the aggregate. It extends `IEntityRoot` so it gets `IsSavable` and `Save()`. The ViewModel, page, and job only ever see this interface.

```csharp
using Neatoo;

namespace zCRM.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
public interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? Name { get; set; }
    bool IsKeyAccount { get; set; }
    bool IsDormant { get; set; }

    /// <summary>Most recent visit, loaded from the Visits table. Null when there has never been one.</summary>
    DateTime? LastVisitUtc { get; }

    /// <summary>Verb used by the nightly job. Sets state only; the rules decide whether it holds.</summary>
    void MarkDormant();
}
```

### `Contacts/KeyAccountCannotBeDormantRule.cs`

The invariant spans two properties, and `AddValidation` takes exactly one trigger, so it is a `RuleBase<T>` with both triggers. It fires whenever either checkbox changes on screen or whenever the job calls `MarkDormant()`. The message attaches to `IsDormant`, which is the field the person would need to change.

```csharp
using Neatoo.Rules;

namespace zCRM.Domain.Contacts;

/// <summary>BR-CON-001: a key account is never dormant.</summary>
internal sealed class KeyAccountCannotBeDormantRule : RuleBase<Contact>
{
    public KeyAccountCannotBeDormantRule() : base(t => t.IsKeyAccount, t => t.IsDormant) { }

    protected override IRuleMessages Execute(Contact target)
    {
        if (target.IsKeyAccount && target.IsDormant)
        {
            return (nameof(Contact.IsDormant), "A key account cannot be dormant.").AsRuleMessages();
        }

        return None;
    }
}
```

### `Contacts/Contact.cs`

The aggregate. `Fetch` loads baseline data without triggering rules. `Insert` and `Update` are the server gate: they re-run every rule and refuse an invalid aggregate before writing, so a caller that bypasses `IsSavable` still cannot persist a dormant key account.

```csharp
using Neatoo;
using Neatoo.RemoteFactory;

namespace zCRM.Domain.Contacts;

[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public partial Guid Id { get; private set; }
    public partial string? Name { get; set; }
    public partial bool IsKeyAccount { get; set; }
    public partial bool IsDormant { get; set; }
    public partial DateTime? LastVisitUtc { get; private set; }

    public Contact(IEntityBaseServices<Contact> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name is required." : string.Empty,
            t => t.Name);

        RuleManager.AddRule(new KeyAccountCannotBeDormantRule());
    }

    [Create]
    public async Task Create()
    {
        Id = Guid.NewGuid();
        await RunRules(RunRulesFlag.All); // so a blank form shows its required fields
    }

    [Remote]
    [Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] IContactRepository repository, CancellationToken ct)
    {
        var data = await repository.GetAsync(id, ct);
        if (data is null)
        {
            return false;
        }

        // Paused inside the operation: plain assignment is the clean baseline.
        Id = data.Id;
        Name = data.Name;
        IsKeyAccount = data.IsKeyAccount;
        IsDormant = data.IsDormant;
        LastVisitUtc = data.LastVisitUtc;
        return true;
    }

    public void MarkDormant()
    {
        IsDormant = true; // the property set runs KeyAccountCannotBeDormantRule
    }

    [Remote]
    [Insert]
    internal async Task Insert([Service] IContactRepository repository, CancellationToken ct)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        await repository.InsertAsync(ToData(), ct);
    }

    [Remote]
    [Update]
    internal async Task Update([Service] IContactRepository repository, CancellationToken ct)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        await repository.UpdateAsync(ToData(), ct);
    }

    private ContactData ToData() => new(Id, Name!, IsKeyAccount, IsDormant, LastVisitUtc);
}
```

### `Contacts/IContactRepository.cs`

The persistence seam. It lives in the domain so the aggregate's operations can take it as a `[Service]`, but the implementation is server-only. `ContactData` keeps EF types out of the domain assembly, which also ships to the browser.

```csharp
namespace zCRM.Domain.Contacts;

public sealed record ContactData(
    Guid Id,
    string Name,
    bool IsKeyAccount,
    bool IsDormant,
    DateTime? LastVisitUtc);

public interface IContactRepository
{
    Task<ContactData?> GetAsync(Guid id, CancellationToken ct);
    Task InsertAsync(ContactData data, CancellationToken ct);
    Task UpdateAsync(ContactData data, CancellationToken ct);

    /// <summary>Ids of contacts not yet dormant with no visit on or after <paramref name="cutoffUtc"/>.</summary>
    Task<IReadOnlyList<Guid>> FindDormancyCandidatesAsync(DateTime cutoffUtc, CancellationToken ct);
}
```

### `Jobs/MarkDormantContacts.cs`

The job itself. It is a static `[Remote, Execute]` command so it can be invoked locally on the server by the scheduler and, if you later want a "Run now" admin button, from the client with the same delegate. It calls no `RunRules` and catches nothing: the verb triggers the rule, `WaitForTasks` settles it, and `IsSavable` decides. Key accounts in the candidate set come back invalid and land in `Rejected` with the rule's message, which is the "reported, not saved" behaviour.

```csharp
using Neatoo.RemoteFactory;
using zCRM.Domain.Contacts;

namespace zCRM.Domain.Jobs;

/// <summary>Nightly job: marks contacts dormant after 180 days without a visit.</summary>
[Factory]
public static partial class MarkDormantContacts
{
    public const int DormantAfterDays = 180;

    [Remote]
    [Execute]
    private static async Task<DormancyReport> _Run(
        DateTime asOfUtc,
        [Service] IContactRepository repository,
        [Service] IContactFactory contactFactory,
        CancellationToken ct)
    {
        var cutoffUtc = asOfUtc.AddDays(-DormantAfterDays);
        var candidateIds = await repository.FindDormancyCandidatesAsync(cutoffUtc, ct);

        var marked = new List<Guid>();
        var rejected = new List<RejectedContact>();
        var unchanged = new List<Guid>();

        foreach (var id in candidateIds)
        {
            ct.ThrowIfCancellationRequested();

            var contact = await contactFactory.Fetch(id, ct);
            if (contact is null)
            {
                unchanged.Add(id); // deleted between the query and the fetch
                continue;
            }

            contact.MarkDormant();
            await contact.WaitForTasks();

            if (contact.IsSavable)
            {
                contact = (IContact)await contact.Save();
                marked.Add(contact.Id);
            }
            else if (!contact.IsValid)
            {
                rejected.Add(new RejectedContact(
                    id,
                    contact.Name,
                    contact.PropertyMessages
                        .Select(m => $"{m.Property.Name}: {m.Message}")
                        .ToList()));
            }
            else
            {
                unchanged.Add(id);
            }
        }

        return new DormancyReport(asOfUtc, cutoffUtc, marked, rejected, unchanged);
    }
}

/// <summary>What the job did: data, not a success flag.</summary>
public sealed record DormancyReport(
    DateTime AsOfUtc,
    DateTime CutoffUtc,
    IReadOnlyList<Guid> Marked,
    IReadOnlyList<RejectedContact> Rejected,
    IReadOnlyList<Guid> Unchanged);

/// <summary>A contact the rules refused, with the messages a screen would have shown.</summary>
public sealed record RejectedContact(Guid ContactId, string? Name, IReadOnlyList<string> Messages);
```

## zCRM.Server

### `Data/EfContactRepository.cs`

Implements the seam over your existing `CrmDbContext`. The EF `Contact` entity is aliased to avoid clashing with the aggregate name. The candidate query deliberately does not filter out key accounts, so the rule remains the single place that decides. If the nightly report gets noisy, add `.Where(c => !c.IsKeyAccount)` here; the rule still guards the write either way.

```csharp
using Microsoft.EntityFrameworkCore;
using zCRM.Domain.Contacts;
using ContactRow = zCRM.Server.Data.Contact;

namespace zCRM.Server.Data;

internal sealed class EfContactRepository : IContactRepository
{
    private readonly CrmDbContext _db;

    public EfContactRepository(CrmDbContext db)
    {
        _db = db;
    }

    public async Task<ContactData?> GetAsync(Guid id, CancellationToken ct)
    {
        return await _db.Contacts
            .Where(c => c.Id == id)
            .Select(c => new ContactData(
                c.Id,
                c.Name,
                c.IsKeyAccount,
                c.IsDormant,
                _db.Visits
                    .Where(v => v.ContactId == c.Id)
                    .Max(v => (DateTime?)v.VisitDate)))
            .SingleOrDefaultAsync(ct);
    }

    public async Task InsertAsync(ContactData data, CancellationToken ct)
    {
        _db.Contacts.Add(new ContactRow
        {
            Id = data.Id,
            Name = data.Name,
            IsKeyAccount = data.IsKeyAccount,
            IsDormant = data.IsDormant,
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ContactData data, CancellationToken ct)
    {
        var row = await _db.Contacts.SingleAsync(c => c.Id == data.Id, ct);
        row.Name = data.Name;
        row.IsKeyAccount = data.IsKeyAccount;
        row.IsDormant = data.IsDormant;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> FindDormancyCandidatesAsync(DateTime cutoffUtc, CancellationToken ct)
    {
        return await _db.Contacts
            .Where(c => !c.IsDormant)
            .Where(c => !_db.Visits.Any(v => v.ContactId == c.Id && v.VisitDate >= cutoffUtc))
            .Select(c => c.Id)
            .ToListAsync(ct);
    }
}
```

### `Jobs/DormancyJobHostedService.cs`

How the job is started. A `BackgroundService` sleeps until the next 02:00, opens a DI scope, resolves the generated `MarkDormantContacts.Run` delegate, and invokes it. On the server that delegate calls the method directly with server services. The scope matters because factories and the DbContext are scoped. Failures are logged and the loop continues to the next night.

```csharp
using zCRM.Domain.Jobs;

namespace zCRM.Server.Jobs;

internal sealed class DormancyJobHostedService : BackgroundService
{
    private static readonly TimeOnly RunAt = new(2, 0);

    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _time;
    private readonly ILogger<DormancyJobHostedService> _log;

    public DormancyJobHostedService(
        IServiceScopeFactory scopes,
        TimeProvider time,
        ILogger<DormancyJobHostedService> log)
    {
        _scopes = scopes;
        _time = time;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNextRun(_time.GetLocalNow());
            _log.LogInformation("Dormancy job next run in {Delay}", delay);

            try
            {
                await Task.Delay(delay, _time, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await RunOnceAsync(stoppingToken);
        }
    }

    internal async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var run = scope.ServiceProvider.GetRequiredService<MarkDormantContacts.Run>();

        try
        {
            var report = await run(_time.GetUtcNow().UtcDateTime, ct);

            _log.LogInformation(
                "Dormancy job: {Marked} marked, {Rejected} rejected, {Unchanged} unchanged (cutoff {Cutoff:u})",
                report.Marked.Count, report.Rejected.Count, report.Unchanged.Count, report.CutoffUtc);

            foreach (var r in report.Rejected)
            {
                _log.LogWarning(
                    "Dormancy job: contact {ContactId} ({Name}) not saved: {Messages}",
                    r.ContactId, r.Name, string.Join("; ", r.Messages));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogError(ex, "Dormancy job failed");
        }
    }

    internal static TimeSpan DelayUntilNextRun(DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.DateTime);
        var next = today.ToDateTime(RunAt);
        if (next <= now.DateTime)
        {
            next = today.AddDays(1).ToDateTime(RunAt);
        }

        return next - now.DateTime;
    }
}
```

### `Program.cs`

Registration order matters: `AddNeatooServices(Server)` registers the factories, the generated delegates, and what `UseNeatoo()` needs. Do not add `AddNeatooAspNetCore` after it. The repository is scoped to match the DbContext, and the hosted service plus `TimeProvider` are what start the job.

```csharp
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Neatoo;
using zCRM.Domain.Contacts;
using zCRM.Server.Data;
using zCRM.Server.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CrmDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Crm")));

// Neatoo in Server mode over the domain assembly. Registers IContactFactory
// and the MarkDormantContacts.Run delegate. Nothing Neatoo-related after this line.
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);

// The [Service] dependency the aggregate's operations and the job resolve.
builder.Services.AddScoped<IContactRepository, EfContactRepository>();

// The scheduler that starts the nightly job.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<DormancyJobHostedService>();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseNeatoo();           // the generated endpoints the client's proxies call
app.MapFallbackToFile("index.html");

app.Run();
```

## zCRM.Client

### `ViewModels/ContactEditViewModel.cs`

Holds the live aggregate and adapts gestures. It decides nothing: the Save button binds to the entity's `IsSavable`, and a dormant key account is already invalid by the time the user looks at the button.

```csharp
using zCRM.Domain.Contacts;

namespace zCRM.Client.ViewModels;

public sealed class ContactEditViewModel
{
    private readonly IContactFactory _factory;

    public ContactEditViewModel(IContactFactory factory)
    {
        _factory = factory;
    }

    public IContact? Contact { get; private set; }

    public bool IsSaving { get; private set; }

    public async Task New()
    {
        Contact = await _factory.Create();
    }

    public async Task Load(Guid id)
    {
        Contact = await _factory.Fetch(id);
    }

    public bool CanSave => !IsSaving && (Contact?.IsSavable ?? false);

    public async Task Save()
    {
        if (Contact is null)
        {
            return;
        }

        await Contact.WaitForTasks();
        if (!Contact.IsSavable)
        {
            return; // the validation summary already shows why
        }

        IsSaving = true; // Save() does not mark the entity busy; guard the double click here
        try
        {
            Contact = (IContact)await Contact.Save();
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

### `Pages/ContactEdit.razor`

The edit screen. Both checkboxes bind straight to the entity's properties, so ticking "Dormant" on a key account surfaces the rule's message on that field and disables Save with no condition written in the page. The page owns one subscription so the Save button re-renders when any rule changes state.

```razor
@page "/contacts/{Id:guid}"
@page "/contacts/new"
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using zCRM.Client.ViewModels
@using zCRM.Domain.Contacts
@inject ContactEditViewModel Vm
@implements IDisposable

<MudText Typo="Typo.h5" Class="mb-4">Contact</MudText>

@if (Vm.Contact is null)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    @* @key: Save returns a new instance; keying rebuilds the bound fields. *@
    <div @key="Vm.Contact">
        <NeatooValidationSummary Entity="@Vm.Contact" Class="mb-4" />

        <MudNeatooTextField T="string"
                            EntityProperty="@Vm.Contact[nameof(IContact.Name)]"
                            Variant="Variant.Outlined"
                            Class="mb-3" />

        <MudNeatooCheckBox T="bool"
                           EntityProperty="@Vm.Contact[nameof(IContact.IsKeyAccount)]"
                           Label="Key account"
                           Class="mb-3" />

        <MudNeatooCheckBox T="bool"
                           EntityProperty="@Vm.Contact[nameof(IContact.IsDormant)]"
                           Label="Dormant"
                           Class="mb-3" />

        <MudText Class="mb-4">
            Last visit: @(Vm.Contact.LastVisitUtc?.ToLocalTime().ToString("d") ?? "never")
        </MudText>

        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   Disabled="@(!Vm.CanSave)"
                   OnClick="SaveAsync">
            Save
        </MudButton>
    </div>
}

@code {
    [Parameter] public Guid? Id { get; set; }

    private IContact? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        if (Id is Guid id)
        {
            await Vm.Load(id);
        }
        else
        {
            await Vm.New();
        }

        Resubscribe();
    }

    private void Resubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnEntityChanged;
        }

        _subscribed = Vm.Contact;

        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged += OnEntityChanged;
        }
    }

    private async Task SaveAsync()
    {
        await Vm.Save();
        Resubscribe();
        StateHasChanged();
    }

    private Task OnEntityChanged(NeatooPropertyChangedEventArgs e)
    {
        return InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnEntityChanged;
        }
    }
}
```

### `Program.cs`

The client registers Neatoo in Remote mode. The generated `IContactFactory` is a proxy here, so `Fetch` and `Save` cross to the server. The `[Remote]` job body and the repository are never needed in the browser and are trimmed away in a published build.

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Client.ViewModels;
using zCRM.Domain.Contacts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);
builder.Services.AddRemoteFactoryTransport(new Uri(builder.HostEnvironment.BaseAddress));
builder.Services.AddMudServices();
builder.Services.AddTransient<ContactEditViewModel>();

await builder.Build().RunAsync();
```

## How the pieces meet the three requirements

- **Nightly marking.** The hosted service wakes at 02:00, the job queries candidates through the repository, and applies `MarkDormant()` to each aggregate.
- **Key account cannot be dormant, in both paths.** One `RuleBase` on the aggregate fires on either property change. The job hits it through the verb, the screen through the checkbox. The `[Update]` gate on the server refuses an invalid aggregate even if a caller skipped `IsSavable`.
- **Failures reported, not saved.** The job never throws for a rule failure. Invalid contacts go into the report's `Rejected` list with the rule's messages, and the hosted service logs each one.
