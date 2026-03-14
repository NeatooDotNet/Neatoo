# Move LazyLoad Registration to PropertyManager

**Date:** 2026-03-14
**Related Todo:** [Move LazyLoad Registration to PropertyManager](../todos/move-lazyload-registration-to-propertymanager.md)
**Status:** Documentation Complete
**Last Updated:** 2026-03-14

---

## Overview

Move ~100 lines of LazyLoad property registration logic from `ValidateBase` into `ValidatePropertyManager` and `EntityPropertyManager`. This is a pure refactoring: the registration logic (reflection discovery, `MakeGenericType`, `Activator.CreateInstance`, PropertyBag lookups, reassignment detection) works correctly but lives in the wrong layer. `ValidateBase` should delegate to the PropertyManager, not implement property creation and registration internals.

---

## Difficulty & Risk Assessment

**Difficulty:** Low
**Risk:** Low
**Justification:** Pure internal refactoring. No public API changes, no new features, no behavioral changes. The code being moved is self-contained (it reads `this.PropertyManager` and calls `PropertyManager.Register`). The call sites in `ValidateBase` become one-line delegations. All existing tests exercise the same paths and should pass unchanged. The only subtlety is that `ValidateBase` must pass `GetType()` to the PropertyManager since only the entity instance knows its concrete runtime type.

---

## Business Rules

Since this is a behavior-preserving refactoring, the "business rules" are invariants that must hold before and after the change:

1. WHEN `RegisterLazyLoadProperties()` is called on an entity with LazyLoad properties, THEN each LazyLoad property gets a corresponding `LazyLoadValidateProperty<T>` or `LazyLoadEntityProperty<T>` registered in PropertyManager
2. WHEN the PropertyManager is an `IEntityPropertyManager`, THEN `LazyLoadEntityProperty<T>` is created (not `LazyLoadValidateProperty<T>`)
3. WHEN the PropertyManager is a `ValidatePropertyManager` (not entity), THEN `LazyLoadValidateProperty<T>` is created
4. WHEN a LazyLoad property is already registered (reassignment), THEN `LoadValue` is called on the existing property instead of creating a new one
5. WHEN `RegisterLazyLoadProperty<TInner>(name, lazyLoad)` is called, THEN it registers a single property without reflection-based discovery
6. WHEN `RegisterLazyLoadProperty<TInner>` is called with a property name that does not exist on the type, THEN `PropertyMissingException` is thrown
7. WHEN the static cache `GetLazyLoadProperties` is called with the same type twice, THEN reflection runs only once (cached)
8. WHEN `LoadValue` is called on a newly created LazyLoad property, THEN it fires `ChangeReason.Load` (not triggering rules)

---

## Approach

**Strategy:** Move the creation and registration logic into the PropertyManager layer using polymorphism to eliminate the `is IEntityPropertyManager` type check.

1. Add a `RegisterLazyLoad(object owner, Type concreteType)` method to `ValidatePropertyManager` that absorbs the bulk registration logic (static cache, reflection discovery, `MakeGenericType`, `Activator.CreateInstance`, reassignment detection)
2. Add a `RegisterLazyLoad<TInner>(object owner, string name, LazyLoad<TInner> lazyLoad)` method for the single-property variant
3. Override in `EntityPropertyManager` to create `LazyLoadEntityProperty<T>` instead of `LazyLoadValidateProperty<T>` (polymorphism replaces type check)
4. Reduce `ValidateBase.RegisterLazyLoadProperties()` and `RegisterLazyLoadProperty<TInner>()` to thin one-line delegations that pass `this` and `GetType()`
5. Move the static cache (`_lazyLoadPropertyCache`, `GetLazyLoadProperties`) to `ValidatePropertyManager` since it is property-management infrastructure

---

## Design

### Delegation mechanism: IValidatePropertyManagerInternal

**Critical constraint:** `ValidateBase.PropertyManager` is typed as `IValidatePropertyManager<IValidateProperty>`. At runtime, for entity objects the concrete type is `EntityPropertyManager : ValidatePropertyManager<IEntityProperty>`. Casting to `ValidatePropertyManager<IValidateProperty>` fails because generic classes are invariant in C#. This rules out casting to the concrete class.

**Solution: Use `IValidatePropertyManagerInternal<out P>`** (option a). This is the covariant (`out P`) internal interface already used by ValidateBase at lines 353, 411, and 681 for the same purpose. Because `IEntityProperty : IValidateProperty` and `P` is covariant, the cast `this.PropertyManager as IValidatePropertyManagerInternal<IValidateProperty>` succeeds for both `ValidatePropertyManager<IValidateProperty>` and `EntityPropertyManager` (which is `ValidatePropertyManager<IEntityProperty>`).

The new registration methods do not use `P` in their signatures (they work with `object`, `Type`, and `LazyLoad<TInner>`), so adding them to the covariant interface causes no variance issues.

### New methods on IValidatePropertyManagerInternal

```csharp
// IValidatePropertyManagerInternal<out P> -- add these two members
void RegisterLazyLoadProperties(object owner, Type concreteType);
void RegisterLazyLoadProperty<TInner>(object owner, string name, LazyLoad<TInner> lazyLoad) where TInner : class?;
```

### Implementation on ValidatePropertyManager

```csharp
// ValidatePropertyManager<P> -- implements the interface methods
void IValidatePropertyManagerInternal<P>.RegisterLazyLoadProperties(object owner, Type concreteType) { ... }
void IValidatePropertyManagerInternal<P>.RegisterLazyLoadProperty<TInner>(object owner, string name, LazyLoad<TInner> lazyLoad) { ... }

// Protected virtual factory method for polymorphism (entity vs validate property creation)
protected virtual IValidateProperty CreateLazyLoadProperty(Type innerType, IPropertyInfo propertyInfo)
```

### EntityPropertyManager override

```csharp
// EntityPropertyManager -- overrides factory method only
protected override IValidateProperty CreateLazyLoadProperty(Type innerType, IPropertyInfo propertyInfo)
```

### ValidateBase thin delegations

```csharp
// ValidateBase<T> -- becomes thin
protected void RegisterLazyLoadProperties()
{
    if (this.PropertyManager is IValidatePropertyManagerInternal<IValidateProperty> pmInternal)
    {
        pmInternal.RegisterLazyLoadProperties(this, GetType());
    }
}

protected void RegisterLazyLoadProperty<TInner>(string name, LazyLoad<TInner> lazyLoad) where TInner : class?
{
    if (this.PropertyManager is IValidatePropertyManagerInternal<IValidateProperty> pmInternal)
    {
        pmInternal.RegisterLazyLoadProperty(this, name, lazyLoad);
    }
}
```

This follows the exact same cast pattern already used at ValidateBase lines 353, 411, and 681.

### What moves where

| Code | From | To |
|------|------|----|
| `_lazyLoadPropertyCache` (static field) | `ValidateBase` | `ValidatePropertyManager` |
| `GetLazyLoadProperties(Type)` (static method) | `ValidateBase` | `ValidatePropertyManager` |
| Reflection loop, `MakeGenericType`, `Activator.CreateInstance` | `ValidateBase.RegisterLazyLoadProperties()` | `ValidatePropertyManager.RegisterLazyLoadProperties()` |
| Reassignment detection (`TryGetRegisteredProperty` + `ILazyLoadProperty` check) | `ValidateBase.RegisterLazyLoadProperties()` | `ValidatePropertyManager.RegisterLazyLoadProperties()` |
| `PropertyInfoWrapper` creation | `ValidateBase.RegisterLazyLoadProperties()` | `ValidatePropertyManager.RegisterLazyLoadProperties()` |
| `is IEntityPropertyManager` branch | `ValidateBase.RegisterLazyLoadProperties()` | Eliminated (polymorphic `CreateLazyLoadProperty`) |
| Single-property registration logic | `ValidateBase.RegisterLazyLoadProperty<T>()` | `ValidatePropertyManager.RegisterLazyLoadProperty<T>()` |
| `PropertyMissingException` throw | `ValidateBase.RegisterLazyLoadProperty<T>()` | `ValidatePropertyManager.RegisterLazyLoadProperty<T>()` |
| Trimming suppression attributes | `ValidateBase` methods | `ValidatePropertyManager` methods |

### What stays in ValidateBase

- `RegisterLazyLoadProperties()` -- protected, 1-2 lines, delegates to PropertyManager
- `RegisterLazyLoadProperty<TInner>(string, LazyLoad<TInner>)` -- protected, 1-2 lines, delegates to PropertyManager
- The `#region` is removed or reduced to just the delegating methods

### Key design decision: owner parameter

The PropertyManager needs `GetType()` from the entity to do reflection discovery, but `ValidatePropertyManager` does not hold a reference to its owning entity. Rather than adding an `Owner` reference (which would couple PropertyManager to the entity), the `RegisterLazyLoadProperties` method takes the owner instance and concrete type as parameters. This keeps the relationship one-directional.

For the single-property variant, the owner instance is needed for `GetType().GetProperty(name)` to create the `PropertyInfoWrapper`. The `owner` parameter provides this.

### IValidatePropertyManagerInternal changes

Two new members are added to `IValidatePropertyManagerInternal<out P>`:
- `void RegisterLazyLoadProperties(object owner, Type concreteType)`
- `void RegisterLazyLoadProperty<TInner>(object owner, string name, LazyLoad<TInner> lazyLoad) where TInner : class?`

Neither method uses `P` in its signature, so the covariant (`out P`) constraint is preserved.

`TryGetRegisteredProperty` is currently on `IValidatePropertyManagerInternal` and was added specifically for LazyLoad registration. Since the registration logic is moving into the PropertyManager itself, `TryGetRegisteredProperty` can become a private/protected method on `ValidatePropertyManager` instead of an internal interface member. However, this is optional cleanup -- the interface member still works. The developer should evaluate whether to simplify this during implementation.

---

## Implementation Steps

1. **Add two new members to `IValidatePropertyManagerInternal<out P>`**: `RegisterLazyLoadProperties(object, Type)` and `RegisterLazyLoadProperty<TInner>(object, string, LazyLoad<TInner>)` -- neither uses `P`, preserving covariance
2. **Add `CreateLazyLoadProperty` protected virtual method** to `ValidatePropertyManager` -- creates `LazyLoadValidateProperty<T>` via `MakeGenericType`/`Activator.CreateInstance`
3. **Override `CreateLazyLoadProperty`** in `EntityPropertyManager` -- creates `LazyLoadEntityProperty<T>` instead
4. **Move static cache** (`_lazyLoadPropertyCache`, `GetLazyLoadProperties`) from `ValidateBase` to `ValidatePropertyManager`
5. **Implement `RegisterLazyLoadProperties` on `ValidatePropertyManager`** -- absorbs the full registration loop from `ValidateBase.RegisterLazyLoadProperties()`, uses `CreateLazyLoadProperty` instead of `is IEntityPropertyManager` check
6. **Implement `RegisterLazyLoadProperty<TInner>` on `ValidatePropertyManager`** -- absorbs the single-property logic from `ValidateBase.RegisterLazyLoadProperty<TInner>()`, uses `CreateLazyLoadProperty`
7. **Reduce `ValidateBase.RegisterLazyLoadProperties()`** to thin delegation via `this.PropertyManager as IValidatePropertyManagerInternal<IValidateProperty>`
8. **Reduce `ValidateBase.RegisterLazyLoadProperty<TInner>()`** to thin delegation via same cast pattern
9. **Move trimming suppression attributes** from ValidateBase methods to the new ValidatePropertyManager methods
10. **Build and run all tests** -- zero failures expected

---

## Acceptance Criteria

- [ ] `ValidateBase.RegisterLazyLoadProperties()` is 1-3 lines (delegation only)
- [ ] `ValidateBase.RegisterLazyLoadProperty<TInner>()` is 1-3 lines (delegation only)
- [ ] `_lazyLoadPropertyCache` and `GetLazyLoadProperties` no longer exist in `ValidateBase`
- [ ] `ValidatePropertyManager` contains the registration logic with static cache
- [ ] `EntityPropertyManager` overrides a virtual method to create entity-variant properties (no type check)
- [ ] `dotnet build src/Neatoo.sln` succeeds with 0 errors
- [ ] `dotnet test src/Neatoo.sln` passes with 0 failures
- [ ] No public API changes (protected methods on ValidateBase retain same signatures)
- [ ] No behavioral changes (all existing call sites work identically)

---

## Risks / Considerations

1. **~~Cast from interface to concrete~~ RESOLVED**: The original plan incorrectly showed casting to `ValidatePropertyManager<IValidateProperty>`, which would fail at runtime for entity objects (`EntityPropertyManager` extends `ValidatePropertyManager<IEntityProperty>`, and generic classes are invariant). Fixed: use `IValidatePropertyManagerInternal<out P>` which is covariant and already the established pattern for this cross-class access at ValidateBase lines 353, 411, 681.

2. **Reflection `GetType()` on owner**: The `RegisterLazyLoadProperties` method needs the owner's `GetType()` result. Passing it explicitly (rather than storing an owner reference) keeps the PropertyManager stateless with respect to ownership. This is the right trade-off.

3. **Trimming attributes**: The `[UnconditionalSuppressMessage]` attributes must move with the reflection code. The justifications still hold because the caller (ValidateBase) has `[DynamicallyAccessedMembers]` on its type parameter, and the concrete types flow through.

4. **`TryGetRegisteredProperty` on internal interface**: Currently exists on `IValidatePropertyManagerInternal` for cross-class access. After the move, the PropertyManager calls its own private `PropertyBag` directly. The interface member could be simplified but is not blocking.

---

## Developer Review

**Reviewed:** 2026-03-14
**Verdict:** Approved

### Initial Concern (Resolved)

1. **BLOCKING (resolved):** Cast to `ValidatePropertyManager<IValidateProperty>` fails at runtime for entity objects because `EntityPropertyManager` extends `ValidatePropertyManager<IEntityProperty>` and generic classes are invariant. **Resolution:** Use `IValidatePropertyManagerInternal<out P>` (covariant) which is the established pattern for this cross-class access. Plan updated with exact mechanism.

### Why This Plan Is Approved

The blocking concern about the cast mechanism was the only substantive issue. The architect's resolution is correct:

1. **Covariance confirmed:** `IValidatePropertyManagerInternal<out P>` at line 20 of `ValidatePropertyManager.cs` has the `out` modifier. The cast `this.PropertyManager as IValidatePropertyManagerInternal<IValidateProperty>` succeeds for both `ValidatePropertyManager<IValidateProperty>` and `EntityPropertyManager` (which is `ValidatePropertyManager<IEntityProperty>`) because `IEntityProperty : IValidateProperty` and the interface is covariant.

2. **Established pattern:** This exact cast pattern already exists at ValidateBase lines 353, 411, and 681. All three sites cast to `IValidatePropertyManagerInternal<IValidateProperty>` and work correctly for both validate and entity objects at runtime.

3. **New methods preserve covariance:** Neither `RegisterLazyLoadProperties(object, Type)` nor `RegisterLazyLoadProperty<TInner>(object, string, LazyLoad<TInner>)` uses `P` in any parameter or return position. Adding them to the `out P` interface is safe. The generic method type parameter `TInner` is independent of `P`.

4. **Business rules 1-8 trace correctly to existing code:** The registration logic, reflection cache, polymorphic property creation, reassignment detection, and `LoadValue` call are all direct moves of existing verified code.

### Review Summary

- Files examined: ValidateBase.cs (lines 303-440, 675-690), ValidatePropertyManager.cs (full), EntityPropertyManager.cs (full), IValidatePropertyManager.cs (full), LazyLoadValidateProperty.cs, LazyLoadEntityProperty.cs, IValidateProperty.cs, LazyLoadStatePropagationTests.cs
- Structured checklist: 18 of 18 items checked
- Devil's advocate items: 4 generated, all addressed (2 by existing code behavior, 1 by Risk 1 resolution, 1 by plan's test strategy)

---

## Implementation Contract

**Created:** 2026-03-14
**Approved by:** neatoo-developer

### In Scope

- [ ] Add `RegisterLazyLoadProperties(object, Type)` and `RegisterLazyLoadProperty<TInner>(object, string, LazyLoad<TInner>)` to `IValidatePropertyManagerInternal<out P>` (file: `src/Neatoo/Internal/ValidatePropertyManager.cs`)
- [ ] Add `CreateLazyLoadProperty(Type, IPropertyInfo)` protected virtual method on `ValidatePropertyManager<P>` -- creates `LazyLoadValidateProperty<T>` (file: `src/Neatoo/Internal/ValidatePropertyManager.cs`)
- [ ] Override `CreateLazyLoadProperty` in `EntityPropertyManager` -- creates `LazyLoadEntityProperty<T>` (file: `src/Neatoo/Internal/EntityPropertyManager.cs`)
- [ ] Move `_lazyLoadPropertyCache` and `GetLazyLoadProperties(Type)` from `ValidateBase` to `ValidatePropertyManager` (from: `src/Neatoo/ValidateBase.cs`, to: `src/Neatoo/Internal/ValidatePropertyManager.cs`)
- [ ] Implement `RegisterLazyLoadProperties` on `ValidatePropertyManager` -- absorb full registration loop, use `CreateLazyLoadProperty` polymorphism, access `PropertyBag` directly for reassignment detection
- [ ] Implement `RegisterLazyLoadProperty<TInner>` on `ValidatePropertyManager` -- absorb single-property logic, use `CreateLazyLoadProperty`
- [ ] Reduce `ValidateBase.RegisterLazyLoadProperties()` to thin delegation via `this.PropertyManager as IValidatePropertyManagerInternal<IValidateProperty>` (file: `src/Neatoo/ValidateBase.cs`)
- [ ] Reduce `ValidateBase.RegisterLazyLoadProperty<TInner>()` to thin delegation via same cast
- [ ] Move `[UnconditionalSuppressMessage]` trimming attributes from ValidateBase methods to ValidatePropertyManager methods
- [ ] Checkpoint: `dotnet build src/Neatoo.sln` -- 0 errors
- [ ] Checkpoint: `dotnet test src/Neatoo.sln` -- 0 failures

### Explicitly Out of Scope

- New features or behavioral changes
- Public API changes (protected methods on ValidateBase keep same signatures)
- Test modifications (no tests should need changing)
- Design project updates
- Documentation updates
- Simplifying `TryGetRegisteredProperty` off the internal interface (optional cleanup, not required)

### Verification Gates

1. After adding interface members and virtual/override pattern (Steps 1-3): Solution must build
2. After moving all logic (Steps 4-8): `dotnet build src/Neatoo.sln` succeeds with 0 errors
3. Final: `dotnet test src/Neatoo.sln` passes with 0 failures

### Stop Conditions

If any of these occur, STOP and report:
- Any test failure at all (this is a pure refactoring -- zero failures expected)
- Need to modify any test code to make tests pass
- Need to change public API signatures
- Architectural contradiction discovered (e.g., covariance issue with new interface members)

---

## Agent IDs

- **Architect:** [agent ID from Step 2]
- **Developer:** [agent ID from Step 4]

---

## Implementation Progress

**Started:** 2026-03-14
**Developer:** neatoo-developer

- [x] Step 1: Add `RegisterLazyLoadProperties` and `RegisterLazyLoadProperty<TInner>` to `IValidatePropertyManagerInternal<out P>`
- [x] Step 2-3: Add `CreateLazyLoadProperty` protected virtual on `ValidatePropertyManager<P>`, override on `EntityPropertyManager`
- [x] Step 4: Move `_lazyLoadPropertyCache` and `GetLazyLoadProperties` static cache from `ValidateBase` to `ValidatePropertyManager`
- [x] Step 5-6: Implement `RegisterLazyLoadProperties` and `RegisterLazyLoadProperty<TInner>` on `ValidatePropertyManager` (explicit interface implementations)
- [x] Step 7-8: Reduce `ValidateBase.RegisterLazyLoadProperties()` and `RegisterLazyLoadProperty<TInner>()` to thin delegations via `IValidatePropertyManagerInternal<IValidateProperty>` cast
- [x] Step 9: Move all `[UnconditionalSuppressMessage]` trimming attributes from ValidateBase to ValidatePropertyManager/EntityPropertyManager methods
- [x] Step 10: Build (0 errors, 0 warnings) and test (all pass)

---

## Completion Evidence

**Reported:** 2026-03-14

- **Build:** `dotnet build src/Neatoo.sln` -- 0 errors, 0 warnings
- **Tests Passing:**
  - Neatoo.BaseGenerator.Tests: 26 passed, 0 failed
  - Samples: 250 passed, 0 failed
  - Person.DomainModel.Tests: 55 passed, 0 failed
  - Neatoo.UnitTest: 1780 passed, 0 failed, 1 skipped (pre-existing AsyncFlowTests_CheckAllRules)
  - Total: 2111 passed, 0 failed
- **All Contract Items:** Complete
- **No tests modified**
- **No public API changes** (protected methods on ValidateBase retain same signatures)
- **Files modified:**
  - `src/Neatoo/Internal/ValidatePropertyManager.cs` -- Added interface members, static cache, `CreateLazyLoadProperty` virtual, `RegisterLazyLoadProperties` and `RegisterLazyLoadProperty<TInner>` implementations
  - `src/Neatoo/Internal/EntityPropertyManager.cs` -- Added `CreateLazyLoadProperty` override (creates `LazyLoadEntityProperty<T>`)
  - `src/Neatoo/ValidateBase.cs` -- Reduced LazyLoad region to two thin delegation methods (3 lines each), removed unused `System.Collections.Concurrent` and `System.Reflection` usings

### Structural verification

- `ValidateBase.RegisterLazyLoadProperties()`: 3 lines (if-cast-delegate pattern)
- `ValidateBase.RegisterLazyLoadProperty<TInner>()`: 3 lines (if-cast-delegate pattern)
- `_lazyLoadPropertyCache` and `GetLazyLoadProperties`: no longer in ValidateBase, now in ValidatePropertyManager
- `EntityPropertyManager.CreateLazyLoadProperty`: overrides virtual, creates `LazyLoadEntityProperty<T>` via `MakeGenericType` (no type check)
- `is IEntityPropertyManager` branch: eliminated from all code paths
- Reassignment detection: uses `PropertyBag.TryGetValue` directly (no interface call needed since code now lives inside PropertyManager)

---

## Architect Verification

**Verified:** 2026-03-14
**Verdict:** VERIFIED

**Build/Test Results:**
- `dotnet build src/Neatoo.sln`: 0 errors (independently verified)
- `dotnet test src/Neatoo.sln`: 2111 passed, 0 failed, 1 skipped (independently verified)
  - Neatoo.BaseGenerator.Tests: 26 passed
  - Samples: 250 passed
  - Person.DomainModel.Tests: 55 passed
  - Neatoo.UnitTest: 1780 passed, 1 skipped

**Design Match:** Yes

**Verification details:**
1. ValidateBase.RegisterLazyLoadProperties() -- 3 lines, thin delegation via `IValidatePropertyManagerInternal<IValidateProperty>` cast. Matches plan.
2. ValidateBase.RegisterLazyLoadProperty<TInner>() -- 3 lines, same cast pattern. Matches plan.
3. No `_lazyLoadPropertyCache` or `GetLazyLoadProperties` in ValidateBase -- confirmed via grep.
4. ValidatePropertyManager has `#region LazyLoad Registration` (lines 383-504) with static cache, `GetLazyLoadProperties`, `CreateLazyLoadProperty` virtual, `RegisterLazyLoadProperties` explicit interface impl, `RegisterLazyLoadProperty<TInner>` explicit interface impl.
5. EntityPropertyManager overrides `CreateLazyLoadProperty` (lines 109-113) to create `LazyLoadEntityProperty<T>`. No type check -- pure polymorphism.
6. IValidatePropertyManagerInternal has two new members (lines 40-55). Neither uses `P`, covariance preserved.
7. Trimming suppression attributes moved to correct locations on ValidatePropertyManager and EntityPropertyManager.
8. Reassignment detection uses PropertyBag directly (no cross-class interface cast needed).
9. No test modifications. No public API changes.

**Issues Found:** [list or "None"]

---

## Documentation

**Agent:** N/A (pure internal refactoring, no documentation deliverables)
**Completed:** [date]

### Updates Made

- None expected (internal refactoring only)
