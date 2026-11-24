I've written zTrain below: the domain model, the page and its ViewModel, the two components the page uses, and the startup code they need. Nothing here has been compiled or run, because you asked for code in the reply rather than files. I built it from the compiled `TrainingPlan` example in the Neatoo skill, which is close to this design. Gaps I couldn't settle are listed at the end.

**Projects.** `zTrain.Domain` is one class library that both the client and the server reference. The same classes run in the browser and on the server, so the minutes-sharing and the validation run in the browser as the coach edits. `zTrain.Client` is the Blazor WebAssembly app. `zTrain.Server` is the ASP.NET Core host.

---

## Domain (`zTrain.Domain`)

### `TrainingPlans/EffortScale.cs`
The effort range is a domain rule, so it lives here. The rules and the slider both read it, so the 1–10 range is written once.

```csharp
namespace zTrain.Domain.TrainingPlans;

/// <summary>The effort scale an interval is rated on.</summary>
public static class EffortScale
{
    public const int Min = 1;
    public const int Max = 10;

    /// <summary>The effort a newly added interval starts at.</summary>
    public const int Default = 5;
}
```

### `TrainingPlans/ITrainingPlanInterfaces.cs`
- The public interfaces are everything the client sees.
- The concrete classes are `internal`, so the page can only touch what is listed here.
- A child interval has no `Save()`.
- The two `*Fit` interfaces are `internal`: the parent and child use them to coordinate the minutes-sharing, and the UI cannot call them.

```csharp
using Neatoo;

namespace zTrain.Domain.TrainingPlans;

public enum PlanStatus
{
    Draft,
    Approved,
}

/// <summary>Training session plan aggregate root.</summary>
public partial interface ITrainingPlan : IEntityRoot
{
    /// <summary>Fact: the minutes the session is approved for. The fit shares them across the intervals.</summary>
    int ApprovedMinutes { get; set; }

    /// <summary>Consequence: the sum of the intervals' minutes.</summary>
    int PlannedMinutes { get; }

    PlanStatus Status { get; }

    /// <summary>Who approved the plan; set by Approve.</summary>
    string? ApprovedBy { get; }

    /// <summary>Consequence: admission for Approve. Draft and valid.</summary>
    bool CanApprove { get; }

    IIntervalList? Intervals { get; }

    /// <summary>Intent: a new interval joins the plan; the fit shares the minutes again.</summary>
    Task<IInterval> AddInterval();

    /// <summary>Intent: record who approved, then the status.</summary>
    Task Approve(string approver);
}

/// <summary>One interval of a training plan.</summary>
public partial interface IInterval : IEntityBase
{
    string? Name { get; set; }

    /// <summary>Fact: the effort slider. Writing it re-weights the fit.</summary>
    int Effort { get; set; }

    /// <summary>Fact: a locked interval keeps its minutes; the fit shares the rest.</summary>
    bool MinutesLocked { get; set; }

    /// <summary>Consequence of the fit, or the value SlideMinutes set.</summary>
    int Minutes { get; }

    /// <summary>Consequences the - and + buttons write into Effort.</summary>
    int NextEffortUp { get; }
    int NextEffortDown { get; }

    /// <summary>Intent: take these minutes and fit the others around this interval, for this one gesture.</summary>
    Task SlideMinutes(int minutes);
}

public interface IIntervalList : IEntityListBase<IInterval> { }

/// <summary>The plan's side of the fit: the anchor one slide holds for its cascade.</summary>
internal interface ITrainingPlanFit
{
    IDisposable AnchorFit(IInterval interval);
}

/// <summary>The interval's side of the fit: the plan writes a share through it.</summary>
internal interface IIntervalFit
{
    Task ApplyFit(int minutes);
}
```

### `TrainingPlans/TrainingPlan.cs`
The root holds the rules that cover the whole plan:
- **The fit** shares the approved minutes across the unlocked intervals by effort.
- **`PlannedMinutes`** is the total of the intervals' minutes.
- **The balance check** is a validation that the intervals add up to the approved minutes.
- **`CanApprove`** says whether Approve is allowed.

Each rule runs whenever one of the values it reads changes, including a child's property and the list's `Count`. So it doesn't matter whether a slider, a button or a test made the change.

```csharp
using System.Linq.Expressions;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace zTrain.Domain.TrainingPlans;

[Factory]
internal partial class TrainingPlan : EntityBase<TrainingPlan>, ITrainingPlan, ITrainingPlanFit
{
    private readonly IIntervalFactory _intervalFactory;

    // Set by SlideMinutes for one cascade; never serialized, never read by a later gesture.
    private IInterval? _anchor;

    public TrainingPlan(IEntityBaseServices<TrainingPlan> services, IIntervalFactory intervalFactory)
        : base(services)
    {
        _intervalFactory = intervalFactory;

        // Consequence: share the approved minutes across the free intervals by effort.
        // Triggered by every fact it reads, including the intervals' own Minutes (a slide)
        // and the list's Count (an add).
        RuleManager.AddActionAsync(
            async t => await t.Fit(),
            new Expression<Func<TrainingPlan, object?>>[]
            {
                t => t.ApprovedMinutes,
                t => t.Intervals!.Count,
                t => t.Intervals![0].Effort,
                t => t.Intervals![0].MinutesLocked,
                t => t.Intervals![0].Minutes,
            });

        // Consequence: the planned total.
        RuleManager.AddActionAsync(
            async t => await t.SetPlannedMinutes(t.Intervals?.Sum(i => i.Minutes) ?? 0),
            t => t.Intervals![0].Minutes,
            t => t.Intervals!.Count);

        RuleManager.AddValidation(
            t => t.ApprovedMinutes <= 0 ? "Approved minutes must be greater than zero" : string.Empty,
            t => t.ApprovedMinutes);

        RuleManager.AddRule(new MinutesBalanceRule());

        // Consequence: admission for Approve. IsValid is a trigger, so the fact follows validity.
        // The Approve button binds CanApprove; the verb never checks and throws.
        RuleManager.AddActionAsync(
            async t => await t.SetCanApprove(t.Status == PlanStatus.Draft && t.IsValid),
            t => t.Status,
            t => t.IsValid);
    }

    public partial int ApprovedMinutes { get; set; }
    public partial int PlannedMinutes { get; private set; }
    public partial PlanStatus Status { get; private set; }
    public partial string? ApprovedBy { get; private set; }
    public partial bool CanApprove { get; private set; }
    public partial IIntervalList? Intervals { get; private set; }

    [Create]
    public async Task Create([Service] IIntervalListFactory intervalsFactory)
    {
        Intervals = intervalsFactory.Create();
        ApprovedMinutes = 60;
        Status = PlanStatus.Draft;

        // Paused: the assignments ran no rule. The empty plan needs its balance message and
        // CanApprove before the coach touches anything. All is safe: the list is empty.
        await RunRules(RunRulesFlag.All);
    }

    /// <summary>
    /// Intent: a new interval, built with its effort, joins the plan. The add is the one change
    /// the fit hears (the list's Count).
    /// </summary>
    public async Task<IInterval> AddInterval()
    {
        var interval = await _intervalFactory.Create(string.Empty, EffortScale.Default);
        Intervals!.Add(interval);
        await WaitForTasks();
        return interval;
    }

    /// <summary>
    /// Intent: who approved, then the status. Each write is awaited, so CanApprove has
    /// recomputed when the verb returns.
    /// </summary>
    public async Task Approve(string approver)
    {
        await SetApprovedBy(approver);
        await SetStatus(PlanStatus.Approved);
    }

    /// <summary>
    /// The fit: locked intervals and the anchored one keep their minutes; the rest share what is
    /// left of the approved minutes in proportion to effort.
    /// </summary>
    private async Task Fit()
    {
        if (Intervals == null || Intervals.Count == 0)
        {
            return;
        }

        var held = Intervals.Where(i => i.MinutesLocked || ReferenceEquals(i, _anchor)).ToList();
        var free = Intervals.Except(held).ToList();
        if (free.Count == 0)
        {
            return;
        }

        var remaining = Math.Max(ApprovedMinutes - held.Sum(i => i.Minutes), 0);
        var shares = Share(remaining, free.Select(i => Math.Clamp(i.Effort, EffortScale.Min, EffortScale.Max)).ToList());

        for (var index = 0; index < free.Count; index++)
        {
            // Each write re-enters this rule; the nested fit computes the same shares and
            // writes nothing new, so the cascade converges.
            await ((IIntervalFit)free[index]).ApplyFit(shares[index]);
        }
    }

    /// <summary>
    /// Largest-remainder apportionment: whole minutes, proportional to weight, summing to total.
    /// Ties go to the earlier interval, so the result is stable across nested fits.
    /// </summary>
    private static int[] Share(int total, IReadOnlyList<int> weights)
    {
        var weightSum = weights.Sum();
        var shares = new int[weights.Count];
        var remainders = new (int Remainder, int Index)[weights.Count];
        var allocated = 0;

        for (var i = 0; i < weights.Count; i++)
        {
            var scaled = total * weights[i];
            shares[i] = scaled / weightSum;
            remainders[i] = (scaled % weightSum, i);
            allocated += shares[i];
        }

        foreach (var (_, index) in remainders
                     .OrderByDescending(r => r.Remainder)
                     .ThenBy(r => r.Index)
                     .Take(total - allocated))
        {
            shares[index]++;
        }

        return shares;
    }

    /// <summary>The anchor for one gesture. Scopes nest: the previous anchor returns when this one ends.</summary>
    IDisposable ITrainingPlanFit.AnchorFit(IInterval interval)
    {
        var previous = _anchor;
        _anchor = interval;
        return new AnchorScope(() => _anchor = previous);
    }

    private sealed class AnchorScope(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}

/// <summary>
/// The intervals must share exactly the approved minutes. Over (a slide past what is left)
/// or under (every interval locked short) both make the plan invalid, so Approve is refused.
/// </summary>
internal class MinutesBalanceRule : RuleBase<TrainingPlan>
{
    public MinutesBalanceRule()
        : base(t => t.PlannedMinutes, t => t.ApprovedMinutes, t => t.Intervals!.Count)
    {
    }

    protected override IRuleMessages Execute(TrainingPlan target)
    {
        var hasIntervals = target.Intervals is { Count: > 0 };

        return new RuleMessages()
            .If(!hasIntervals,
                nameof(TrainingPlan.PlannedMinutes),
                "Add at least one interval")
            .If(hasIntervals && target.PlannedMinutes != target.ApprovedMinutes,
                nameof(TrainingPlan.PlannedMinutes),
                $"The intervals plan {target.PlannedMinutes} of the {target.ApprovedMinutes} approved minutes");
    }
}
```

### `TrainingPlans/Interval.cs`
The interval owns its own validation and the next effort marks (the effort values the - and + buttons step to). Dragging the minutes slider is the one gesture that is more than setting a value: it holds this interval for that single gesture. So it is the method `SlideMinutes`, which sets a temporary anchor on the plan and then writes the minutes.

```csharp
using Neatoo;
using Neatoo.RemoteFactory;

namespace zTrain.Domain.TrainingPlans;

[Factory]
internal partial class Interval : EntityBase<Interval>, IInterval, IIntervalFit
{
    public Interval(IEntityBaseServices<Interval> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name is required" : string.Empty,
            t => t.Name);

        RuleManager.AddValidation(
            t => t.Effort is < EffortScale.Min or > EffortScale.Max
                ? $"Effort must be from {EffortScale.Min} to {EffortScale.Max}"
                : string.Empty,
            t => t.Effort);

        // Consequences the UI writes back: a nudge is a plain write of Effort.
        RuleManager.AddActionAsync(
            async t =>
            {
                await t.SetNextEffortUp(Math.Min(t.Effort + 1, EffortScale.Max));
                await t.SetNextEffortDown(Math.Max(t.Effort - 1, EffortScale.Min));
            },
            t => t.Effort);
    }

    public partial string? Name { get; set; }
    public partial int Effort { get; set; }
    public partial bool MinutesLocked { get; set; }
    public partial int Minutes { get; private set; }
    public partial int NextEffortUp { get; private set; }
    public partial int NextEffortDown { get; private set; }

    /// <summary>Public and local: the plan's AddInterval builds a new interval on the client.</summary>
    [Create]
    public async Task Create(string name, int effort)
    {
        Name = name;
        Effort = effort;

        // Paused: compute NextEffortUp/Down, and show "Name is required" on a blank new row.
        // All is safe: an interval has no children.
        await RunRules(RunRulesFlag.All);
    }

    /// <summary>
    /// Intent: take these minutes and fit the others around this interval. The anchor lives for
    /// this one cascade (every nested fit sees it) and evaporates when the gesture ends, so the
    /// next effort change shares everything again.
    /// </summary>
    public async Task SlideMinutes(int minutes)
    {
        using (((ITrainingPlanFit)Parent!).AnchorFit(this))
        {
            await SetMinutes(minutes);
        }
    }

    Task IIntervalFit.ApplyFit(int minutes) => SetMinutes(minutes);
}
```

### `TrainingPlans/IntervalList.cs
The child list. It only needs a `Create` for now; the fetch and save methods come with persistence.

```csharp
using Neatoo;
using Neatoo.RemoteFactory;

namespace zTrain.Domain.TrainingPlans;

[Factory]
internal partial class IntervalList : EntityListBase<IInterval>, IIntervalList
{
    [Create]
    public void Create()
    {
    }
}
```

---

## Client (`zTrain.Client`)

### `ViewModels/TrainingPlanViewModel.cs`
The ViewModel holds the plan and passes on the two actions that aren't tied to an input control: adding an interval and approving. Its one job besides that is supplying who is approving. It doesn't recheck validity, because the button is bound to `CanApprove`.

```csharp
using Microsoft.AspNetCore.Components.Authorization;
using zTrain.Domain.TrainingPlans;

namespace zTrain.Client.ViewModels;

/// <summary>ViewModel for the training plan screen: holds the plan and forwards its gestures.</summary>
internal sealed class TrainingPlanViewModel
{
    private readonly ITrainingPlanFactory _factory;
    private readonly AuthenticationStateProvider _authentication;

    public TrainingPlanViewModel(ITrainingPlanFactory factory, AuthenticationStateProvider authentication)
    {
        _factory = factory;
        _authentication = authentication;
    }

    /// <summary>The aggregate the page and its components bind to. Null until created.</summary>
    public ITrainingPlan? Plan { get; private set; }

    public async Task New()
    {
        Plan = await _factory.Create();
    }

    /// <summary>A new interval joins the plan; the fit shares the minutes again.</summary>
    public async Task AddInterval()
    {
        if (Plan is not null)
        {
            await Plan.AddInterval();
        }
    }

    /// <summary>
    /// Calls the verb with the signed-in coach. The Approve button is enabled by the plan's
    /// own CanApprove, so no admission check lives here.
    /// </summary>
    public async Task Approve()
    {
        if (Plan is null)
        {
            return;
        }

        var state = await _authentication.GetAuthenticationStateAsync();
        var approver = state.User.Identity?.Name
            ?? throw new InvalidOperationException("Approving a plan requires a signed-in coach.");

        await Plan.Approve(approver);
    }
}
```

### `Pages/TrainingPlanPage.razor`
The page lays out the editor and passes on the add-interval action. It doesn't subscribe to the plan's change events. Each input refreshes from its own property, and each component below refreshes for what it shows.

```razor
@page "/plans/new"
@inject TrainingPlanViewModel Vm

<PageTitle>Training plan</PageTitle>
<MudText Typo="Typo.h4" Class="mb-4">Training plan</MudText>

@if (Vm.Plan is not null)
{
    <div @key="Vm.Plan">
        <MudNeatooNumericField T="int"
                               EntityProperty="@Vm.Plan[nameof(ITrainingPlan.ApprovedMinutes)]"
                               Class="mb-4" />

        @foreach (var interval in Vm.Plan.Intervals!)
        {
            <IntervalRow @key="interval" Interval="@interval" />
        }

        <MudButton Variant="Variant.Outlined"
                   StartIcon="@Icons.Material.Filled.Add"
                   OnClick="Vm.AddInterval"
                   Class="mb-4">
            Add interval
        </MudButton>

        <PlanSummary Plan="@Vm.Plan" OnApprove="Vm.Approve" />
    </div>
}

@code {
    protected override async Task OnInitializedAsync()
    {
        await Vm.New();
    }
}
```

### `Components/IntervalRow.razor`
One row per interval:
- **Effort:** the slider and the -/+ buttons all set `Effort`.
- **Lock:** the switch sets `MinutesLocked`.
- **Minutes:** the slider shows `Minutes`, but on a drag it calls `SlideMinutes` (through `Setter`).

The row redraws for changes to its own interval only. The "x min" text outside the inputs needs that.

```razor
@using System.ComponentModel
@implements IDisposable

<MudPaper Class="pa-3 mb-2" Outlined="true">
    <MudGrid Spacing="2">
        <MudItem xs="12" sm="3">
            <MudNeatooTextField T="string" EntityProperty="@Interval[nameof(IInterval.Name)]" />
        </MudItem>

        <MudItem xs="12" sm="4" Class="d-flex align-center">
            @* A nudge is the same fact as the slider: the domain publishes the next mark, the button writes it. *@
            <MudIconButton Icon="@Icons.Material.Filled.Remove"
                           OnClick="@(() => Interval.SetEffort(Interval.NextEffortDown))" />
            <MudNeatooSlider T="int"
                             EntityProperty="@Interval[nameof(IInterval.Effort)]"
                             Label="Effort"
                             Min="EffortScale.Min"
                             Max="EffortScale.Max"
                             Step="1"
                             TickMarks="true" />
            <MudIconButton Icon="@Icons.Material.Filled.Add"
                           OnClick="@(() => Interval.SetEffort(Interval.NextEffortUp))" />
        </MudItem>

        <MudItem xs="12" sm="4">
            @* An intent: reads Minutes, writes through SlideMinutes, which fits the others around this interval. *@
            <MudNeatooSlider T="int"
                             EntityProperty="@Interval[nameof(IInterval.Minutes)]"
                             Setter="@Interval.SlideMinutes"
                             Label="Minutes"
                             Min="0"
                             Max="240"
                             Step="1" />
            <MudText Typo="Typo.caption">@Interval.Minutes min</MudText>
        </MudItem>

        <MudItem xs="12" sm="1" Class="d-flex align-center">
            <MudNeatooSwitch T="bool" EntityProperty="@Interval[nameof(IInterval.MinutesLocked)]" Label="Locked" />
        </MudItem>
    </MudGrid>
</MudPaper>

@code {
    [Parameter, EditorRequired]
    public IInterval Interval { get; set; } = default!;

    private IInterval? _subscribed;

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribed, Interval))
        {
            return;
        }

        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnIntervalChanged;
        }

        _subscribed = Interval;
        _subscribed.PropertyChanged += OnIntervalChanged;
    }

    private void OnIntervalChanged(object? sender, PropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnIntervalChanged;
        }
    }
}
```

### `Components/PlanSummary.razor`
This component shows the plan-level values: the planned total, the status, who approved it, the validation messages, and the Approve button. It redraws when the plan's own values change. A change to an interval reaches it only through the values the rules write onto the plan.

```razor
@using System.ComponentModel
@implements IDisposable

<NeatooValidationSummary Entity="@Plan" ShowHeader="false" Dense="true" Class="mb-3" />

<MudText Typo="Typo.subtitle1">Planned @Plan.PlannedMinutes of @Plan.ApprovedMinutes minutes · @Plan.Status</MudText>
@if (Plan.ApprovedBy is not null)
{
    <MudText Typo="Typo.body2" Class="mb-2">Approved by @Plan.ApprovedBy</MudText>
}

<MudButton Variant="Variant.Filled"
           Color="Color.Primary"
           Disabled="@(!Plan.CanApprove)"
           OnClick="@OnApprove"
           Class="mt-2">
    Approve
</MudButton>

@code {
    [Parameter, EditorRequired]
    public ITrainingPlan Plan { get; set; } = default!;

    [Parameter]
    public EventCallback OnApprove { get; set; }

    private ITrainingPlan? _subscribed;

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribed, Plan))
        {
            return;
        }

        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnPlanChanged;
        }

        _subscribed = Plan;
        _subscribed.PropertyChanged += OnPlanChanged;
    }

    private void OnPlanChanged(object? sender, PropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnPlanChanged;
        }
    }
}
```

### `_Imports.razor`, `App.razor`, `MainLayout.razor`
These are the minimum shell the page needs, because there is no existing app: routing, the layout, and the MudBlazor providers.

```razor
@* _Imports.razor *@
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using MudBlazor
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using zTrain.Domain.TrainingPlans
@using zTrain.Client
@using zTrain.Client.Components
@using zTrain.Client.ViewModels
```

```razor
@* App.razor *@
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
    </Found>
    <NotFound>
        <LayoutView Layout="@typeof(MainLayout)">
            <MudText>Page not found.</MudText>
        </LayoutView>
    </NotFound>
</Router>
```

```razor
@* MainLayout.razor *@
@inherits LayoutComponentBase

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.Large" Class="pt-6">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>
```

### `Program.cs` (client)
Neatoo runs in Remote mode on the client: the generated factories are proxies, and any `[Remote]` operation calls the server. Nothing in this design is `[Remote]` yet, so every operation here runs in the browser.

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using zTrain.Client;
using zTrain.Client.ViewModels;
using zTrain.Domain.TrainingPlans;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(ITrainingPlan).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore();

builder.Services.AddTransient<TrainingPlanViewModel>();

await builder.Build().RunAsync();
```

---

## Server (`zTrain.Server/Program.cs`)
The server hosts the client and maps the single RemoteFactory endpoint. It has nothing to do yet, but saving will arrive through this endpoint when persistence is added. Don't also call `AddNeatooAspNetCore` after `AddNeatooServices(Server)`: a known bug (RemoteFactory#117) breaks every remote save if you do.

```csharp
using Neatoo;
using Neatoo.RemoteFactory;
using zTrain.Domain.TrainingPlans;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(ITrainingPlan).Assembly);

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseNeatoo();
app.MapFallbackToFile("index.html");

await app.RunAsync();
```

---

## Gaps you'll need to fill
1. **Sign-in isn't written.** To record who approved, the ViewModel reads the signed-in user from `AuthenticationStateProvider`. You need to register a provider for whatever sign-in the app uses (and add the `Microsoft.AspNetCore.Components.Authorization` package). Without it, the ViewModel can't be created and the page won't load.
2. **Some `using` lines are guesses.** I couldn't find `Neatoo.Rules` and `Neatoo.RemoteFactory` in the reference examples. Check them on the first build.
3. **Field labels.** The name field and the approved-minutes field use Neatoo's default label, which is probably the raw property name (e.g. "ApprovedMinutes"). I didn't use a `[DisplayName]` attribute because I couldn't confirm Neatoo reads it.
4. **Minutes slider maximum** is fixed at 240. If a drag goes past what is left, the plan becomes invalid and shows a message rather than blocking the drag.
5. **Not in your spec, so not added:** removing an interval, and whether an approved plan can still be edited. Right now, edits after approval keep the status `Approved`. Tell me which rule you want.
6. **Choices I made:**
   - A new interval starts with a blank name and effort 5, so it shows "Name is required" until the coach names it.
   - An empty plan, or one where every interval is locked and the minutes don't add up, is invalid, so it can't be approved.