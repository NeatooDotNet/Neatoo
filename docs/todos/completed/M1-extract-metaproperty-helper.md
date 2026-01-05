# M1: Extract Meta-Property Change Helper

**Priority:** Medium
**Category:** Code Duplication
**Effort:** Low
**Status:** ✅ COMPLETE
**Completed:** 2026-01-04

---

## Problem Statement

Similar meta-property change detection code is repeated across multiple base classes:

- `ValidateBase.cs` - checks IsValid, IsSelfValid, IsBusy
- `EntityBase.cs` - checks IsModified, IsSelfModified, IsSavable, IsDeleted
- `ValidateListBase.cs` - checks IsValid, IsSelfValid, IsBusy
- `EntityListBase.cs` - checks IsModified, IsSelfModified, IsSavable

Each follows the same pattern:
```csharp
if (this.MetaState.PropertyName != this.PropertyName)
{
    this.RaisePropertyChanged(nameof(this.PropertyName));  // or OnPropertyChanged for lists
    this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.PropertyName), this));
}
```

---

## Current Class Hierarchy (Post v10.4.0)

After the Base layer collapse in v10.4.0:

```
ValidateBase<T>          (root for entities)
  └── EntityBase<T>

ValidateListBase<I>      (root for lists, extends ObservableCollection<I>)
  └── EntityListBase<I>
```

Note: `Base<T>` no longer exists. `ValidateBase<T>` is now the root class.

---

## Proposed Solution

Add helper methods to both root classes:

### ValidateBase<T> Helper

```csharp
/// <summary>
/// Raises property changed events if the value has changed from the cached state.
/// </summary>
protected void RaiseIfChanged<TValue>(TValue cachedValue, TValue currentValue, string propertyName)
{
    if (!EqualityComparer<TValue>.Default.Equals(cachedValue, currentValue))
    {
        RaisePropertyChanged(propertyName);
        RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(propertyName, this));
    }
}
```

### ValidateListBase<I> Helper

```csharp
/// <summary>
/// Raises property changed events if the value has changed from the cached state.
/// </summary>
protected void RaiseIfChanged<TValue>(TValue cachedValue, TValue currentValue, string propertyName)
{
    if (!EqualityComparer<TValue>.Default.Equals(cachedValue, currentValue))
    {
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(propertyName, this));
    }
}
```

### Refactored Code

**ValidateBase.cs:**
```csharp
protected virtual void CheckIfMetaPropertiesChanged()
{
    RaiseIfChanged(MetaState.IsValid, IsValid, nameof(IsValid));
    RaiseIfChanged(MetaState.IsSelfValid, IsSelfValid, nameof(IsSelfValid));
    RaiseIfChanged(MetaState.IsBusy, IsBusy, nameof(IsBusy));
    ResetMetaState();
}
```

**EntityBase.cs:**
```csharp
protected override void CheckIfMetaPropertiesChanged()
{
    if (!IsPaused)
    {
        RaiseIfChanged(EntityMetaState.IsModified, IsModified, nameof(IsModified));
        RaiseIfChanged(EntityMetaState.IsSelfModified, IsSelfModified, nameof(IsSelfModified));
        RaiseIfChanged(EntityMetaState.IsSavable, IsSavable, nameof(IsSavable));
        RaiseIfChanged(EntityMetaState.IsDeleted, IsDeleted, nameof(IsDeleted));
    }
    base.CheckIfMetaPropertiesChanged();
}
```

**ValidateListBase.cs:**
```csharp
protected virtual void CheckIfMetaPropertiesChanged()
{
    RaiseIfChanged(MetaState.IsValid, IsValid, nameof(IsValid));
    RaiseIfChanged(MetaState.IsSelfValid, IsSelfValid, nameof(IsSelfValid));
    RaiseIfChanged(MetaState.IsBusy, IsBusy, nameof(IsBusy));
    ResetMetaState();
}
```

**EntityListBase.cs:**
```csharp
protected override void CheckIfMetaPropertiesChanged()
{
    base.CheckIfMetaPropertiesChanged();
    RaiseIfChanged(EntityMetaState.IsModified, IsModified, nameof(IsModified));
    RaiseIfChanged(EntityMetaState.IsSelfModified, IsSelfModified, nameof(IsSelfModified));
    RaiseIfChanged(EntityMetaState.IsSavable, IsSavable, nameof(IsSavable));
    ResetMetaState();
}
```

---

## Benefits

1. **DRY:** Eliminates ~40 lines of repeated code pattern
2. **Maintainable:** Single place to change if event raising logic changes
3. **Readable:** Intent is clearer (check and raise if changed)

---

## Implementation Tasks

- [x] Add `RaiseIfChanged<TValue>` method to `ValidateBase<T>`
- [x] Add `RaiseIfChanged<TValue>` method to `ValidateListBase<I>`
- [x] Refactor `ValidateBase.CheckIfMetaPropertiesChanged()`
- [x] Refactor `EntityBase.CheckIfMetaPropertiesChanged()`
- [x] Refactor `ValidateListBase.CheckIfMetaPropertiesChanged()`
- [x] Refactor `EntityListBase.CheckIfMetaPropertiesChanged()`
- [x] Ensure all existing tests pass (1787 tests passed)

---

## Files to Modify

| File | Action |
|------|--------|
| `src/Neatoo/ValidateBase.cs` | Add helper, refactor method |
| `src/Neatoo/EntityBase.cs` | Refactor to use helper |
| `src/Neatoo/ValidateListBase.cs` | Add helper, refactor method |
| `src/Neatoo/EntityListBase.cs` | Refactor to use helper |
