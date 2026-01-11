# List IsValid/IsModified Caching Optimization

## Problem

`ValidateListBase.IsValid` and `EntityListBase.IsModified` are computed properties that iterate ALL children on every access:

```csharp
// Current implementation - O(n) on every access
public bool IsValid => !this.Any(c => !c.IsValid);
public bool IsModified => this.Any(c => c.IsModified) || this.DeletedList.Any();
```

When `CheckIfMetaPropertiesChanged()` is called (on every child PropertyChanged), this triggers O(n) iteration. With large lists and frequent property changes, this becomes expensive.

## Solution

Cache the boolean result and update it intelligently based on child state transitions. Exploit the "any" semantics:

- `IsValid = false` if **ANY** child is invalid
- `IsModified = true` if **ANY** child is modified

### Key Insight

When a child's state changes, we often don't need to iterate:

| Child became | List was | Action needed |
|--------------|----------|---------------|
| Invalid | Valid | Set cache to false (O(1)) |
| Invalid | Invalid | No change needed (O(1)) |
| Valid | Valid | No change needed (O(1)) |
| Valid | Invalid | Must check if others still invalid (O(k) where k = position of first invalid) |

The `Any()` short-circuits, so even when iteration is needed, it stops at the first match.

## Implementation Plan

### Phase 1: ValidateListBase.IsValid Caching

- [ ] Add cached field `private bool _cachedIsValid = true;`
- [ ] Add public property that returns cached value:
  ```csharp
  public bool IsValid => _cachedIsValid;
  ```
- [ ] Update `HandlePropertyChanged` to handle IsValid changes:
  ```csharp
  protected virtual void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
  {
      if (e.PropertyName == nameof(IValidateMetaProperties.IsValid) && sender is IValidateBase child)
      {
          if (!child.IsValid)
          {
              // Child BECAME invalid → we're definitely invalid now
              _cachedIsValid = false;
          }
          else if (!_cachedIsValid)
          {
              // Child BECAME valid, and we were invalid
              // Check if any other child is still invalid
              _cachedIsValid = !this.Any(c => !c.IsValid);
          }
          // else: child became valid, we were already valid → no-op
      }

      CheckIfMetaPropertiesChanged();
  }
  ```
- [ ] Update `InsertItem` to check new child's state:
  ```csharp
  protected override void InsertItem(int index, I item)
  {
      base.InsertItem(index, item);

      if (!IsPaused && !item.IsValid)
      {
          _cachedIsValid = false;
      }
  }
  ```
- [ ] Update `RemoveItem` to potentially recalculate:
  ```csharp
  protected override void RemoveItem(int index)
  {
      var item = this[index];
      bool wasItemInvalid = !item.IsValid;

      base.RemoveItem(index);

      if (!IsPaused && wasItemInvalid && !_cachedIsValid)
      {
          // Removed an invalid item, might be valid now
          _cachedIsValid = !this.Any(c => !c.IsValid);
      }
  }
  ```
- [ ] Update `ClearItems` to reset cache:
  ```csharp
  protected override void ClearItems()
  {
      base.ClearItems();
      _cachedIsValid = true;
  }
  ```
- [ ] Update `ResumeAllActions` to recalculate cache:
  ```csharp
  public override void ResumeAllActions()
  {
      base.ResumeAllActions();
      _cachedIsValid = !this.Any(c => !c.IsValid);
  }
  ```
- [ ] Update `OnDeserialized` to initialize cache:
  ```csharp
  public override void OnDeserialized()
  {
      base.OnDeserialized();
      _cachedIsValid = !this.Any(c => !c.IsValid);
  }
  ```

### Phase 2: ValidateListBase.IsBusy Caching (same pattern)

- [ ] Add cached field `private bool _cachedIsBusy = false;`
- [ ] Update property to return cached value
- [ ] Update `HandlePropertyChanged` for IsBusy changes:
  - Child became busy → `_cachedIsBusy = true` (O(1))
  - Child became not busy, we were busy → check `Any(c => c.IsBusy)` (O(k))
- [ ] Update Insert/Remove/Clear/Resume/Deserialize

### Phase 3: EntityListBase.IsModified Caching

- [ ] Add cached field `private bool _cachedIsModified = false;`
- [ ] Update property:
  ```csharp
  public bool IsModified => _cachedIsModified || DeletedList.Any();
  ```
  Note: DeletedList is typically small, so `Any()` there is fine.
- [ ] Update `HandlePropertyChanged` for IsModified changes:
  - Child became modified → `_cachedIsModified = true` (O(1))
  - Child became unmodified, we were modified → check `Any(c => c.IsModified)` (O(k))
- [ ] Update `InsertItem` - check new child's IsModified state
- [ ] Update `RemoveItem`:
  - If removing modified item and we were modified → may need to recalculate
  - Note: existing items go to DeletedList, so IsModified stays true anyway
- [ ] Update `FactoryComplete(Update)` - after DeletedList.Clear(), recalculate
- [ ] Update Resume/Deserialize

### Phase 4: Handle IsSelfValid and IsSelfModified

These follow the same pattern but may need separate consideration:

- [ ] Review if `IsSelfValid` needs caching (lists don't have self-validation, always true)
- [ ] Review if `IsSelfModified` needs caching (lists don't have self-modification, always false)

These are likely fine as-is since they're constant values for lists.

## Edge Cases to Test

### Existing tests (should still pass)
- [ ] All tests in `ValidateListBaseTests`
- [ ] All tests in `EntityListBaseTests`
- [ ] All tests in `FatClientValidateListTests`

### New scenarios to add tests for

- [ ] **Add invalid item to valid list** → list becomes invalid immediately
- [ ] **Add valid item to invalid list** → list stays invalid
- [ ] **Remove the only invalid item** → list becomes valid
- [ ] **Remove one of multiple invalid items** → list stays invalid
- [ ] **Clear list with invalid items** → list becomes valid
- [ ] **Pause, add invalid items, resume** → cache recalculated on resume
- [ ] **Deserialize list with invalid items** → cache correct after deserialize
- [ ] **Rapid state changes** → cache stays consistent

### IsModified specific tests

- [ ] **Add modified item** → list becomes modified
- [ ] **Remove item to DeletedList** → list is modified (DeletedList.Any())
- [ ] **FactoryComplete clears DeletedList** → list may become unmodified
- [ ] **Item becomes modified then unmodified** → list state tracks correctly

## Performance Validation

After implementation, consider adding a performance test:

```csharp
[TestMethod]
public void Performance_LargeList_FrequentChanges()
{
    var list = new TestValidateList();

    // Add 1000 items
    for (int i = 0; i < 1000; i++)
        list.Add(new TestValidateItem());

    var sw = Stopwatch.StartNew();

    // Make 100 items invalid, then valid again
    for (int round = 0; round < 10; round++)
    {
        for (int i = 0; i < 100; i++)
            list[i].AddValidationError("Error");

        for (int i = 0; i < 100; i++)
            list[i].ClearErrors();
    }

    sw.Stop();

    // Should complete quickly with caching
    Assert.IsTrue(sw.ElapsedMilliseconds < 1000,
        $"Took {sw.ElapsedMilliseconds}ms - caching may not be working");
}
```

## Files to Modify

1. `src/Neatoo/ValidateListBase.cs`
   - Add `_cachedIsValid`, `_cachedIsBusy` fields
   - Modify `IsValid`, `IsBusy` properties
   - Modify `HandlePropertyChanged`
   - Modify `InsertItem`, `RemoveItem`, `ClearItems`
   - Modify `ResumeAllActions`, `OnDeserialized`

2. `src/Neatoo/EntityListBase.cs`
   - Add `_cachedIsModified` field
   - Modify `IsModified` property
   - Override `HandlePropertyChanged` (or extend base implementation)
   - Modify `InsertItem`, `RemoveItem` overrides
   - Modify `FactoryComplete`

3. `src/Neatoo.UnitTest/Unit/Core/ValidateListBaseTests.cs`
   - Add edge case tests

4. `src/Neatoo.UnitTest/Unit/Core/EntityListBaseTests.cs`
   - Add edge case tests

## Rollback Plan

If issues are discovered:
1. Revert cached fields to computed properties
2. Remove HandlePropertyChanged optimizations
3. All existing tests should pass (they test behavior, not implementation)

## Success Criteria

- [ ] All existing tests pass
- [ ] New edge case tests pass
- [ ] IsValid/IsModified return correct values in all scenarios
- [ ] PropertyChanged fires correctly when state changes
- [ ] No performance regression (should be improvement)
- [ ] No memory leaks (not holding references to removed children)
