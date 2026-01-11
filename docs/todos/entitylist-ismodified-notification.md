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

- [ ] Add `PropertyChanged` notification for `IsModified` in `EntityProperty`
- [ ] Consider caching `IsModified` value similar to how `ValidatePropertyManager` caches `IsValid`
- [ ] Add tests similar to `ValidateListBaseTests.PropertyChanged_FiredOncePerTransition`
- [ ] Verify parent `IsSavable` updates correctly when child list items are modified

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
