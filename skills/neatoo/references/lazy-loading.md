# Lazy Loading

`LazyLoad<T>` provides explicit async lazy loading for child entities or related data within Neatoo domain objects. Loading is always explicit — accessing `Value` never triggers a load.

## Key Principles

- **Explicit loading only** — `Value` returns current state (null if not loaded). Use `await` or `LoadAsync()` to load.
- **Nullable reference types supported** — The generic constraint is `where T : class?`, so `T` can be a nullable reference type (e.g., `LazyLoad<IOrderItemList?>`).
- **Thread-safe** — Multiple concurrent awaits share a single load operation.
- **UI-friendly** — Implements `INotifyPropertyChanged` with `IsLoading`, `IsLoaded`, `HasLoadError`, and `LoadError` for binding.
- **Meta property delegation** — Implements `IValidateMetaProperties` and `IEntityMetaProperties`, delegating to the loaded value.
- **JSON serialization** — `Value` and `IsLoaded` are serialized; the loader delegate is not. The converter merges deserialized state into the existing constructor-created instance, preserving the loader.

## Creating Instances

Always use `ILazyLoadFactory` (registered in DI via `AddNeatooServices`):

```csharp
// Deferred loading — value loaded on first await
var lazy = lazyLoadFactory.Create<IChild>(async () => await childFactory.Fetch(parentId));

// Pre-loaded — IsLoaded is immediately true
var lazy = lazyLoadFactory.Create<IChild>(existingChild);
```

## The Correct Pattern: Constructor-Based LazyLoad

**Create `LazyLoad<T>` in the constructor.** The constructor runs on both server and client (during DI-based deserialization), so the loader delegate is always present. The Neatoo JSON converter merges deserialized state (`Value`, `IsLoaded`) into the constructor-created instance, preserving the loader.

The loader lambda captures factory dependencies from DI and references `this.Id` (or similar state). `this.Id` is resolved at load-time, not capture-time, so it works even though the constructor runs before `[Fetch]` sets the Id.

<!-- snippet: skill-lazyload-constructor-pattern -->
<a id='snippet-skill-lazyload-constructor-pattern'></a>
```cs
[Factory]
public partial class SkillLazyParent : EntityBase<SkillLazyParent>, ISkillLazyParent
{
    public SkillLazyParent(
        IEntityBaseServices<SkillLazyParent> services,
        ISkillLazyChildFactory childFactory,
        ILazyLoadFactory lazyLoadFactory) : base(services)
    {
        // Create LazyLoad in the constructor.
        // The loader lambda captures the factory from DI and references this.Id,
        // which is resolved at load-time (not capture-time).
        // This instance survives serialization because the converter merges
        // deserialized state into it instead of replacing it.
        LazyChild = lazyLoadFactory.Create<ISkillLazyChild>(async () =>
        {
            return await childFactory.Fetch(this.Id);
        });

        // AddActionAsync: when Trigger changes, await the lazy-loaded child
        RuleManager.AddActionAsync(async parent =>
        {
            if (parent.LazyChild != null)
            {
                var child = await parent.LazyChild;
                if (child != null)
                {
                    parent.LoadedData = child.Data;
                }
            }
        }, p => p.Trigger);
    }

    public partial string Trigger { get; set; }
    public partial string LoadedData { get; set; }
    public partial Guid Id { get; set; }

    // LazyLoad property with private setter.
    // The setter calls SubscribeToLazyLoadProperties() so meta properties
    // (IsValid, IsModified, etc.) propagate from the loaded child.
    private LazyLoad<ISkillLazyChild>? _lazyChild;
    public LazyLoad<ISkillLazyChild>? LazyChild
    {
        get => _lazyChild;
        private set
        {
            _lazyChild = value;
            SubscribeToLazyLoadProperties();
        }
    }

    [Remote]
    [Fetch]
    internal Task Fetch(Guid id)
    {
        using (PauseAllActions())
        {
            this["Id"].LoadValue(id);
        }
        // LazyChild already created in the constructor with a loader
        // that uses this.Id — no need to create it here.
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/samples/LazyLoadSamples.cs#L52-L116' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-lazyload-constructor-pattern' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Why This Works with Serialization

1. **Server**: Constructor runs → creates `LazyLoad` with loader. `[Fetch]` runs → sets `Id`.
2. **Serialization**: `LazyLoad` written to JSON (`Value`, `IsLoaded`). Loader delegate is `[JsonIgnore]`.
3. **Client deserialization**: Constructor runs again via DI → creates **new** `LazyLoad` with loader (factory injected from client DI).
4. **Converter merges**: `NeatooBaseJsonTypeConverter` finds the existing `LazyLoad` instance and merges deserialized state into it via `ILazyLoadDeserializable.ApplyDeserializedState` — the loader is preserved.
5. **Usage**: `AddActionAsync` triggers → awaits `LazyChild` → loader executes with correct `this.Id` → child loaded via `[Remote]` call.

### LazyLoad Property Declaration

Declare with a private setter and call `SubscribeToLazyLoadProperties()` so meta properties (`IsValid`, `IsModified`, etc.) propagate from the loaded child:

```csharp
private LazyLoad<IChild>? _lazyChild;
public LazyLoad<IChild>? LazyChild
{
    get => _lazyChild;
    private set
    {
        _lazyChild = value;
        SubscribeToLazyLoadProperties();
    }
}
```

## Anti-Patterns

- **Do NOT create `LazyLoad<T>` in `[Fetch]` or `[Create]`** — these only run server-side. The loader delegate is `[JsonIgnore]` and lost during serialization. Always create in the constructor.
- **Do NOT use `OnDeserialized`/`InitializeLazyLoaders`/`ReinitializeLazyLoaders`** — unnecessary complexity. The converter preserves constructor-created instances. Move LazyLoad creation to the constructor instead.

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

## When to Use vs. Eager Loading

**Eager loading in the parent's `[Fetch]` method is preferred** for most cases — it keeps data access visible and avoids N+1 query problems.

Use `LazyLoad<T>` when:
- The child data is large and not always needed
- Loading the child data is expensive (separate API call, complex query)
- The UI can progressively reveal data as it loads

Do **not** use `LazyLoad<T>` when:
- The child data is always needed immediately
- Loading a small, cheap collection (eager load in `[Fetch]` instead)

## Related

- [Properties](properties.md) — Partial property system and change tracking
- [Entities](entities.md) — EntityBase lifecycle and Save routing
- [Collections](collections.md) — EntityListBase and ValidateListBase
