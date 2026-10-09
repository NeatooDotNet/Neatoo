# ViewModels Over a Live Aggregate

Derived values live in the aggregate, as rules. A ViewModel that owns an aggregate the user edits through MudNeatoo does not compute anything from it. It holds the aggregate, forwards gestures to entity verbs, and — when a binding observes the ViewModel rather than the entity — re-raises change notification for values it only reads.

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

```csharp
// RIGHT: rules on the entity, private-set partial properties.
public Plan(IEntityBaseServices<Plan> services) : base(services)
{
    RuleManager.AddAction(
        t => t.IsApproved = t.ApprovedById.HasValue,
        t => t.ApprovedById);

    RuleManager.AddAction(
        t => t.ApprovedDurationMinutes = (int)Math.Round(t.ApprovedDuration / 60.0),
        t => t.ApprovedDuration);

    RuleManager.AddAction(
        t => t.ProgressPercent = t.ApprovedTreatments > 0
            ? (double)t.CompletedTreatmentCount / t.ApprovedTreatments * 100 : 0,
        t => t.ApprovedTreatments, t => t.CompletedTreatmentCount);

    RuleManager.AddAction(
        t => t.CanExtend = t.IsApproved && t.Active && !t.EndedEarly && !t.HasBeenExtended,
        t => t.IsApproved, t => t.Active, t => t.EndedEarly, t => t.HasBeenExtended);
}

public partial bool IsApproved { get; private set; }
public partial int ApprovedDurationMinutes { get; private set; }
public partial double ProgressPercent { get; private set; }
public partial bool CanExtend { get; private set; }
```

The component binds to the entity for these values, the same way it binds the editable fields:

```razor
<MudNeatooNumericField T="int"
                       EntityProperty="@VM.Plan[nameof(IPlan.ApprovedTreatments)]" />

<MudText>@VM.Plan.ApprovedDurationMinutes min</MudText>
<MudProgressLinear Value="@VM.Plan.ProgressPercent" />

<MudButton Disabled="@(!VM.Plan.CanExtend)" OnClick="@(() => VM.ExtendAsync(4))">Extend Plan</MudButton>
```

Nothing on the ViewModel is derived from the aggregate. There is no trigger map to maintain, because the rule declares its own triggers.

## What the ViewModel Does

It owns the aggregate reference, subscribes to it so the page re-renders, forwards gestures to entity verbs, and saves through the root. `Plan` here is an aggregate root (`IPlan : IEntityRoot`); a child entity is saved through its root, never by the ViewModel.

```csharp
public partial class PlanViewModel : ObservableObject, IDisposable
{
    private readonly IPlanFactory _factory;
    private IPlan _plan = default!;

    public IPlan Plan
    {
        get => _plan;
        private set
        {
            if (_plan != null)
                _plan.PropertyChanged -= OnPlanPropertyChanged;
            _plan = value;
            if (_plan != null)
                _plan.PropertyChanged += OnPlanPropertyChanged;
            OnPropertyChanged();
        }
    }

    // The VM derives nothing. It re-raises so a binding that observes the VM re-renders.
    private void OnPlanPropertyChanged(object? sender, PropertyChangedEventArgs e)
        => OnPropertyChanged(nameof(Plan));

    // A gesture: the verb sets state, rules validate, the VM saves through the root.
    public async Task ExtendAsync(int additionalTreatments)
    {
        _plan.Extend(additionalTreatments);
        await _plan.WaitForTasks();
        if (!_plan.IsSavable) return;   // the button is already disabled by CanExtend / IsSavable
        Plan = (IPlan)await _plan.Save(); // Save returns a new instance; the setter resubscribes
    }

    public void Dispose()
    {
        if (_plan != null)
            _plan.PropertyChanged -= OnPlanPropertyChanged;
    }
}
```

### Key Points

1. **Derived values are entity rules.** If the ViewModel has a `=>` over entity values, move it into an `AddAction` on the entity and bind to the resulting `private set` property.
2. **The VM re-raises; it does not re-derive.** The only reason the VM subscribes is that Blazor does not observe `INotifyPropertyChanged` on its own. One handler that calls `OnPropertyChanged(nameof(Plan))` (or `StateHasChanged` on a page without a VM) is enough — the entity's own `PropertyChanged` has already fired for the derived property.
3. **Who subscribes.** A page with a ViewModel lets the VM own the subscription. A page without one subscribes to the entity itself (see SKILL.md → Page Structure Pattern). Not both.
4. **Save through the root, after `WaitForTasks()`, guarded by `IsSavable`.** `Save()` returns a new instance; assign it through the property so the setter resubscribes.
