# L6: Consolidate Lifecycle Interfaces

**Status**: Not Started
**Priority**: Low
**Category**: Code Quality
**Created**: 2026-01-15

## Overview

`ValidateBase<T>` implements 8 interfaces. Four of these are lifecycle hooks that could potentially be consolidated:

- `IJsonOnDeserializing`
- `IJsonOnDeserialized`
- `IFactoryOnStart`
- `IFactoryOnComplete`

## Current State

```csharp
public abstract class ValidateBase<T> :
    INeatooObject,
    IValidateBase,
    IValidateBaseInternal,
    ISetParent,
    INotifyPropertyChanged,
    IJsonOnDeserializing,      // Could consolidate
    IJsonOnDeserialized,       // Could consolidate
    IFactoryOnStart,           // Could consolidate
    IFactoryOnComplete         // Could consolidate
    where T : ValidateBase<T>
```

## Proposed Consolidation

### Option A: Two Combined Interfaces

```csharp
// Combine JSON lifecycle
public interface IJsonLifecycle
{
    void OnDeserializing();
    void OnDeserialized();
}

// Combine Factory lifecycle
public interface IFactoryLifecycle
{
    void FactoryStart(FactoryOperation operation);
    void FactoryComplete(FactoryOperation operation);
}
```

**Result**: 8 interfaces → 6 interfaces

### Option B: Single Lifecycle Interface

```csharp
public interface INeatooLifecycle
{
    void OnDeserializing();
    void OnDeserialized();
    void FactoryStart(FactoryOperation operation);
    void FactoryComplete(FactoryOperation operation);
}
```

**Result**: 8 interfaces → 5 interfaces

## Considerations

### Why This Matters (or Doesn't)

**Arguments for consolidation**:
- Reduces interface count on base class
- Groups related concerns
- Slightly cleaner inheritance list

**Arguments against**:
- These interfaces come from RemoteFactory, not Neatoo
- Changing them requires coordinating with RemoteFactory
- The current separation allows implementing only what's needed
- This is cosmetic - doesn't affect runtime behavior

### RemoteFactory Dependency

These interfaces are defined in RemoteFactory:
- `IJsonOnDeserializing` / `IJsonOnDeserialized` - JSON serialization hooks
- `IFactoryOnStart` / `IFactoryOnComplete` - Factory operation hooks

Any consolidation would need to happen in RemoteFactory first, then Neatoo would adopt.

## Task List

- [ ] Evaluate if this is worth the effort (cosmetic improvement)
- [ ] Check if RemoteFactory would benefit from consolidated interfaces
- [ ] If proceeding: Create consolidated interfaces in RemoteFactory
- [ ] If proceeding: Update Neatoo to use consolidated interfaces
- [ ] Update documentation

## Recommendation

**Low priority** - This is a cosmetic improvement. The 8 interfaces don't cause runtime issues; they just look verbose in the class declaration. Consider deferring unless RemoteFactory independently wants to consolidate.

## Related

- ValidateBase implements these for proper serialization and factory lifecycle
- EntityBase inherits all 8 and adds IFactorySaveMeta
