# H4: Make IsMarkedBusy Thread-Safe

**Priority:** High
**Category:** Code Bug
**Effort:** Low
**Status:** Completed
**Completed Date:** 2025-12-31
**File:** `src/Neatoo/Internal/Property.cs`

---

## Problem Statement

The `IsMarkedBusy` property exposed a `List<long>` that was modified under a lock but read without synchronization:

```csharp
private readonly object _isMarkedBusyLock = new object();
public List<long> IsMarkedBusy { get; } = new List<long>();

public void AddMarkedBusy(long id)
{
    lock (this._isMarkedBusyLock)
    {
        if (!this.IsMarkedBusy.Contains(id))
        {
            this.IsMarkedBusy.Add(id);
        }
    }
    this.OnPropertyChanged(nameof(IsMarkedBusy));
}
```

**Problem:** If another thread read `IsMarkedBusy` while `AddMarkedBusy` or `RemoveMarkedBusy` was executing, it could:
1. Throw `InvalidOperationException` (collection modified during enumeration)
2. Return inconsistent data
3. Cause undefined behavior

---

## Solution Implemented

Used Option A (return a copy under lock) as recommended:

```csharp
private readonly object _isMarkedBusyLock = new object();
private readonly List<long> _isMarkedBusy = new List<long>();

/// <summary>
/// Gets a thread-safe snapshot of the busy operation identifiers.
/// </summary>
/// <remarks>
/// Returns a copy of the internal list to ensure thread safety. Each access
/// returns a new snapshot that is safe to enumerate without risk of
/// <see cref="InvalidOperationException"/> from concurrent modifications.
/// </remarks>
[JsonIgnore]
public IReadOnlyList<long> IsMarkedBusy
{
    get
    {
        lock (_isMarkedBusyLock)
        {
            return _isMarkedBusy.ToList().AsReadOnly();
        }
    }
}

public bool IsBusy
{
    get
    {
        lock (_isMarkedBusyLock)
        {
            return ValueAsBase?.IsBusy ?? false || IsSelfBusy || _isMarkedBusy.Count > 0;
        }
    }
}

public void AddMarkedBusy(long id)
{
    lock (_isMarkedBusyLock)
    {
        if (!_isMarkedBusy.Contains(id))
        {
            _isMarkedBusy.Add(id);
        }
    }
    OnPropertyChanged(nameof(IsMarkedBusy));
    OnPropertyChanged(nameof(IsBusy));
}

public void RemoveMarkedBusy(long id)
{
    lock (_isMarkedBusyLock)
    {
        _isMarkedBusy.Remove(id);
    }
    OnPropertyChanged(nameof(IsMarkedBusy));
    OnPropertyChanged(nameof(IsBusy));
}
```

---

## Breaking Change Assessment

| Change | Breaking? | Mitigation |
|--------|-----------|------------|
| `List<long>` -> `IReadOnlyList<long>` | Minor | `Count` and `Contains()` still work (LINQ extension) |
| Returns copy vs live reference | Behavior | Documented in XML comments |

---

## Implementation Tasks

- [x] Change property type from `List<long>` to `IReadOnlyList<long>`
- [x] Add private backing field `_isMarkedBusy`
- [x] Implement thread-safe getter that returns copy
- [x] Update `AddMarkedBusy` to use backing field
- [x] Update `RemoveMarkedBusy` to use backing field
- [x] Update `IsBusy` property to use backing field under lock
- [x] Add unit tests for concurrent access
- [x] Add test to verify snapshot behavior (not live reference)
- [x] Verify all existing tests pass (1622 passed)
- [x] Interface `IProperty` did not need updates (no `IsMarkedBusy` property)

---

## Tests Added

Two new tests in `PropertyTests.cs`:

1. `IsMarkedBusy_ConcurrentReadWrite_NoException` - Verifies concurrent read/write does not throw
2. `IsMarkedBusy_ReturnsSnapshot_NotLiveReference` - Verifies snapshot semantics

---

## Files Modified

| File | Action |
|------|--------|
| `src/Neatoo/Internal/Property.cs` | Implemented thread-safe IsMarkedBusy |
| `src/Neatoo.UnitTest/Unit/Core/PropertyTests.cs` | Added concurrency and snapshot tests |
