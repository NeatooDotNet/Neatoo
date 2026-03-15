# LazyLoad Value Propagation

**Date:** 2026-03-14
**Related Todo:** [LazyLoad Value Propagation](../todos/lazyload-value-propagation.md)
**Status:** Documentation Complete
**Last Updated:** 2026-03-14

---

## Overview

Make LazyLoad property subclasses present the inner entity (or null) as their `Value` to the rest of the framework, instead of the `LazyLoad<T>` wrapper. Both getter and setter operate on the inner entity level -- the getter returns it, the setter calls a new `LazyLoad<T>.SetValue(T?)` method that bypasses the loader and marks the value as loaded. This eliminates LazyLoad-specific look-through code from ValidateBase and makes the framework uniformly unaware of the lazy-loading nature of these properties. Additionally, replace explicit `RegisterLazyLoadProperties()` calls in ValidateBase lifecycle methods with a generic `FinalizeRegistration` hook on PropertyManager.

---

## Difficulty & Risk Assessment

**Difficulty:** Medium
**Risk:** Medium
**Justification:** The Value override is straightforward -- the inner entity is already accessible via BoxedValue and the LazyLoad subclasses already override every other framework-facing accessor. The new `LazyLoad<T>.SetValue()` method adds API surface but follows the existing `ApplyDeserializedState` pattern. This is a semantic change to what `IValidateProperty.Value` returns AND accepts for LazyLoad properties. The serializer does NOT read PropertyManager.Value for LazyLoad properties (it reads entity fields directly), so serialization is safe. Key virtual dispatch insight from developer review: `PassThruValueNeatooPropertyChanged` and `OnDeserialized` call `this.Value` through virtual dispatch which resolves to the base member (returns LazyLoad wrapper), not the `new` member. This is correct and means event Source is unchanged.

---

## Business Rules

### Value getter

1. WHEN a LazyLoad property has a loaded inner entity, THEN `property.Value` getter (via `IValidateProperty.Value`) RETURNS the inner entity (not the `LazyLoad<T>` wrapper)
2. WHEN a LazyLoad property has no loaded inner entity (null BoxedValue), THEN `property.Value` getter RETURNS null

### Value setter (NEW)

3. WHEN `property.Value` setter is called with a value on a LazyLoad property, THEN the value is set on the underlying `LazyLoad<T>` via `LazyLoad<T>.SetValue(T?)`, marking it as loaded
4. WHEN `property.Value` setter is called with null on a LazyLoad property, THEN the LazyLoad is marked as loaded with a null value (null is a valid loaded state)
5. WHEN `LazyLoad<T>.SetValue(T?)` is called, THEN `_isLoaded` becomes true, `_loadError` is cleared, `_loadTask` is nulled, and PropertyChanged fires for Value/IsLoaded/HasLoadError
6. WHEN `LazyLoad<T>.SetValue(T?)` is called while a load is in progress, THEN the setter wins -- `_isLoaded = true` prevents the in-flight loader from overwriting (LoadAsync checks `_isLoaded` first)
7. WHEN `property.Value` setter is called but the `_value` field (the LazyLoad wrapper) is null, THEN `InvalidOperationException` is thrown (no wrapper to set the value on)

### SetParent propagation

8. WHEN `_PropertyManager_NeatooPropertyChanged` fires for a LazyLoad property with a loaded inner entity, THEN `eventArgs.Property.Value is ISetParent` succeeds directly (no look-through needed)
9. WHEN `_PropertyManager_NeatooPropertyChanged` fires for a LazyLoad property with an unloaded entity, THEN SetParent is not called (Value is null, which is not ISetParent)
10. WHEN `OnDeserialized` iterates properties and encounters a LazyLoad property with a loaded inner entity, THEN `property.Value is ISetParent` succeeds directly
11. WHEN `EntityBase.ChildNeatooPropertyChanged` fires for a LazyLoad property with a loaded entity child, THEN `eventArgs.Property.Value is IEntityBase` succeeds directly

### FinalizeRegistration lifecycle

12. WHEN `FactoryComplete` is called, THEN LazyLoad properties are registered with PropertyManager (via the generic FinalizeRegistration hook, not via LazyLoad-specific method)
13. WHEN `OnDeserialized` completes, THEN LazyLoad properties are registered with PropertyManager (via the generic FinalizeRegistration hook)
14. WHEN ValidateBase calls `FinalizeRegistration`, THEN it does not reference any LazyLoad type

### Unchanged invariants

15. WHEN serialization writes a LazyLoad property, THEN it reads the `LazyLoad<T>` field from the entity via reflection (not from PropertyManager.Value) -- serialization is unaffected
16. WHEN IsBusy is checked on a LazyLoad property, THEN it still delegates to `LazyLoad.IsBusy` (not through `Value`) -- IsBusy reads `_value` field directly
17. WHEN WaitForTasks is called on a LazyLoad property, THEN it still delegates to `LazyLoad.WaitForTasks()` -- reads `_value` field directly
18. WHEN `PassThruValueNeatooPropertyChanged` fires on a LazyLoad property, THEN the event args Source still carries the LazyLoad wrapper (PassThruValueNeatooPropertyChanged is inherited from `ValidateProperty<LazyLoad<T>>` which uses virtual dispatch on `this.Value` -- but this resolves to `ValidateProperty<LazyLoad<T>>.Value` not the `new` member, so Source remains the LazyLoad wrapper). The check at ValidateBase line 429 (`eventArgs.Property == eventArgs.Source`) still correctly identifies pass-through events.
19. WHEN `ValidateProperty<T>.OnDeserialized()` runs for a LazyLoad property subclass, THEN `this.Value` resolves via virtual dispatch to the base `ValidateProperty<LazyLoad<T>>.Value` getter (returns the LazyLoad wrapper), correctly subscribing to its PropertyChanged events

---

## Approach

Three coordinated changes:

**Change 1: Value property override (getter AND setter).** Override the `Value` property on `LazyLoadValidateProperty<T>` and `LazyLoadEntityProperty<T>`. The getter returns `((ILazyLoadDeserializable)_value)?.BoxedValue` (the inner entity or null). The setter delegates to a new `LazyLoad<T>.SetValue(T?)` method that sets the inner value, marks `_isLoaded = true`, clears errors, and fires PropertyChanged events. This replaces the current `throw InvalidOperationException` with a meaningful operation. The explicit `IValidateProperty.Value` interface implementation on `ValidateProperty<T>` (line 45: `object? IValidateProperty.Value { get => this.Value; set => this.SetValue(value); }`) dispatches through the overridden `SetValue` method.

**Change 2: Remove SetParent look-through.** Delete lines 435-440 of ValidateBase._PropertyManager_NeatooPropertyChanged (the `else if (eventArgs.Property.Value is ILazyLoadDeserializable ll ...)` branch). The first branch (`eventArgs.Property.Value is ISetParent child`) now catches LazyLoad properties directly because `Value` returns the inner entity.

**Change 3: Generic FinalizeRegistration lifecycle hook.** Add `void FinalizeRegistration(object owner, Type concreteType)` to `IValidatePropertyManagerInternal`. Rename the existing `RegisterLazyLoadProperties` implementation to `FinalizeRegistration` (it is the only thing that needs to happen at this lifecycle point). ValidateBase calls `FinalizeRegistration` in FactoryComplete and OnDeserialized instead of `RegisterLazyLoadProperties`. The protected `RegisterLazyLoadProperties()` and `RegisterLazyLoadProperty<TInner>()` methods on ValidateBase stay as thin delegations for user code (custom property setters).

---

## Design

### Change 1: Value property (getter and setter) on LazyLoad subclasses

**New public method on `LazyLoad<T>`:**

```csharp
// LazyLoad<T> -- new public method
public void SetValue(T? value)
{
    UnsubscribeFromValuePropertyChanged(_value);
    _value = value;
    _isLoaded = true;
    _loadError = null;
    _loadTask = null;  // Clear any pending load reference
    SubscribeToValuePropertyChanged(_value);
    OnPropertyChanged(nameof(Value));
    OnPropertyChanged(nameof(IsLoaded));
    OnPropertyChanged(nameof(HasLoadError));
}
```

This method bypasses the loader delegate and directly sets the inner value, marking the LazyLoad as loaded. Null is a valid loaded state. The loader delegate is preserved (not cleared) in case a future "reload" scenario is needed.

**Value property on LazyLoad subclasses:**

```csharp
// LazyLoadValidateProperty<T> -- getter returns inner entity, setter delegates to LazyLoad.SetValue
public new object? Value
{
    get => ((ILazyLoadDeserializable?)this._value)?.BoxedValue;
    set
    {
        if (this._value == null)
            throw new InvalidOperationException("Cannot set value: no LazyLoad wrapper is assigned.");
        this._value.SetValue((T?)value);
    }
}

// LazyLoadEntityProperty<T> -- identical
public new object? Value
{
    get => ((ILazyLoadDeserializable?)this._value)?.BoxedValue;
    set
    {
        if (this._value == null)
            throw new InvalidOperationException("Cannot set value: no LazyLoad wrapper is assigned.");
        this._value.SetValue((T?)value);
    }
}
```

The `new` keyword is needed because `ValidateProperty<T>.Value` is virtual and typed as `T?` (which is `LazyLoad<T>?`), but we need `object?` for the inner entity. The LazyLoad subclasses already re-declare `IValidateProperty` to force interface re-implementation, so `IValidateProperty.Value` dispatches to this `new` member when accessed through the interface.

**SetValue override:** The existing `override Task SetValue(object? newValue)` that currently throws `InvalidOperationException` is updated to delegate to `LazyLoad<T>.SetValue()` as well. This is the path used by `IValidateProperty.SetValue()` and the property setter infrastructure.

```csharp
public override Task SetValue(object? newValue)
{
    if (this._value == null)
        throw new InvalidOperationException("Cannot set value: no LazyLoad wrapper is assigned.");
    this._value.SetValue((T?)newValue);
    return Task.CompletedTask;
}
```

**Critical:** The `_value` field itself remains `LazyLoad<T>`. The `Value` getter presents the inner entity view. The setter routes through to `LazyLoad<T>.SetValue()`. All internal logic that needs the LazyLoad wrapper (IsBusy, WaitForTasks, PassThruValuePropertyChanged, HandleNonNullValue, LoadValue) already accesses `_value` directly, not through `Value`.

**The `ValueIsValidateBase` fallback** at `LazyLoadValidateProperty<T>` line 130 (`return this._value as IValidateMetaProperties`) still needs the LazyLoad wrapper for error state propagation when the inner entity is not loaded. This reads `_value` directly and is unaffected.

**Event flow after SetValue:** When `LazyLoad<T>.SetValue()` fires `PropertyChanged("Value")`, the `PassThruValuePropertyChanged` handler on the LazyLoad property subclass catches it, runs disconnect/reconnect on the inner child, and fires `NeatooPropertyChanged`. This triggers `_PropertyManager_NeatooPropertyChanged` which calls `SetParent` on the new value. The existing event plumbing handles everything automatically.

### Change 2: Remove SetParent look-through

ValidateBase._PropertyManager_NeatooPropertyChanged becomes:

```csharp
if (eventArgs.Property != null && eventArgs.Property == eventArgs.Source)
{
    if (eventArgs.Property.Value is ISetParent child)
    {
        child.SetParent(this);
    }

    this.RaisePropertyChanged(eventArgs.FullPropertyName);
}
```

The `ILazyLoadDeserializable` import and the else-if branch are removed.

### Change 3: FinalizeRegistration lifecycle hook

**On `IValidatePropertyManagerInternal`:** Rename `RegisterLazyLoadProperties` to `FinalizeRegistration` (same signature: `void FinalizeRegistration(object owner, Type concreteType)`).

**On `ValidatePropertyManager`:** Rename the explicit interface implementation from `IValidatePropertyManagerInternal<P>.RegisterLazyLoadProperties` to `IValidatePropertyManagerInternal<P>.FinalizeRegistration`. The implementation body is unchanged -- it still discovers LazyLoad properties via reflection and registers them.

**On ValidateBase:** The two lifecycle call sites change:
- `OnDeserialized` line 585: `this.RegisterLazyLoadProperties()` becomes a call through the internal interface to `FinalizeRegistration`
- `FactoryComplete` line 1011: Same change

The existing `protected void RegisterLazyLoadProperties()` on ValidateBase is updated to delegate to `FinalizeRegistration` instead of `RegisterLazyLoadProperties` on the interface. It stays as a protected method for user code (custom property setters still call it).

The `protected void RegisterLazyLoadProperty<TInner>(...)` method stays unchanged -- it is the single-property explicit registration that users call from setters. It delegates to `RegisterLazyLoadProperty` on the internal interface, which is not renamed (it is a distinct operation from the bulk finalization).

### What moves where

| Code | From | To |
|------|------|----|
| `Value` property (getter + setter) | Not present | `LazyLoadValidateProperty<T>`, `LazyLoadEntityProperty<T>` |
| `LazyLoad<T>.SetValue(T?)` method | Not present | New public method on `LazyLoad<T>` |
| `SetValue(object?)` override | Throws `InvalidOperationException` | Delegates to `LazyLoad<T>.SetValue()` |
| SetParent look-through (lines 435-440) | `ValidateBase._PropertyManager_NeatooPropertyChanged` | Deleted |
| `RegisterLazyLoadProperties` on interface | `IValidatePropertyManagerInternal` | Renamed to `FinalizeRegistration` |
| `RegisterLazyLoadProperties` impl | `ValidatePropertyManager` explicit interface impl | Renamed to `FinalizeRegistration` |
| `RegisterLazyLoadProperties()` calls in OnDeserialized/FactoryComplete | Direct call to protected method | Delegation through internal interface to `FinalizeRegistration` |

### What stays unchanged

- `LazyLoadValidateProperty._value` field (still holds `LazyLoad<T>`)
- `IsBusy`, `WaitForTasks`, `ValueIsValidateBase`, `ValueAsBase` overrides (read `_value` directly)
- `PassThruValueNeatooPropertyChanged` (inherited from base, uses virtual dispatch which resolves to base `Value` getter -- returns LazyLoad wrapper as Source)
- `PassThruValuePropertyChanged` handler (reads from event source parameter)
- `HandleNonNullValue`, `HandleNullValue`, `LoadValue` (operate on `_value`)
- `ValidateProperty<T>.OnDeserialized()` (virtual dispatch on `this.Value` returns LazyLoad wrapper -- correct for event subscription)
- Serialization (`NeatooBaseJsonTypeConverter` reads entity fields, not PropertyManager.Value)
- `RegisterLazyLoadProperty<TInner>` on the internal interface (not renamed)
- Protected `RegisterLazyLoadProperty<TInner>()` on ValidateBase (user API, unchanged)
- `LazyLoad<T>._loader` delegate (preserved by SetValue, not cleared)

---

## Implementation Steps

1. **Add `SetValue(T?)` public method** to `LazyLoad<T>` -- sets inner value, marks loaded, clears errors, fires PropertyChanged
2. **Override `Value` property (getter + setter)** on `LazyLoadValidateProperty<T>` -- getter returns inner entity via BoxedValue, setter delegates to `_value.SetValue()`
3. **Override `Value` property (getter + setter)** on `LazyLoadEntityProperty<T>` -- same implementation
4. **Update `SetValue(object?)` override** on both LazyLoad property subclasses -- delegate to `LazyLoad<T>.SetValue()` instead of throwing
5. **Remove SetParent look-through** from `ValidateBase._PropertyManager_NeatooPropertyChanged` -- delete lines 435-440 (the `else if (... is ILazyLoadDeserializable ...)` branch and its comment)
6. **Rename interface method** on `IValidatePropertyManagerInternal` -- `RegisterLazyLoadProperties` becomes `FinalizeRegistration` (same parameters)
7. **Rename implementation** on `ValidatePropertyManager` -- explicit interface impl renamed to match
8. **Update ValidateBase protected method** `RegisterLazyLoadProperties()` -- delegate to `FinalizeRegistration` on the internal interface instead of `RegisterLazyLoadProperties`
9. **Update FactoryComplete and OnDeserialized** -- call through internal interface to `FinalizeRegistration` directly (instead of going through the protected `RegisterLazyLoadProperties()` method)
10. **Build and run all tests** -- zero failures expected

---

## Acceptance Criteria

- [ ] `LazyLoad<T>.SetValue(T?)` exists as a public method and sets value, marks loaded, clears errors, fires PropertyChanged
- [ ] `LazyLoadValidateProperty<T>.Value` getter returns inner entity (or null), not `LazyLoad<T>` wrapper
- [ ] `LazyLoadValidateProperty<T>.Value` setter delegates to `LazyLoad<T>.SetValue()`
- [ ] `LazyLoadEntityProperty<T>.Value` getter returns inner entity (or null), not `LazyLoad<T>` wrapper
- [ ] `LazyLoadEntityProperty<T>.Value` setter delegates to `LazyLoad<T>.SetValue()`
- [ ] `SetValue(object?)` override no longer throws -- delegates to `LazyLoad<T>.SetValue()`
- [ ] No `LazyLoad`-specific look-through code in `_PropertyManager_NeatooPropertyChanged`
- [ ] `IValidatePropertyManagerInternal` has `FinalizeRegistration` (not `RegisterLazyLoadProperties`)
- [ ] `FactoryComplete` and `OnDeserialized` call `FinalizeRegistration` without referencing LazyLoad
- [ ] Protected `RegisterLazyLoadProperties()` on ValidateBase still works for user custom setters
- [ ] Protected `RegisterLazyLoadProperty<TInner>()` on ValidateBase still works
- [ ] Serialization round-trip works (no behavioral change)
- [ ] `dotnet build src/Neatoo.sln` succeeds with 0 errors
- [ ] `dotnet test src/Neatoo.sln` passes with 0 failures
- [ ] No test modifications needed (behavior-preserving from the framework consumer perspective)

---

## Risks / Considerations

1. **`new` keyword vs virtual override for Value.** `ValidateProperty<T>.Value` is `virtual T? Value`, but we need `object? Value` on the LazyLoad subclasses. The `new` keyword hides the base member. This works because: (a) the LazyLoad subclasses already re-declare `IValidateProperty` to force interface re-implementation, so `IValidateProperty.Value` dispatches to the `new` member; (b) no code accesses `Value` through the concrete `LazyLoadValidateProperty<T>` type -- it is always accessed through `IValidateProperty` or `IEntityProperty` interface references.

2. **~~PassThruValueNeatooPropertyChanged carries different Source~~ RESOLVED.** `PassThruValueNeatooPropertyChanged` is defined on `ValidateProperty<T>` as `virtual` and calls `this.Value!` which uses virtual dispatch. Since `LazyLoadValidateProperty<T>` does NOT override `PassThruValueNeatooPropertyChanged`, the base implementation runs. The base `ValidateProperty<LazyLoad<T>>` virtual `Value` getter returns `_value` (the LazyLoad wrapper) -- the `new` keyword on the subclass hides but does not override. So `this.Value!` in the base method still returns the LazyLoad wrapper as Source. The event args Source is unchanged. No behavioral difference.

3. **OnDeserialized SetParent loop (line 576-582).** This loop iterates all properties and calls `property.Value is ISetParent`. After the Value change, LazyLoad properties return the inner entity directly. If the inner entity implements ISetParent, SetParent is called. If not loaded yet (null BoxedValue), Value returns null and SetParent is skipped. This matches the current behavior (currently the LazyLoad wrapper does not implement ISetParent, so it falls through).

4. **FinalizeRegistration naming.** The method is still internally about LazyLoad discovery, but the name is generic. This is intentional -- if future registration needs arise (e.g., other property subclass types), FinalizeRegistration is the right hook. The name accurately describes _when_ it runs (at lifecycle finalization), not _what_ it does internally.

5. **User code calling RegisterLazyLoadProperties().** The protected method on ValidateBase stays for backward compatibility. User code in custom property setters (e.g., `Person.cs` line 60) calls it and does not need to change. The method now delegates to `FinalizeRegistration` internally.

6. **Race condition: SetValue while load in progress.** If `SetValue()` is called while `LoadAsyncCore` is running, `SetValue` sets `_isLoaded = true` immediately. When `LoadAsyncCore` completes, it also sets `_isLoaded = true` and fires PropertyChanged. The in-flight load may overwrite `_value` with the loader's result. To handle this cleanly, `SetValue` nulls `_loadTask`. However, the in-flight async operation cannot be cancelled (it is already running). The developer should document that calling `SetValue` during an active load is a fire-and-forget race where the last write wins. In practice, this is an edge case -- SetValue is intended for the "bypass the loader" pattern, which typically happens before a load is triggered.

7. **ValidateProperty<T>.OnDeserialized virtual dispatch.** `ValidateProperty<T>.OnDeserialized()` (line 353-362) uses `this.Value` to subscribe to PropertyChanged events. For LazyLoad subclasses, `this.Value` resolves via virtual dispatch to `ValidateProperty<LazyLoad<T>>.Value` (the base virtual member, not the `new` member on the subclass). This correctly returns the LazyLoad wrapper, which is what OnDeserialized needs to subscribe to. The `new` keyword on the subclass does not interfere with virtual dispatch from the base class.

---

## Developer Review

**Reviewed:** 2026-03-14
**Verdict:** Approved

### Review Summary

- Files examined: ValidateBase.cs, LazyLoadValidateProperty.cs, LazyLoadEntityProperty.cs, ValidateProperty.cs, IValidatePropertyManager.cs, ValidatePropertyManager.cs, EntityPropertyManager.cs (EntityProperty<T>), EntityBase.cs (ChildNeatooPropertyChanged), IValidateProperty.cs, IEntityProperty.cs, LazyLoad.cs, NeatooBaseJsonTypeConverter.cs
- Tests examined: LazyLoadStatePropagationTests.cs, FatClientLazyLoadTests.cs
- Design project examined: Design.Domain/PropertySystem/LazyLoadProperty.cs
- Checklist questions checked: 17 of 17
- Devil's advocate items: 3 generated, all addressed by the plan

### Why This Plan Is Approved

First review identified four concerns (Value getter-only vs get/set interface, Business Rule 13 inaccuracy, OnDeserialized virtual dispatch undocumented, import cleanup step). All four have been resolved in this revision:

1. Value property now has both getter and setter -- setter delegates to new `LazyLoad<T>.SetValue(T?)`. This satisfies `IValidateProperty.Value { get; set; }` when interface re-mapping occurs.
2. Business Rule 18 (was 13) correctly states PassThruValueNeatooPropertyChanged uses virtual dispatch and Source still carries the LazyLoad wrapper.
3. Business Rule 19 and Risk 7 document `ValidateProperty<T>.OnDeserialized()` virtual dispatch behavior.
4. Import cleanup step removed (using Neatoo.Internal is needed for other types).

The `new` keyword approach for Value is consistent with the existing pattern used by IsBusy, WaitForTasks, and ValueAsBase on these same classes. The new `LazyLoad<T>.SetValue(T?)` method follows the existing `ApplyDeserializedState` pattern but is public and semantically clear.

### Previous Concerns (All Resolved)

1. **Value getter/setter (was BLOCKING)** -- Fixed: `public new object? Value { get; set; }` with both accessors
2. **Business Rule 13 accuracy** -- Fixed: Rule 18 correctly describes virtual dispatch behavior
3. **OnDeserialized virtual dispatch** -- Fixed: Rule 19 and Risk 7 document this
4. **Import cleanup step** -- Fixed: Step removed

---

## Implementation Contract

**Created:** 2026-03-14
**Approved by:** neatoo-developer

### In Scope

- [ ] **Step 1:** Add `LazyLoad<T>.SetValue(T?)` public method to `src/Neatoo/LazyLoad.cs` -- sets inner value, marks loaded, clears errors/loadTask, fires PropertyChanged
- [ ] **Step 2:** Add `public new object? Value { get; set; }` on `LazyLoadValidateProperty<T>` in `src/Neatoo/Internal/LazyLoadValidateProperty.cs` -- getter returns inner entity via BoxedValue, setter delegates to `_value.SetValue()`
- [ ] **Step 3:** Add `public new object? Value { get; set; }` on `LazyLoadEntityProperty<T>` in `src/Neatoo/Internal/LazyLoadEntityProperty.cs` -- same implementation
- [ ] **Step 4:** Update `SetValue(object?)` override on both LazyLoad property subclasses -- delegate to `LazyLoad<T>.SetValue()` instead of throwing
- [ ] **Checkpoint:** `dotnet build src/Neatoo.sln` succeeds
- [ ] **Step 5:** Remove SetParent look-through (lines 435-440) from `ValidateBase._PropertyManager_NeatooPropertyChanged` in `src/Neatoo/ValidateBase.cs`
- [ ] **Checkpoint:** `dotnet build src/Neatoo.sln` succeeds
- [ ] **Step 6:** Rename `RegisterLazyLoadProperties` to `FinalizeRegistration` on `IValidatePropertyManagerInternal` in `src/Neatoo/Internal/ValidatePropertyManager.cs`
- [ ] **Step 7:** Rename explicit interface implementation on `ValidatePropertyManager` in same file
- [ ] **Step 8:** Update `ValidateBase.RegisterLazyLoadProperties()` protected method to delegate to `FinalizeRegistration` instead of `RegisterLazyLoadProperties`
- [ ] **Step 9:** Update `FactoryComplete` and `OnDeserialized` in `ValidateBase.cs` to call `FinalizeRegistration` through internal interface
- [ ] **Checkpoint:** `dotnet build src/Neatoo.sln` succeeds
- [ ] **Final:** `dotnet test src/Neatoo.sln` passes with 0 failures and 0 test modifications

### Explicitly Out of Scope

- Removing ILazyLoadFactory
- Removing protected RegisterLazyLoadProperties/RegisterLazyLoadProperty from ValidateBase user API
- Serialization changes
- New tests (existing tests cover all paths)
- Design project updates (internal refactoring, no API surface change)
- Documentation updates (internal refactoring only)

### Verification Gates

1. After Steps 1-4 (Value property changes): `dotnet build src/Neatoo.sln` succeeds -- confirms the `new` keyword, interface re-mapping, and LazyLoad.SetValue compile correctly
2. After Step 5 (SetParent removal): `dotnet build src/Neatoo.sln` succeeds -- confirms no other code depends on the removed branch
3. After Steps 6-9 (FinalizeRegistration): `dotnet build src/Neatoo.sln` succeeds -- confirms all rename references updated
4. Final: `dotnet test src/Neatoo.sln` passes with 0 failures -- confirms behavior-preserving change

### Stop Conditions

If any of these occur, STOP and report:
- Any test failure (this should be behavior-preserving for all consumers)
- Need to modify test code to make tests pass
- Serialization round-trip failure
- Compilation error in consumer code (Person.cs, Design projects, samples, etc.)
- Out-of-scope test fails
- Architectural contradiction discovered

---

## Agent IDs

- **Architect:** [agent ID from Step 2]
- **Developer:** [agent ID from Step 4]

---

## Implementation Progress

**Started:** 2026-03-14
**Developer:** neatoo-developer

- [x] Step 1: Add LazyLoad<T>.SetValue(T?) method -- added to `src/Neatoo/LazyLoad.cs`
- [x] Step 2-4: Value property overrides and SetValue delegation on LazyLoad subclasses -- added to `LazyLoadValidateProperty.cs` and `LazyLoadEntityProperty.cs`
- [x] Checkpoint: `dotnet build src/Neatoo.sln` -- 0 errors
- [x] Step 5: Remove SetParent look-through from ValidateBase -- deleted lines 435-440
- [x] Checkpoint: `dotnet build src/Neatoo.sln` -- 0 errors
- [x] Step 6-7: Rename interface method and implementation to FinalizeRegistration -- renamed on `IValidatePropertyManagerInternal` and `ValidatePropertyManager`
- [x] Step 8-9: Update ValidateBase lifecycle calls -- `FactoryComplete` and `OnDeserialized` call `FinalizeRegistration` through internal interface; protected `RegisterLazyLoadProperties()` delegates to `FinalizeRegistration`
- [x] Checkpoint: `dotnet build src/Neatoo.sln` -- 0 errors
- [x] Step 10: `dotnet test src/Neatoo.sln` -- 2,111 tests: 0 failed, 1 skipped (pre-existing), 0 test modifications

---

## Completion Evidence

**Reported:** 2026-03-14

### Test Results

- **Neatoo.BaseGenerator.Tests:** 26 passed, 0 failed
- **Samples:** 250 passed, 0 failed
- **Person.DomainModel.Tests:** 55 passed, 0 failed
- **Neatoo.UnitTest:** 1,780 passed, 0 failed, 1 skipped (pre-existing `AsyncFlowTests_CheckAllRules`)
- **Total: 2,111 passed, 0 failed, 0 test modifications**

### Build Results

- `dotnet build src/Neatoo.sln` -- 0 errors, 465 warnings (all pre-existing)
- `dotnet build src/Design/Design.sln` -- 101 errors (all pre-existing NF0105 analyzer errors, none related to our changes)

### All Contract Items

- [x] Add `LazyLoad<T>.SetValue(T?)` public method
- [x] Override Value property (getter + setter) on both LazyLoad property subclasses
- [x] Update `SetValue(object?)` override to delegate instead of throw
- [x] Remove SetParent look-through from ValidateBase
- [x] Rename RegisterLazyLoadProperties to FinalizeRegistration on interface and implementation
- [x] Update ValidateBase lifecycle calls to use FinalizeRegistration
- [x] All existing tests passing unchanged

### Files Modified

1. `src/Neatoo/LazyLoad.cs` -- Added `SetValue(T?)` public method
2. `src/Neatoo/Internal/LazyLoadValidateProperty.cs` -- Added `public new object? Value { get; set; }`, updated `SetValue(object?)` override
3. `src/Neatoo/Internal/LazyLoadEntityProperty.cs` -- Added `public new object? Value { get; set; }`, updated `SetValue(object?)` override
4. `src/Neatoo/ValidateBase.cs` -- Removed SetParent look-through (lines 435-440), updated `FactoryComplete` and `OnDeserialized` to call `FinalizeRegistration`, updated `RegisterLazyLoadProperties()` delegation
5. `src/Neatoo/Internal/ValidatePropertyManager.cs` -- Renamed `RegisterLazyLoadProperties` to `FinalizeRegistration` on interface and implementation

### Stop Conditions

- No test failures occurred
- No test modifications needed
- No serialization round-trip failures
- No compilation errors in consumer code
- No out-of-scope test failures
- No architectural contradictions discovered

---

## Architect Verification

**Verified:** 2026-03-14
**Verdict:** VERIFIED

**Build/Test Results:**
- `dotnet build src/Neatoo.sln`: 0 errors (independently verified)
- `dotnet test src/Neatoo.sln`: 2111 passed, 0 failed, 1 skipped (independently verified, two runs)
  - Run 1: 1 flaky failure in `Samples.AsyncSamplesTests.IsBusy_TracksAsyncOperationState` -- passes in isolation and on re-run (async timing issue, not related to changes)
  - Run 2: 0 failures across all 4 test projects
  - Neatoo.BaseGenerator.Tests: 26 passed
  - Samples: 250 passed
  - Person.DomainModel.Tests: 55 passed
  - Neatoo.UnitTest: 1780 passed, 1 skipped

**Design Match:** Yes

**Verification details:**

1. **`LazyLoad<T>.SetValue(T?)`** -- Exists at `LazyLoad.cs` line 162. Correctly sets `_value`, `_isLoaded = true`, clears `_loadError` and `_loadTask`, fires PropertyChanged for Value/IsLoaded/HasLoadError. Unsubscribes from old value, subscribes to new. Matches plan.

2. **`LazyLoadValidateProperty<T>.Value` getter** -- `LazyLoadValidateProperty.cs` line 127. Returns `((ILazyLoadDeserializable?)this._value)?.BoxedValue`. Matches plan.

3. **`LazyLoadValidateProperty<T>.Value` setter** -- Line 128-133. Throws if `_value` is null, delegates to `_value.SetValue((T?)value)`. Matches plan.

4. **`LazyLoadEntityProperty<T>.Value` getter** -- `LazyLoadEntityProperty.cs` line 55. Same implementation as validate variant. Matches plan.

5. **`LazyLoadEntityProperty<T>.Value` setter** -- Lines 56-61. Same implementation. Matches plan.

6. **`SetValue(object?)` override (validate)** -- `LazyLoadValidateProperty.cs` line 199. Delegates to `_value.SetValue()` instead of throwing. Matches plan.

7. **`SetValue(object?)` override (entity)** -- `LazyLoadEntityProperty.cs` line 132. Same delegation. Matches plan.

8. **SetParent look-through removed** -- Grep for `ILazyLoadDeserializable` in ValidateBase: zero matches. `_PropertyManager_NeatooPropertyChanged` at line 427 has only the direct `is ISetParent` check. Matches plan.

9. **FinalizeRegistration on internal interface** -- `ValidatePropertyManager.cs` line 47: `void FinalizeRegistration(object owner, Type concreteType)`. No `RegisterLazyLoadProperties` on the interface. Matches plan.

10. **FinalizeRegistration implementation** -- `ValidatePropertyManager.cs` line 434: explicit interface impl `IValidatePropertyManagerInternal<P>.FinalizeRegistration`. Matches plan.

11. **ValidateBase lifecycle calls** -- Lines 578, 1008: `pmInternal.FinalizeRegistration(this, GetType())` in OnDeserialized and FactoryComplete. Line 315: protected `RegisterLazyLoadProperties()` delegates to `FinalizeRegistration`. Matches plan.

12. **ValidateBase zero LazyLoad logic** -- Only remaining LazyLoad references are the two thin protected methods (`RegisterLazyLoadProperties`, `RegisterLazyLoadProperty<TInner>`) and their XML doc comments. Zero LazyLoad-specific logic in event handlers, lifecycle methods, or framework plumbing. Matches plan goal.

13. **Virtual dispatch correctness** -- Both LazyLoad property subclasses document in XML comments (lines 120-123 in validate, lines 48-51 in entity) that `PassThruValueNeatooPropertyChanged` and `OnDeserialized` use virtual dispatch which resolves to the base `Value` getter (returns LazyLoad wrapper). The `new` keyword does not interfere. Matches corrected business rules 18-19.

**Issues Found:** None

---

## Documentation

**Agent:** N/A (internal refactoring, no documentation deliverables)
**Completed:** [date]

### Updates Made

- None expected (internal refactoring only)
