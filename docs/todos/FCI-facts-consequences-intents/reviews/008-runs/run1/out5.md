I haven't compiled any of this. I wrote it from the Neatoo and RemoteFactory reference patterns, so check the namespaces (`Neatoo`, `Neatoo.Rules`, `Neatoo.RemoteFactory`) against your package versions.

**The design in short:** "A key account cannot be dormant" is one validation rule on `Contact`. The browser, the server and the job all run that same rule. The job does no checking of its own: it sets `IsDormant`, and the contact's `IsSavable` decides whether it is saved or reported.

## 1. Persistence (assumed, shown so the names resolve)

Lives in `ZCrm.Persistence`, a server-only project. The domain project references it with `PrivateAssets="all"`, and EF Core the same way, so they don't ship to the browser.

```csharp
using Microsoft.EntityFrameworkCore;

namespace ZCrm.Persistence;

public sealed class CrmDbContext(DbContextOptions<CrmDbContext> options) : DbContext(options)
{
    public DbSet<ContactRow> Contacts => Set<ContactRow>();
    public DbSet<VisitRow> Visits => Set<VisitRow>();
}

public sealed class ContactRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsKeyAccount { get; set; }
    public bool IsDormant { get; set; }
}

public sealed class VisitRow
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public DateTime VisitedOn { get; set; }
}
```

## 2. Domain changes: `ZCrm.Domain` (loaded in both browser and server)

**The rule.** It goes in the domain because the same compiled class must run in the browser (screen edits), in the save check on the server and in the job. It runs when either property changes, and it attaches its message to `IsDormant`. So if a person ticks "Key account" on a dormant contact, the error appears under the Dormant field, and it clears when either box is unticked.

```csharp
using Neatoo.Rules;

namespace ZCrm.Domain.Contacts;

/// <summary>A key account cannot be dormant. The message sits on IsDormant.</summary>
internal sealed class KeyAccountCannotBeDormantRule : RuleBase<Contact>
{
    public KeyAccountCannotBeDormantRule() : base(t => t.IsDormant, t => t.IsKeyAccount) { }

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

**The entity.** The only real change is the `AddRule` line in the constructor. The rest is shown because there is no existing code. `[Insert]` and `[Update]` re-run the rules and refuse an invalid contact. That protects against a caller that skips `IsSavable`. The job and the screen never reach that check because they check `IsSavable` first.

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using ZCrm.Persistence;

namespace ZCrm.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
public partial interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? Name { get; set; }
    bool IsKeyAccount { get; set; }
    bool IsDormant { get; set; }
}

[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public Contact(IEntityBaseServices<Contact> services) : base(services)
    {
        RuleManager.AddRule(new KeyAccountCannotBeDormantRule());
    }

    public partial Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public partial string? Name { get; set; }

    public partial bool IsKeyAccount { get; set; }

    public partial bool IsDormant { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] CrmDbContext db, CancellationToken cancellationToken)
    {
        var row = await db.Contacts.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
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
    internal async Task Insert([Service] CrmDbContext db)
    {
        // Server gate: re-run every rule and refuse an invalid contact. Throw, never return.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = new ContactRow { Id = Id };
        MapTo(row);
        db.Contacts.Add(row);
        await db.SaveChangesAsync();
    }

    [Remote, Update]
    internal async Task Update([Service] CrmDbContext db)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await db.Contacts.FindAsync(Id)
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

**The job.** It is a domain command, not code in the server project, because database access is only allowed inside a factory operation. Marking it `[Remote]` keeps its body (and the EF code in it) out of the published browser bundle. For each candidate it fetches the contact, sets `IsDormant` through the generated `SetIsDormant`, waits for the rules, and lets `IsSavable` decide. It catches nothing and has no dormancy logic of its own.

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using ZCrm.Persistence;

namespace ZCrm.Domain.Contacts;

/// <summary>
/// Job: marks contacts dormant after 180 days without a visit. Reuses the Contact
/// aggregate and its rules; a contact the rules reject is reported, not saved.
/// </summary>
[Factory]
public static partial class MarkDormantContacts
{
    public static readonly TimeSpan DormantAfter = TimeSpan.FromDays(180);

    [Remote, Execute]
    private static async Task<DormancyReport> _Run(
        [Service] CrmDbContext db,
        [Service] IContactFactory contactFactory,
        [Service] TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var cutoffUtc = timeProvider.GetUtcNow().UtcDateTime - DormantAfter;

        // Candidates: not yet dormant, no visit since the cutoff. Key accounts are
        // deliberately not filtered here - the Contact's rule decides that.
        var candidateIds = await db.Contacts.AsNoTracking()
            .Where(c => !c.IsDormant
                && !db.Visits.Any(v => v.ContactId == c.Id && v.VisitedOn >= cutoffUtc))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var marked = new List<Guid>();
        var rejected = new List<RejectedContact>();

        foreach (var id in candidateIds)
        {
            var contact = await contactFactory.Fetch(id, cancellationToken);
            if (contact is null)
            {
                continue; // deleted since the query
            }

            await contact.SetIsDormant(true);
            await contact.WaitForTasks();

            if (contact.IsSavable)
            {
                contact = (IContact)await contact.Save();
                marked.Add(contact.Id);
            }
            else
            {
                rejected.Add(new RejectedContact(
                    id,
                    contact.Name,
                    contact.PropertyMessages.Select(m => $"{m.Property.Name}: {m.Message}").ToList()));
            }
        }

        return new DormancyReport(cutoffUtc, marked, rejected);
    }
}

/// <summary>What the dormancy job did.</summary>
public sealed record DormancyReport(
    DateTime CutoffUtc,
    IReadOnlyList<Guid> MarkedDormant,
    IReadOnlyList<RejectedContact> Rejected);

/// <summary>A contact the rules refused to mark dormant, with the messages a screen would show.</summary>
public sealed record RejectedContact(Guid ContactId, string? Name, IReadOnlyList<string> Messages);
```

## 3. How it is started: `ZCrm.Server`

**The scheduler** is a `BackgroundService` in the server project, because only the server runs nightly work. It owns only the timing, the DI scope and the logging. It calls the generated `MarkDormantContacts.Run` delegate, which runs locally on the server. It catches failures so one bad night doesn't stop the host.

```csharp
using ZCrm.Domain.Contacts;

namespace ZCrm.Server.Jobs;

/// <summary>Runs MarkDormantContacts every night at 02:00 server local time and logs the report.</summary>
internal sealed class DormancyJobHostedService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<DormancyJobHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan RunAtLocalTime = new(2, 0, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(UntilNextRun(), timeProvider, stoppingToken);
            await RunOnce(stoppingToken);
        }
    }

    private TimeSpan UntilNextRun()
    {
        var now = timeProvider.GetLocalNow();
        var next = new DateTimeOffset(now.Date + RunAtLocalTime, now.Offset);
        if (next <= now)
        {
            next = next.AddDays(1);
        }

        return next - now;
    }

    private async Task RunOnce(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var run = scope.ServiceProvider.GetRequiredService<MarkDormantContacts.Run>();
            var report = await run(stoppingToken);

            logger.LogInformation(
                "Dormancy job: {Marked} contacts marked dormant, {Rejected} rejected (no visit since {CutoffUtc:u}).",
                report.MarkedDormant.Count, report.Rejected.Count, report.CutoffUtc);

            foreach (var r in report.Rejected)
            {
                logger.LogWarning(
                    "Contact {ContactId} ({Name}) not marked dormant: {Messages}",
                    r.ContactId, r.Name, string.Join("; ", r.Messages));
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutting down
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dormancy job failed; it runs again tomorrow.");
        }
    }
}
```

**Server `Program.cs`.** Use `AddNeatooServices(NeatooFactory.Server, …)` and don't add `AddNeatooAspNetCore` on top of it. Registering both breaks every remote save (RemoteFactory#117).

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using ZCrm.Domain.Contacts;
using ZCrm.Persistence;
using ZCrm.Server.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);

builder.Services.AddDbContext<CrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Crm")));
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddHostedService<DormancyJobHostedService>();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseNeatoo();
app.MapFallbackToFile("index.html");

await app.RunAsync();
```

**The browser client needs no changes.** The rule is created in `Contact`'s constructor, so it already runs in the browser. Any edit form that binds `IsDormant` with MudNeatoo shows the message under that field, and a Save button bound to `IsSavable` stays disabled. No Razor is needed for this feature.

## Decisions to confirm

1. **Key accounts aren't filtered out of the job's query.** The rule decides, so a key account with no visit in 180 days shows up in the "rejected" log every night. That may be useful ("nobody has visited this key account"). If you'd rather not see it, add `&& !c.IsKeyAccount` to the query, but then the rule exists in two places.
2. **Contacts with no visits at all get marked dormant,** including one created yesterday. If the "180 days" should count from when the contact was created, the contact needs a `CreatedOn` column, and the query needs a matching condition.
3. **The job's endpoint has no authorization.** Because it's `[Remote]`, a browser could call `MarkDormantContacts.Run`. Add authorization before shipping. I haven't checked how `[AspAuthorize]` behaves when the caller is a background service with no HTTP request.
4. **Running more than one server instance** means each one runs the job. You'd then need a distributed lock or a single instance that runs jobs.
5. **Index** `Visits(ContactId, VisitedOn)` to keep the candidate query fast.