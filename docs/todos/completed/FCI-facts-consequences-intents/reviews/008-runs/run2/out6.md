I wrote the code below in this reply only. No files were created and nothing was compiled. It follows the compiled TrainingPlan examples in the Neatoo, RemoteFactory and MudNeatoo skill references. Three things are my own choices and are listed at the end.

There are three projects:
- **zTrain.Domain** is one class library that both the client and the server reference.
- **zTrain.Client** is the Blazor WebAssembly app.
- **zTrain.Server** is the ASP.NET Core host.

---

## zTrain.Domain

**Why it's here:** the client and server run the same compiled classes. The fit, the validation and the approval rule run in the browser while the coach drags a slider, with no server call. Because persistence is out of scope, nothing here is `[Remote]`.

### `GlobalUsings.cs`
The skill references never show the namespaces for the core Neatoo types. If the compiler disagrees, this is the only file to fix.

```cs
global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.Linq.Expressions;
global using Neatoo;
global using Neatoo.RemoteFactory;
global using Neatoo.Rules;
```

### `TrainingPlanInterfaces.cs`
**Why it's here:** the interfaces are public and the classes are `internal`, so every reference goes through an interface. The interfaces are `partial` so the generator can add `Task Set{Name}(…)` for each public setter. The page's nudge buttons use those methods.

```cs
namespace zTrain.Domain;

public enum PlanStatus
{
    Draft,
    Approved,
}

/// <summary>
/// Training session plan aggregate root.
/// </summary>
public partial interface ITrainingPlan : IEntityRoot
{
    /// <summary>Fact: the minutes the session is approved for. The fit shares them across the intervals.</summary>
    int ApprovedMinutes { get; set; }

    /// <summary>Consequence: the sum of the intervals' minutes.</summary>
    int PlannedMinutes { get; }

    PlanStatus Status { get; }

    /// <summary>Who approved the plan; null until approved.</summary>
    string? ApprovedBy { get; }

    /// <summary>Consequence: admission for Approve. A draft plan that is valid.</summary>
    bool CanApprove { get; }

    IIntervalList? Intervals { get; }

    /// <summary>Intent: a new interval and its effort land together.</summary>
    Task<IInterval> AddInterval(string name, int effort);

    /// <summary>Intent: who approved, then the status.</summary>
    Task Approve(string approver);
}

/// <summary>
/// One interval of a training plan.
/// </summary>
public partial interface IInterval : IEntityBase
{
    string? Name { get; set; }

    /// <summary>Fact: 1 to 10, written by the slider and the nudge buttons. Re-weights the fit.</summary>
    int Effort { get; set; }

    /// <summary>Fact: a locked interval keeps its minutes; the fit shares the rest.</summary>
    bool MinutesLocked { get; set; }

    /// <summary>Consequence of the fit, or the value a slide set.</summary>
    int Minutes { get; }

    /// <summary>Consequences: the effort marks the - and + buttons write into Effort.</summary>
    int NextEffortUp { get; }
    int NextEffortDown { get; }

    /// <summary>Intent: take these minutes and fit the other intervals around this one, for this one gesture.</summary>
    Task SlideMinutes(int minutes);
}

public interface IIntervalList : IEntityListBase<IInterval> { }
```

### `TrainingPlan.cs`
**Why it's here:** how the minutes are shared is a rule on the root, because only the root sees every interval. It is triggered by every fact it reads: approved minutes, the interval count, and each interval's effort, lock and minutes. When the coach drags an interval's minutes slider, that interval becomes an "anchor" that keeps its minutes for that one drag. The anchor is held in a scope that clears when the gesture ends, so the next effort change shares everything again. Validity and `CanApprove` are rules, so the Approve button only reads a value.

```cs
namespace zTrain.Domain;

/// <summary>
/// The root's fit, as an interval's verb sees it.
/// </summary>
internal interface ITrainingPlanFit
{
    /// <summary>Holds this interval's minutes for every fit inside the returned scope.</summary>
    IDisposable AnchorFit(IInterval interval);
}

/// <summary>
/// Training session plan aggregate root.
/// </summary>
[Factory]
internal partial class TrainingPlan : EntityBase<TrainingPlan>, ITrainingPlan, ITrainingPlanFit
{
    private const int DefaultApprovedMinutes = 60;

    private readonly IIntervalFactory _intervalFactory;

    // The interval being slid, for one gesture only. Not state: nothing reads it after the gesture.
    private IInterval? _anchor;

    public TrainingPlan(IEntityBaseServices<TrainingPlan> services, IIntervalFactory intervalFactory)
        : base(services)
    {
        _intervalFactory = intervalFactory;

        RuleManager.AddValidation(
            t => t.ApprovedMinutes <= 0 ? "Approved minutes must be more than zero" : string.Empty,
            t => t.ApprovedMinutes);

        // Consequence: share the approved minutes across the free intervals by effort.
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
            t => t.Intervals!.Count,
            t => t.Intervals![0].Minutes);

        RuleManager.AddRule(new PlanSharesApprovedMinutesRule());

        // Consequence: admission for Approve. IsValid is a trigger, so CanApprove follows validity.
        RuleManager.AddActionAsync(
            async t => await t.SetCanApprove(t.Status == PlanStatus.Draft && t.IsValid),
            t => t.Status,
            t => t.IsValid);
    }

    [DisplayName("Approved minutes")]
    public partial int ApprovedMinutes { get; set; }

    [DisplayName("Planned minutes")]
    public partial int PlannedMinutes { get; private set; }

    public partial PlanStatus Status { get; private set; }

    public partial string? ApprovedBy { get; private set; }

    public partial bool CanApprove { get; private set; }

    public partial IIntervalList? Intervals { get; private set; }

    [Create]
    public async Task Create([Service] IIntervalListFactory intervalsFactory)
    {
        Intervals = intervalsFactory.Create();
        ApprovedMinutes = DefaultApprovedMinutes;
        Status = PlanStatus.Draft;

        // Paused: the assignments ran no rule. Run them so CanApprove and the validation
        // messages are right before the coach touches anything. All is safe: no intervals yet.
        await RunRules(RunRulesFlag.All);
    }

    /// <summary>
    /// Intent: a new interval, built with its name and effort, joins the plan. The add is the
    /// one change the fit hears (the list's Count).
    /// </summary>
    public async Task<IInterval> AddInterval(string name, int effort)
    {
        var interval = await _intervalFactory.Create(name, effort);
        Intervals!.Add(interval);
        await WaitForTasks();
        return interval;
    }

    /// <summary>
    /// Intent: who approved, then the status. Admission is CanApprove, which the button binds.
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
        var shares = Share(remaining, free.Select(i => Math.Max(i.Effort, 1)).ToList());

        for (var index = 0; index < free.Count; index++)
        {
            // Each write re-enters this rule; the nested fit computes the same shares and
            // writes nothing new, so the cascade converges.
            await ((IIntervalFit)free[index]).ApplyFit(shares[index]);
        }
    }

    /// <summary>
    /// Whole minutes in proportion to the weights, summing exactly to the total
    /// (largest remainder; ties go to the earlier interval).
    /// </summary>
    private static int[] Share(int total, IReadOnlyList<int> weights)
    {
        var weightSum = weights.Sum();
        var shares = new int[weights.Count];
        var remainders = new (int Index, long Remainder)[weights.Count];
        var assigned = 0;

        for (var i = 0; i < weights.Count; i++)
        {
            var exact = (long)total * weights[i];
            shares[i] = (int)(exact / weightSum);
            remainders[i] = (i, exact % weightSum);
            assigned += shares[i];
        }

        foreach (var (index, _) in remainders
                     .OrderByDescending(r => r.Remainder)
                     .ThenBy(r => r.Index)
                     .Take(total - assigned))
        {
            shares[index]++;
        }

        return shares;
    }

    /// <summary>
    /// The anchor for one gesture. Scopes nest: the previous anchor comes back when this one ends.
    /// </summary>
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
/// The approved minutes must be fully shared: at least one interval, and the intervals'
/// minutes add up to the approved minutes. They fall short only when every interval is
/// locked, and run over only when locked minutes exceed the approved minutes.
/// </summary>
internal sealed class PlanSharesApprovedMinutesRule : RuleBase<TrainingPlan>
{
    public PlanSharesApprovedMinutesRule()
        : base(t => t.ApprovedMinutes, t => t.PlannedMinutes, t => t.Intervals!.Count) { }

    protected override IRuleMessages Execute(TrainingPlan target)
    {
        var hasIntervals = target.Intervals is { Count: > 0 };

        return new RuleMessages()
            .If(!hasIntervals,
                nameof(TrainingPlan.ApprovedMinutes),
                "Add an interval to share the approved minutes")
            .If(hasIntervals && target.PlannedMinutes != target.ApprovedMinutes,
                nameof(TrainingPlan.ApprovedMinutes),
                $"The intervals plan {target.PlannedMinutes} minutes; {target.ApprovedMinutes} are approved");
    }
}
```

### `Interval.cs`
**Why it's here:** effort and the lock are facts, so the controls write them directly. The marks that the - and + buttons step to are published by a rule on the interval. Dragging the minutes slider is the one thing the stored state can't express (fit the other intervals around this one, for this drag only), so it is the only method you call directly on an interval. `Minutes` has a `private set`: only the fit and the slide write it.

```cs
namespace zTrain.Domain;

/// <summary>
/// The fit's write into an interval, as the root sees it.
/// </summary>
internal interface IIntervalFit
{
    Task ApplyFit(int minutes);
}

/// <summary>
/// One interval of a training plan.
/// </summary>
[Factory]
internal partial class Interval : EntityBase<Interval>, IInterval, IIntervalFit
{
    private const int MinEffort = 1;
    private const int MaxEffort = 10;

    public Interval(IEntityBaseServices<Interval> services) : base(services)
    {
        // Consequences the - and + buttons write back into Effort.
        RuleManager.AddActionAsync(
            async t =>
            {
                await t.SetNextEffortUp(Math.Min(t.Effort + 1, MaxEffort));
                await t.SetNextEffortDown(Math.Max(t.Effort - 1, MinEffort));
            },
            t => t.Effort);
    }

    [DisplayName("Interval")]
    [Required(ErrorMessage = "Name the interval")]
    public partial string? Name { get; set; }

    [DisplayName("Effort")]
    [Range(MinEffort, MaxEffort, ErrorMessage = "Effort is from 1 to 10")]
    public partial int Effort { get; set; }

    [DisplayName("Lock minutes")]
    public partial bool MinutesLocked { get; set; }

    [DisplayName("Minutes")]
    public partial int Minutes { get; private set; }

    public partial int NextEffortUp { get; private set; }

    public partial int NextEffortDown { get; private set; }

    /// <summary>
    /// Public and local: the plan builds an interval on the client before adding it.
    /// </summary>
    [Create]
    public async Task Create(string name, int effort)
    {
        Name = name;
        Effort = effort;

        // Paused: compute the next marks now. All is safe: an interval has no children.
        await RunRules(RunRulesFlag.All);
    }

    /// <summary>
    /// Intent: take these minutes and fit the other intervals around this one. The anchor lives
    /// for this one cascade and is gone when the gesture ends.
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

### `IntervalList.cs`

```cs
namespace zTrain.Domain;

/// <summary>
/// The intervals of a training plan.
/// </summary>
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

## zTrain.Client

### `ViewModels/TrainingPlanViewModel.cs`
**Why it's here:** it holds the plan and turns the two gestures that aren't a control (add an interval, approve) into calls on the domain. It decides nothing. Knowing who is signed in belongs to the client tier, so that is the only thing it adds to `Approve`.

```cs
using Microsoft.AspNetCore.Components.Authorization;
using zTrain.Domain;

namespace zTrain.Client.ViewModels;

/// <summary>
/// ViewModel for the training plan screen: holds the plan and forwards its two gestures.
/// </summary>
internal sealed class TrainingPlanViewModel
{
    private readonly ITrainingPlanFactory _factory;
    private readonly AuthenticationStateProvider _authentication;

    public TrainingPlanViewModel(ITrainingPlanFactory factory, AuthenticationStateProvider authentication)
    {
        _factory = factory;
        _authentication = authentication;
    }

    /// <summary>The aggregate the page and its rows bind to. Null until created.</summary>
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
            await Plan.AddInterval("New interval", 5);
        }
    }

    /// <summary>
    /// Calls the verb with the signed-in coach. The button is enabled by the plan's CanApprove.
    /// </summary>
    public async Task Approve()
    {
        if (Plan is null)
        {
            return;
        }

        var state = await _authentication.GetAuthenticationStateAsync();
        var coach = state.User.Identity?.Name
            ?? throw new InvalidOperationException("Approving a plan needs a signed-in coach.");

        await Plan.Approve(coach);
    }
}
```

### `Pages/TrainingPlanPage.razor`
**Why it's here:** the page binds approved minutes, the summary and the Approve button to the plan's own facts. It also passes approved minutes to each row as the minutes slider's maximum. That's why it subscribes to the plan's own `PropertyChanged` (only the root's own properties), and never to the whole-graph `NeatooPropertyChanged`. Each row refreshes itself.

```razor
@page "/plans/new"
@implements IDisposable
@inject TrainingPlanViewModel Vm

<PageTitle>Session plan</PageTitle>

@if (Vm.Plan is null)
{
    <MudProgressLinear Indeterminate="true" />
}
else
{
    <MudPaper Class="pa-4">
        <MudText Typo="Typo.h5" Class="mb-3">Session plan</MudText>

        <NeatooValidationSummary Entity="@Vm.Plan" ShowHeader="false" Dense="true" Class="mb-3" />

        <MudNeatooNumericField T="int"
                               EntityProperty="@Vm.Plan[nameof(ITrainingPlan.ApprovedMinutes)]"
                               Min="0"
                               Class="mb-4" />

        @foreach (var interval in Vm.Plan.Intervals!)
        {
            @* Keyed on the interval: each row's inputs subscribe to that interval's wrappers. *@
            <IntervalRow @key="interval" Interval="@interval" MaxMinutes="@Vm.Plan.ApprovedMinutes" />
        }

        <MudButton Variant="Variant.Outlined"
                   StartIcon="@Icons.Material.Filled.Add"
                   OnClick="Vm.AddInterval"
                   Class="mb-4">
            Add interval
        </MudButton>

        <MudText Typo="Typo.subtitle1">
            Planned @Vm.Plan.PlannedMinutes of @Vm.Plan.ApprovedMinutes minutes · @Vm.Plan.Status
        </MudText>

        @if (Vm.Plan.ApprovedBy is not null)
        {
            <MudText Typo="Typo.body2" Class="mb-2">Approved by @Vm.Plan.ApprovedBy</MudText>
        }

        @* Enabled by the plan's admission fact; the page has no condition of its own. *@
        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   Disabled="@(!Vm.Plan.CanApprove)"
                   OnClick="Vm.Approve">
            Approve
        </MudButton>
    </MudPaper>
}

@code {
    private ITrainingPlan? _subscribed;

    protected override async Task OnInitializedAsync()
    {
        await Vm.New();

        // The page shows the plan's own facts and hands ApprovedMinutes to the rows, so it
        // re-renders on the plan's own PropertyChanged. Rows subscribe to their own interval.
        _subscribed = Vm.Plan;
        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged += OnPlanChanged;
        }
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

### `Components/IntervalRow.razor`
**Why it's here:** in Blazor, re-rendering happens per component. Each row re-renders for changes to its own interval, which keeps the minutes text current while another row's slider moves.
- The effort slider and the lock switch write facts.
- The - and + buttons write the mark the domain publishes.
- The minutes slider reads `Minutes` but writes through `SlideMinutes`, using `Setter`.

```razor
@implements IDisposable

<MudGrid Spacing="2" Class="mb-2">
    <MudItem xs="12" sm="3">
        <MudNeatooTextField T="string" EntityProperty="@Interval[nameof(IInterval.Name)]" />
    </MudItem>

    <MudItem xs="12" sm="4">
        <MudStack Row="true" AlignItems="AlignItems.Center">
            @* A nudge is a plain write of Effort: the domain publishes the next mark. *@
            <MudIconButton Icon="@Icons.Material.Filled.Remove"
                           OnClick="@(() => Interval.SetEffort(Interval.NextEffortDown))" />
            @* A fact: the slider writes Effort, and the fit follows whoever wrote it. *@
            <MudNeatooSlider T="int"
                             EntityProperty="@Interval[nameof(IInterval.Effort)]"
                             Min="1" Max="10" Step="1"
                             TickMarks="true"
                             ValueLabel="true" />
            <MudIconButton Icon="@Icons.Material.Filled.Add"
                           OnClick="@(() => Interval.SetEffort(Interval.NextEffortUp))" />
        </MudStack>
    </MudItem>

    <MudItem xs="12" sm="3">
        @* An intent: reads Minutes, writes through SlideMinutes, which fits the other
           intervals around this one for this one gesture. *@
        <MudNeatooSlider T="int"
                         EntityProperty="@Interval[nameof(IInterval.Minutes)]"
                         Setter="@Interval.SlideMinutes"
                         Min="0" Max="@MaxMinutes" Step="1" />
        <MudText Typo="Typo.caption">@Interval.Minutes min</MudText>
    </MudItem>

    <MudItem xs="12" sm="2">
        <MudNeatooSwitch T="bool"
                         EntityProperty="@Interval[nameof(IInterval.MinutesLocked)]"
                         Label="@Interval[nameof(IInterval.MinutesLocked)].DisplayName" />
    </MudItem>
</MudGrid>

@code {
    [Parameter, EditorRequired]
    public IInterval Interval { get; set; } = default!;

    /// <summary>The plan's approved minutes: the most one interval can take.</summary>
    [Parameter]
    public int MaxMinutes { get; set; }

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

### `_Imports.razor`

```razor
@using System.ComponentModel
@using Microsoft.AspNetCore.Components.Web
@using MudBlazor
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using Neatoo.Blazor.MudNeatoo.Extensions
@using zTrain.Domain
@using zTrain.Client.Components
@using zTrain.Client.ViewModels
```

### `App.razor` and `Layout/MainLayout.razor`
**Why they're here:** MudBlazor needs its providers mounted once, in the layout.

```razor
@* App.razor *@
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(Layout.MainLayout)" />
    </Found>
    <NotFound>
        <MudText>Nothing here.</MudText>
    </NotFound>
</Router>
```

```razor
@* Layout/MainLayout.razor *@
@inherits LayoutComponentBase

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudMainContent Class="pa-4">
        @Body
    </MudMainContent>
</MudLayout>
```

### `Program.cs` (client)
**Why it's here:** it registers Neatoo in Remote mode, MudBlazor and the ViewModel. Nothing in this domain crosses the wire yet. The `HttpClient` is registered so that later `[Remote]` operations (persistence) work without changing the setup.

```cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using zTrain.Client;
using zTrain.Client.ViewModels;
using zTrain.Domain;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(ITrainingPlan).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore();   // the sign-in scheme registers the AuthenticationStateProvider

builder.Services.AddTransient<TrainingPlanViewModel>();

await builder.Build().RunAsync();
```

---

## zTrain.Server

### `Program.cs`
**Why it's here:** it hosts the WebAssembly client and maps the single RemoteFactory endpoint. It uses Neatoo's Server-mode registration, and only that one: adding `AddNeatooAspNetCore` after it breaks remote saves (RemoteFactory#117).

```cs
using Neatoo;
using Neatoo.RemoteFactory;
using zTrain.Domain;

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

## What to check

- **Not compiled.** The core Neatoo `using` namespaces (`Neatoo`, `Neatoo.Rules`) are my guess; the reference material never shows them. If they're wrong, the build will say so in `GlobalUsings.cs` and the two `Program.cs` files.
- **My definition of "valid".** The spec doesn't define it. I chose:
  - approved minutes must be above zero;
  - every interval needs a name and an effort from 1 to 10;
  - there must be at least one interval;
  - the planned total must equal the approved minutes.

  The last one only fails when locked minutes go over the approved minutes, or when every interval is locked and their minutes fall short.
- **Who approved comes from the signed-in user.** The ViewModel reads it from `AuthenticationStateProvider`, and sign-in isn't part of this answer. Until your sign-in scheme registers a provider, the ViewModel can't be created. A user who isn't signed in gets an exception rather than an approval with no name.
- **Rounding.** Shares are whole minutes using the largest-remainder method, so they always add up exactly. My numbers won't match the reference implementation in every case: efforts 2/6/6 over 60 minutes give 8/26/26 here, and 9/26/25 there.
- **No Save, and no "edit after approval" rule.** With no persistence there is no Save button. The spec doesn't say whether an approved plan can still be edited, so this code allows it.
- **`index.html` needs MudBlazor's CSS and JS links.** That file isn't C# or Razor, so it isn't included above.