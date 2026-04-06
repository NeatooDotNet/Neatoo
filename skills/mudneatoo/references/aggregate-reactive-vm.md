# Aggregate-Reactive Computed Properties on ViewModels

When a ViewModel owns a Neatoo aggregate that the user edits through MudNeatoo databinding, the VM often needs computed properties derived from the aggregate's state. These must stay in sync as the user edits.

## The Problem

MudNeatoo components bind directly to the aggregate via `EntityProperty`. When the user changes a value, MudNeatoo calls `SetValue()` on the aggregate — the VM is not involved. If the VM has computed properties derived from the aggregate (e.g., `ApprovedDurationMinutes` derived from `Plan.ApprovedDuration`), they go stale unless the VM knows the aggregate changed.

## The Pattern

The VM subscribes to the aggregate's `PropertyChanged` event. When a property that's an input to a VM computed property changes, the VM raises `OnPropertyChanged` for its own computed property.

```
User edits form field
  → MudNeatoo calls EntityProperty.SetValue()
    → Aggregate partial property changes, fires PropertyChanged
      → VM listener checks: is this property an input to a computed property?
        → Yes: VM raises OnPropertyChanged(nameof(ComputedProperty))
          → Blazor re-renders the computed value
```

## Which Aggregate Properties Fire PropertyChanged

**Only Neatoo `partial` properties fire `PropertyChanged`.** These go through `Getter<T>()`/`Setter()` internally and the source generator wires up change notification.

```csharp
// FIRES PropertyChanged — Neatoo partial property
public partial double ApprovedDuration { get; set; }
public partial long? ApprovedById { get; set; }
public partial int ApprovedTreatments { get; set; }
public partial bool Active { get; set; }

// DOES NOT fire PropertyChanged — plain C# expression-body
public bool IsApproved => ApprovedById.HasValue;
public bool CanEndEarly => !EndedEarly && Active && PreHasCompletedSigns;
public int CompletedTreatmentCount { get { ... } }
```

When mapping aggregate properties to VM computed property triggers, you must trace through expression-body properties to find the underlying `partial` properties that actually fire events.

Example: `IsApproved` doesn't fire, but it derives from `ApprovedById` which does. So the VM watches for `ApprovedById` changes to recompute anything that depends on `IsApproved`.

## Implementation

### ViewModel (CommunityToolkit.Mvvm)

```csharp
public partial class PlanViewModel : ObservableObject, IPanelViewModel, IDisposable
{
    private readonly IPlanFactory _factory;
    private readonly IVisitEventService _eventService;

    private IPlan? _plan;

    public IPlan? Plan
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

    // Computed from aggregate state
    public int ApprovedDurationMinutes =>
        _plan != null ? (int)Math.Round(_plan.ApprovedDuration / 60.0) : 0;

    public double ProgressPercent =>
        _plan is { ApprovedTreatments: > 0 }
            ? (double)_plan.CompletedTreatmentCount / _plan.ApprovedTreatments * 100
            : 0;

    public bool CanExtend => (_plan as IAcute)?.CanExtend ?? false;

    private void OnPlanPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPlan.ApprovedDuration))
            OnPropertyChanged(nameof(ApprovedDurationMinutes));

        if (e.PropertyName is nameof(IPlan.ApprovedTreatments))
        {
            OnPropertyChanged(nameof(ProgressPercent));
            OnPropertyChanged(nameof(CanExtend));
        }

        if (e.PropertyName is "ApprovedById" or "EndedEarly" or "Active"
            or "PreHasBeenExtended")
        {
            OnPropertyChanged(nameof(CanExtend));
        }
    }

    // After save, the factory returns a new aggregate instance — resubscribe
    public async Task ExtendAsync(int additionalTreatments)
    {
        if (_plan is not IAcute acute) return;
        acute.ExtendPlan(additionalTreatments);
        Plan = (IPlan)await _plan.Save(); // Setter unsubscribes old, subscribes new
        _eventService.Publish(new PlanSaved(_plan.Visit!.Id));
    }

    public void Dispose()
    {
        if (_plan != null)
            _plan.PropertyChanged -= OnPlanPropertyChanged;
    }
}
```

### Key Points

1. **Use the `Plan` property setter for subscribe/unsubscribe.** The setter detaches from the old instance and attaches to the new one. This handles initialization, save (which returns a new instance), and disposal.

2. **Map triggers to partial properties, not expression-body properties.** Trace through the aggregate to find what actually fires. `CanExtend` depends on `IsApproved` which depends on `ApprovedById`. Watch `ApprovedById`.

3. **After save, reassign the Plan property.** `Save()` returns a new aggregate instance. Assigning it through the property setter automatically resubscribes.

4. **Don't over-granularize.** If several VM properties share overlapping triggers, it's fine to raise all of them when any shared input changes. The cost of a few extra `OnPropertyChanged` calls is negligible compared to the complexity of precise tracking.

### Razor Panel (Thin Binding Layer)

The panel binds to both the aggregate (via MudNeatoo EntityProperty) and the VM's computed properties:

```razor
@* Aggregate binding — MudNeatoo handles value sync *@
<MudNeatooNumericField T="int"
                       EntityProperty="@VM.Plan[nameof(IPlan.ApprovedTreatments)]" />

@* VM computed property — plain Blazor binding *@
<MudText>@VM.ApprovedDurationMinutes min</MudText>
<MudProgressLinear Value="@VM.ProgressPercent" />

@* VM computed property drives UI state *@
@if (VM.CanExtend)
{
    <MudButton OnClick="OnExtendClicked">Extend Plan</MudButton>
}
```

The panel does NOT compute anything. It binds to EntityProperty for editable fields and to VM properties for derived display values and UI state.

## Anti-Patterns

```csharp
// WRONG: Computing derived values in Razor
<MudText>@((int)Math.Round(plan.ApprovedDuration / 60.0)) min</MudText>

// WRONG: Polling or manual refresh
private void RefreshComputedProperties() { /* called from everywhere */ }

// WRONG: Subscribing to PropertyChanged in the Razor component
// The VM owns the subscription — the panel is a thin binding layer

// WRONG: Watching expression-body properties that never fire
// plan.PropertyChanged won't fire for IsApproved — watch ApprovedById instead
```
