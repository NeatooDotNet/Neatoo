I've written the code below in my reply only (no files were created) and none of it has been compiled. A few API details I couldn't confirm from the Neatoo/RemoteFactory references are listed at the end.

**The core design:** "A key account cannot be dormant" is one validation rule on the `Contact` entity. The nightly job and the edit screen both change the same properties on the same object, so both hit the same rule. The job never repeats the rule in its own code. It sets `IsDormant`, waits for the rules to finish, and lets `IsSavable` decide whether the contact is saved, reported or skipped.

## 1. Domain (`ZCrm.Domain`, the assembly both the browser and the server load)

**Where and why:** the entity and its rule live in the shared domain assembly so the browser and the server run the same rule. The job can't skip it and the screen can't miss it. I've assumed your EF row class is `ZCrm.Data.Contact`. It's aliased as `ContactRow` so it doesn't clash with the Neatoo entity.

`Contacts/IContact.cs`: `partial`, so the generator adds `SetIsDormant(...)` and the other `Set` methods to the interface.
```cs
using Neatoo;

namespace ZCrm.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
public partial interface IContact : IEntityRoot
{
    int Id { get; }
    string? Name { get; set; }
    bool IsKeyAccount { get; set; }
    bool IsDormant { get; set; }
}
```

`Contacts/KeyAccountCannotBeDormantRule.cs`: a rule class rather than `AddValidation`, because `AddValidation` accepts only one trigger property. This rule needs two, so it re-checks when either checkbox changes. The message goes on `IsDormant`.
```cs
using Neatoo.Rules;

namespace ZCrm.Domain.Contacts;

/// <summary>A key account cannot be dormant. Re-checked when either flag changes.</summary>
internal sealed class KeyAccountCannotBeDormantRule : RuleBase<Contact>
{
    public KeyAccountCannotBeDormantRule() : base(t => t.IsKeyAccount, t => t.IsDormant) { }

    protected override IRuleMessages Execute(Contact target)
    {
        return new RuleMessages()
            .If(target.IsKeyAccount && target.IsDormant,
                nameof(Contact.IsDormant),
                "A key account cannot be dormant.");
    }
}
```

`Contacts/Contact.cs`:
```cs
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using ZCrm.Data;
using ContactRow = ZCrm.Data.Contact;

namespace ZCrm.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public Contact(IEntityBaseServices<Contact> services) : base(services)
    {
        RuleManager.AddRule(new KeyAccountCannotBeDormantRule());
    }

    public partial int Id { get; private set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(200)]
    public partial string? Name { get; set; }

    public partial bool IsKeyAccount { get; set; }

    public partial bool IsDormant { get; set; }

    [Create]
    public void Create()
    {
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(int id, [Service] ZCrmDbContext db, CancellationToken cancellationToken)
    {
        var row = await db.Contacts.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (row is null)
        {
            return false;
        }

        Id = row.Id;
        Name = row.Name;
        IsKeyAccount = row.IsKeyAccount;
        IsDormant = row.IsDormant;
        return true;
    }

    [Remote, Insert]
    internal async Task Insert([Service] ZCrmDbContext db)
    {
        // Server gate: refuse an invalid contact from a caller that skipped IsSavable.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = new ContactRow();
        MapTo(row);
        db.Contacts.Add(row);
        await db.SaveChangesAsync();
        Id = row.Id;
    }

    [Remote, Update]
    internal async Task Update([Service] ZCrmDbContext db)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await db.Contacts.SingleOrDefaultAsync(c => c.Id == Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");
        MapTo(row);
        await db.SaveChangesAsync();
    }

    private void MapTo(ContactRow row)
    {
        row.Name = Name!;
        row.IsKeyAccount = IsKeyAccount;
        row.IsDormant = IsDormant;
    }
}
```
`Fetch` doesn't run the rules because stored data is assumed valid. `Insert`/`Update` re-run every rule and throw on an invalid contact. That's a safety net for callers that bypass `IsSavable`, not the place where users get validation messages.

## 2. The job (`ZCrm.Server/Jobs`, server only)

**Where and why:** it's a RemoteFactory command, so its database query runs inside a factory operation. It has no `[Remote]`, so the browser has no endpoint to start it. It sits in the server project, so its EF query never ships to the browser.

`Jobs/MarkDormantContacts.cs`:
```cs
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using ZCrm.Data;
using ZCrm.Domain.Contacts;

namespace ZCrm.Server.Jobs;

/// <summary>
/// Nightly job: marks contacts with no visit in the last 180 days as dormant. Reuses the
/// Contact aggregate and its rules; a contact the rules refuse is reported, not saved.
/// </summary>
[Factory]
public static partial class MarkDormantContacts
{
    public const int InactiveDays = 180;

    // No [Remote]: the nightly host is the only caller.
    [Execute]
    private static async Task<DormancyReport> _Run(
        [Service] ZCrmDbContext db,
        [Service] IContactFactory contactFactory,
        [Service] TimeProvider time,
        CancellationToken cancellationToken)
    {
        var cutoffUtc = time.GetUtcNow().UtcDateTime.AddDays(-InactiveDays);

        // Selected by visit history only. Key accounts are not filtered here:
        // that rule is Contact's, and it reports them below.
        var candidateIds = await db.Contacts
            .AsNoTracking()
            .Where(c => !c.IsDormant)
            .Where(c => !db.Visits.Any(v => v.ContactId == c.Id && v.VisitedOn >= cutoffUtc))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var markedDormant = new List<int>();
        var rejected = new List<RejectedContact>();
        var unchanged = new List<int>();

        foreach (var id in candidateIds)
        {
            var contact = await contactFactory.Fetch(id, cancellationToken);
            if (contact is null)
            {
                continue;  // deleted since the query ran
            }

            // The same write the Dormant checkbox makes; the rules run now.
            await contact.SetIsDormant(true);
            await contact.WaitForTasks();

            if (contact.IsSavable)
            {
                contact = (IContact)await contact.Save();
                markedDormant.Add(contact.Id);
            }
            else if (!contact.IsValid)
            {
                rejected.Add(new RejectedContact(
                    contact.Id,
                    contact.Name,
                    contact.PropertyMessages.Select(m => $"{m.Property.Name}: {m.Message}").ToList()));
            }
            else
            {
                unchanged.Add(contact.Id);
            }
        }

        return new DormancyReport(cutoffUtc, markedDormant, rejected, unchanged);
    }
}

/// <summary>What one night's run did.</summary>
public sealed record DormancyReport(
    DateTime CutoffUtc,
    IReadOnlyList<int> MarkedDormant,
    IReadOnlyList<RejectedContact> Rejected,
    IReadOnlyList<int> Unchanged);

/// <summary>A contact the rules refused, with the messages a screen would show.</summary>
public sealed record RejectedContact(int ContactId, string? Name, IReadOnlyList<string> Messages);
```

## 3. How it is started (`ZCrm.Server`)

**Where and why:** a hosted service only handles timing and logging. It runs as a singleton, so it creates a new DI scope for each run to get a fresh `DbContext` and fresh factories.

`Jobs/DormancyJobHostedService.cs`:
```cs
namespace ZCrm.Server.Jobs;

/// <summary>Runs MarkDormantContacts every night at 02:00 server local time and logs the report.</summary>
internal sealed class DormancyJobHostedService : BackgroundService
{
    private static readonly TimeOnly RunAt = new(2, 0);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _time;
    private readonly ILogger<DormancyJobHostedService> _logger;

    public DormancyJobHostedService(
        IServiceScopeFactory scopeFactory,
        TimeProvider time,
        ILogger<DormancyJobHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(DelayUntilNextRun(), _time, stoppingToken);
            await RunOnce(stoppingToken);
        }
    }

    private async Task RunOnce(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var run = scope.ServiceProvider.GetRequiredService<MarkDormantContacts.Run>();
            var report = await run(stoppingToken);

            _logger.LogInformation(
                "Dormancy job (cutoff {Cutoff:u}): {Marked} marked dormant, {Rejected} rejected, {Unchanged} unchanged",
                report.CutoffUtc, report.MarkedDormant.Count, report.Rejected.Count, report.Unchanged.Count);

            foreach (var contact in report.Rejected)
            {
                _logger.LogWarning(
                    "Contact {ContactId} ({Name}) not marked dormant: {Messages}",
                    contact.ContactId, contact.Name, string.Join("; ", contact.Messages));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Host boundary: log and keep the schedule; the job is safe to repeat.
            _logger.LogError(ex, "Dormancy job failed; it runs again tomorrow night");
        }
    }

    private TimeSpan DelayUntilNextRun()
    {
        var now = _time.GetLocalNow();
        var next = new DateTimeOffset(now.Date + RunAt.ToTimeSpan(), now.Offset);
        if (next <= now)
        {
            next = next.AddDays(1);
        }

        return next - now;
    }
}
```

`Program.cs`:
```cs
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using ZCrm.Data;
using ZCrm.Domain.Contacts;
using ZCrm.Server.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Neatoo in Server mode over both assemblies that declare [Factory] types.
// Do not also call AddNeatooAspNetCore: registered after this, it breaks every remote Save (RemoteFactory#117).
builder.Services.AddNeatooServices(
    NeatooFactory.Server,
    typeof(IContact).Assembly,
    typeof(MarkDormantContacts).Assembly);

builder.Services.AddDbContext<ZCrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ZCrm")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<DormancyJobHostedService>();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// The one RemoteFactory endpoint; no controllers.
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

## 4. The screen (`ZCrm.Client`)

Nothing on the screen enforces the rule. The checkboxes write the properties, the rule runs in the browser, the message shows up, and Save stays disabled.

`Program.cs`:
```cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using ZCrm.Client;
using ZCrm.Client.ViewModels;
using ZCrm.Domain.Contacts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The job's assembly is not registered here: the client knows nothing about it.
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddTransient<ContactEditViewModel>();

await builder.Build().RunAsync();
```

`_Imports.razor` (add these lines to the template's):
```razor
@using MudBlazor
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using Neatoo.Blazor.MudNeatoo.Extensions
```

`ViewModels/ContactEditViewModel.cs`:
```cs
using ZCrm.Domain.Contacts;

namespace ZCrm.Client.ViewModels;

/// <summary>ViewModel for the contact edit screen: holds the contact, saves it.</summary>
internal sealed class ContactEditViewModel
{
    private readonly IContactFactory _contactFactory;

    public ContactEditViewModel(IContactFactory contactFactory)
    {
        _contactFactory = contactFactory;
    }

    public IContact? Contact { get; private set; }

    public bool IsSaving { get; private set; }

    // Also disabled while a save is in flight: Save() does not mark the entity busy (Neatoo#103).
    public bool CanSave => !IsSaving && (Contact?.IsSavable ?? false);

    public async Task Load(int id)
    {
        Contact = await _contactFactory.Fetch(id);
    }

    public async Task Save()
    {
        if (Contact is null || IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            await Contact.WaitForTasks();
            if (!Contact.IsSavable)
            {
                return;
            }

            Contact = (IContact)await Contact.Save();
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

`Pages/ContactEdit.razor`:
```razor
@page "/contacts/{Id:int}"
@using System.ComponentModel
@using ZCrm.Client.ViewModels
@using ZCrm.Domain.Contacts
@implements IDisposable
@inject ContactEditViewModel Vm

@if (!_loaded)
{
    <MudProgressCircular Indeterminate="true" />
}
else if (Vm.Contact is null)
{
    <MudAlert Severity="Severity.Warning">Contact not found.</MudAlert>
}
else
{
    @* @key: Save returns a new instance; rebuild the fields over it. *@
    <div @key="Vm.Contact">
        <NeatooValidationSummary Entity="@Vm.Contact" ShowHeader="false" Dense="true" Class="mb-4" />

        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.Name)]" Variant="Variant.Outlined" Class="mb-3" />
        <MudNeatooCheckBox T="bool" EntityProperty="@Vm.Contact[nameof(IContact.IsKeyAccount)]" Label="Key account" Class="mb-3" />
        <MudNeatooCheckBox T="bool" EntityProperty="@Vm.Contact[nameof(IContact.IsDormant)]" Label="Dormant" Class="mb-3" />

        <MudButton Variant="Variant.Filled" Color="Color.Primary" Disabled="@(!Vm.CanSave)" OnClick="SaveAsync">
            Save
        </MudButton>
    </div>
}

@code {
    [Parameter] public int Id { get; set; }

    private bool _loaded;
    private IContact? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        await Vm.Load(Id);
        _loaded = true;
        Resubscribe();
    }

    // The Save button reads the contact's IsSavable, so the page re-renders on its PropertyChanged.
    private void Resubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnContactChanged;
        }

        _subscribed = Vm.Contact;

        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged += OnContactChanged;
        }
    }

    private async Task SaveAsync()
    {
        await Vm.Save();
        Resubscribe();
        StateHasChanged();
    }

    private void OnContactChanged(object? sender, PropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnContactChanged;
        }
    }
}
```

## Decisions for you

1. **Key accounts will show up in the report every night.** I read your spec as "the job relies on the rule," so key accounts with no recent visit are refused and reported. That keeps the rule in one place, and the report doubles as a list of neglected key accounts. If you'd rather they not appear, add `.Where(c => !c.IsKeyAccount)` to the candidate query. The rule would then exist in two places. Which do you want?
2. **Existing bad data can stop a run.** If a stored contact already breaks a different rule (say, a blank `Name` from before `[Required]` existed), the job's `IsSavable` check still passes. That's because setting `IsDormant` only re-runs the rules that watch `IsDormant` and `IsKeyAccount`. The safety check in `Update` then throws, and that night's run stops at that contact. Contacts saved before it stay saved. Either clean the data up front, or tell me if you want the job handled differently.
3. **Contacts with no visits at all count as inactive,** including one created yesterday. If that's wrong, you'll need a `CreatedOn` column to exclude new contacts.
4. **If you run more than one server instance, each one runs the job.** That's harmless because the job can safely run twice, but it is wasted work. A missed night (server down at 02:00) is caught up the next night.

## Assumptions and unverified details

- **Assumed names:** `ZCrmDbContext`, an EF `Contact` with `int Id`, `Name`, `IsKeyAccount`, `IsDormant`, a `Visit` with `ContactId` and a UTC `VisitedOn`, and SQL Server.
- **Not confirmed from the references:**
  - the Neatoo `using` namespaces;
  - `AddNeatooServices` accepting two assemblies (the RemoteFactory docs say to pass every `[Factory]` assembly to the one call);
  - whether `MudNeatooCheckBox` shows its own message by default. If it doesn't, the validation summary still does.