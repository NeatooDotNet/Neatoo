# Event System Internals

Neatoo uses two distinct event systems for property changes. Understanding when and why each fires is essential for advanced scenarios like custom change tracking, parent-child coordination, and debugging unexpected behavior.

## The Two Event Types

| Event | Purpose | Sync/Async | Standard .NET |
|-------|---------|------------|---------------|
| `PropertyChanged` | UI data binding | Synchronous | Yes (`INotifyPropertyChanged`) |
| `NeatooPropertyChanged` | Internal Neatoo infrastructure | Asynchronous | No |

### Why Two Events?

**PropertyChanged** must be synchronous because UI frameworks (Blazor, WPF) expect `INotifyPropertyChanged` handlers to complete immediately. The binding system doesn't await handlers.

**NeatooPropertyChanged** must be asynchronous because it triggers business rules, which can be async (database lookups, API calls). It also bubbles through the parent-child hierarchy, which requires awaiting child completion.

## What Each Event Does

### PropertyChanged (UI Binding)

Fires for any property change:
- `Value` changes on properties
- Meta-property changes (`IsBusy`, `IsValid`, `IsModified`, etc.)
- Any other bindable property

```csharp
// Blazor/WPF binding automatically subscribes
<MudTextField @bind-Value="person.Name" />

// Manual subscription (rarely needed)
person.PropertyChanged += (sender, e) =>
{
    Console.WriteLine($"Property changed: {e.PropertyName}");
};
```

### NeatooPropertyChanged (Internal Logic)

Fires when a managed property's `Value` changes. Triggers:

1. **SetParent** - Establishes parent-child relationships for child objects
2. **Rules** - Runs validation rules for the changed property
3. **Bubbling** - Propagates change notification up the parent hierarchy
4. **UI Translation** - Fires `PropertyChanged` for the property name on the entity

```csharp
// Subscribe for custom async processing
person.NeatooPropertyChanged += async (args) =>
{
    await LogChangeAsync(args.FullPropertyName, args.Source);
};
```

## Event Flow

When a property value changes (e.g., `person.Name = "Alice"`):

```
Property.Value = "Alice"
         │
         ▼
┌────────────────────────────────────────────────────────────────┐
│ ValidateProperty.HandleNonNullValue()                          │
│                                                                │
│  1. PropertyChanged("Value")  ──► UI: property value changed   │
│  2. NeatooPropertyChanged     ──► Internal: trigger handlers   │
└────────────────────────────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────────────────┐
│ ValidateBase._PropertyManager_NeatooPropertyChanged()          │
│                                                                │
│  1. SetParent(this) on child   ──► Structural (always)         │
│  2. PropertyChanged("Name")    ──► UI: entity property changed │
│  3. ChildNeatooPropertyChanged ──► Rules + bubbling            │
└────────────────────────────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────────────────┐
│ ValidateBase.ChildNeatooPropertyChanged()                      │
│                                                                │
│  if (!IsPaused)                                                │
│  {                                                             │
│      RunRules("Name")          ──► Behavioral                  │
│      RaiseNeatooPropertyChanged ──► Bubble to parent           │
│      CheckIfMetaPropertiesChanged                              │
│  }                                                             │
│  else                                                          │
│  {                                                             │
│      ResetMetaState()          ──► Skip rules during factory   │
│  }                                                             │
└────────────────────────────────────────────────────────────────┘
```

## PropertyChanged Translation

The property fires `PropertyChanged("Value")` (the property's own value changed), but UI binds to `entity.Name` (not `entity.NameProperty.Value`).

ValidateBase translates this by firing `PropertyChanged("Name")` on the entity when it receives `NeatooPropertyChanged`. This tells the UI "the Name property on this entity changed."

```csharp
// In ValidateBase._PropertyManager_NeatooPropertyChanged:

// The property fires PropertyChanged("Value"), but UI binds to entity.Name (not entity.NameProperty.Value).
// Translate property-level event to entity-level event for UI binding.
// This goes to immediate outside listeners only, not up the Neatoo parent tree.
this.RaisePropertyChanged(eventArgs.FullPropertyName);
```

## NeatooPropertyChangedEventArgs

The async event provides richer information than standard `PropertyChangedEventArgs`:

```csharp
public record NeatooPropertyChangedEventArgs
{
    public string PropertyName { get; }           // "Name"
    public IValidateProperty? Property { get; }   // The property that changed
    public object? Source { get; }                // The object that owns the property
    public string FullPropertyName { get; }       // "Address.City" for nested
    public NeatooPropertyChangedEventArgs? InnerEventArgs { get; }  // For bubbling
    public NeatooPropertyChangedEventArgs OriginalEventArgs { get; } // Root of chain
    public ChangeReason Reason { get; }           // UserEdit or Load
}
```

### ChangeReason

The `Reason` property distinguishes between user edits and data loading:

| Value | Description | Rule Execution |
|-------|-------------|----------------|
| `UserEdit` | Normal setter assignment | Yes - runs rules |
| `Load` | `LoadValue()` call | No - structural only |

```csharp
protected override async Task ChildNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
{
    // Skip custom processing for data loads
    if (eventArgs.Reason == ChangeReason.Load)
    {
        await base.ChildNeatooPropertyChanged(eventArgs);
        return;
    }

    // Only react to user edits
    if (eventArgs.FullPropertyName.StartsWith("OrderLines"))
    {
        RecalculateTotal();
    }

    await base.ChildNeatooPropertyChanged(eventArgs);
}
```

This enables handlers to distinguish between a user changing a value (which should trigger business logic) and the system loading data (which should not).

### Nested Property Names

When a child property changes, events bubble up with breadcrumbs:

```
person.Address.City = "Seattle"

At Address level:  PropertyName = "City",    FullPropertyName = "City"
At Person level:   PropertyName = "Address", FullPropertyName = "Address.City"
```

## IsPaused and Factory Operations

During factory operations (Create, Fetch), `IsPaused = true` prevents:
- Rules from running (object not fully initialized)
- Events from bubbling up (prevents n! algorithm when building object trees)

After factory completes, `IsPaused = false` and rules run normally.

## When to Subscribe to Each

| Scenario | Use | Why |
|----------|-----|-----|
| Blazor/WPF data binding | `PropertyChanged` | Standard binding support |
| Trigger UI re-render | `PropertyChanged` | Synchronous, works with `StateHasChanged` |
| Custom async processing | `NeatooPropertyChanged` | Handlers can be async |
| Track which object changed | `NeatooPropertyChanged` | `Source` property identifies origin |
| Audit logging | `NeatooPropertyChanged` | Rich event args with full property path |
| Parent tracking child changes | `NeatooPropertyChanged` | Provides nested breadcrumbs |

## Common Patterns

### Re-render Blazor Component on Changes

```razor
@implements IDisposable

@code {
    private IPerson person = default!;

    protected override void OnInitialized()
    {
        person = PersonFactory.Create();
        person.PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        person.PropertyChanged -= OnPropertyChanged;
    }
}
```

### Async Change Tracking

```csharp
public class ChangeTracker
{
    public void Track(IValidateBase target)
    {
        target.NeatooPropertyChanged += async (args) =>
        {
            await _auditService.LogChangeAsync(
                args.Source,
                args.FullPropertyName,
                DateTime.UtcNow);
        };
    }
}
```

### Override Child Change Handling

```csharp
public partial class Order : EntityBase<Order>, IOrder
{
    protected override async Task ChildNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        // Custom logic when any child property changes
        if (eventArgs.FullPropertyName.StartsWith("OrderLines"))
        {
            RecalculateTotal();
        }

        await base.ChildNeatooPropertyChanged(eventArgs);
    }
}
```

## See Also

- [Property System](../property-system.md) - Property basics and meta-properties
- [Blazor Binding](../blazor-binding.md) - UI integration patterns
- [Validation and Rules](../validation-and-rules.md) - How rules are triggered by events
