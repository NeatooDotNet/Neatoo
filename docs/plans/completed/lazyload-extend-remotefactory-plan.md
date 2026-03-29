# LazyLoad: Extend from RemoteFactory — Implementation Plan

**Date:** 2026-03-29
**Related Todo:** [LazyLoad: Extend from RemoteFactory](../todos/lazyload-extend-remotefactory.md)
**Status:** Complete
**Last Updated:** 2026-03-29

---

## Overview

Refactor Neatoo's `LazyLoad<T>` to inherit from `Neatoo.RemoteFactory.LazyLoad<T>` instead of duplicating its core loading logic. **Rename** the Neatoo subclass from `LazyLoad<T>` to `EntityLazyLoad<T>` to avoid namespace collision with `Neatoo.RemoteFactory.LazyLoad<T>`. The subclass retains only the Neatoo-specific meta-property interfaces (`IValidateMetaProperties`, `IEntityMetaProperties`). All `ILazyLoadDeserializable` references switch to RemoteFactory's now-public interface.

---

## Business Requirements Context

**Source: Requirements Review (todo, 2026-03-29)**

This is a pure internal refactoring -- no business-facing behavior changes. All 22 behavioral contracts identified by the reviewer are preserved by the inheritance approach:

- **3 Design Decisions** (LazyLoadProperty.cs): LazyLoad as partial property, Value as passive read, WaitForTasks awaits in-progress loads. All preserved because the subclass delegates core behavior to the identical base class.

- **17 Unit Test Contracts** (LazyLoadTests.cs): Core loading lifecycle (passive read, concurrent load, error state, serialization, meta-property delegation). All preserved because test code constructs `new LazyLoad<T>(...)` which creates the Neatoo subclass, and all public API surface is unchanged.

- **15+ Integration Test Contracts** (state propagation, serialization, deferred loading, WaitForTasks crash tests): All preserved because `IValidateMetaProperties` and `IEntityMetaProperties` implementations stay on the subclass, and `ILazyLoadDeserializable` interface is structurally identical.

- **1 Generator Contract** (PropertyExtractor.cs): Detection by `OriginalDefinition.Name == "LazyLoad"` AND `ContainingNamespace == "Neatoo"`. Preserved because the property type remains `Neatoo.LazyLoad<T>`.

- **1 Skill Doc Reference** (lazy-loading.md): Full API surface documented. All preserved.

**Critical issue from review:** `typeof(LazyLoad<>)` in `NeatooBaseJsonTypeConverter.cs` will silently resolve to `Neatoo.RemoteFactory.LazyLoad<>` after upgrading to RemoteFactory 0.26.0, breaking serialization. Fixed by using `typeof(Neatoo.LazyLoad<>)` fully-qualified. See Design section 7.

---

## Business Rules (Testable Assertions)

All assertions below are behavioral preservation contracts -- they must hold true both before and after the refactoring. None are NEW; all trace to existing tests and design decisions.

### Core Loading Lifecycle

1. WHEN Value is accessed on an unloaded instance, THEN returns null with no side effects (no load triggered, no state change). Traces to: `Value_BeforeLoad_ReturnsNullWithNoSideEffects`
2. WHEN LoadAsync() is called, THEN loads the value via the loader delegate, sets IsLoaded=true, updates Value. Traces to: `LoadAsync_LoadsValue`
3. WHEN LoadAsync() is called concurrently, THEN only one actual load occurs and all callers get the same result. Traces to: `LoadAsync_CalledConcurrently_OnlyLoadsOnce`
4. WHEN the loader delegate throws, THEN HasLoadError=true, LoadError contains the message, exception propagates to caller. Traces to: `LoadAsync_OnFailure_SetsErrorState`, `LoadAsync_OnFailure_PropagatesException`
5. WHEN LoadAsync() completes, THEN PropertyChanged fires for Value, IsLoaded, IsLoading. Traces to: `LoadAsync_RaisesPropertyChangedForAllStateProperties`
6. WHEN pre-loaded via constructor, THEN IsLoaded=true immediately and Value returns the pre-loaded value. Traces to: `Factory_Create_WithValue_CreatesPreLoadedLazyLoad`

### Meta-Property Delegation (IValidateMetaProperties)

7. WHEN IsLoading is true, THEN IsBusy returns true. Traces to: `IsBusy_WhenLoading_ReturnsTrue`
8. WHEN the loaded value's IsBusy is true, THEN LazyLoad.IsBusy returns true. Traces to: `IsBusy_DelegatesToValue_WhenLoaded`
9. WHEN HasLoadError is true, THEN IsValid returns false. Traces to: `IsValid_WhenHasLoadError_ReturnsFalse`
10. WHEN IsSelfValid is checked, THEN returns !HasLoadError (independent of loaded value). Traces to: Design Decision in `LazyLoadProperty.cs`
11. WHEN WaitForTasks() is called with an in-progress load, THEN awaits the load task. Traces to: `WaitForTasks_AfterExplicitLoad_AwaitsLoad`
12. WHEN WaitForTasks() is called with no load in progress and no loaded value, THEN completes immediately without triggering a load. Traces to: `ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger`
13. WHEN ClearAllMessages() is called, THEN clears load error AND delegates to loaded value. Traces to: LazyLoad.cs lines 317-320

### Meta-Property Delegation (IEntityMetaProperties)

14. WHEN the loaded value's IsModified is true, THEN LazyLoad.IsModified returns true. Traces to: `IsModified_DelegatesToValue_WhenLoaded`
15. WHEN IsSelfModified is checked, THEN always returns false (LazyLoad wrapper itself is never modified). Traces to: `IsSelfModified_AlwaysReturnsFalse`
16. WHEN not loaded, THEN IsModified, IsNew, IsDeleted all return false. Traces to: `IsModified_BeforeLoad_ReturnsFalse`, `IsNew_BeforeLoad_ReturnsFalse`, `IsDeleted_BeforeLoad_ReturnsFalse`

### Serialization

17. WHEN serialized/deserialized via STJ, THEN Value and IsLoaded survive round-trip. Traces to: `Serialization_PreserveValueAndLoadedState`
18. WHEN NeatooBaseJsonTypeConverter detects a LazyLoad property, THEN serializes it as a top-level JSON property (not in PropertyManager array). Traces to: FatClientLazyLoadTests, TwoContainerLazyLoadTests
19. WHEN deserializing a LazyLoad property, THEN ApplyDeserializedState merges into constructor-created instance (preserving loader). Traces to: FatClientLazyLoadTests, NeatooBaseJsonTypeConverter.cs line 204

### State Propagation (Integration)

20. WHEN a child entity in a LazyLoad property is modified, THEN the parent's IsModified returns true. Traces to: `LazyLoadChild_ModifyChild_ParentIsModified`
21. WHEN a child entity in a LazyLoad property is modified, THEN the parent's IsSelfModified remains false. Traces to: `LazyLoadChild_ModifyChild_ParentNotSelfModified`
22. WHEN the LazyLoad child has a load error, THEN the parent's IsValid returns false. Traces to: `ParentIsValid_AfterExplicitChildLoadFailure`

### Generator Detection

23. WHEN a partial property has type `LazyLoad<T>` in namespace `Neatoo`, THEN the generator detects it and generates LazyLoad-specific backing field code. Traces to: PropertyExtractor.cs lines 63-71

### Test Scenarios

Each assertion above traces to one or more existing tests. No new test scenarios needed -- all tests must pass unchanged after the refactoring. The complete test matrix:

| Scenario | Test(s) | Assertion(s) |
|----------|---------|--------------|
| Unloaded passive read | `Value_BeforeLoad_ReturnsNullWithNoSideEffects` | 1 |
| Basic load | `LoadAsync_LoadsValue`, `LoadAsync_Works` | 2 |
| Concurrent load | `LoadAsync_CalledConcurrently_OnlyLoadsOnce` | 3 |
| Load error | `LoadAsync_OnFailure_SetsErrorState`, `LoadAsync_OnFailure_PropagatesException` | 4 |
| PropertyChanged on load | `LoadAsync_RaisesPropertyChangedForAllStateProperties` | 5 |
| Pre-loaded value | `Factory_Create_WithValue_CreatesPreLoadedLazyLoad`, `ValueAccess_AlreadyLoaded_ReturnsCachedValue` | 6 |
| IsBusy during load | `IsBusy_WhenLoading_ReturnsTrue` | 7 |
| IsBusy delegation | `IsBusy_DelegatesToValue_WhenLoaded` | 8 |
| IsValid with error | `IsValid_WhenHasLoadError_ReturnsFalse` | 9 |
| WaitForTasks | `WaitForTasks_AfterExplicitLoad_AwaitsLoad` | 11 |
| WaitForTasks no trigger | `ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger` | 12 |
| IsSelfModified | `IsSelfModified_AlwaysReturnsFalse` | 15 |
| Entity delegation | `IsModified_DelegatesToValue_WhenLoaded`, `IsNew_DelegatesToValue_WhenLoaded`, `IsDeleted_DelegatesToValue_WhenLoaded` | 14, 16 |
| STJ serialization | `Serialization_PreserveValueAndLoadedState` | 17 |
| Full serialization | FatClientLazyLoadTests (6), TwoContainerLazyLoadTests (2) | 18, 19 |
| State propagation | LazyLoadStatePropagationTests (4), LazyLoadExplicitLoadPropagationTests (4) | 20, 21, 22 |
| Deferred loading | FatClientDeferredLazyLoadTests, TwoContainerDeferredLazyLoadTests | 18, 19 |
| Nullable types | NullableType tests (4) | 1, 2, 6 |
| Factory DI registration | `Factory_RegisteredInDI` | N/A (infrastructure) |

---

## Approach

**Inheritance over duplication.** Neatoo's `LazyLoad<T>` becomes a thin subclass that adds only meta-property behavior. The core loading state machine (loader, lock, loadTask, value, error, PropertyChanged) lives entirely in RemoteFactory's base class.

Key insight: All meta-property implementations can use public properties (`Value`, `IsLoading`, `HasLoadError`, `LoadError`, `IsLoaded`) and the three new protected members (`LoadTask`, `ClearLoadError()`, `OnPropertyChanged(string)`). No private field access needed.

---

## Domain Model Behavioral Design

N/A -- this is internal framework refactoring with no behavioral changes. The public API surface of `Neatoo.LazyLoad<T>` is unchanged.

---

## Design

### 1. Package Update

Update `Directory.Packages.props`:
- `Neatoo.RemoteFactory` 0.24.1 → 0.26.0
- `Neatoo.RemoteFactory.AspNetCore` 0.24.1 → 0.26.0

### 2. Remove Neatoo's `ILazyLoadDeserializable`

Delete the internal `ILazyLoadDeserializable` interface from `LazyLoad.cs` (lines 12-17). RemoteFactory's public `Neatoo.RemoteFactory.Internal.ILazyLoadDeserializable` replaces it.

### 3. Rewrite `LazyLoad<T>` as Subclass

```csharp
using Neatoo.RemoteFactory.Internal; // for ILazyLoadDeserializable (now public)

namespace Neatoo;

public class LazyLoad<T> : Neatoo.RemoteFactory.LazyLoad<T>, IValidateMetaProperties, IEntityMetaProperties
    where T : class?
{
    // Constructors — delegate to base
    public LazyLoad() : base() { }
    public LazyLoad(Func<Task<T?>> loader) : base(loader) { }
    public LazyLoad(T? value) : base(value) { }

    // IValidateMetaProperties — uses base public properties + protected members
    public bool IsBusy => IsLoading || ((Value as IValidateMetaProperties)?.IsBusy ?? false);
    bool IValidateMetaProperties.IsValid => !HasLoadError && ((Value as IValidateMetaProperties)?.IsValid ?? true);
    public bool IsSelfValid => !HasLoadError;
    // ... (WaitForTasks uses protected LoadTask, ClearMessages uses ClearLoadError())

    // IEntityMetaProperties — delegates to Value
    // ... (IsModified, IsNew, IsDeleted, etc.)
}
```

**What gets removed:** All core fields (`_loader`, `_loadLock`, `_value`, `_isLoaded`, `_isLoading`, `_loadTask`, `_loadError`), `PropertyChanged` event, `OnPropertyChanged`, `SubscribeToValuePropertyChanged`, `UnsubscribeFromValuePropertyChanged`, `OnValuePropertyChanged`, `SetValue`, `LoadAsync`, `LoadAsyncCore`, property definitions (`Value`, `IsLoaded`, `IsLoading`, `HasLoadError`, `LoadError`), and the `ILazyLoadDeserializable` explicit implementation.

**What stays:** Constructors (calling base), `IValidateMetaProperties` region, `IEntityMetaProperties` region. Meta-property implementations change from `_value` to `Value` and from `_loadTask` to `LoadTask`.

### 4. Update `ILazyLoadDeserializable` References

All files that cast to `Neatoo.ILazyLoadDeserializable` switch to `Neatoo.RemoteFactory.Internal.ILazyLoadDeserializable`:

| File | Change |
|------|--------|
| `Internal/LazyLoadValidateProperty.cs` | Add `using Neatoo.RemoteFactory.Internal;`, remove `Neatoo.` prefix on casts |
| `Internal/LazyLoadEntityProperty.cs` | Same |
| `RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` | Same |

Since the interfaces are identical, all casts work unchanged.

### 5. ILazyLoadFactory

**Keep Neatoo's `ILazyLoadFactory`** — it returns `Neatoo.LazyLoad<T>` (the subclass), which is what the property system needs. RemoteFactory's `ILazyLoadFactory` returns `RemoteFactory.LazyLoad<T>` (the base), which wouldn't carry the meta-property interfaces.

The `LazyLoadFactory` implementation stays, constructing `new Neatoo.LazyLoad<T>(...)`.

### 6. Generator Verification

`PropertyExtractor.cs` detects LazyLoad by:
```csharp
namedType.OriginalDefinition.Name == "LazyLoad"
    && namedType.OriginalDefinition.ContainingNamespace?.ToString() == "Neatoo"
```

Neatoo's `LazyLoad<T>` is still in namespace `Neatoo`, so detection still works. **No generator changes needed.**

### 7. Serializer Fix (typeof(LazyLoad<>) namespace resolution)

`NeatooBaseJsonTypeConverter.cs` detects LazyLoad properties by:
```csharp
p.PropertyType.IsGenericType
    && p.PropertyType.GetGenericTypeDefinition() == typeof(LazyLoad<>)
```

**CRITICAL FIX REQUIRED.** The file is in namespace `Neatoo.RemoteFactory.Internal`. After upgrading to RemoteFactory 0.26.0, `Neatoo.RemoteFactory.LazyLoad<T>` exists. C# namespace resolution finds `LazyLoad` in the closer `Neatoo.RemoteFactory` namespace first, so `typeof(LazyLoad<>)` silently becomes `typeof(Neatoo.RemoteFactory.LazyLoad<>)`. Meanwhile, entity property types are still `Neatoo.LazyLoad<T>`, so `GetGenericTypeDefinition()` returns `Neatoo.LazyLoad<>`. The comparison fails -- LazyLoad properties are silently dropped from JSON.

**Fix:** Change both occurrences (line 136, Read path; line 408, Write path) to `typeof(Neatoo.LazyLoad<>)` -- fully qualified. This is explicit and unambiguous regardless of namespace resolution.

The `ILazyLoadDeserializable` cast on line 204 resolves correctly because the file is already in `Neatoo.RemoteFactory.Internal` namespace, which is the same namespace as RemoteFactory's public `ILazyLoadDeserializable`.

---

## Implementation Steps

1. Update `Directory.Packages.props` -- RemoteFactory 0.24.1 -> 0.26.0
2. Rewrite `LazyLoad.cs`:
   - Delete the internal `ILazyLoadDeserializable` interface definition (lines 12-17)
   - Change class declaration to inherit from `Neatoo.RemoteFactory.LazyLoad<T>`
   - Remove all duplicated core members (private fields, PropertyChanged event, OnPropertyChanged, Subscribe/Unsubscribe helpers, LoadAsync, LoadAsyncCore, SetValue, Value, IsLoaded, IsLoading, HasLoadError, LoadError, explicit ILazyLoadDeserializable implementation)
   - Keep: Constructors (delegating to base), `[JsonConstructor]` on parameterless constructor, `IValidateMetaProperties` region, `IEntityMetaProperties` region
   - In meta-property implementations: change `_value` to `Value`, `_loadTask` to `LoadTask`, `_loadError = null` to `ClearLoadError()`
3. Update `LazyLoadValidateProperty.cs` -- add `using Neatoo.RemoteFactory.Internal;` for `ILazyLoadDeserializable` casts
4. Update `LazyLoadEntityProperty.cs` -- add `using Neatoo.RemoteFactory.Internal;` for `ILazyLoadDeserializable` casts
5. **Fix `NeatooBaseJsonTypeConverter.cs`** -- change `typeof(LazyLoad<>)` to `typeof(Neatoo.LazyLoad<>)` at both line 136 (Read path) and line 408 (Write path). The `ILazyLoadDeserializable` cast on line 204 resolves correctly already.
6. Build and fix any compilation errors
7. Run all tests -- verify zero regressions

---

## Acceptance Criteria

- [ ] `Neatoo.LazyLoad<T>` inherits from `Neatoo.RemoteFactory.LazyLoad<T>`
- [ ] No duplicate loading logic in Neatoo's LazyLoad
- [ ] Neatoo's `ILazyLoadDeserializable` interface removed (uses RemoteFactory's)
- [ ] All existing LazyLoad unit tests pass
- [ ] All existing LazyLoad integration tests pass
- [ ] All Design.Tests pass
- [ ] Full solution builds with zero errors

---

## Agent Phasing

**Single phase.** This is a straightforward refactoring touching 5 files with clear, mechanical changes. No phase boundaries needed. A single developer agent invocation can complete all implementation steps, build verification, and test execution.

---

## Dependencies

- RemoteFactory 0.26.0 on NuGet (confirmed available)

---

## Risks / Considerations

1. **Constructor chaining** -- `[JsonConstructor]` must stay on Neatoo's parameterless constructor so JSON deserialization creates the correct subclass type.
2. **Serializer type detection** -- `typeof(EntityLazyLoad<>)` in NeatooBaseJsonTypeConverter must use the new name.
3. **PropertyChanged event** -- Base class owns the event. Neatoo's subclass does not re-declare it. `OnPropertyChanged` is virtual on the base, so the subclass could override if needed but does not need to.
4. **ILazyLoadDeserializable** -- Base class explicitly implements it. The subclass inherits the implementation. Neatoo's serializer casts to the interface, which works because the base class implements it.

---

## Addendum: Rename LazyLoad → EntityLazyLoad (2026-03-29)

**Reason:** After the initial implementation, a namespace collision was discovered. Any file with both `using Neatoo;` and `using Neatoo.RemoteFactory;` gets CS0104 ambiguity on unqualified `LazyLoad<T>`. This affects the source generator output, Person example, samples, and potentially Design projects. **User decided to rename** Neatoo's subclass from `LazyLoad<T>` to `EntityLazyLoad<T>`.

### Rename Scope

The rename is mechanical — find all references to `LazyLoad` (as a Neatoo type, NOT RemoteFactory's `LazyLoad`) and replace with `EntityLazyLoad`. This covers:

#### Core Library (`src/Neatoo/`)

| File | Rename |
|------|--------|
| `LazyLoad.cs` → `EntityLazyLoad.cs` | Class: `LazyLoad<T>` → `EntityLazyLoad<T>` |
| `ILazyLoadFactory.cs` → `IEntityLazyLoadFactory.cs` | Interface: `ILazyLoadFactory` → `IEntityLazyLoadFactory`, Class: `LazyLoadFactory` → `EntityLazyLoadFactory`, return types |
| `Internal/LazyLoadValidateProperty.cs` | Generic type parameter: `LazyLoad<T>` → `EntityLazyLoad<T>` |
| `Internal/LazyLoadEntityProperty.cs` | Same |
| `Internal/DefaultPropertyFactory.cs` | `CreateLazyLoad` method, `LazyLoadValidateProperty` constructor |
| `Internal/EntityPropertyFactory.cs` | Same |
| `Internal/IPropertyFactory.cs` | `CreateLazyLoad` method signature |
| `RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` | `typeof(LazyLoad<>)` → `typeof(EntityLazyLoad<>)` |
| `AddNeatooServices.cs` | DI registration of `ILazyLoadFactory` → `IEntityLazyLoadFactory` |

#### Source Generator (`src/Neatoo.BaseGenerator/`)

| File | Rename |
|------|--------|
| `Extractors/PropertyExtractor.cs` | Name detection: `"LazyLoad"` → `"EntityLazyLoad"` |
| `Generators/PropertyGenerator.cs` | Generated code references |
| `Generators/InitializerGenerator.cs` | `CreateLazyLoad` → `CreateEntityLazyLoad` |

#### Tests, Design, Samples

All references to `LazyLoad<` (as Neatoo type), `ILazyLoadFactory`, `LazyLoadFactory`, `new LazyLoad<` need updating across:
- `src/Neatoo.UnitTest/` — all LazyLoad test files
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs`
- `src/samples/LazyLoadSamples.cs`
- `src/Examples/Person/`
- `skills/neatoo/references/lazy-loading.md`

### What does NOT get renamed

- `LazyLoadValidateProperty` / `LazyLoadEntityProperty` — internal types, the "LazyLoad" prefix is descriptive. Renaming to `EntityLazyLoadValidateProperty` / `EntityLazyLoadEntityProperty` would be redundant.
- `ILazyLoadProperty` — internal interface
- `LazyLoadPropertyHelper` — internal helper
- RemoteFactory's `LazyLoad<T>` — stays as-is
