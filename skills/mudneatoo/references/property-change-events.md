# Property Change Events: PropertyChanged vs NeatooPropertyChanged

Neatoo raises two distinct property-change events. Choosing the right one depends on **what you're subscribing to**, not on what you want to observe.

## The Two Events

| Event | Interface | Shape | Bubbles child names? |
|-------|-----------|-------|----------------------|
| `PropertyChanged` | `INotifyPropertyChanged` | Synchronous, `PropertyChangedEventArgs` | No — only meta flags bubble (see below) |
| `NeatooPropertyChanged` | `INotifyNeatooPropertyChanged` | Async `Task`, `NeatooPropertyChangedEventArgs` with dotted `FullPropertyName` | Yes — full path like `"Items.Total"` |

### What `PropertyChanged` raises on an entity

- The entity's own directly-set properties (e.g. `FirstName`)
- The entity's own meta flags when they flip — including flips caused by descendant changes:
  - `IsValid`, `IsSelfValid`, `IsBusy` (from `ValidateBase`)
  - `IsModified`, `IsSelfModified` (from `EntityListBase`; `EntityBase` tracks its children the same way)

It does **not** raise `PropertyChanged("Total")` when `Child.Total` changes. The child's property name never appears on the parent's `PropertyChanged`.

### What `NeatooPropertyChanged` raises on an entity

Every change anywhere in the graph, with a dotted `FullPropertyName` like `"Items.Total"` or `"Address.City"`. Each level wraps the child event rather than flattening, so you can walk `InnerEventArgs` if you need the exact source.

## The Rule

Pick the event by looking at **what object you subscribe to**:

| Subscribing to... | Use | Reason |
|---|---|---|
| A leaf `IEntityProperty` (from `entity["Name"]`) | `PropertyChanged` | Property wrapper raises its own meta events (`PropertyMessages`, `IsValid`, `IsBusy`, `IsReadOnly`). Nothing below it to bubble. |
| An entity, caring only about its own fields + meta flags | `PropertyChanged` | Meta flags already bubble through this event. |
| An entity, caring about anything in the graph | `NeatooPropertyChanged` | The only event that carries property names from descendants. |

## How MudNeatoo Applies the Rule

**Input components** (`MudNeatooTextField`, `MudNeatooSelect`, etc.) subscribe to `this.EntityProperty.PropertyChanged`. The subscription target is the **property wrapper**, not the entity. Each component is scoped to a single leaf, so `PropertyChanged` is sufficient — there is nothing below the property to bubble from.

**`NeatooValidationSummary`** subscribes to **both** on the root entity:

```csharp
if (this.Entity is INotifyPropertyChanged notifyPropertyChanged)
    notifyPropertyChanged.PropertyChanged += this.OnPropertyChanged;
if (this.Entity is INotifyNeatooPropertyChanged neatooNotify)
    neatooNotify.NeatooPropertyChanged += this.OnNeatooPropertyChanged;
```

Because the summary displays `entity.PropertyMessages` across the whole aggregate, it needs notification whenever any descendant becomes invalid. That's what `NeatooPropertyChanged` provides.

## Guidance for Custom Container Components

If you're building a Blazor component that takes a root entity as a parameter and renders state that depends on descendants:

1. **Subscribe in `OnInitialized`, unsubscribe in `Dispose`.** Neither event is auto-managed by Blazor.
2. **Use `NeatooPropertyChanged` for graph-wide awareness.** Marshal `StateHasChanged` via `InvokeAsync` — the delegate is async and may run off the UI thread.
3. **Use `PropertyChanged` alone if you only care about root-level flags** like `IsSavable`, `IsValid`, `IsBusy` — the Page Structure Pattern in SKILL.md shows this case.

```csharp
@implements IDisposable

@code {
    [Parameter, EditorRequired] public IOrder Entity { get; set; } = default!;

    protected override void OnInitialized()
    {
        Entity.NeatooPropertyChanged += OnAnyChange;
    }

    private Task OnAnyChange(NeatooPropertyChangedEventArgs e)
    {
        return InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Entity.NeatooPropertyChanged -= OnAnyChange;
    }
}
```

## Common Misconception: Blazor Does Not Auto-Subscribe

Blazor does **not** observe `INotifyPropertyChanged` automatically. A binding like `<MudText>@entity.Total</MudText>` only re-renders when the containing component calls `StateHasChanged`. In practice that call happens because the form page is already subscribed to entity `PropertyChanged` for `IsSavable` (see the Page Structure Pattern in SKILL.md) — the display binding piggybacks on the form's re-render cycle. It is not automatic.

If you render a view-only page with no form subscription, you must subscribe to `PropertyChanged` (for root-level state) or `NeatooPropertyChanged` (for any descendant state) yourself.
