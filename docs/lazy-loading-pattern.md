# Lazy Loading Pattern for Neatoo

## Status: Planned (Not Yet Implemented)

This document describes the planned lazy loading pattern for child objects in Neatoo, designed to work with Blazor's synchronous binding model.

## Design Overview

### The Challenge

- C# properties cannot have `async` getters
- Blazor's `@bind:get` requires synchronous values
- Child objects may need to be loaded on-demand from a database or API

### The Solution

Use a synchronous getter that returns `null`/default until loaded, combined with:
- `IsLoaded` property on `IProperty` to check load state
- Async `LoadAsync()` method to trigger loading
- `IsBusy` tracking integration (already exists in Neatoo)
- `StateHasChanged()` called when loading completes

## Proposed API

### IProperty Extensions

```csharp
public interface IProperty
{
    // Existing
    object? Value { get; }
    bool IsBusy { get; }

    // New for lazy loading
    bool IsLoaded { get; }
    Task LoadAsync();
    Task<T> GetValueAsync<T>();
}
```

### Property Getter Behavior

```csharp
// In domain object
public ChildObject? Child
{
    get => Getter<ChildObject>();
    set => Setter(value);
}

// Getter returns null if not loaded
// Access triggers load automatically (fire-and-forget)
// Or explicitly load via PropertyManager
```

### Explicit Loading

```csharp
// Load a specific property
await myObject.PropertyManager["Child"].LoadAsync();

// Load and get value
var child = await myObject.PropertyManager["Child"].GetValueAsync<ChildObject>();

// Check if loaded
if (myObject.PropertyManager["Child"].IsLoaded)
{
    // Safe to access synchronously
}
```

## Blazor Usage Patterns

### Pattern 1: Conditional Rendering

```razor
@if (myObject.PropertyManager["Child"].IsLoaded)
{
    <MudTextField @bind-Value="myObject.Child.Name" />
}
else
{
    <MudProgressCircular Indeterminate="true" Size="Size.Small" />
}
```

### Pattern 2: Pre-load in Lifecycle

```csharp
protected override async Task OnParametersSetAsync()
{
    await myObject.PropertyManager["Child"].LoadAsync();
}
```

### Pattern 3: Disabled Until Loaded

```razor
<MudTextField Value="@(myObject.Child?.Name ?? "")"
              ValueChanged="@(v => { if (myObject.Child != null) myObject.Child.Name = v; })"
              Disabled="@(!myObject.PropertyManager["Child"].IsLoaded)" />
```

### Pattern 4: Load on Expand (Accordion/TreeView)

```razor
<MudExpansionPanel @bind-Expanded="expanded">
    <TitleContent>Order Details</TitleContent>
    <ChildContent>
        @if (expanded && !order.PropertyManager["LineItems"].IsLoaded)
        {
            <MudProgressLinear Indeterminate="true" />
            @{ _ = LoadLineItemsAsync(); }
        }
        else
        {
            <OrderLineItemsGrid Items="@order.LineItems" />
        }
    </ChildContent>
</MudExpansionPanel>

@code {
    private bool expanded;

    private async Task LoadLineItemsAsync()
    {
        await order.PropertyManager["LineItems"].LoadAsync();
        StateHasChanged();
    }
}
```

## Factory Integration

### [Load] Attribute for Lazy Load Methods

```csharp
public class Order : EntityBase<Order>
{
    public OrderLineItemList? LineItems
    {
        get => Getter<OrderLineItemList>();
        set => Setter(value);
    }

    [Load(nameof(LineItems))]
    public async Task LoadLineItems(
        [Service] IOrderDbContext context,
        [Service] IOrderLineItemListFactory factory)
    {
        var entities = await context.OrderLineItems
            .Where(li => li.OrderId == this.Id)
            .ToListAsync();

        LineItems = factory.Fetch(entities);
    }
}
```

## Integration with Existing Infrastructure

### IsBusy Tracking

The existing `IsBusy` property will reflect loading state:

```csharp
// Already exists in Base.cs
public bool IsBusy => this.RunningTasks.IsRunning || this.PropertyManager.IsBusy;
```

Loading operations will be tracked through `AsyncTasks`, so:
- Parent objects show `IsBusy = true` while children load
- `WaitForTasks()` awaits all pending loads
- UI can show loading indicators based on `IsBusy`

### Parent-Child Propagation

When a lazy property loads, the existing event system will:
- Set the parent reference via `ISetParent`
- Propagate `NeatooPropertyChanged` events
- Update validation state if applicable

## Implementation Phases

### Phase 1: Core Infrastructure
- [ ] Add `IsLoaded` to `IProperty`
- [ ] Add `LoadAsync()` to `IProperty`
- [ ] Create `[Load]` attribute
- [ ] Wire load methods through factory system

### Phase 2: PropertyManager Integration
- [ ] Track load state per property
- [ ] Coordinate with pause/resume
- [ ] Handle load failures gracefully

### Phase 3: Blazor Components
- [ ] Update MudNeatoo components for lazy property support
- [ ] Add loading state indicators
- [ ] Document component usage patterns

### Phase 4: Collection Support
- [ ] Lazy loading for `EntityListBase` children
- [ ] Pagination support for large collections

## Files That Will Need Changes

- `src/Neatoo/IProperty.cs` - Add lazy loading interface members
- `src/Neatoo/Internal/Property.cs` - Implement lazy loading
- `src/Neatoo/Internal/PropertyManager.cs` - Coordinate loading
- `src/Neatoo/Base.cs` - Optional auto-load trigger in Getter
- `src/Neatoo.RemoteFactory/` - Add [Load] attribute support
- `src/Neatoo.Blazor.MudNeatoo/` - Component updates for loading states
