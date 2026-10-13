# ViewModels Over a Live Aggregate

Derived values live in the aggregate, as rules. A ViewModel that owns an aggregate the user edits through MudNeatoo does not compute anything from it. It holds the aggregate, forwards gestures to entity verbs, and saves; the page binds the aggregate and subscribes to it.

## The Derived Value Is a Rule on the Entity

MudNeatoo components bind directly to the aggregate through `EntityProperty`. When the user changes a value, the component calls `SetValue()` on the aggregate; the ViewModel is not involved. Anything derived from that value therefore has to be computed where the change happens: an `AddAction` rule on the entity, writing a `private set` partial property. That property raises `PropertyChanged`, serializes, and is read-only to the UI.

```csharp
// WRONG: the ViewModel derives from the aggregate. The rule now has two owners,
// and the VM has to know which entity properties feed it.
public int ApprovedDurationMinutes => (int)Math.Round(_plan.ApprovedDuration / 60.0);
public double ProgressPercent =>
    _plan.ApprovedTreatments > 0
        ? (double)_plan.CompletedTreatmentCount / _plan.ApprovedTreatments * 100 : 0;

// WRONG: an expression-body on the entity. It raises no PropertyChanged and is not serialized,
// so a binding to it never refreshes.
public bool IsApproved => ApprovedById.HasValue;
```

The rules on `WorkOrder` write its facts into `private set` properties:

<!-- snippet: skill-derived-facts -->
<a id='snippet-skill-derived-facts'></a>
```cs
// A business fact derived from two others. The UI binds to it (for
// example @if (workOrder.IsHeld) around a banner) and decides nothing;
// the entity never names a screen element (no "ShowHoldBanner").
RuleManager.AddAction(
    t => t.IsHeld = t.IsOnHold && t.Status != "Closed",
    t => t.IsOnHold,
    t => t.Status);

// A flag derived from a child property. The trigger is the child's
// property path, so any task's IsSchedulable change recomputes it.
RuleManager.AddAction(
    t => t.HasUnschedulableTasks = t.Tasks?.Any(task => !task.IsSchedulable) ?? false,
    t => t.Tasks![0].IsSchedulable);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L56-L70' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-derived-facts' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The page binds those facts and the editable fields alike, and has no condition of its own:

<!-- snippet: skill-page-binds-facts -->
<a id='snippet-skill-page-binds-facts'></a>
```razor
@* @key: Save returns a new instance; keying on it rebuilds the bound
   components so each field subscribes to the new instance's wrappers. *@
<div @key="Vm.WorkOrder">
@* Every show/hide and enable/disable here is a business fact the
   aggregate computed. The page has no condition of its own. *@
@if (Vm.ShowHoldBanner)
{
    <MudAlert Severity="Severity.Warning" Class="mb-3">This work order is on hold.</MudAlert>
}

@if (Vm.WorkOrder.IsOverBudget)
{
    <MudAlert Severity="Severity.Error" Class="mb-3">Over budget: @Vm.WorkOrder.Summary</MudAlert>
}

@if (Vm.WorkOrder.HasUnschedulableTasks)
{
    <MudAlert Severity="Severity.Info" Class="mb-3">Some tasks cannot be scheduled.</MudAlert>
}

<NeatooValidationSummary Entity="@Vm.WorkOrder" Class="mb-4" />

<MudText Typo="Typo.subtitle1">Status: @Vm.WorkOrder.Status</MudText>

<MudNeatooNumericField T="decimal" EntityProperty="@Vm.WorkOrder[nameof(IWorkOrder.Budget)]" Class="mb-3" />
<MudNeatooNumericField T="decimal" EntityProperty="@Vm.WorkOrder[nameof(IWorkOrder.Discount)]" Class="mb-3" />
<MudNeatooSwitch T="bool" EntityProperty="@Vm.WorkOrder[nameof(IWorkOrder.IsOnHold)]" Class="mb-3" />

<MudText Typo="Typo.h6" Class="my-3">Tasks</MudText>

@if (Vm.WorkOrder.Tasks is not null)
{
    @foreach (var task in Vm.WorkOrder.Tasks)
    {
        <MudGrid Class="mb-2" Spacing="2">
            <MudItem xs="12" sm="5">
                <MudNeatooTextField T="string" EntityProperty="@task[nameof(IWorkOrderTask.Name)]" />
            </MudItem>
            <MudItem xs="6" sm="2">
                <MudNeatooNumericField T="decimal" EntityProperty="@task[nameof(IWorkOrderTask.Hours)]" />
            </MudItem>
            <MudItem xs="6" sm="2">
                <MudNeatooNumericField T="decimal" EntityProperty="@task[nameof(IWorkOrderTask.Rate)]" />
            </MudItem>
            <MudItem xs="12" sm="3" Class="d-flex align-center">
                <MudText>@task.Cost.ToString("C")</MudText>
            </MudItem>
        </MudGrid>
    }
}

<MudButton Variant="Variant.Outlined" OnClick="@(() => Vm.AddTask("New task"))" Class="mb-3">Add Task</MudButton>

<MudText Typo="Typo.subtitle1" Class="mb-3">Total: @Vm.WorkOrder.TotalCost.ToString("C")</MudText>

@* The verb's button is enabled by the entity's admission fact. *@
<MudButton Variant="Variant.Outlined"
           Color="Color.Secondary"
           Disabled="@(!Vm.CanApprove)"
           OnClick="@(() => Vm.Approve("design.app"))"
           Class="mr-2">
    Approve
</MudButton>

<MudButton Variant="Variant.Filled"
           Color="Color.Primary"
           Disabled="@(!Vm.CanSave)"
           OnClick="SaveAsync">
    Save
</MudButton>
</div>
```
<sup><a href='/src/Design/Design.App/Pages/WorkOrderPage.razor#L20-L92' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-page-binds-facts' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Nothing on the ViewModel is derived from the aggregate. There is no trigger map to maintain, because each rule declares its own triggers.

## What the ViewModel Does

It holds the aggregate, forwards gestures to entity verbs, and saves through the root. `WorkOrder` is an aggregate root (`IWorkOrder : IEntityRoot`); a child is saved through its root, never by the ViewModel. The one thing it may add is the screen's own name for a fact, mapped one-to-one (`ShowHoldBanner` for `IsHeld`).

<!-- snippet: skill-viewmodel-over-aggregate -->
<a id='snippet-skill-viewmodel-over-aggregate'></a>
```cs
/// <summary>
/// ViewModel for the WorkOrder screen: holds the aggregate; the page binds to
/// the aggregate's facts and this class's one screen-named mapping.
/// </summary>
internal sealed class WorkOrderViewModel
{
    private readonly IWorkOrderFactory _workOrderFactory;
    private readonly IWorkOrderTaskFactory _taskFactory;

    public WorkOrderViewModel(IWorkOrderFactory workOrderFactory, IWorkOrderTaskFactory taskFactory)
    {
        _workOrderFactory = workOrderFactory;
        _taskFactory = taskFactory;
    }

    /// <summary>The aggregate the page binds to. Null until loaded or created.</summary>
    public IWorkOrder? WorkOrder { get; private set; }

    public async Task New()
    {
        WorkOrder = await _workOrderFactory.Create();
    }

    public async Task Load(Guid id)
    {
        WorkOrder = await _workOrderFactory.Fetch(id);
    }

    public void AddTask(string name)
    {
        if (WorkOrder?.Tasks is null)
        {
            return;
        }

        // Count + 1 is an input default for the new task's position, which the
        // user may change; the task's own rule validates uniqueness. Not a decision.
        WorkOrder.Tasks.Add(_taskFactory.Create(name, WorkOrder.Tasks.Count + 1));
    }

    /// <summary>
    /// This screen's name for the entity's IsHeld fact. A mapping, not a decision.
    /// </summary>
    public bool ShowHoldBanner => WorkOrder?.IsHeld ?? false;

    /// <summary>The Approve button binds to the entity's own admission fact.</summary>
    public bool CanApprove => WorkOrder?.CanApprove ?? false;

    /// <summary>Calls the verb; the entity's rules update CanApprove and Status.</summary>
    public void Approve(string approver)
    {
        WorkOrder?.Approve(approver);
    }

    public bool CanSave => WorkOrder?.IsSavable ?? false;

    public async Task Save()
    {
        if (WorkOrder is null)
        {
            return;
        }

        await WorkOrder.WaitForTasks();

        if (!WorkOrder.IsSavable)
        {
            return;
        }

        WorkOrder = (IWorkOrder)await WorkOrder.Save();
    }
}
```
<sup><a href='/src/Design/Design.App/ViewModels/WorkOrderViewModel.cs#L28-L102' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-viewmodel-over-aggregate' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Key Points

1. **Derived values are entity rules.** If the ViewModel has a `=>` that combines entity values, move it into an `AddAction` on the entity and bind to the resulting `private set` property. A one-to-one rename of a fact is the only `=>` it keeps.
2. **The ViewModel is a plain class.** It raises nothing and subscribes to nothing: the entity's own `PropertyChanged` has already fired for the derived property.
3. **The page subscribes.** Blazor does not observe `INotifyPropertyChanged` on its own, so the page subscribes to the entity the ViewModel holds and calls `StateHasChanged` — once, in the page, and moved to the new instance after a save (SKILL.md → Page Structure Pattern).
4. **Save through the root, after `WaitForTasks()`, guarded by `IsSavable`.** `Save()` returns a new instance; the ViewModel reassigns it.
