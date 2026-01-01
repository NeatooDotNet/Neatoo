# M1: Extract Meta-Property Change Helper

**Priority:** Medium
**Category:** Code Duplication
**Effort:** Low
**Status:** Not Started

---

## Problem Statement

Similar meta-property change detection code is repeated across multiple base classes:

- `ValidateBase.cs` - checks IsValid, IsSelfValid, IsBusy
- `EntityBase.cs` - checks IsModified, IsSelfModified, IsSavable, IsDeleted
- `EntityListBase.cs` - checks IsModified, IsSavable

Each follows the same pattern:
```csharp
if (this.MetaState.PropertyName != this.PropertyName)
{
    this.RaisePropertyChanged(nameof(this.PropertyName));
    this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.PropertyName), this));
}
```

---

## Current Duplication

### ValidateBase.cs
```csharp
protected override void CheckIfMetaPropertiesChanged()
{
    base.CheckIfMetaPropertiesChanged();

    if (this.MetaState.IsValid != this.IsValid)
    {
        this.RaisePropertyChanged(nameof(this.IsValid));
        this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsValid), this));
    }

    if (this.MetaState.IsSelfValid != this.IsSelfValid)
    {
        this.RaisePropertyChanged(nameof(this.IsSelfValid));
        this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsSelfValid), this));
    }

    if (this.MetaState.IsBusy != this.IsBusy)
    {
        this.RaisePropertyChanged(nameof(this.IsBusy));
        this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsBusy), this));
    }
}
```

### EntityBase.cs
```csharp
protected override void CheckIfMetaPropertiesChanged()
{
    if (!this.IsPaused)
    {
        if (this.EntityMetaState.IsModified != this.IsModified)
        {
            this.RaisePropertyChanged(nameof(this.IsModified));
            this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsModified), this));
        }
        // ... same pattern for IsSelfModified, IsSavable, IsDeleted
    }
    base.CheckIfMetaPropertiesChanged();
}
```

---

## Proposed Solution

Add a helper method to `Base<T>`:

```csharp
/// <summary>
/// Raises property changed events if the value has changed from the cached state.
/// </summary>
protected void RaiseIfChanged<TValue>(
    TValue cachedValue,
    TValue currentValue,
    string propertyName)
{
    if (!EqualityComparer<TValue>.Default.Equals(cachedValue, currentValue))
    {
        RaisePropertyChanged(propertyName);
        RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(propertyName, this));
    }
}
```

### Refactored ValidateBase.cs
```csharp
protected override void CheckIfMetaPropertiesChanged()
{
    base.CheckIfMetaPropertiesChanged();

    RaiseIfChanged(MetaState.IsValid, IsValid, nameof(IsValid));
    RaiseIfChanged(MetaState.IsSelfValid, IsSelfValid, nameof(IsSelfValid));
    RaiseIfChanged(MetaState.IsBusy, IsBusy, nameof(IsBusy));
}
```

### Refactored EntityBase.cs
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

---

## Benefits

1. **DRY:** Eliminates repeated code pattern
2. **Maintainable:** Single place to change if event raising logic changes
3. **Readable:** Intent is clearer (check and raise if changed)
4. **Testable:** Helper method can be unit tested directly

---

## Implementation Tasks

- [ ] Add `RaiseIfChanged<TValue>` method to `Base<T>`
- [ ] Refactor `ValidateBase.CheckIfMetaPropertiesChanged()`
- [ ] Refactor `EntityBase.CheckIfMetaPropertiesChanged()`
- [ ] Refactor `EntityListBase.CheckIfMetaPropertiesChanged()`
- [ ] Ensure all existing tests pass
- [ ] Add unit test for `RaiseIfChanged` method

---

## Files to Modify

| File | Action |
|------|--------|
| `src/Neatoo/Base.cs` | Add RaiseIfChanged method |
| `src/Neatoo/ValidateBase.cs` | Refactor to use helper |
| `src/Neatoo/EntityBase.cs` | Refactor to use helper |
| `src/Neatoo/EntityListBase.cs` | Refactor to use helper |
