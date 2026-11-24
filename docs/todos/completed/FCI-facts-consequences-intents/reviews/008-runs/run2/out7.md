I've read the Neatoo, RemoteFactory and MudNeatoo guidance. The design and all the code are below. Nothing has been compiled: per your instructions I created no files. A few `using` namespaces are from memory and are the most likely thing to need fixing.

## Projects

| Project | References | Why |
|---|---|---|
| `ZTrack.Dal` | — | Row classes and the store interface. No EF Core, so it is safe for the browser to load. |
| `ZTrack.Domain` | Dal, Neatoo | The aggregate. The browser and the server run the same compiled classes. |
| `ZTrack.Ef` | Dal, EF Core | Your DbContext, which implements the Dal interface. Only the server can see it. |
| `ZTrack.Server` | Domain, Ef, Client | Hosts the one RemoteFactory endpoint and the WASM client. |
| `ZTrack.Client` | Domain, MudNeatoo | The page, its ViewModel and the task row component. |

## How the behaviour is split

- **Facts** are plain properties the user writes: `Name`, `Title`, `IsDone`.
- **Consequences** are rules on the root: `PercentDone`, `AllTasksDone` and `CanClose`. The page only displays them and never calculates them.
- **Verbs** are used where a plain property write isn't enough:
  - `MarkDone` records who completed the task and when. A property can't know who ticked it, so the checkbox writes through this method.
  - `Close` is the only way to close a sprint.
- The Close button is enabled by `CanClose`, which is `AllTasksDone && IsValid && !IsClosed`. A rule triggered by `IsValid` keeps it current.

---

## ZTrack.Dal

These are the shapes of your `SprintRow` and `TaskRow` tables. The domain only talks to `IZTrackDb`, so EF Core never ends up in the browser.

```csharp
// ZTrack.Dal/SprintRow.cs
namespace ZTrack.Dal;

public class SprintRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsClosed { get; set; }
    public List<TaskRow> Tasks { get; set; } = [];
}

public class TaskRow
{
    public Guid Id { get; set; }
    public Guid SprintId { get; set; }
    public string Title { get; set; } = "";
    public bool IsDone { get; set; }
    public string? CompletedBy { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
```

```csharp
// ZTrack.Dal/IZTrackDb.cs
namespace ZTrack.Dal;

/// <summary>The store the domain's server-side operations take as a [Service].</summary>
public interface IZTrackDb
{
    /// <summary>The sprint row with its task rows, tracked; null when there is none.</summary>
    Task<SprintRow?> FindSprintAsync(Guid id, CancellationToken cancellationToken = default);

    void AddSprint(SprintRow row);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

## ZTrack.Domain

```csharp
// ZTrack.Domain/GlobalUsings.cs
global using Neatoo;
global using Neatoo.RemoteFactory;
global using Neatoo.Rules;
```

**Interfaces.** The classes are `internal` and everything outside refers to these interfaces. `ISprintTask` extends `IEntityBase`, which has no `Save()`, so the compiler stops anyone from saving a task on its own.

```csharp
// ZTrack.Domain/Sprints/ISprintInterfaces.cs
namespace ZTrack.Domain.Sprints;

/// <summary>Sprint aggregate root.</summary>
public interface ISprint : IEntityRoot
{
    Guid Id { get; }

    /// <summary>Fact. Required.</summary>
    string? Name { get; set; }

    ISprintTaskList? Tasks { get; }

    /// <summary>Consequence: share of tasks done, 0-100, rounded down.</summary>
    int PercentDone { get; }

    /// <summary>Consequence: the sprint has tasks and every one is done.</summary>
    bool AllTasksDone { get; }

    bool IsClosed { get; }

    /// <summary>Consequence: admission for Close. Every task done, sprint valid, not yet closed.</summary>
    bool CanClose { get; }

    /// <summary>Intent: close the sprint. Never persists; the caller saves.</summary>
    Task Close();
}

/// <summary>One task of a sprint.</summary>
public interface ISprintTask : IEntityBase
{
    Guid Id { get; }
    string? Title { get; set; }

    /// <summary>Fact, written only through MarkDone so the who-stamp lands with it.</summary>
    bool IsDone { get; }

    string? CompletedBy { get; }
    DateTimeOffset? CompletedAt { get; }

    /// <summary>Intent: tick records who and when; untick clears both.</summary>
    Task MarkDone(bool done, string user);
}

public interface ISprintTaskList : IEntityListBase<ISprintTask> { }
```

**The progress calculation.** It lives in one place and is used twice:
- the rule writes it through the awaited setters;
- `Fetch` assigns it directly, because nothing is tracked or run during a load.

It rounds down, so the sprint shows 100% only when every task is done.

```csharp
// ZTrack.Domain/Sprints/SprintProgress.cs
namespace ZTrack.Domain.Sprints;

internal readonly record struct SprintProgress(int PercentDone, bool AllDone)
{
    public static SprintProgress Of(IEnumerable<ISprintTask>? tasks)
    {
        var all = tasks?.ToList() ?? [];
        if (all.Count == 0)
        {
            return new SprintProgress(0, false);
        }

        var done = all.Count(t => t.IsDone);
        return new SprintProgress(done * 100 / all.Count, done == all.Count);
    }
}
```

**A closed sprint can't have open tasks.** This is a validation rule rather than just a disabled button. The server re-runs every rule before it writes anything, so a client that skips the button still can't save a closed sprint with open tasks.

```csharp
// ZTrack.Domain/Sprints/ClosedSprintHasNoOpenTasksRule.cs
namespace ZTrack.Domain.Sprints;

internal sealed class ClosedSprintHasNoOpenTasksRule : RuleBase<Sprint>
{
    public ClosedSprintHasNoOpenTasksRule() : base(t => t.IsClosed, t => t.AllTasksDone) { }

    protected override IRuleMessages Execute(Sprint target)
    {
        if (target.IsClosed && !target.AllTasksDone)
        {
            return (nameof(Sprint.IsClosed), "A closed sprint cannot have open tasks").AsRuleMessages();
        }

        return None;
    }
}
```

**The aggregate root.**
- `Create` runs the rules so an empty name shows its error straight away and Save starts disabled. Running all rules is safe here because there are no tasks yet.
- `Fetch` runs only the rules triggered by `AllTasksDone`, which recalculate `CanClose`. Stored data is assumed valid, so it isn't re-validated.
- `Insert` and `Update` re-run every rule and refuse an invalid sprint before writing anything.

```csharp
// ZTrack.Domain/Sprints/Sprint.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ZTrack.Dal;

namespace ZTrack.Domain.Sprints;

/// <summary>Sprint aggregate root.</summary>
[Factory]
internal partial class Sprint : EntityBase<Sprint>, ISprint
{
    public Sprint(IEntityBaseServices<Sprint> services) : base(services)
    {
        // Consequence: progress. Count covers add and remove; the child-property
        // path covers any task's tick or untick.
        RuleManager.AddActionAsync(
            async t =>
            {
                var progress = SprintProgress.Of(t.Tasks);
                await t.SetPercentDone(progress.PercentDone);
                await t.SetAllTasksDone(progress.AllDone);
            },
            t => t.Tasks!.Count,
            t => t.Tasks![0].IsDone);

        // Consequence: admission for Close. It reads IsValid, so IsValid is a
        // trigger: a cleared name disables Close as soon as validity flips.
        RuleManager.AddActionAsync(
            async t => await t.SetCanClose(t.AllTasksDone && t.IsValid && !t.IsClosed),
            t => t.AllTasksDone,
            t => t.IsValid,
            t => t.IsClosed);

        RuleManager.AddRule(new ClosedSprintHasNoOpenTasksRule());
    }

    public partial Guid Id { get; private set; }

    [DisplayName("Sprint name")]
    [Required(ErrorMessage = "Sprint name is required")]
    public partial string? Name { get; set; }

    public partial ISprintTaskList? Tasks { get; private set; }

    public partial int PercentDone { get; private set; }

    public partial bool AllTasksDone { get; private set; }

    public partial bool IsClosed { get; private set; }

    public partial bool CanClose { get; private set; }

    /// <summary>
    /// The verb sets state. Admission is the CanClose rule, which the button binds;
    /// the verb does not check and throw.
    /// </summary>
    public async Task Close()
    {
        await SetIsClosed(true);
    }

    [Create]
    public async Task Create([Service] ISprintTaskListFactory tasksFactory)
    {
        Tasks = tasksFactory.Create();

        // A new sprint is an empty form: run the rules so the required name shows
        // its message and IsSavable is false until it is filled. All is safe here:
        // the task list is empty.
        await RunRules(RunRulesFlag.All);
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id,
        [Service] IZTrackDb db,
        [Service] ISprintTaskListFactory tasksFactory)
    {
        var row = await db.FindSprintAsync(id);
        if (row is null)
        {
            return false;
        }

        Id = row.Id;
        Name = row.Name;
        IsClosed = row.IsClosed;
        Tasks = tasksFactory.Fetch(row.Tasks);

        // Rules do not fire during an operation, so this one computes what it needs.
        // Progress is assigned from the loaded tasks (paused: baseline). CanClose is
        // recomputed by running the rules AllTasksDone triggers, which write only
        // this object - never All, which would run every task's rules too.
        (PercentDone, AllTasksDone) = SprintProgress.Of(Tasks);
        await RunRules(nameof(AllTasksDone));
        return true;
    }

    [Remote, Insert]
    internal async Task Insert([Service] IZTrackDb db, [Service] ISprintTaskListFactory tasksFactory)
    {
        // Server gate: refuse an invalid aggregate from a caller that skipped IsSavable.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.NewGuid();

        var row = new SprintRow();
        MapTo(row);
        tasksFactory.Save(Tasks!, row.Tasks);

        // Added after the task rows are in place, so EF tracks the whole graph as Added.
        db.AddSprint(row);
        await db.SaveChangesAsync();
    }

    [Remote, Update]
    internal async Task Update([Service] IZTrackDb db, [Service] ISprintTaskListFactory tasksFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await db.FindSprintAsync(Id)
            ?? throw new KeyNotFoundException($"Sprint {Id} not found");

        if (IsSelfModified)
        {
            MapTo(row);
        }

        tasksFactory.Save(Tasks!, row.Tasks);
        await db.SaveChangesAsync();
    }

    private void MapTo(SprintRow row)
    {
        row.Id = Id;
        row.Name = Name!;
        row.IsClosed = IsClosed;
    }
}
```

**The task.**
- **The setter is `internal`, not `private`.** MudNeatoo shows a field with a private setter as read-only, so a private setter would lock the checkbox. With `internal`, code outside the domain can't write `IsDone`; the checkbox writes through `MarkDone` instead.
- **The clock comes in through the constructor as `TimeProvider`.** Both the server and the browser register it, so the task can be built on either side.
- **The task's save methods are `internal` and not `[Remote]`.** Only the list calls them, and by then the call is already on the server.

```csharp
// ZTrack.Domain/Sprints/SprintTask.cs
using System.ComponentModel;
using ZTrack.Dal;

namespace ZTrack.Domain.Sprints;

/// <summary>One task of a sprint.</summary>
[Factory]
internal partial class SprintTask : EntityBase<SprintTask>, ISprintTask
{
    private readonly TimeProvider _clock;

    public SprintTask(IEntityBaseServices<SprintTask> services, TimeProvider clock) : base(services)
    {
        _clock = clock;
    }

    public partial Guid Id { get; private set; }

    [DisplayName("Title")]
    public partial string? Title { get; set; }

    [DisplayName("Done")]
    public partial bool IsDone { get; internal set; }

    public partial string? CompletedBy { get; private set; }

    public partial DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Intent: the who-stamp, then the fact. Each write is awaited, so when IsDone's
    /// consequences (the sprint's progress and CanClose) land, the stamp is in place.
    /// </summary>
    public async Task MarkDone(bool done, string user)
    {
        if (done)
        {
            await SetCompletedBy(user);
            await SetCompletedAt(_clock.GetUtcNow());
        }
        else
        {
            await SetCompletedBy(null);
            await SetCompletedAt(null);
        }

        await SetIsDone(done);
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
        CompletedAt = row.CompletedAt;
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
        row.Title = Title ?? "";
        row.IsDone = IsDone;
        row.CompletedBy = CompletedBy;
        row.CompletedAt = CompletedAt;
    }
}
```

**The list.** It manages the task rows: it gives each task its own row to write, and it removes the rows of deleted tasks. The sprint never maps a task row itself; if it did, saved tasks would stay marked as new and modified.

```csharp
// ZTrack.Domain/Sprints/SprintTaskList.cs
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

## ZTrack.Ef

This is your DbContext, implementing the Dal interface. Two settings matter:
- **`ValueGeneratedNever()` on the keys.** The domain generates its own Guids. Without this, EF Core 3+ assumes a new `TaskRow` that already has a key is an existing row, and issues an UPDATE instead of an INSERT.
- **A required relationship.** When the list removes a task row from `Tasks`, EF deletes the orphaned row on `SaveChanges`.

```csharp
// ZTrack.Ef/ZTrackDbContext.cs
using Microsoft.EntityFrameworkCore;
using ZTrack.Dal;

namespace ZTrack.Ef;

public sealed class ZTrackDbContext(DbContextOptions<ZTrackDbContext> options) : DbContext(options), IZTrackDb
{
    public DbSet<SprintRow> Sprints => Set<SprintRow>();
    public DbSet<TaskRow> Tasks => Set<TaskRow>();

    public Task<SprintRow?> FindSprintAsync(Guid id, CancellationToken cancellationToken = default)
        => Sprints.Include(s => s.Tasks).SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void AddSprint(SprintRow row) => Sprints.Add(row);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SprintRow>(sprint =>
        {
            sprint.ToTable("Sprint");
            sprint.HasKey(s => s.Id);
            sprint.Property(s => s.Id).ValueGeneratedNever();
            sprint.Property(s => s.Name).IsRequired().HasMaxLength(200);
            sprint.HasMany(s => s.Tasks)
                  .WithOne()
                  .HasForeignKey(t => t.SprintId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskRow>(task =>
        {
            task.ToTable("Task");
            task.HasKey(t => t.Id);
            task.Property(t => t.Id).ValueGeneratedNever();
            task.Property(t => t.Title).IsRequired().HasMaxLength(500);
            task.Property(t => t.CompletedBy).HasMaxLength(256);
        });
    }
}
```

## ZTrack.Server

The server uses Server mode with `UseNeatoo()`, which gives one endpoint and no controllers. It calls `AddNeatooServices`, not `AddNeatooAspNetCore`: adding the latter after Server mode breaks every remote Save (RemoteFactory#117).

```csharp
// ZTrack.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using ZTrack.Dal;
using ZTrack.Domain.Sprints;
using ZTrack.Ef;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(ISprint).Assembly);

// Constructor services the domain needs, registered on both tiers.
builder.Services.AddSingleton(TimeProvider.System);

// [Service] parameters: server only.
builder.Services.AddDbContext<ZTrackDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("ZTrack")));
builder.Services.AddScoped<IZTrackDb>(sp => sp.GetRequiredService<ZTrackDbContext>());

// Your existing authentication/authorization registration goes here.

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

## ZTrack.Client

```csharp
// ZTrack.Client/Program.cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using ZTrack.Client;
using ZTrack.Client.ViewModels;
using ZTrack.Domain.Sprints;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Remote mode: [Remote] operations cross to the server.
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(ISprint).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// The same constructor services the server registers; nothing server-only.
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddMudServices();
builder.Services.AddTransient<SprintViewModel>();

// Your existing AddAuthorizationCore() + AuthenticationStateProvider registration goes here.

await builder.Build().RunAsync();
```

```razor
@* ZTrack.Client/_Imports.razor *@
@using System.ComponentModel
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using MudBlazor
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using ZTrack.Domain.Sprints
@using ZTrack.Client.ViewModels
@using ZTrack.Client.Components
```

**The ViewModel.** It holds the sprint, turns each user action into a call on the domain, and saves. It decides nothing:
- `CanClose` passes through the sprint's own `CanClose`.
- `MarkDone` adds the signed-in user, the way the Neatoo examples pass the approver into `Approve`.
- The `_saving` flag only stops a double click from sending two Inserts (Neatoo#103). It is not a business rule.

```csharp
// ZTrack.Client/ViewModels/SprintViewModel.cs
using Microsoft.AspNetCore.Components.Authorization;
using ZTrack.Domain.Sprints;

namespace ZTrack.Client.ViewModels;

/// <summary>
/// ViewModel for the Sprint screen: holds the aggregate, forwards its gestures, saves.
/// </summary>
internal sealed class SprintViewModel
{
    private readonly ISprintFactory _sprintFactory;
    private readonly ISprintTaskFactory _taskFactory;
    private readonly AuthenticationStateProvider _authState;
    private bool _saving;

    public SprintViewModel(
        ISprintFactory sprintFactory,
        ISprintTaskFactory taskFactory,
        AuthenticationStateProvider authState)
    {
        _sprintFactory = sprintFactory;
        _taskFactory = taskFactory;
        _authState = authState;
    }

    /// <summary>The aggregate the page binds to. Null until created or loaded.</summary>
    public ISprint? Sprint { get; private set; }

    /// <summary>A load ran and the store had no such sprint.</summary>
    public bool NotFound { get; private set; }

    public async Task New()
    {
        Sprint = await _sprintFactory.Create();
        NotFound = false;
    }

    public async Task Load(Guid id)
    {
        Sprint = await _sprintFactory.Fetch(id);
        NotFound = Sprint is null;
    }

    public void AddTask(string title)
    {
        Sprint?.Tasks?.Add(_taskFactory.Create(title));
    }

    public void RemoveTask(ISprintTask task)
    {
        Sprint?.Tasks?.Remove(task);
    }

    /// <summary>The Done checkbox's gesture: the verb, with the signed-in user as the actor.</summary>
    public async Task MarkDone(ISprintTask task, bool done)
    {
        var state = await _authState.GetAuthenticationStateAsync();
        var user = state.User.Identity?.Name
            ?? throw new InvalidOperationException("Marking a task done needs a signed-in user.");

        await task.MarkDone(done, user);
    }

    /// <summary>The Close button binds the sprint's own admission fact.</summary>
    public bool CanClose => !_saving && (Sprint?.CanClose ?? false);

    /// <summary>The Save button binds the sprint's IsSavable; _saving only blocks a double click (Neatoo#103).</summary>
    public bool CanSave => !_saving && (Sprint?.IsSavable ?? false);

    /// <summary>"Close sprint": the verb, then the save that persists it.</summary>
    public async Task Close()
    {
        if (Sprint is null)
        {
            return;
        }

        await Sprint.Close();
        await Save();
    }

    public async Task Save()
    {
        if (Sprint is null || _saving)
        {
            return;
        }

        _saving = true;
        try
        {
            await Sprint.WaitForTasks();

            if (!Sprint.IsSavable)
            {
                return;  // invalid or unchanged; the validation summary shows why
            }

            Sprint = (ISprint)await Sprint.Save();  // Save returns a new instance
        }
        finally
        {
            _saving = false;
        }
    }
}
```

**The task row.** Each row subscribes to its own task, so ticking a box redraws only that row (to show the who/when line). The checkbox gets its value, error messages and busy state from `IsDone`, but writes through `SetDone`, which ends up calling the `MarkDone` verb.

```razor
@* ZTrack.Client/Components/SprintTaskRow.razor *@
@implements IDisposable

<MudGrid Spacing="2" Class="mb-2">
    <MudItem xs="1" Class="d-flex align-center">
        <MudNeatooCheckBox T="bool"
                           EntityProperty="@Item[nameof(ISprintTask.IsDone)]"
                           Setter="@SetDone" />
    </MudItem>
    <MudItem xs="6">
        <MudNeatooTextField T="string" EntityProperty="@Item[nameof(ISprintTask.Title)]" />
    </MudItem>
    <MudItem xs="4" Class="d-flex align-center">
        @if (Item.IsDone)
        {
            <MudText Typo="Typo.caption">Done by @Item.CompletedBy, @Item.CompletedAt?.ToLocalTime().ToString("g")</MudText>
        }
    </MudItem>
    <MudItem xs="1" Class="d-flex align-center">
        <MudIconButton Icon="@Icons.Material.Filled.Delete" OnClick="@OnRemove" />
    </MudItem>
</MudGrid>

@code {
    [Parameter, EditorRequired]
    public ISprintTask Item { get; set; } = default!;

    /// <summary>What a tick or untick calls; the page routes it to the ViewModel.</summary>
    [Parameter, EditorRequired]
    public Func<bool, Task> SetDone { get; set; } = default!;

    [Parameter]
    public EventCallback OnRemove { get; set; }

    private ISprintTask? _subscribed;

    // The row re-renders for its own task's changes (the stamp text), not the sprint's.
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribed, Item))
        {
            return;
        }

        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnTaskChanged;
        }

        _subscribed = Item;
        _subscribed.PropertyChanged += OnTaskChanged;
    }

    private void OnTaskChanged(object? sender, PropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnTaskChanged;
        }
    }
}
```

**The page.** It subscribes only to the sprint's own `PropertyChanged`, which covers what it shows (`PercentDone`, `IsClosed` and the Save/Close buttons). `@key` rebuilds the form after a save, because `Save()` returns a new instance. The page itself has no conditions or arithmetic; it only binds values the domain calculated.

```razor
@* ZTrack.Client/Pages/SprintPage.razor *@
@page "/sprints/new"
@page "/sprints/{Id:guid}"
@attribute [Authorize]
@implements IDisposable
@inject SprintViewModel Vm

<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-4">
@if (Vm.NotFound)
{
    <MudAlert Severity="Severity.Warning">Sprint not found.</MudAlert>
}
else if (Vm.Sprint is null)
{
    <MudProgressCircular Indeterminate="true" />
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
        <MudProgressLinear Value="@Vm.Sprint.PercentDone" Color="Color.Primary" Class="mb-4" />

        <MudText Typo="Typo.h6" Class="mb-2">Tasks</MudText>
        @foreach (var task in Vm.Sprint.Tasks!)
        {
            <SprintTaskRow @key="task"
                           Item="@task"
                           SetDone="@(done => Vm.MarkDone(task, done))"
                           OnRemove="@(() => Vm.RemoveTask(task))" />
        }

        <MudButton Variant="Variant.Outlined" OnClick="@(() => Vm.AddTask("New task"))" Class="mb-4">Add task</MudButton>

        <MudStack Row="true">
            <MudButton Variant="Variant.Filled" Color="Color.Primary" Disabled="@(!Vm.CanSave)" OnClick="SaveAsync">
                Save
            </MudButton>
            <MudButton Variant="Variant.Filled" Color="Color.Secondary" Disabled="@(!Vm.CanClose)" OnClick="CloseAsync">
                Close sprint
            </MudButton>
        </MudStack>
    </div>
}
</MudContainer>

@code {
    [Parameter] public Guid? Id { get; set; }

    private ISprint? _subscribed;

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

    // The tasks' rows subscribe to their own tasks. The page shows the sprint's own
    // facts and meta flags, so it listens to the sprint's own PropertyChanged - never
    // NeatooPropertyChanged, which carries every change in the graph.
    private void Resubscribe()
    {
        if (ReferenceEquals(_subscribed, Vm.Sprint))
        {
            return;
        }

        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnSprintChanged;
        }

        _subscribed = Vm.Sprint;

        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged += OnSprintChanged;
        }
    }

    private void OnSprintChanged(object? sender, PropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnSprintChanged;
        }
    }
}
```

---

## Decisions I made that your spec didn't cover

Each is a one-line change if you want it the other way:

1. **A sprint with no tasks can't be closed** and shows 0%. If you'd rather "every task is done" be true for an empty sprint, change the `Count == 0` branch in `SprintProgress.Of`.
2. **A closed sprint can't have open tasks** (`ClosedSprintHasNoOpenTasksRule`). This is what makes the Close condition hold on the server, not just in the button. As a side effect, unticking a task after closing makes the sprint invalid and unsavable. Nothing else is locked after closing: you can still rename the sprint, edit titles, and add or remove tasks.
3. **Who and when are recorded in the browser:** the user name comes from the signed-in user and the time from the browser's clock. If the server must be the authority for these, that needs a different design (for example, the task's `Update` stamps tasks whose `IsDone` changed).
4. **Task titles aren't required**, because the spec only defines the sprint name as making the sprint valid. A `[Required]` on `Title` would also block Close.
5. **There's no sprint `[Delete]`**, and a new sprint keeps the `/sprints/new` URL after its first save, because nothing redirects to `/sprints/{id}`.

Authentication isn't written out. The code assumes `AddAuthorizationCore()` with an `AuthenticationStateProvider` on the client and your authentication setup on the server.