# Lazy Loading v2 - Explicit Async Design

**Status:** Design Complete
**Priority:** High
**Created:** 2026-01-16

---

## Problem

The current lazy loading implementation tries to serve two masters:
- **UI binding** needs fire-and-forget with PropertyChanged rebinding
- **Imperative code** needs explicit async with clear completion

This creates tension where the same property getter behaves magically, and developers don't know if they're getting a loaded value or triggering a background load.

---

## Solution

Introduce `LazyLoad<T>` wrapper that separates concerns:

```csharp
public class LazyLoad<T> where T : class
{
    public T? Value { get; }              // Current value, NEVER triggers load
    public bool IsLoaded { get; }
    public bool IsLoading { get; }

    public Task<T?> LoadAsync();          // Explicit load

    // Makes the whole object awaitable
    public TaskAwaiter<T?> GetAwaiter() => LoadAsync().GetAwaiter();
}
```

### Key Principle

**No magic.** Accessing `.Value` never triggers a load - it returns current state. Loading is always explicit via `await` or `LoadAsync()`.

---

## Usage Patterns

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

## Entity Declaration

```csharp
public partial class Consultation : EntityBase<Consultation>
{
    public LazyLoad<IConsultationHistory> History { get; private set; }

    [Create]
    public void Create(Func<Task<IConsultationHistory>> historyLoader)
    {
        History = new LazyLoad<IConsultationHistory>(historyLoader);
    }
}
```

---

## Implementation Tasks

- [ ] Create `LazyLoad<T>` class in Neatoo
- [ ] Implement `INotifyPropertyChanged` for `IsLoading`, `IsLoaded`, `Value`
- [ ] Handle concurrent load requests (only one active load)
- [ ] Handle load failures (error state?)
- [ ] Integrate with `WaitForTasks()` on parent entity
- [ ] Update source generator to support `LazyLoad<T>` properties
- [ ] Serialization strategy for `LazyLoad<T>` across client-server boundary
- [ ] Migration guide from v1 lazy loading
- [ ] Documentation and examples

---

## Open Questions

1. **Error handling** - Should `LazyLoad<T>` have an `Error` property, or throw on await?
2. **Retry** - Should there be a `ReloadAsync()` for retry after failure?
3. **Serialization** - When serializing to client, send loaded value or lazy reference?

---

## Progress Log

### 2026-01-16
- Brainstormed approaches: dual access, explicit async, wrapper types
- Identified that `.Value` should never trigger load
- Designed wrapper with `GetAwaiter()` for clean `await entity.Property` syntax
- Reviewed patterns from TanStack Query, Angular async pipe, Svelte await blocks
- Decided on `LazyLoad<T>` naming (avoids conflict with `System.Lazy<T>`)

---
