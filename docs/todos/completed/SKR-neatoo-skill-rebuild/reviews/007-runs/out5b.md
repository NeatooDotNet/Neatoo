I have what I need: the compiled "job over entities" shape, the `RuleBase<T>` multi-trigger API, the server gate, the generated command delegate type (`Class.Method`), and the server registration order. Here is the full design.

Assumptions, stated once: the EF entity is `zCRM.Server.Data.Contact` with `Id`, `Name`, `Email`, `IsKeyAccount`, `IsDormant`, and `Visit` has `ContactId` and `VisitedOnUtc`. "No visit in 180 days" includes contacts that were never visited. The nightly query does not exclude key accounts on purpose: the entity rule is the single place that says a key account cannot be dormant, so the job reaches it and reports the refusal, exactly as the screen would. If that nightly report is unwanted noise, add `!c.IsKeyAccount` to the candidate query and the rule stays as the safety net.

## Domain (zCRM.Domain, shared by both tiers)

**The rule** lives in the domain because it runs in the browser on each toggle and on the server inside the job and the save gate, from one class. It triggers on both properties so flipping either one re-evaluates it.

```csharp
// zCRM.Domain/Contacts/KeyAccountCannotBeDormantRule.cs
using Neatoo.Rules;

namespace zCRM.Domain.Contacts;

/// <summary>BR-CON-001: a key account is never dormant.</summary>
internal sealed class KeyAccountCannotBeDormantRule : RuleBase<Contact>
{
    public KeyAccountCannotBeDormantRule()
        : base(t => t.IsKeyAccount, t => t.IsDormant) { }

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

**The repository contract and row** sit in the domain so the aggregate's operations can take them as `[Service]` parameters without the domain referencing EF Core. The row is a plain class the server maps to and from the EF entity.

```csharp
// zCRM.Domain/Contacts/IContactRepository.cs
namespace zCRM.Domain.Contacts;

/// <summary>Persistence shape of a Contact aggregate.</summary>
public sealed class ContactRow
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public bool IsKeyAccount { get; set; }
    public bool IsDormant { get; set; }
    public DateTime? LastVisitOnUtc { get; set; }
}

/// <summary>Repository for Contact aggregates. Server-only implementation.</summary>
public interface IContactRepository
{
    Task<ContactRow?> Get(Guid id);
    Task Insert(ContactRow row);
    Task Update(ContactRow row);

    /// <summary>Ids of non-dormant contacts with no visit on or after <paramref name="cutoffUtc"/>.</summary>
    Task<IReadOnlyList<Guid>> GetDormancyCandidateIds(DateTime cutoffUtc);
}
```

**The aggregate** is a Neatoo entity because both a person and the job edit it and rely on `IsValid` and `IsSavable`. `MarkDormant` is the verb the job calls; it only sets state. `Insert` and `Update` re-run the rules and throw, which is the server's net against a caller that skipped `IsSavable`.

```csharp
// zCRM.Domain/Contacts/IContact.cs
using Neatoo;

namespace zCRM.Domain.Contacts;

public interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? Name { get; set; }
    string? Email { get; set; }
    bool IsKeyAccount { get; set; }
    bool IsDormant { get; set; }
    DateTime? LastVisitOnUtc { get; }

    /// <summary>Verb used by the nightly job. Sets state; the rules decide validity.</summary>
    void MarkDormant();
}
```

```csharp
// zCRM.Domain/Contacts/Contact.cs
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;

namespace zCRM.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public partial Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(200)]
    public partial string? Name { get; set; }

    [EmailAddress]
    public partial string? Email { get; set; }

    public partial bool IsKeyAccount { get; set; }

    public partial bool IsDormant { get; set; }

    public partial DateTime? LastVisitOnUtc { get; set; }

    public Contact(IEntityBaseServices<Contact> services) : base(services)
    {
        RuleManager.AddRule(new KeyAccountCannotBeDormantRule());
    }

    public void MarkDormant()
    {
        IsDormant = true;
    }

    [Create]
    public void Create()
    {
    }

    // The object is paused for the body: assignment is a clean baseline load.
    [Remote]
    [Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] IContactRepository repository)
    {
        var row = await repository.Get(id);
        if (row is null)
        {
            return false;
        }

        Id = row.Id;
        Name = row.Name;
        Email = row.Email;
        IsKeyAccount = row.IsKeyAccount;
        IsDormant = row.IsDormant;
        LastVisitOnUtc = row.LastVisitOnUtc;
        return true;
    }

    [Remote]
    [Insert]
    internal async Task Insert([Service] IContactRepository repository)
    {
        await RefuseIfInvalid();
        Id = Guid.NewGuid();
        await repository.Insert(ToRow());
    }

    [Remote]
    [Update]
    internal async Task Update([Service] IContactRepository repository)
    {
        await RefuseIfInvalid();
        await repository.Update(ToRow());
    }

    // Server gate: re-run every rule and throw, never return, on an invalid
    // aggregate. After [Insert]/[Update] return the framework marks the entity
    // saved whether or not anything was written.
    private async Task RefuseIfInvalid()
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }
    }

    private ContactRow ToRow() => new()
    {
        Id = Id,
        Name = Name,
        Email = Email,
        IsKeyAccount = IsKeyAccount,
        IsDormant = IsDormant,
        LastVisitOnUtc = LastVisitOnUtc,
    };
}
```

**The job** is a `[Remote, Execute]` command in the domain. It is the seam that drives many aggregates with server services, so it is neither a verb on `Contact` nor code in the host. It calls no `RunRules` and catches nothing: the verb triggers the rule, `WaitForTasks` settles it, and `IsSavable` decides. `[Remote]` also lets an admin button run it from the client later.

```csharp
// zCRM.Domain/Jobs/MarkDormantContacts.cs
using Neatoo.RemoteFactory;
using zCRM.Domain.Contacts;

namespace zCRM.Domain.Jobs;

/// <summary>
/// Job: marks contacts with no visit in the inactivity period as dormant.
/// Reuses the Contact aggregate, its MarkDormant verb and its rules.
/// </summary>
[Factory]
public static partial class MarkDormantContacts
{
    public static readonly TimeSpan InactivityPeriod = TimeSpan.FromDays(180);

    [Remote]
    [Execute]
    private static async Task<DormancyReport> _Run(
        DateTime asOfUtc,
        [Service] IContactRepository repository,
        [Service] IContactFactory contactFactory)
    {
        var cutoffUtc = asOfUtc - InactivityPeriod;
        var candidateIds = await repository.GetDormancyCandidateIds(cutoffUtc);

        var marked = new List<Guid>();
        var rejected = new List<RejectedContact>();
        var unchanged = new List<Guid>();

        foreach (var id in candidateIds)
        {
            var contact = await contactFactory.Fetch(id)
                ?? throw new InvalidOperationException($"Contact {id} not found");

            // The verb sets IsDormant; the key-account rule runs now.
            contact.MarkDormant();
            await contact.WaitForTasks();

            if (contact.IsSavable)
            {
                contact = (IContact)await contact.Save();
                marked.Add(contact.Id);
            }
            else if (!contact.IsValid)
            {
                // The rules have spoken: report, do not save, do not throw.
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

## Server (zCRM.Server)

**The repository** implements the domain contract over the existing DbContext. It lives on the server because it is the only place EF Core exists. The Neatoo `Contact` is `internal` to the domain assembly, so the EF `Contact` name does not clash here.

```csharp
// zCRM.Server/Data/ContactRepository.cs
using Microsoft.EntityFrameworkCore;
using zCRM.Domain.Contacts;

namespace zCRM.Server.Data;

internal sealed class ContactRepository : IContactRepository
{
    private readonly CrmDbContext _db;

    public ContactRepository(CrmDbContext db)
    {
        _db = db;
    }

    public Task<ContactRow?> Get(Guid id)
    {
        return _db.Contacts
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ContactRow
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                IsKeyAccount = c.IsKeyAccount,
                IsDormant = c.IsDormant,
                LastVisitOnUtc = _db.Visits
                    .Where(v => v.ContactId == c.Id)
                    .Max(v => (DateTime?)v.VisitedOnUtc),
            })
            .SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<Guid>> GetDormancyCandidateIds(DateTime cutoffUtc)
    {
        return await _db.Contacts
            .AsNoTracking()
            .Where(c => !c.IsDormant)
            .Where(c => !_db.Visits.Any(v => v.ContactId == c.Id && v.VisitedOnUtc >= cutoffUtc))
            .OrderBy(c => c.Id)
            .Select(c => c.Id)
            .ToListAsync();
    }

    public async Task Insert(ContactRow row)
    {
        _db.Contacts.Add(new Contact
        {
            Id = row.Id,
            Name = row.Name!,
            Email = row.Email,
            IsKeyAccount = row.IsKeyAccount,
            IsDormant = row.IsDormant,
        });
        await _db.SaveChangesAsync();
    }

    public async Task Update(ContactRow row)
    {
        var entity = await _db.Contacts.SingleAsync(c => c.Id == row.Id);
        entity.Name = row.Name!;
        entity.Email = row.Email;
        entity.IsKeyAccount = row.IsKeyAccount;
        entity.IsDormant = row.IsDormant;
        await _db.SaveChangesAsync();
    }
}
```

**The hosted service** is how the job is started. It is host plumbing, so it lives on the server, not in the domain. It owns the schedule and a DI scope and nothing else. Each run resolves the generated command delegate, `MarkDormantContacts.Run`, from the scope and logs the report. A failed night is logged and the loop continues.

```csharp
// zCRM.Server/Jobs/DormancyJobOptions.cs
namespace zCRM.Server.Jobs;

public sealed class DormancyJobOptions
{
    /// <summary>Time of day, UTC, the job runs. Default 02:00.</summary>
    public TimeOnly RunAtUtc { get; set; } = new(2, 0);
}
```

```csharp
// zCRM.Server/Jobs/NightlyDormancyJob.cs
using Microsoft.Extensions.Options;
using zCRM.Domain.Jobs;

namespace zCRM.Server.Jobs;

/// <summary>Runs MarkDormantContacts once a night.</summary>
internal sealed class NightlyDormancyJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _time;
    private readonly DormancyJobOptions _options;
    private readonly ILogger<NightlyDormancyJob> _logger;

    public NightlyDormancyJob(
        IServiceScopeFactory scopeFactory,
        TimeProvider time,
        IOptions<DormancyJobOptions> options,
        ILogger<NightlyDormancyJob> logger)
    {
        _scopeFactory = scopeFactory;
        _time = time;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNextRun(_time.GetUtcNow(), _options.RunAtUtc);
            _logger.LogInformation("Dormancy job next run in {Delay}", delay);

            try
            {
                await Task.Delay(delay, _time, stoppingToken);
                await RunOnce(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // One bad night must not stop every following night.
                _logger.LogError(ex, "Dormancy job failed");
            }
        }
    }

    internal async Task RunOnce(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        // The generated delegate for MarkDormantContacts._Run. On the server
        // it calls the method directly with this scope's services.
        var run = scope.ServiceProvider.GetRequiredService<MarkDormantContacts.Run>();

        var report = await run(_time.GetUtcNow().UtcDateTime);

        _logger.LogInformation(
            "Dormancy job: {Marked} marked dormant, {Rejected} rejected, {Unchanged} unchanged (cutoff {Cutoff:u})",
            report.Marked.Count, report.Rejected.Count, report.Unchanged.Count, report.CutoffUtc);

        foreach (var r in report.Rejected)
        {
            _logger.LogWarning(
                "Contact {ContactId} '{Name}' not marked dormant: {Messages}",
                r.ContactId, r.Name, string.Join("; ", r.Messages));
        }
    }

    internal static TimeSpan DelayUntilNextRun(DateTimeOffset nowUtc, TimeOnly runAtUtc)
    {
        var today = nowUtc.Date.Add(runAtUtc.ToTimeSpan());
        var next = today > nowUtc.UtcDateTime ? today : today.AddDays(1);
        return next - nowUtc.UtcDateTime;
    }
}
```

**Server composition.** Neatoo in Server mode registers the factories and the command delegate and also what `UseNeatoo` resolves. Do not add `AddNeatooAspNetCore` after it; that breaks every remote save (RemoteFactory issue 117).

```csharp
// zCRM.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Neatoo;
using zCRM.Domain.Contacts;
using zCRM.Server.Data;
using zCRM.Server.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CrmDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Crm")));

// Neatoo on the server: Server mode, the domain assembly.
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);

// [Service] dependencies the domain's operations resolve.
builder.Services.AddScoped<IContactRepository, ContactRepository>();

// The nightly job and its schedule.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<DormancyJobOptions>(builder.Configuration.GetSection("DormancyJob"));
builder.Services.AddHostedService<NightlyDormancyJob>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseNeatoo();            // the /api/neatoo endpoint the client proxies call

app.MapRazorPages();
app.MapFallbackToFile("index.html");

app.Run();
```

```json
// zCRM.Server/appsettings.json (fragment)
"DormancyJob": { "RunAtUtc": "02:00" }
```

## Client (zCRM.Client, Blazor WebAssembly)

**The ViewModel** holds the live aggregate and adapts gestures. It decides nothing: the Save button binds to `IsSavable`, and the key-account message is already on the entity. The in-flight flag covers the double-click gap in `Save` (Neatoo issue 103).

```csharp
// zCRM.Client/ViewModels/ContactEditViewModel.cs
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

    public bool CanSave => Contact is { IsSavable: true } && !IsSaving;

    public Task New()
    {
        Contact = _factory.Create();
        return Task.CompletedTask;
    }

    public async Task Load(Guid id)
    {
        Contact = await _factory.Fetch(id);
    }

    public async Task Save()
    {
        if (Contact is null || IsSaving)
        {
            return;
        }

        await Contact.WaitForTasks();
        if (!Contact.IsSavable)
        {
            return;   // the validation summary already shows why
        }

        IsSaving = true;
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

**The page** binds fields to entity properties through MudNeatoo. Toggling either switch runs the same rule the job runs, and the summary shows the message. The `@key` rebuilds the bound components after `Save` returns a new instance.

```razor
@* zCRM.Client/Pages/ContactEdit.razor *@
@page "/contacts/new"
@page "/contacts/{Id:guid}"
@using zCRM.Domain.Contacts
@using zCRM.Client.ViewModels
@inject ContactEditViewModel Vm

<MudText Typo="Typo.h5" Class="mb-4">Contact</MudText>

@if (Vm.Contact is null)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    <div @key="Vm.Contact">
        <NeatooValidationSummary Entity="@Vm.Contact" Class="mb-4" />

        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.Name)]" Class="mb-3" />
        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.Email)]" Class="mb-3" />

        <MudNeatooSwitch T="bool" EntityProperty="@Vm.Contact[nameof(IContact.IsKeyAccount)]" Class="mb-3" />
        <MudNeatooSwitch T="bool" EntityProperty="@Vm.Contact[nameof(IContact.IsDormant)]" Class="mb-3" />

        @if (Vm.Contact.LastVisitOnUtc is { } lastVisit)
        {
            <MudText Typo="Typo.body2" Class="mb-3">Last visit: @lastVisit.ToLocalTime().ToString("d")</MudText>
        }
        else
        {
            <MudText Typo="Typo.body2" Class="mb-3">No visits recorded.</MudText>
        }

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

    protected override async Task OnParametersSetAsync()
    {
        if (Id is { } id)
        {
            await Vm.Load(id);
        }
        else
        {
            await Vm.New();
        }
    }

    private async Task SaveAsync()
    {
        await Vm.Save();
        StateHasChanged();
    }
}
```

**Client composition.** Remote mode makes every factory a proxy; `[Remote]` operations cross to the server, while the rule and `IsSavable` run in the browser.

```csharp
// zCRM.Client/Program.cs
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

// Neatoo on the client: Remote mode, the domain assembly.
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);

// The keyed HttpClient the generated proxies call through.
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactEditViewModel>();

await builder.Build().RunAsync();
```

How the pieces meet the requirement:

- **One rule, two drivers.** The key-account rule is a `RuleBase<Contact>` on the aggregate. The screen triggers it per toggle; the job triggers it through `MarkDormant`. Neither side re-derives it.
- **Report, do not save.** The job reads `IsSavable` after `WaitForTasks`. Invalid contacts go into the report with their property messages and are never sent to `Save`.
- **Server net.** `Insert` and `Update` re-run every rule and throw on an invalid aggregate, so a direct factory save cannot write a dormant key account either.
- **Starting the job.** A `BackgroundService` sleeps until the configured UTC time, opens a scope, resolves the generated command delegate, and logs the report.
