# M3: Document PropertyChanged vs NeatooPropertyChanged

**Priority:** Medium
**Category:** Documentation
**Effort:** Low
**Status:** Not Started

---

## Problem Statement

Neatoo has two property changed event systems:

1. `PropertyChanged` - Standard `INotifyPropertyChanged` implementation
2. `NeatooPropertyChanged` - Async-aware custom event with richer args

Developers are confused about:
- When to use each
- How they interact
- Why both exist
- Which to subscribe to in different scenarios

---

## Current State

```csharp
// Standard .NET event
public event PropertyChangedEventHandler? PropertyChanged;

// Neatoo-specific async event
public event NeatooPropertyChanged? NeatooPropertyChanged;

// Event args for NeatooPropertyChanged
public class NeatooPropertyChangedEventArgs : PropertyChangedEventArgs
{
    public IBase Source { get; }
    public NeatooPropertyChangedEventArgs? OriginalEventArgs { get; }
    // ... additional properties
}
```

---

## Documentation to Create

### When to Use Each Event

| Scenario | Use | Reason |
|----------|-----|--------|
| Blazor component binding | `PropertyChanged` | Standard data binding support |
| WPF/MAUI binding | `PropertyChanged` | Standard data binding support |
| Parent tracking child changes | `NeatooPropertyChanged` | Provides Source reference |
| Async event handling | `NeatooPropertyChanged` | Handlers can be async |
| Meta-property updates | Either | Both are raised |
| Custom property tracking | `NeatooPropertyChanged` | Richer event args |

### How They Work Together

```
Property Value Changes
         ↓
   RaisePropertyChanged()
         ↓
   ┌─────┴─────┐
   ↓           ↓
PropertyChanged  NeatooPropertyChanged
   ↓           ↓
UI Binding    Parent notification
              Async processing
```

### Key Differences

| Feature | PropertyChanged | NeatooPropertyChanged |
|---------|-----------------|----------------------|
| .NET Standard | Yes | No |
| Event Args | `PropertyChangedEventArgs` | `NeatooPropertyChangedEventArgs` |
| Source tracking | No | Yes (`Source` property) |
| Bubbling support | No | Yes (`OriginalEventArgs`) |
| Async handlers | No | Yes (delegate returns Task) |
| UI binding | Yes | Not directly |

---

## Example Documentation

### Subscribing to PropertyChanged (UI Binding)

```csharp
// Blazor component - automatic via @bind
<MudTextField @bind-Value="@person.Name" />

// Manual subscription (rarely needed)
person.PropertyChanged += (sender, e) =>
{
    if (e.PropertyName == nameof(person.Name))
    {
        // Handle synchronously
    }
};
```

### Subscribing to NeatooPropertyChanged (Async Processing)

```csharp
// In a parent entity tracking child changes
protected override async Task ChildNeatooPropertyChanged(NeatooPropertyChangedEventArgs e)
{
    // Can await async operations
    await LogChangeAsync(e.Source, e.PropertyName);

    // Bubble up to parent
    await base.ChildNeatooPropertyChanged(e);
}
```

### Custom Change Tracking

```csharp
public class ChangeTracker
{
    private readonly List<ChangeRecord> _changes = new();

    public void Track(IBase target)
    {
        target.NeatooPropertyChanged += async (args) =>
        {
            _changes.Add(new ChangeRecord
            {
                Source = args.Source,
                Property = args.PropertyName,
                Timestamp = DateTime.UtcNow
            });
            return Task.CompletedTask;
        };
    }
}
```

---

## Implementation Tasks

- [ ] Create `docs/property-changed-events.md`
- [ ] Add comparison table
- [ ] Add usage examples for each scenario
- [ ] Add diagrams showing event flow
- [ ] Link from API reference
- [ ] Add FAQ section

---

## FAQ to Include

**Q: Why not just use INotifyPropertyChanged?**

A: `PropertyChanged` is synchronous and doesn't track the source when events bubble through parent-child relationships. `NeatooPropertyChanged` provides async support and source tracking needed for aggregate state management.

**Q: Do I need to subscribe to both?**

A: Usually no. UI binding uses `PropertyChanged` automatically. Framework internals use `NeatooPropertyChanged`. You only need to manually subscribe for custom scenarios.

**Q: Does raising one automatically raise the other?**

A: Yes. When `RaisePropertyChanged` is called, both events are raised.

---

## Files to Create

| File | Description |
|------|-------------|
| `docs/property-changed-events.md` | Main documentation |
