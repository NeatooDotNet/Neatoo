I've written the whole slice below. I couldn't compile it because I wasn't allowed to create files, so treat it as unbuilt; the most likely fixes are `using` namespaces.

The design choices that shape everything else:
- **"Done" is a value the checkbox writes.** Ticking goes through a `Complete(user)` method, because the domain can't know who the user is. That method records who and when, then sets Done. Unticking is a plain write of `IsDone = false`, and a rule clears who and when.
- **"Close" is enabled by a value the domain calculates.** `CanClose` is recalculated by a rule whenever any task's Done changes, a task is added or removed, the sprint's validity changes, or the sprint is closed. The button only reads it.
- **Percent done and "all tasks done" are calculated on the sprint**, not in the page.

## Project layout

| Project | References | Why |
|---|---|---|
| `ZTrack.Dal` | nothing | Row types and the store interface. No EF Core, so the domain can be loaded in the browser. |
| `ZTrack.Domain` | Dal, Neatoo | The sprint and task classes. The same compiled code runs on both client and server. |
| `ZTrack.Ef` | Dal, EF Core | The DbContext and the store. Server only. |
| `ZTrack.Server` | Domain, Ef, Client | Hosts the client and the one Neatoo endpoint. |
| `ZTrack.Client` | Domain, MudNeatoo | The page and its ViewModel. |

---

## 1. Data access (`ZTrack.Dal`)

The domain's save and load code takes `ISprintStore` as a server-side `[Service]` parameter. Because it's only an interface, the domain never references EF Core.

```csharp
namespace ZTrack.Dal;

public class SprintRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public ICollection<TaskRow> Tasks { get; set; } = new List<TaskRow>();
}

public class TaskRow
{
    public Guid Id { get; set; }
    public Guid SprintId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public string? CompletedBy { get; set; }
    public DateTimeOffset? CompletedOn { get; set; }
}

/// <summary>Store for Sprint aggregates. Resolved only on the server.</summary>
public interface ISprintStore
{
    /// <summary>The sprint row with its task rows, tracked; null when absent.</summary>
    Task<SprintRow?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(SprintRow row);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

## 2. Domain (`ZTrack.Domain`)

### Interfaces

Each class has a public interface, and the classes themselves are `internal`. The task interface extends `IEntityBase`, so a task has no `Save()` and can only be saved through its sprint. The interfaces are `partial` so the source generator can add `Task SetIsDone(bool)` to `ISprintTask`.

```csharp
using Neatoo;

namespace ZTrack.Domain.Sprints;

/// <summary>Sprint aggregate root.</summary>
public partial interface ISprint : IEntityRoot
{
    Guid Id { get; }
    string? Name { get; set; }
    ISprintTaskList? Tasks { get; }

    /// <summary>Calculated: share of tasks done, 0-100, floored so 100 means all done.</summary>
    int PercentDone { get; }

    /// <summary>Calculated: every task is done.</summary>
    bool AllTasksDone { get; }

    bool IsClosed { get; }

    /// <summary>Calculated: enables the Close button. Not closed, all tasks done, sprint valid.</summary>
    bool CanClose { get; }

    /// <summary>Closes the sprint. The button that calls this is enabled by CanClose.</summary>
    Task Close();
}

/// <summary>A task in a sprint.</summary>
public partial interface ISprintTask : IEntityBase
{
    Guid Id { get; }
    string? Title { get; set; }

    /// <summary>The checkbox. Unticking writes it directly; a rule clears the completion details.</summary>
    bool IsDone { get; set; }

    string? CompletedBy { get; }
    DateTimeOffset? CompletedOn { get; }

    /// <summary>Records who and when, then marks the task done.</summary>
    Task Complete(string completedBy);
}

public interface ISprintTaskList : IEntityListBase<ISprintTask> { }
```

### Sprint

All the sprint's rules live here, so the page and ViewModel contain no logic. The `[Remote]` methods are the only ones the client calls across the network. Insert and Update re-run every rule on the server and refuse an invalid sprint. That protects against any caller that skipped `IsSavable`.

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Neatoo;
using Neatoo.RemoteFactory;
using ZTrack.Dal;

namespace ZTrack.Domain.Sprints;

/// <summary>Sprint aggregate root.</summary>
[Factory]
internal partial class Sprint : EntityBase<Sprint>, ISprint
{
    public partial Guid Id { get; private set; }

    [DisplayName("Sprint name")]
    [Required(ErrorMessage = "Sprint name is required")]
    public partial string? Name { get; set; }

    public partial ISprintTaskList? Tasks { get; private set; }
    public partial int PercentDone { get; private set; }
    public partial bool AllTasksDone { get; private set; }
    public partial bool IsClosed { get; private set; }
    public partial bool CanClose { get; private set; }

    public Sprint(IEntityBaseServices<Sprint> services) : base(services)
    {
        // Progress. Triggered by any task's IsDone and by adding or removing a task (Count).
        // The Tasks trigger fires only when the list is assigned. It is here so Fetch can
        // run this rule by name.
        RuleManager.AddActionAsync(
            async t =>
            {
                var total = t.Tasks?.Count ?? 0;
                var done = t.Tasks?.Count(task => task.IsDone) ?? 0;
                await t.SetPercentDone(total == 0 ? 0 : done * 100 / total);
                await t.SetAllTasksDone(done == total);
            },
            new Expression<Func<Sprint, object?>>[]
            {
                t => t.Tasks,
                t => t.Tasks!.Count,
                t => t.Tasks![0].IsDone,
            });

        // Enables Close. It reads IsValid, so a change in validity re-runs it.
        RuleManager.AddActionAsync(
            async t => await t.SetCanClose(!t.IsClosed && t.AllTasksDone && t.IsValid),
            t => t.AllTasksDone,
            t => t.IsClosed,
            t => t.IsValid);

        // A closed sprint stays all-done. Unticking a task afterwards is reported, and the
        // server's validity check refuses a close that skipped CanClose.
        RuleManager.AddValidation(
            t => t.IsClosed && !t.AllTasksDone ? "A closed sprint cannot have open tasks" : string.Empty,
            t => t.AllTasksDone);
    }

    public async Task Close()
    {
        await SetIsClosed(true);
    }

    [Create]
    public async Task Create([Service] ISprintTaskListFactory tasksFactory)
    {
        Tasks = tasksFactory.Create();

        // Rules don't run during Create. This sprint goes to a form, so run them now:
        // the empty name shows its message and CanClose starts false.
        // RunRulesFlag.All is safe because there are no tasks yet.
        await RunRules(RunRulesFlag.All);
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id,
        [Service] ISprintStore store,
        [Service] ISprintTaskListFactory tasksFactory)
    {
        var row = await store.FindAsync(id);
        if (row is null)
        {
            return false;
        }

        Id = row.Id;
        Name = row.Name;
        IsClosed = row.IsClosed;
        Tasks = tasksFactory.Fetch(row.Tasks);

        // Calculated values aren't stored, so run the rules that set them, by trigger name.
        await RunRules(nameof(Tasks));
        await RunRules(nameof(IsClosed));
        return true;
    }

    [Remote, Insert]
    internal async Task Insert(
        [Service] ISprintStore store,
        [Service] ISprintTaskListFactory tasksFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.NewGuid();
        var row = new SprintRow();
        MapTo(row);
        store.Add(row);

        tasksFactory.Save(Tasks!, row.Tasks);

        await store.SaveChangesAsync();
    }

    [Remote, Update]
    internal async Task Update(
        [Service] ISprintStore store,
        [Service] ISprintTaskListFactory tasksFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await store.FindAsync(Id)
            ?? throw new KeyNotFoundException($"Sprint {Id} not found");

        if (IsSelfModified)
        {
            MapTo(row);
        }

        tasksFactory.Save(Tasks!, row.Tasks);

        await store.SaveChangesAsync();
    }

    private void MapTo(SprintRow row)
    {
        row.Id = Id;
        row.Name = Name!;
        row.IsClosed = IsClosed;
    }
}
```

### SprintTask

The task owns its own completion details. `TimeProvider` comes in through the constructor because the task is built through dependency injection on both client and server, and it keeps the clock testable.

Why the timestamp isn't set by a rule: the server re-runs every rule when saving. A rule that set who-and-when would overwrite them with the server's clock and identity.

```csharp
using System.ComponentModel;
using Neatoo;
using Neatoo.RemoteFactory;
using ZTrack.Dal;

namespace ZTrack.Domain.Sprints;

/// <summary>A task in a sprint.</summary>
[Factory]
internal partial class SprintTask : EntityBase<SprintTask>, ISprintTask
{
    private readonly TimeProvider _time;

    public partial Guid Id { get; private set; }

    [DisplayName("Title")]
    public partial string? Title { get; set; }

    [DisplayName("Done")]
    public partial bool IsDone { get; set; }

    public partial string? CompletedBy { get; private set; }
    public partial DateTimeOffset? CompletedOn { get; private set; }

    public SprintTask(IEntityBaseServices<SprintTask> services, TimeProvider time) : base(services)
    {
        _time = time;

        // Unticking clears the completion details, whoever wrote IsDone.
        // Running it again changes nothing, so the server's re-run of the rules is harmless.
        RuleManager.AddActionAsync(
            async t =>
            {
                if (!t.IsDone)
                {
                    await t.SetCompletedBy(null);
                    await t.SetCompletedOn(null);
                }
            },
            t => t.IsDone);

        // A tick must record who did it. Writing IsDone = true without Complete()
        // shows an error instead of being silently accepted.
        RuleManager.AddValidation(
            t => t.IsDone && t.CompletedBy is null
                ? "A completed task must record who completed it"
                : string.Empty,
            t => t.IsDone);
    }

    /// <summary>
    /// Records who and when, then sets IsDone, in that order, so the IsDone rules see the
    /// completion details. The Done checkbox is enabled by the domain; this method doesn't check.
    /// </summary>
    public async Task Complete(string completedBy)
    {
        await SetCompletedBy(completedBy);
        await SetCompletedOn(_time.GetUtcNow());
        await SetIsDone(true);
    }

    [Create]
    public void Create(string title)
    {
        Title = title;
    }

    [Fetch]
    internal void Fetch(TaskRow row)
    {
        Id = row.Id;
        Title = row.Title;
        IsDone = row.IsDone;
        CompletedBy = row.CompletedBy;
        CompletedOn = row.CompletedOn;
    }

    [Insert]
    internal void Insert(TaskRow row)
    {
        Id = Guid.NewGuid();
        MapTo(row);
    }

    [Update]
    internal void Update(TaskRow row)
    {
        MapTo(row);
    }

    private void MapTo(TaskRow row)
    {
        row.Id = Id;
        row.Title = Title ?? string.Empty;
        row.IsDone = IsDone;
        row.CompletedBy = CompletedBy;
        row.CompletedOn = CompletedOn;
    }
}
```

### SprintTaskList

The list coordinates saving: it hands each task its own row, and each task writes itself. Its methods are `internal` and not `[Remote]` because they only ever run on the server, called from the sprint's Insert or Update.

```csharp
using Neatoo;
using Neatoo.RemoteFactory;
using ZTrack.Dal;

namespace ZTrack.Domain.Sprints;

[Factory]
internal partial class SprintTaskList : EntityListBase<ISprintTask>, ISprintTaskList
{
    [Create]
    public void Create() { }

    [Fetch]
    internal void Fetch(IEnumerable<TaskRow> rows, [Service] ISprintTaskFactory taskFactory)
    {
        foreach (var row in rows)
        {
            Add(taskFactory.Fetch(row));
        }
    }

    [Update]
    internal void Update(ICollection<TaskRow> rows, [Service] ISprintTaskFactory taskFactory)
    {
        foreach (var task in this.Union(DeletedList))
        {
            if (task.IsDeleted)
            {
                if (!task.IsNew)
                {
                    rows.Remove(rows.Single(r => r.Id == task.Id));
                }
            }
            else if (task.IsNew)
            {
                var row = new TaskRow();
                rows.Add(row);
                taskFactory.Save(task, row);
            }
            else if (task.IsModified)
            {
                taskFactory.Save(task, rows.Single(r => r.Id == task.Id));
            }
        }
    }
}
```

## 3. Server persistence (`ZTrack.Ef`)

`ValueGeneratedNever()` matters. The domain creates its own Guid keys. By default EF treats a new row that already has a Guid key and was reached through `row.Tasks` as an existing row. It would then UPDATE instead of INSERT, and fail with a concurrency exception.

```csharp
using Microsoft.EntityFrameworkCore;
using ZTrack.Dal;

namespace ZTrack.Ef;

public class ZTrackDbContext(DbContextOptions<ZTrackDbContext> options) : DbContext(options)
{
    public DbSet<SprintRow> Sprints => Set<SprintRow>();
    public DbSet<TaskRow> Tasks => Set<TaskRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SprintRow>(e =>
        {
            e.ToTable("SprintRow");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).ValueGeneratedNever();   // the domain assigns keys
            e.Property(s => s.Name).IsRequired().HasMaxLength(200);
            e.HasMany(s => s.Tasks)
             .WithOne()
             .HasForeignKey(t => t.SprintId)
             .OnDelete(DeleteBehavior.Cascade);             // removed task rows are deleted as orphans
        });

        modelBuilder.Entity<TaskRow>(e =>
        {
            e.ToTable("TaskRow");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).ValueGeneratedNever();
            e.Property(t => t.Title).IsRequired().HasMaxLength(200);
            e.Property(t => t.CompletedBy).HasMaxLength(256);
        });
    }
}

/// <summary>ISprintStore over EF Core. Registered on the server only.</summary>
internal sealed class EfSprintStore(ZTrackDbContext db) : ISprintStore
{
    public Task<SprintRow?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Sprints.Include(s => s.Tasks).SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Add(SprintRow row) => db.Sprints.Add(row);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);
}

public static class EfRegistration
{
    public static IServiceCollection AddZTrackEf(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ZTrackDbContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped<ISprintStore, EfSprintStore>();
        return services;
    }
}
```

## 4. Wiring

Server `Program.cs`. Don't also call `AddNeatooAspNetCore`: registering it after `AddNeatooServices(Server)` breaks every remote Save (RemoteFactory#117).

```csharp
using Neatoo;
using Neatoo.RemoteFactory;
using ZTrack.Domain.Sprints;
using ZTrack.Ef;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(ISprint).Assembly);
builder.Services.AddSingleton(TimeProvider.System);     // SprintTask's constructor; both tiers
builder.Services.AddZTrackEf(builder.Configuration.GetConnectionString("ZTrack")!);
builder.Services.AddAuthentication();                    // your scheme
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseNeatoo();
app.MapFallbackToFile("index.html");

await app.RunAsync();
```

Client `Program.cs`:

```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using ZTrack.Client;
using ZTrack.Client.Sprints;
using ZTrack.Domain.Sprints;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(ISprint).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore();                 // plus your AuthenticationStateProvider
builder.Services.AddTransient<SprintViewModel>();

await builder.Build().RunAsync();
```

## 5. ViewModel (`ZTrack.Client`)

The ViewModel only turns user actions into domain calls. It adds two things:
- **The user's name**, which only the client knows.
- **`IsSaving`**: `Save()` doesn't mark the sprint busy while the call is in flight, so without this a double-click sends two inserts (Neatoo#103).

Choosing `Complete` or `IsDone = false` based on the checkbox value is part of translating the click, not a business rule. The Close button calls the domain's `Close()` and then saves.

```csharp
using Microsoft.AspNetCore.Components.Authorization;
using ZTrack.Domain.Sprints;

namespace ZTrack.Client.Sprints;

/// <summary>ViewModel for the sprint screen: holds the sprint, turns clicks into domain calls, saves.</summary>
internal sealed class SprintViewModel
{
    private readonly ISprintFactory _sprintFactory;
    private readonly ISprintTaskFactory _taskFactory;
    private readonly AuthenticationStateProvider _authentication;

    public SprintViewModel(
        ISprintFactory sprintFactory,
        ISprintTaskFactory taskFactory,
        AuthenticationStateProvider authentication)
    {
        _sprintFactory = sprintFactory;
        _taskFactory = taskFactory;
        _authentication = authentication;
    }

    /// <summary>The sprint the page binds to. Null until loaded, or when not found.</summary>
    public ISprint? Sprint { get; private set; }

    /// <summary>A save is in flight (Save() does not mark the entity busy, Neatoo#103).</summary>
    public bool IsSaving { get; private set; }

    public async Task New()
    {
        Sprint = await _sprintFactory.Create();
    }

    public async Task Load(Guid id)
    {
        Sprint = await _sprintFactory.Fetch(id);
    }

    public void AddTask()
    {
        Sprint?.Tasks?.Add(_taskFactory.Create("New task"));
    }

    /// <summary>The Done checkbox: a tick records the current user; an untick is a plain write.</summary>
    public async Task SetDone(ISprintTask task, bool done)
    {
        if (done)
        {
            await task.Complete(await CurrentUserName());
        }
        else
        {
            await task.SetIsDone(false);
        }
    }

    public bool CanSave => !IsSaving && (Sprint?.IsSavable ?? false);

    /// <summary>The Close button reads the sprint's own CanClose.</summary>
    public bool CanClose => !IsSaving && (Sprint?.CanClose ?? false);

    public async Task Save()
    {
        if (Sprint is null || IsSaving)
        {
            return;
        }

        await Sprint.WaitForTasks();

        if (!Sprint.IsSavable)
        {
            return;   // invalid or unchanged; the validation summary shows why
        }

        IsSaving = true;
        try
        {
            Sprint = (ISprint)await Sprint.Save();   // Save returns a new instance
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>The Close sprint button: close the sprint, then save it.</summary>
    public async Task Close()
    {
        if (Sprint is null)
        {
            return;
        }

        await Sprint.Close();
        await Save();
    }

    private async Task<string> CurrentUserName()
    {
        var state = await _authentication.GetAuthenticationStateAsync();
        return state.User.Identity?.Name
            ?? throw new InvalidOperationException("No signed-in user to record as the completer.");
    }
}
```

## 6. Page (`ZTrack.Client`)

The page only binds. Each input subscribes to its own property. The page displays the percentage, the completion details and the buttons, so it subscribes to the sprint's change event (fine for a screen this small).

- `@key` on the wrapper rebuilds the inputs against the new instance that Save returns.
- `@key` on each row keeps a row tied to its task.
- The checkbox reads `IsDone` and writes through `Setter`, so ticking goes through `Complete`.

```razor
@page "/sprints/new"
@page "/sprints/{Id:guid}"
@attribute [Authorize]
@implements IDisposable
@using Microsoft.AspNetCore.Authorization
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using ZTrack.Domain.Sprints
@inject SprintViewModel Vm

@if (!_loaded)
{
    <MudProgressCircular Indeterminate="true" />
}
else if (Vm.Sprint is null)
{
    <MudAlert Severity="Severity.Warning">Sprint not found.</MudAlert>
}
else
{
<div @key="Vm.Sprint">
    <NeatooValidationSummary Entity="@Vm.Sprint" ShowHeader="false" Dense="true" Class="mb-4" />

    <MudNeatooTextField T="string" EntityProperty="@Vm.Sprint[nameof(ISprint.Name)]" Variant="Variant.Outlined" Class="mb-3" />

    @if (Vm.Sprint.IsClosed)
    {
        <MudChip T="string" Color="Color.Success" Class="mb-3">Closed</MudChip>
    }

    <MudText Typo="Typo.subtitle1">@Vm.Sprint.PercentDone% done</MudText>
    <MudProgressLinear Value="@Vm.Sprint.PercentDone" Color="Color.Success" Class="mb-4" />

    @foreach (var task in Vm.Sprint.Tasks!)
    {
        <MudGrid @key="task" Spacing="2" Class="mb-2">
            <MudItem xs="2" sm="1" Class="d-flex align-center">
                <MudNeatooCheckBox T="bool"
                                   EntityProperty="@task[nameof(ISprintTask.IsDone)]"
                                   Setter="@(done => Vm.SetDone(task, done))" />
            </MudItem>
            <MudItem xs="10" sm="7">
                <MudNeatooTextField T="string" EntityProperty="@task[nameof(ISprintTask.Title)]" />
            </MudItem>
            <MudItem xs="12" sm="4" Class="d-flex align-center">
                @if (task.IsDone)
                {
                    <MudText Typo="Typo.caption">
                        Done by @task.CompletedBy, @task.CompletedOn?.ToLocalTime().ToString("g")
                    </MudText>
                }
            </MudItem>
        </MudGrid>
    }

    <MudButton Variant="Variant.Outlined" Disabled="@Vm.IsSaving" OnClick="Vm.AddTask" Class="my-3">Add task</MudButton>

    <div class="d-flex gap-2">
        <MudButton Variant="Variant.Filled" Color="Color.Primary" Disabled="@(!Vm.CanSave)" OnClick="SaveAsync">
            Save
        </MudButton>
        <MudButton Variant="Variant.Filled" Color="Color.Success" Disabled="@(!Vm.CanClose)" OnClick="CloseAsync">
            Close sprint
        </MudButton>
    </div>
</div>
}

@code {
    [Parameter] public Guid? Id { get; set; }

    private bool _loaded;
    private ISprint? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        _loaded = false;

        if (Id is Guid id)
        {
            await Vm.Load(id);
        }
        else
        {
            await Vm.New();
        }

        _loaded = true;
        Resubscribe();
    }

    // The page shows values outside the inputs (percent, completion details, buttons),
    // so it re-renders on any change in the sprint.
    private void Resubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnSprintChanged;
        }

        _subscribed = Vm.Sprint;

        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged += OnSprintChanged;
        }
    }

    // Save and Close each leave the ViewModel holding a new instance.
    private async Task SaveAsync()
    {
        await Vm.Save();
        Resubscribe();
    }

    private async Task CloseAsync()
    {
        await Vm.Close();
        Resubscribe();
    }

    private Task OnSprintChanged(NeatooPropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnSprintChanged;
        }
    }
}
```

---

## Decisions for you

The spec didn't cover these, so I picked the literal or simplest reading:

1. **A sprint with no tasks can be closed.** "Every task is done" is true when there are none, so `AllTasksDone` is true and Close is enabled, while the page shows 0% done. If an empty sprint shouldn't close, change the progress rule to `SetAllTasksDone(total > 0 && done == total)`.
2. **The client supplies "who completed it", and the server trusts it.** If that matters, have the task's `[Update]` take the server's current user as a `[Service]` and compare or overwrite.
3. **Task order isn't stable.** `TaskRow` has no position column, so tasks come back in whatever order the database returns. Adding a `Position` column fixes it.
4. **Task titles aren't required, and a closed sprint stays editable** apart from the rule that its tasks must all stay done. Neither was in the spec.
5. **No delete and no task removal.** Neither was asked for. The list's `[Update]` already handles removed tasks, so adding a remove button later is only a ViewModel change.