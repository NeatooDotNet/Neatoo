# Lazy Loading v2 - Explicit Async Design

**Status:** Complete
**Priority:** High
**Created:** 2026-01-16

---

## Problem

The current lazy loading implementation tries to serve two masters:
- **UI binding** needs state properties (IsLoading, Value) for reactive updates
- **Imperative code** needs explicit async with clear completion

This creates tension where the same property getter behaves magically, and developers don't know if they're getting a loaded value or triggering a background load.

---

## Solution

Introduce `LazyLoad<T>` wrapper that separates concerns:

```csharp
public class LazyLoad<T> : IValidateMetaProperties, IEntityMetaProperties, INotifyPropertyChanged
    where T : class
{
    // Core state
    public T? Value { get; }              // Current value, NEVER triggers load
    public bool IsLoaded { get; }
    public bool IsLoading { get; }

    // Error state
    public bool HasLoadError { get; }
    public string? LoadError { get; }

    // IValidateMetaProperties - delegate to Value or defaults
    public bool IsBusy => IsLoading || (Value as IValidateMetaProperties)?.IsBusy ?? false;
    public bool IsValid => !HasLoadError && (Value as IValidateMetaProperties)?.IsValid ?? true;
    public bool IsSelfValid => !HasLoadError;
    public IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }

    // IEntityMetaProperties - delegate to Value or defaults
    public bool IsChild => (Value as IEntityMetaProperties)?.IsChild ?? false;
    public bool IsModified => (Value as IEntityMetaProperties)?.IsModified ?? false;
    public bool IsSelfModified => false;  // LazyLoad itself is never modified
    public bool IsMarkedModified => (Value as IEntityMetaProperties)?.IsMarkedModified ?? false;
    public bool IsSavable => (Value as IEntityMetaProperties)?.IsSavable ?? false;

    // Explicit load
    public Task<T?> LoadAsync();

    // Makes the whole object awaitable
    public TaskAwaiter<T?> GetAwaiter() => LoadAsync().GetAwaiter();
}
```

### Key Principles

1. **No magic.** Accessing `.Value` never triggers a load - it returns current state.
2. **Loading is always explicit** via `await` or `LoadAsync()`.
3. **Load failures break the entity** - `HasLoadError` makes `IsValid = false`.
4. **DI everywhere** - Always use `ILazyLoadFactory`, never `new LazyLoad<T>()`.

---

## Factory Interface

```csharp
public interface ILazyLoadFactory
{
    // Create with lazy loader
    LazyLoad<TChild> Create<TChild>(Func<Task<TChild?>> loader)
        where TChild : class;

    // Create with pre-loaded value
    LazyLoad<TChild> Create<TChild>(TChild? value)
        where TChild : class;
}
```

---

## Usage Patterns

### Entity Declaration

```csharp
public partial class Consultation : EntityBase<Consultation>
{
    public LazyLoad<IHistory> History { get; private set; }

    public Consultation([Service] ILazyLoadFactory lazyLoadFactory,
                        Func<int, Task<IHistory>> historyLoader)
    {
        History = lazyLoadFactory.Create(() => historyLoader(Id));
    }

    [Fetch]
    public async Task Fetch(int id,
                            IHistoryRepository repo,
                            [Service] ILazyLoadFactory lazyLoadFactory)
    {
        var history = await repo.GetByConsultationId(id);
        History = lazyLoadFactory.Create(history);  // Pre-loaded
    }
}
```

### UI Binding (Blazor)

```razor
@if (entity.History.IsLoading)
{
    <MudProgressCircular />
}
else if (entity.History.Value is { } history)
{
    <div>@history.Name</div>
}
```

### Component Lifecycle

```csharp
protected override async Task OnInitializedAsync()
{
    await entity.History;  // Triggers load, UI updates via binding
}
```

### Imperative Code

```csharp
var history = await entity.History;

if (history.IsNew || history.IsModified)
{
    await history.Save();
}
```

### Check Without Loading

```csharp
if (entity.History.IsLoaded && entity.History.Value!.IsModified)
{
    // Only runs if already loaded
}
```

---

## Implementation Tasks

- [x] Create `LazyLoad<T>` class in Neatoo
- [x] Implement `IValidateMetaProperties` with delegation to Value
- [x] Implement `IEntityMetaProperties` with delegation to Value
- [x] Implement `INotifyPropertyChanged` for all state properties
- [x] Handle concurrent load requests (only one active load)
- [x] Handle load failures (HasLoadError, PropertyMessages)
- [x] Create `ILazyLoadFactory` interface
- [x] Create `LazyLoadFactory` implementation
- [x] Register factory in DI
- [x] Remove old lazy loading from `ValidateProperty<T>`
- [x] Serialization strategy for `LazyLoad<T>` across client-server boundary
- [ ] Update documentation and examples

---

## Migration

Remove from `ValidateProperty<T>`:
- `OnLoad` property
- `IsLoaded` property
- `LoadTask` property
- `LoadAsync()` method
- `TriggerLazyLoadAsync()` method
- `SetLazyLoadError()` / `ClearLazyLoadError()` methods
- `LazyLoadRuleId` constant

Existing code using `NameProperty.OnLoad = ...` pattern must migrate to `LazyLoad<T>`.

---

## Progress Log

### 2026-01-16 (Design)
- Brainstormed approaches: dual access, explicit async, wrapper types
- Identified that `.Value` should never trigger load
- Designed wrapper with `GetAwaiter()` for clean `await entity.Property` syntax
- Reviewed patterns from TanStack Query, Angular async pipe, Svelte await blocks
- Decided on `LazyLoad<T>` naming (avoids conflict with `System.Lazy<T>`)
- Added `IValidateMetaProperties` for validation bubbling
- Added `IEntityMetaProperties` for entity state bubbling (IsModified, etc.)
- Load failures create broken state via `HasLoadError`
- DI factory approach: `ILazyLoadFactory` injected via `[Service]` parameter
- Two factory overloads: `Create(loader)` for lazy, `Create(value)` for pre-loaded

### 2026-01-16 (Implementation Complete)
- Implemented `LazyLoad<T>` class with all properties (Value, IsLoaded, IsLoading, HasLoadError, LoadError)
- Implemented `IValidateMetaProperties` and `IEntityMetaProperties` with delegation to wrapped value
- Implemented `INotifyPropertyChanged` for UI binding support
- Implemented concurrent load protection (only one active load)
- Created `ILazyLoadFactory` interface and `LazyLoadFactory` implementation
- Registered `ILazyLoadFactory` in DI as transient
- Removed old lazy loading from `IValidateProperty` and `ValidateProperty<T>`
- Removed old `LazyLoadingTests.cs` (replaced by `LazyLoadTests.cs` with 26 tests)
- Added JSON serialization support (Value and IsLoaded preserved)
- All 1722 tests passing

---
