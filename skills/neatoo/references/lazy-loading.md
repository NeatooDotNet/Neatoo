# Lazy Loading

`LazyLoad<T>` provides async lazy loading for child entities or related data within Neatoo domain objects. `Value` is a passive read that returns the current value or `null` if not yet loaded. Call `LoadAsync()` explicitly to trigger loading. `IsLoading`, `IsLoaded`, `HasLoadError`, and `LoadError` are side-effect-free state checks.

## Key Principles

- **Value is a passive read** — Accessing `Value` returns the current value or `null` if not yet loaded. It never triggers a load. Call `LoadAsync()` to trigger loading explicitly.
- **Nullable reference types supported** — The generic constraint is `where T : class?`, so `T` can be a nullable reference type (e.g., `LazyLoad<IOrderItemList?>`).
- **Thread-safe** — Multiple concurrent awaits share a single load operation.
- **UI-friendly** — Implements `INotifyPropertyChanged` with `IsLoading`, `IsLoaded`, `HasLoadError`, and `LoadError` for binding.
- **Meta property delegation** — Implements `IValidateMetaProperties` and `IEntityMetaProperties`, delegating to the loaded value.
- **JSON serialization** — `Value` and `IsLoaded` are serialized; the loader delegate is not. The converter merges deserialized state into the existing constructor-created instance, preserving the loader.

## Creating Instances

Always use `ILazyLoadFactory` (registered in DI via `AddNeatooServices`):

```csharp
// Deferred loading — value loaded via explicit LoadAsync() call
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
            var child = await parent.LazyChild.LoadAsync();
            if (child != null)
            {
                parent.LoadedData = child.Data;
            }
        }, p => p.Trigger);
    }

    public partial string Trigger { get; set; }
    public partial string LoadedData { get; set; }
    public partial Guid Id { get; set; }

    // LazyLoad property -- partial, just like every other Neatoo property.
    // The generator handles backing field, setter (LoadValue), and registration.
    // Meta properties (IsValid, IsModified, etc.) propagate from the loaded child.
    public partial LazyLoad<ISkillLazyChild> LazyChild { get; set; }

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
<sup><a href='/src/samples/LazyLoadSamples.cs#L52-L104' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-lazyload-constructor-pattern' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Why This Works with Serialization

1. **Server**: Constructor runs → creates `LazyLoad` with loader. `[Fetch]` runs → sets `Id`.
2. **Serialization**: `LazyLoad` written to JSON (`Value`, `IsLoaded`). Loader delegate is `[JsonIgnore]`.
3. **Client deserialization**: Constructor runs again via DI → creates **new** `LazyLoad` with loader (factory injected from client DI).
4. **Converter merges**: `NeatooBaseJsonTypeConverter` finds the existing `LazyLoad` instance and merges deserialized state into it via `ILazyLoadDeserializable.ApplyDeserializedState` — the loader is preserved.
5. **Usage**: `AddActionAsync` triggers → calls `LazyChild.LoadAsync()` → loader executes with correct `this.Id` → child loaded via `[Remote]` call.

### LazyLoad Property Declaration

Declare as a `partial` property — just like every other Neatoo property. The source generator handles the backing field, setter (using `LoadValue`), and registration (using `factory.CreateLazyLoad<TInner>()`). Meta properties (`IsValid`, `IsModified`, `IsBusy`, etc.) automatically propagate from the loaded child through look-through property subclasses in PropertyManager.

```csharp
// That's it. No manual backing field, no SubscribeToLazyLoadProperties().
public partial LazyLoad<IChild> LazyChild { get; set; }
```

The generator produces a `LazyLoadValidateProperty<IChild>` (or `LazyLoadEntityProperty<IChild>` for EntityBase) backing field that sees through to the inner entity for all framework operations.

## SetValue — Direct Value Assignment

`LazyLoad<T>.SetValue(T?)` assigns a value directly, bypassing the loader delegate. This marks the instance as loaded, clears any load error, and fires `PropertyChanged`. Use this in `[Create]` methods to pre-load with an empty or default value:

```csharp
[Create]
public void Create([Service] IPersonPhoneList emptyPhoneList)
{
    // Pre-load with empty list — IsLoaded becomes true immediately
    PersonPhoneList.SetValue(emptyPhoneList);
}
```

`SetValue` manages child event subscriptions (unsubscribes from old value, subscribes to new). The loaded child integrates into PropertyManager's parent-child tracking automatically.

## Anti-Patterns

- **Do NOT create `LazyLoad<T>` in `[Fetch]` or `[Create]`** — these only run server-side. The loader delegate is `[JsonIgnore]` and lost during serialization. Always create in the constructor.
- **Do NOT use `OnDeserialized`/`InitializeLazyLoaders`/`ReinitializeLazyLoaders`** — unnecessary complexity. The converter preserves constructor-created instances. Move LazyLoad creation to the constructor instead.
- **Do NOT use manual backing fields or `SubscribeToLazyLoadProperties()`** — this is the old pattern. Declare as `partial` and let the generator handle registration and meta property propagation.

## Loading

```csharp
// Explicit method call — the only way to trigger a load
var value = await lazy.LoadAsync();
```

Loading is idempotent — once loaded, subsequent calls return the cached value without invoking the loader again. Concurrent calls during the first load share the same task.

`.Value` is a passive read — it returns the current value (or `null` if not yet loaded) with no side effects. Use `LoadAsync()` in imperative code (domain logic, tests, `OnInitializedAsync`). Use `.Value` for UI binding after the load has been triggered.

### WaitForTasks Integration

`ValidateBase.WaitForTasks()` awaits in-progress LazyLoad children. This means `await entity.WaitForTasks()` before Save ensures any explicitly triggered loads have completed:

```csharp
// Explicitly trigger the load (fire-and-forget style)
_ = entity.OrderLines.LoadAsync();

// WaitForTasks awaits the in-progress LazyLoad load
await entity.WaitForTasks();

// Now entity.OrderLines.Value is populated
```

`WaitForTasks()` does NOT trigger loads on LazyLoad children. Only explicit `LoadAsync()` calls trigger loading.

## State Properties

| Property | Type | Description |
|----------|------|-------------|
| `Value` | `T?` | Current value. `null` if not yet loaded. Passive read with no side effects — never triggers a load. |
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

If the loader throws, the exception propagates to the `LoadAsync()` caller. Error state is also captured on the `LazyLoad<T>` instance:

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

If the load was triggered fire-and-forget (`_ = lazy.LoadAsync()`) and the parent calls `WaitForTasks()` while the load is still in progress, a load failure exception propagates through `WaitForTasks()`.

## UI Binding (Blazor / WPF)

`LazyLoad<T>` implements `INotifyPropertyChanged` and fires change events for `Value`, `IsLoaded`, and `IsLoading` during the load lifecycle. Trigger the load explicitly in `OnInitializedAsync()`, then bind to `.Value` and state properties in Razor markup. Blazor re-renders when `PropertyChanged` fires on load completion.

**Trigger the load in `OnInitializedAsync()`:**

```csharp
protected override async Task OnInitializedAsync()
{
    entity = await entityFactory.Fetch(id);
    // Explicitly trigger the lazy load — does not block rendering
    _ = entity.OrderLines.LoadAsync();
}
```

**Bind to `.Value` and state properties in Razor:**

```razor
@if (Model.OrderLines.HasLoadError)
{
    <ErrorDisplay Message="@Model.OrderLines.LoadError" />
}
else if (Model.OrderLines.Value is { } orderLines)
{
    <OrderLinesList Items="@orderLines" />
}
else if (Model.OrderLines.IsLoaded)
{
    <MudAlert Severity="Severity.Warning">No data available</MudAlert>
}
else
{
    <LoadingSpinner />
}
```

The 4-branch pattern handles all states: error, loaded with data, loaded with null, and loading. `.Value` is a passive read here — the load was already triggered in `OnInitializedAsync()`.

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
