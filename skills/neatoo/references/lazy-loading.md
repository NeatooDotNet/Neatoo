# Lazy Loading

`LazyLoad<T>` provides explicit async lazy loading for child entities or related data within Neatoo domain objects. Loading is always explicit — accessing `Value` never triggers a load.

## Key Principles

- **Explicit loading only** — `Value` returns current state (null if not loaded). Use `await` or `LoadAsync()` to load.
- **Nullable reference types supported** — The generic constraint is `where T : class?`, so `T` can be a nullable reference type (e.g., `LazyLoad<IOrderItemList?>`). This is useful when the property type in the entity interface is nullable.
- **Thread-safe** — Multiple concurrent awaits share a single load operation.
- **UI-friendly** — Implements `INotifyPropertyChanged` with `IsLoading`, `IsLoaded`, `HasLoadError`, and `LoadError` for binding.
- **Meta property delegation** — Implements `IValidateMetaProperties` and `IEntityMetaProperties`, delegating to the loaded value.
- **JSON serialization** — `Value` and `IsLoaded` are serialized; the loader delegate is not.

## Creating Instances

Always use `ILazyLoadFactory` (registered in DI via `AddNeatooServices`). Inject it with `[Service]`:

```csharp
// Deferred loading — value loaded on first await
var lazy = lazyLoadFactory.Create<OrderLines>(async () => await fetchOrderLines(orderId));

// Pre-loaded — IsLoaded is immediately true
var lazy = lazyLoadFactory.Create<OrderLines>(existingLines);
```

## Loading

```csharp
// Await syntax (uses GetAwaiter)
var value = await lazy;

// Explicit method call
var value = await lazy.LoadAsync();
```

Loading is idempotent — once loaded, subsequent calls return the cached value without invoking the loader again. Concurrent calls during the first load share the same task.

## State Properties

| Property | Type | Description |
|----------|------|-------------|
| `Value` | `T?` | Current value. `null` if not yet loaded. Never triggers a load. |
| `IsLoaded` | `bool` | Whether the value has been loaded. |
| `IsLoading` | `bool` | Whether a load operation is in progress. |
| `HasLoadError` | `bool` | Whether the last load attempt failed. |
| `LoadError` | `string?` | Error message from the last failed load, or `null`. |

## Meta Property Delegation

`LazyLoad<T>` delegates meta properties to the loaded value when present:

| Property | Before Load | After Load |
|----------|-------------|------------|
| `IsBusy` | `true` if loading | Delegates to value's `IsBusy` |
| `IsValid` | `true` (unless load error) | Delegates to value's `IsValid` |
| `IsSelfValid` | `!HasLoadError` | `!HasLoadError` |
| `IsModified` | `false` | Delegates to value's `IsModified` |
| `IsSelfModified` | Always `false` | Always `false` (wrapper itself is never modified) |
| `IsNew` | `false` | Delegates to value's `IsNew` |
| `IsDeleted` | `false` | Delegates to value's `IsDeleted` |
| `IsSavable` | `false` | Delegates to value's `IsSavable` |

## Error Handling

If the loader throws, the exception propagates to the caller. The error state is captured:

```csharp
try
{
    var value = await lazy.LoadAsync();
}
catch (Exception)
{
    // lazy.HasLoadError == true
    // lazy.LoadError contains the exception message
    // lazy.IsLoaded == false (can retry)
    // lazy.IsValid == false (due to error)
}
```

## UI Binding (Blazor / WPF)

`LazyLoad<T>` implements `INotifyPropertyChanged` and fires change events for `Value`, `IsLoaded`, and `IsLoading` during the load lifecycle. Bind directly:

```razor
@if (Model.OrderLines.IsLoading)
{
    <LoadingSpinner />
}
else if (Model.OrderLines.HasLoadError)
{
    <ErrorDisplay Message="@Model.OrderLines.LoadError" />
}
else if (Model.OrderLines.IsLoaded)
{
    <OrderLinesList Items="@Model.OrderLines.Value" />
}
```

## Usage Within Domain Objects

Declare as a regular property on your entity or validate object:

```csharp
[Factory]
public partial class Order : EntityBase<Order>
{
    public Order(IEntityBaseServices<Order> services) : base(services) { }

    public partial string CustomerName { get; set; }
    public LazyLoad<OrderLineList> OrderLines { get; private set; }

    [Create]
    public void Create([Service] ILazyLoadFactory lazyLoadFactory)
    {
        this.OrderLines = lazyLoadFactory.Create<OrderLineList>(
            async () => await orderLineFactory.Fetch(/* ... */));
    }

    [Fetch]
    public void Fetch(int id,
        [Service] ILazyLoadFactory lazyLoadFactory,
        [Service] IOrderLineListFactory orderLineFactory)
    {
        // ... load order data ...
        this.OrderLines = lazyLoadFactory.Create<OrderLineList>(
            async () => await orderLineFactory.Fetch(id));
    }
}
```

## When to Use vs. Eager Loading

The Design reference (`src/Design/Design.Domain/FactoryOperations/FetchPatterns.cs`) notes that **eager loading in the parent's `[Fetch]` method is preferred** for most cases — it keeps data access visible and avoids N+1 query problems.

Use `LazyLoad<T>` when:
- The child data is large and not always needed
- Loading the child data is expensive (separate API call, complex query)
- The UI can progressively reveal data as it loads

Do **not** use `LazyLoad<T>` when:
- The child data is always needed immediately
- You're loading a small, cheap collection (eager load in `[Fetch]` instead)

## Related

- [Properties](properties.md) — Partial property system and change tracking
- [Entities](entities.md) — EntityBase lifecycle and Save routing
- [Collections](collections.md) — EntityListBase and ValidateListBase
