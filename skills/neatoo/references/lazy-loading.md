# Lazy Loading

`EntityLazyLoad<T>` provides async lazy loading for child entities or related data within Neatoo domain objects. It inherits core loading logic from `Neatoo.RemoteFactory.LazyLoad<T>` and adds Neatoo-specific meta-property interfaces (`IValidateMetaProperties`, `IEntityMetaProperties`). `Value` is a passive read that returns the current value or `null` if not yet loaded. Call `LoadAsync()` explicitly to trigger loading. `IsLoading`, `IsLoaded`, `HasLoadError`, and `LoadError` are side-effect-free state checks.

## Key Principles

- **Value is a passive read** — Accessing `Value` returns the current value or `null` if not yet loaded. It never triggers a load. Call `LoadAsync()` to trigger loading explicitly.
- **Nullable reference types supported** — The generic constraint is `where T : class?`, so `T` can be a nullable reference type (e.g., `EntityLazyLoad<IOrderItemList?>`).
- **Thread-safe** — Multiple concurrent awaits share a single load operation.
- **UI-friendly** — Implements `INotifyPropertyChanged` with `IsLoading`, `IsLoaded`, `HasLoadError`, and `LoadError` for binding.
- **Meta property delegation** — Implements `IValidateMetaProperties` and `IEntityMetaProperties`, delegating to the loaded value.
- **JSON serialization** — `Value` and `IsLoaded` are serialized; the loader delegate is not. The converter merges deserialized state into the existing constructor-created instance, preserving the loader.

## Creating Instances

Always use `IEntityLazyLoadFactory` (registered in DI via `AddNeatooServices`). It has two shapes: deferred, `lazyLoadFactory.Create<IChild>(async () => await childFactory.Fetch(this.Id))`, whose value arrives on the first `LoadAsync()`; and pre-loaded, `lazyLoadFactory.Create<IChild>(existingChild)`, whose `IsLoaded` is immediately true. The constructor pattern below uses the first; `SetValue` (further down) is the usual way to get the second.

## The Correct Pattern: Constructor-Based EntityLazyLoad

**Create `EntityLazyLoad<T>` in the constructor.** The constructor runs on every tier that builds the object — the client included, when the object is deserialized — so the loader delegate is always present. The loader delegate is not serialized; the Neatoo JSON converter merges deserialized state (`Value`, `IsLoaded`) into the constructor-created instance.

The loader lambda captures the child factory from DI and reads `this.Id` when it runs, not when it is created, so it works even though the constructor runs before `[Fetch]` sets the Id. Inside `[Fetch]`, assign the key directly; the object is paused.

<!-- snippet: skill-lazy-load-constructor -->
<a id='snippet-skill-lazy-load-constructor'></a>
```cs
/// <summary>
/// Demonstrates: EntityLazyLoad created in the constructor with a loader that
/// reads this.Id at load time.
/// </summary>
[Factory]
internal partial class LazyLoadParentDemo : EntityBase<LazyLoadParentDemo>, ILazyLoadParentDemo
{
    public partial Guid Id { get; set; }
    public partial string? Name { get; set; }

    // Partial, like every other Neatoo property. The generator creates a
    // look-through backing field, so the child's IsValid/IsModified/IsBusy
    // flow into this entity's once the child is loaded.
    public partial EntityLazyLoad<ILazyLoadChildDemo> Details { get; set; }

    public LazyLoadParentDemo(
        IEntityBaseServices<LazyLoadParentDemo> services,
        ILazyLoadChildDemoFactory childFactory,
        IEntityLazyLoadFactory lazyLoadFactory) : base(services)
    {
        // Created here, on every tier. this.Id is read when the loader runs.
        Details = lazyLoadFactory.Create<ILazyLoadChildDemo>(
            async () => await childFactory.Fetch(this.Id));
    }

    [Create]
    public void Create([Service] ILazyLoadChildDemoFactory childFactory)
    {
        Id = Guid.NewGuid();

        // A new parent's details exist from the start, so there is nothing to
        // load: SetValue bypasses the loader and marks the EntityLazyLoad loaded.
        Details.SetValue(childFactory.Create(Id));
    }

    [Remote]
    [Fetch]
    internal void Fetch(Guid id, [Service] ILazyLoadParentRepository repository)
    {
        Id = id;
        Name = repository.GetName(id);
        // Details keeps the loader from the constructor; nothing to do here
    }
}
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs#L215-L262' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-lazy-load-constructor' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The child's `[Fetch]` is the one child operation the client calls directly — the loader runs on the client — so it carries `[Remote]`. It is still `internal`: only the loader calls it, not application code.

<!-- snippet: skill-lazy-load-test -->
<a id='snippet-skill-lazy-load-test'></a>
```cs
[TestMethod]
public async Task Details_LoadOnFirstLoadAsync_UsingTheParentsId()
{
    var id = Guid.NewGuid();
    var parent = await _factory.Fetch(id);

    // Value is a passive read: nothing is loaded until asked
    Assert.IsFalse(parent.Details.IsLoaded);
    Assert.IsNull(parent.Details.Value);

    var details = await parent.Details.LoadAsync();

    Assert.IsNotNull(details);
    Assert.AreEqual(id, details.ParentId, "The loader read this.Id at load time, not in the constructor");
    Assert.IsTrue(parent.Details.IsLoaded);
    Assert.AreSame(details, parent.Details.Value);
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/LazyLoadTests.cs#L36-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-lazy-load-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Why This Works with Serialization

1. **Server**: Constructor runs → creates `EntityLazyLoad` with loader. `[Fetch]` runs → sets `Id`.
2. **Serialization**: `EntityLazyLoad` written to JSON (`Value`, `IsLoaded`). Loader delegate is `[JsonIgnore]`.
3. **Client deserialization**: Constructor runs again via DI → creates **new** `EntityLazyLoad` with loader (factory injected from client DI).
4. **Converter merges**: `NeatooBaseJsonTypeConverter` finds the existing `EntityLazyLoad` instance and merges deserialized state into it via `ILazyLoadDeserializable.ApplyDeserializedState` — the loader is preserved.
5. **Usage**: `LoadAsync()` → loader executes with the correct `this.Id` → child loaded through its `[Remote]` `[Fetch]`.

### EntityLazyLoad Property Declaration

Declare it as a `partial` property — `public partial EntityLazyLoad<IChild> LazyChild { get; set; }`, as in the snippet above — just like every other Neatoo property: no manual backing field, no subscription code. The source generator handles the backing field, setter (using `LoadValue`), and registration (using `factory.CreateEntityLazyLoad<TInner>()`). Meta properties (`IsValid`, `IsModified`, `IsBusy`, etc.) propagate from the loaded child through look-through property subclasses in PropertyManager, so editing the loaded child marks the parent modified.

The generator produces a `LazyLoadValidateProperty<IChild>` (or `LazyLoadEntityProperty<IChild>` for EntityBase) backing field that sees through to the inner entity for all framework operations.

## SetValue — Direct Value Assignment

`EntityLazyLoad<T>.SetValue(T?)` assigns a value directly, bypassing the loader delegate. This marks the instance as loaded, clears any load error, and fires `PropertyChanged`. Use it in `[Create]` to pre-load a new parent's child, which exists from the start and has nothing to load:

<!-- snippet: skill-lazy-load-set-value -->
<a id='snippet-skill-lazy-load-set-value'></a>
```cs
[Create]
public void Create([Service] ILazyLoadChildDemoFactory childFactory)
{
    Id = Guid.NewGuid();

    // A new parent's details exist from the start, so there is nothing to
    // load: SetValue bypasses the loader and marks the EntityLazyLoad loaded.
    Details.SetValue(childFactory.Create(Id));
}
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs#L241-L251' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-lazy-load-set-value' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-lazy-load-set-value-test -->
<a id='snippet-skill-lazy-load-set-value-test'></a>
```cs
[TestMethod]
public void Create_PreloadsDetails_SoThereIsNothingToLoad()
{
    var parent = _factory.Create();

    Assert.IsTrue(parent.Details.IsLoaded);
    Assert.IsNotNull(parent.Details.Value);
    Assert.AreEqual(parent.Id, parent.Details.Value!.ParentId);
    Assert.AreEqual(0, _childRepository.LoadCount, "SetValue bypassed the loader");
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/LazyLoadTests.cs#L56-L67' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-lazy-load-set-value-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`SetValue` manages child event subscriptions (unsubscribes from old value, subscribes to new). The loaded child integrates into PropertyManager's parent-child tracking automatically.

## Anti-Patterns

- **Do NOT create `EntityLazyLoad<T>` in `[Fetch]` or `[Create]`.** A factory method body runs on one tier, and the loader delegate is not serialized, so the other tier's copy has no loader and `LoadAsync()` throws. The constructor runs on both tiers; create it there.
- **Do NOT use `OnDeserialized`/`InitializeLazyLoaders`/`ReinitializeLazyLoaders`** — unnecessary complexity. The converter preserves constructor-created instances. Move EntityLazyLoad creation to the constructor instead.
- **Do NOT use manual backing fields** — this is the old pattern. Declare as `partial` and let the generator handle registration and meta property propagation.

## Loading

`await lazy.LoadAsync()` is the only way to trigger a load; it returns the loaded value (the test under "The Correct Pattern" above shows the sequence). Loading is idempotent — once loaded, subsequent calls return the cached value without invoking the loader again. Concurrent calls during the first load share the same task.

`.Value` is a passive read — it returns the current value (or `null` if not yet loaded) with no side effects. Use `LoadAsync()` in imperative code (domain logic, tests, `OnInitializedAsync`). Use `.Value` for UI binding after the load has been triggered.

### WaitForTasks Integration

`ValidateBase.WaitForTasks()` awaits in-progress LazyLoad children. This means `await entity.WaitForTasks()` before Save ensures any explicitly triggered loads have completed:

<!-- snippet: skill-lazy-load-wait-for-tasks -->
<a id='snippet-skill-lazy-load-wait-for-tasks'></a>
```cs
[TestMethod]
public async Task WaitForTasks_AwaitsAFireAndForgetLoad()
{
    var parent = await _factory.Fetch(Guid.NewGuid());

    // Fire-and-forget, as a page does in OnInitializedAsync so rendering is not blocked
    _ = parent.Details.LoadAsync();

    // WaitForTasks awaits the in-progress load; it never starts one
    await parent.WaitForTasks();

    Assert.IsTrue(parent.Details.IsLoaded);
    Assert.IsNotNull(parent.Details.Value);
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/LazyLoadTests.cs#L69-L84' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-lazy-load-wait-for-tasks' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

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

`EntityLazyLoad<T>` delegates meta properties to the loaded value when present:

| Property | Before Load | After Load |
|----------|-------------|------------|
| `IsBusy` | `true` if loading | Delegates to value's `IsBusy` |
| `IsValid` | `true` (unless load error) | Delegates to value's `IsValid` |
| `IsSelfValid` | `!HasLoadError` | `!HasLoadError` |
| `IsModified` | `false` | Delegates to value's `IsModified` |
| `IsSelfModified` | Always `false` | Always `false` (wrapper itself is never modified) |
| `IsNew` | `false` | Delegates to value's `IsNew` |
| `IsDeleted` | `false` | Delegates to value's `IsDeleted` |

## Error Handling

If the loader throws, the exception propagates to the `LoadAsync()` caller as-is. Error state is also captured on the `EntityLazyLoad<T>` instance (`HasLoadError`, `LoadError`), `IsLoaded` stays false so the load can be retried, and the wrapper reports `IsValid == false` while the error stands:

<!-- snippet: skill-lazy-load-error -->
<a id='snippet-skill-lazy-load-error'></a>
```cs
[TestMethod]
public async Task LoadAsync_WhenTheLoaderThrows_RecordsTheErrorAndRethrows()
{
    // The mock child repository has no details for an empty parent id
    var parent = await _factory.Fetch(Guid.Empty);

    await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => parent.Details.LoadAsync());

    Assert.IsTrue(parent.Details.HasLoadError);
    Assert.IsNotNull(parent.Details.LoadError);
    Assert.IsFalse(parent.Details.IsLoaded, "A failed load is not loaded; it can be retried");
    Assert.IsNull(parent.Details.Value);
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/LazyLoadTests.cs#L86-L100' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-lazy-load-error' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

If the load was triggered fire-and-forget (`_ = lazy.LoadAsync()`) and the parent calls `WaitForTasks()` while the load is still in progress, a load failure exception propagates through `WaitForTasks()`.

## UI Binding (Blazor / WPF)

`EntityLazyLoad<T>` implements `INotifyPropertyChanged` and fires change events for `Value`, `IsLoaded`, and `IsLoading` during the load lifecycle. Trigger the load explicitly in `OnInitializedAsync()`, then bind to `.Value` and state properties in Razor markup. The component re-renders on load completion because it (or its page) observes the entity's `PropertyChanged`; Blazor does not subscribe on its own.

**Trigger the load in `OnInitializedAsync()`:** after `entity = await entityFactory.Fetch(id);`, start the load without awaiting it — `_ = entity.OrderLines.LoadAsync();` — so rendering is not blocked (the `WaitForTasks` test above shows the same fire-and-forget shape).

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

The 4-branch pattern handles all states: error, loaded with data, loaded with null, and loading. `.Value` is a passive read here — the load was already triggered in `OnInitializedAsync()`. The `HasLoadError` branch is what surfaces a failed fire-and-forget load; without it the failure is silent.

## When to Use vs. Eager Loading

**Eager loading in the parent's `[Fetch]` method is preferred** for most cases — it keeps data access visible and avoids N+1 query problems.

Use `EntityLazyLoad<T>` when:
- The child data is large and not always needed
- Loading the child data is expensive (separate API call, complex query)
- The UI can progressively reveal data as it loads

Do **not** use `EntityLazyLoad<T>` when:
- The child data is always needed immediately
- Loading a small, cheap collection (eager load in `[Fetch]` instead)

## Related

- [Properties](properties.md) — Partial property system and change tracking
- [Entities](entities.md) — EntityBase lifecycle and Save routing
- [Collections](collections.md) — EntityListBase and ValidateListBase
