# EntityListBase.IsModified PropertyChanged Notification Bug

## Problem

`EntityListBase.IsModified` doesn't fire `PropertyChanged` when child items become modified/unmodified.

### Root Cause

1. `EntityProperty.IsModified` is a computed property:
   ```csharp
   public bool IsModified => this.IsSelfModified || (this.EntityChild?.IsModified ?? false);
   ```

2. When `IsSelfModified` changes, `EntityProperty` does NOT raise `PropertyChanged("IsModified")`

3. `EntityListBase` subscribes to child `PropertyChanged` events, but never receives `IsModified` notifications

4. Therefore, `EntityListBase.CheckIfMetaPropertiesChanged()` is never called when children's modification state changes

### Contrast with IsValid (which works correctly)

| Step | IsValid | IsModified |
|------|---------|------------|
| Child property changes | ValidateProperty raises PropertyChanged("IsValid") | EntityProperty does NOT raise PropertyChanged("IsModified") |
| List receives event | Yes | No |
| List checks meta state | Yes | No |
| List raises PropertyChanged | Yes | No |
| UI updates | Yes | No |

## Impact

- **Save button bound to `IsSavable` won't enable** when user modifies a list item's property
- **UI state can be stale** until something else triggers a refresh
- `IsModified` value is **correct** when queried (it's computed), but UI bindings don't update

## Tasks

- [x] Add `PropertyChanged` notification for `IsModified` in `EntityProperty`
- [x] Cache `IsModified` value in `EntityPropertyManager` (similar to how `ValidatePropertyManager` caches `IsValid`)
- [x] Add tests similar to `ValidateListBaseTests.PropertyChanged_FiredOncePerTransition` (existing test `EntityListBaseTest_AddInvalidChild_MakeValid_PropertyChanged` covers this)
- [x] Verify parent `IsSavable` updates correctly when child list items are modified

## Solution Summary

### Files Modified

1. **EntityProperty<T>** (`EntityPropertyManager.cs:36-83`)
   - Added PropertyChanged notifications for `IsSelfModified` and `IsModified` in:
     - `OnPropertyChanged()` when Value changes
     - `MarkSelfUnmodified()`
     - `LoadValue()`

2. **EntityPropertyManager** (`EntityPropertyManager.cs:88-188`)
   - Changed `IsModified` and `IsSelfModified` from computed to cached properties
   - Added `Property_PropertyChanged` override to recalculate and raise PropertyChanged when child properties change
   - Updated `OnDeserialized()` to initialize cached values

3. **EntityListBase** (`EntityListBase.cs:169-184`)
   - Fixed `CheckIfMetaPropertiesChanged()` ordering bug: entity-specific comparisons must happen BEFORE calling `base.CheckIfMetaPropertiesChanged()` (base calls `ResetMetaState()` via virtual dispatch)

4. **ValidatePropertyManager** (`ValidatePropertyManager.cs:76-80`)
   - Added `RaisePropertyChanged()` helper method for derived classes

## Related

- This was discovered while reviewing list performance optimization
- `ValidateListBase.IsValid` uses same pattern but works because `ValidateProperty` DOES raise events

## Notes

The fix should mirror how `ValidateProperty` handles `IsValid`:
```csharp
// In ValidateProperty.cs - this pattern works
public bool IsValid => this.ValueIsValidateBase != null
    ? this.ValueIsValidateBase.IsValid
    : this.RuleMessages.Count == 0;

// When RuleMessages changes:
this.OnPropertyChanged(nameof(IsValid));  // <-- EntityProperty needs this for IsModified
```
