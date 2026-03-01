# Meta Property Recalculation After ResumeAllActions

**Date:** 2026-03-01
**Related Todo:** [Meta Property Recalculation After ResumeAllActions](../todos/meta-property-recalculation-on-resume.md)
**Status:** Ready for Implementation
**Last Updated:** 2026-03-01 (Architecture investigation complete -- Option C confirmed, Option D rejected, red-green testing strategy added)

---

## Overview

When a parent entity is paused during factory operations, child entities that go through independent factory lifecycles can become modified or invalid. The parent's property managers receive `PropertyChanged` events from children but **drop them** because they are paused. When `ResumeAllActions()` is called, cached meta property values (`IsModified`, `IsSelfModified`, `IsValid`, `IsSelfValid`) are never recalculated, leaving the parent with stale state.

This affects `EntityPropertyManager` and `ValidatePropertyManager` but NOT `EntityListBase` or `ValidateListBase`, which already recalculate on resume (fixed in v10.7.1).

---

## Approach

Two changes:

1. **Fix the root cause:** Refactor `EntityPropertyManager` to use `override` instead of implicit `new` for `PauseAllActions()` and `ResumeAllActions()`, and remove EPM's hidden `IsPaused` property. This fixes the interface dispatch mismatch where VPM's `IsPaused` was never set for EntityBase objects.

2. **Add recalculation:** Add recalculation logic to both `ValidatePropertyManager.ResumeAllActions()` and `EntityPropertyManager.ResumeAllActions()` (now an override), matching the pattern already used by the list base classes and `OnDeserialized()`.

---

## Design

### Revision History

This is a revised design that corrects the interface dispatch analysis from the original plan. The developer review identified that the original plan's analysis of how `ValidateBase.ResumeAllActions()` dispatches to the property manager was **wrong**. The correction changes the implementation approach from "fix VPM and EPM independently" to "fix the inheritance hierarchy first, then add recalculation."

### Root Cause Analysis

**The problem exists at two layers:**

1. **Missing recalculation on resume.** Both `ValidatePropertyManager.ResumeAllActions()` and `EntityPropertyManager.ResumeAllActions()` do not recalculate cached meta properties (IsValid, IsSelfValid, IsBusy, IsModified, IsSelfModified) from property state. Events received while paused are dropped.

2. **Broken inheritance: EPM hides VPM methods with implicit `new`.** `EntityPropertyManager` declares `PauseAllActions()`, `ResumeAllActions()`, and `IsPaused` without `override`, hiding `ValidatePropertyManager`'s virtual methods. Due to C# interface re-implementation (`EntityPropertyManager : ValidatePropertyManager<IEntityProperty>, IEntityPropertyManager`), ALL interface calls to these methods -- even through `IValidatePropertyManager<IValidateProperty>` -- dispatch to EPM's `new` methods, never to VPM's virtual methods.

**Both `OnDeserialized()` methods DO recalculate:**
- `ValidatePropertyManager.OnDeserialized()` (line 236-237): `this.IsValid = !this.PropertyBag.Any(p => !p.Value.IsValid); this.IsSelfValid = ...`
- `EntityPropertyManager.OnDeserialized()` (line 185-186): `this.IsModified = this.PropertyBag.Any(p => p.Value.IsModified); this.IsSelfModified = ...`

**The list base classes ALREADY recalculate on resume (added in v10.7.1):**
- `ValidateListBase.ResumeAllActions()` recalculates `_cachedIsValid` and `_cachedIsBusy`
- `EntityListBase.ResumeAllActions()` recalculates `_cachedChildrenModified`

### Interface Dispatch Issue (Corrected)

**Developer's finding (confirmed correct):** Due to C# interface re-implementation, `EntityPropertyManager.ResumeAllActions()` is ALWAYS called when accessing any `IValidatePropertyManager<...>` interface reference that points to an EPM object. `ValidatePropertyManager.ResumeAllActions()` is NEVER called for EntityBase objects. The same applies to `PauseAllActions()` and `IsPaused`.

**Evidence chain:**

1. `EntityPropertyManager` class declaration:
   ```csharp
   public class EntityPropertyManager : ValidatePropertyManager<IEntityProperty>, IEntityPropertyManager
   ```

2. `IEntityPropertyManager` extends `IValidatePropertyManager<IEntityProperty>`:
   ```csharp
   public interface IEntityPropertyManager : IValidatePropertyManager<IEntityProperty>
   ```

3. Since `EntityPropertyManager` re-implements `IEntityPropertyManager` (which includes `IValidatePropertyManager<IEntityProperty>`), C# interface re-implementation applies. The CLR maps ALL interface methods to EPM's implementations, including `PauseAllActions()` and `ResumeAllActions()`.

4. `IValidatePropertyManager<out P>` is covariant, so `ValidateBase.PropertyManager` (typed as `IValidatePropertyManager<IValidateProperty>`) can hold an `EntityPropertyManager` reference. Interface calls through this covariant reference still dispatch to EPM's `new` methods due to re-implementation.

**Consequences of this dispatch behavior (current bugs):**

- **VPM's `IsPaused` is never set to `true`** for EntityBase objects. EPM has its own `IsPaused` (`{ get; private set; }`) that hides VPM's `IsPaused` (`{ get; protected set; }`). Only EPM's IsPaused is set during PauseAllActions.

- **VPM's `Property_PropertyChanged` processes events during pause.** EPM's `Property_PropertyChanged` (which IS an override) calls `base.Property_PropertyChanged()` when EPM is paused. VPM's handler checks `this.IsPaused` -- but since VPM's IsPaused is always false, VPM proceeds to recalculate IsValid/IsSelfValid and raise PropertyChanged events even though the manager should be paused. This is a secondary bug that has gone unnoticed because the entity-level `RaisePropertyChanged` (in ValidateBase) separately checks `ValidateBase.IsPaused` and suppresses outward propagation.

### Option Analysis

Three options were evaluated:

**(A) EPM recalculates all 5 properties itself.** Self-contained but duplicates VPM logic, leaves VPM.IsPaused permanently false for EPM objects, and does not fix the secondary event-processing-during-pause bug.

**(B) EPM calls base.ResumeAllActions() first.** Cleaner inheritance but still uses `new` methods, still has the interface re-implementation issue as a latent source of confusion.

**(C) Refactor EPM to use `override` instead of `new`.** Fixes the root cause. All dispatch works correctly through both inheritance and interfaces. VPM's IsPaused is properly maintained. base calls propagate correctly. Fixes both the recalculation bug and the secondary event-processing-during-pause bug. The larger change, but the most correct.

**Decision: Option C.** It is the most architecturally correct approach. The change is contained to two files (EntityPropertyManager.cs and ValidatePropertyManager.cs) and eliminates a class of subtle dispatch bugs rather than working around them.

### What Changes in EntityPropertyManager (Option C)

1. **Remove** `public bool IsPaused { get; private set; } = false;` -- use VPM's `IsPaused` instead
2. **Change** `PauseAllActions()` from implicit `new` to `override`
3. **Change** `ResumeAllActions()` from implicit `new` to `override`
4. **Add** `IsPaused` guards matching VPM's pattern
5. **Add** IsModified/IsSelfModified recalculation to ResumeAllActions

### What Changes in ValidatePropertyManager

1. **Add** IsValid, IsSelfValid, IsBusy recalculation to ResumeAllActions()

### What Does NOT Change

- `CreateProperty<PV>` -- intentionally `new` because EPM creates EntityProperty instances. Called via reflection, so `new` works correctly.
- `Property_PropertyChanged` -- already uses `override`.
- `OnDeserialized` -- already uses `override`.
- `EntityBase.PauseAllActions()` / `EntityBase.ResumeAllActions()` -- these call `this.PropertyManager.PauseAllActions()` / `this.PropertyManager.ResumeAllActions()` explicitly. With Option C, the calls in `ValidateBase` already dispatch correctly to EPM's override, so EntityBase's explicit calls become redundant NO-OPs. We leave them in place for now; they are harmless and can be cleaned up in a future pass.
- `EntityListBase` / `ValidateListBase` -- already fixed in v10.7.1.
- No rule re-execution on resume.
- No change to the `using (PauseAllActions())` batch-load semantics.

### Proposed Code Changes

#### 1. ValidatePropertyManager.ResumeAllActions() -- add recalculation

```csharp
public virtual void ResumeAllActions()
{
    if (this.IsPaused)
    {
        this.IsPaused = false;

        // Recalculate cached validity from current property state.
        // Events received while paused were dropped, so caches may be stale.
        var wasValid = this.IsValid;
        this.IsValid = !this.PropertyBag.Any(p => !p.Value.IsValid);
        if (wasValid != this.IsValid)
        {
            RaisePropertyChanged(nameof(IsValid));
        }

        var wasSelfValid = this.IsSelfValid;
        this.IsSelfValid = !this.PropertyBag.Any(p => !p.Value.IsSelfValid);
        if (wasSelfValid != this.IsSelfValid)
        {
            RaisePropertyChanged(nameof(IsSelfValid));
        }

        this.IsBusy = this.PropertyBag.Any(p => p.Value.IsBusy);
    }
}
```

#### 2. EntityPropertyManager -- refactor to use override, add recalculation

Remove:
```csharp
public bool IsPaused { get; private set; } = false;
```

Change `PauseAllActions`:
```csharp
public override void PauseAllActions()
{
    if (!this.IsPaused)
    {
        base.PauseAllActions();
        foreach (var fd in this.PropertyBag)
        {
            fd.Value.IsPaused = true;
        }
    }
}
```

Change `ResumeAllActions`:
```csharp
public override void ResumeAllActions()
{
    if (this.IsPaused)
    {
        base.ResumeAllActions();  // Sets VPM.IsPaused = false, recalculates IsValid/IsSelfValid/IsBusy

        foreach (var fd in this.PropertyBag)
        {
            fd.Value.IsPaused = false;
        }

        // Recalculate cached modification state from current property state.
        // Events received while paused were dropped, so caches may be stale.
        var wasModified = this.IsModified;
        this.IsModified = this.PropertyBag.Any(p => p.Value.IsModified);
        if (wasModified != this.IsModified)
        {
            RaisePropertyChanged(nameof(IsModified));
        }

        var wasSelfModified = this.IsSelfModified;
        this.IsSelfModified = this.PropertyBag.Any(p => p.Value.IsSelfModified);
        if (wasSelfModified != this.IsSelfModified)
        {
            RaisePropertyChanged(nameof(IsSelfModified));
        }
    }
}
```

Update `CreateProperty<PV>` to reference `this.IsPaused` (still works -- now resolves to VPM's IsPaused, which is correctly maintained):
```csharp
protected new IValidateProperty CreateProperty<PV>(IPropertyInfo propertyInfo)
{
    var property = this.Factory.CreateEntityProperty<PV>(propertyInfo);
    property.IsPaused = this.IsPaused;  // Now reads VPM.IsPaused (correct)
    return property;
}
```

Update `Property_PropertyChanged` -- the `this.IsPaused` reference now reads VPM's IsPaused (correct, no code change needed since VPM.IsPaused is now properly set):
```csharp
protected override void Property_PropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (this.IsPaused)  // Was EPM.IsPaused, now VPM.IsPaused -- same value with Option C
    {
        base.Property_PropertyChanged(sender, e);  // VPM now correctly returns early (IsPaused=true)
        return;
    }
    // ... rest unchanged
}
```

### Secondary Bug Fix: VPM Event Processing During Pause

With Option C, VPM's `IsPaused` is correctly set to `true` when EPM is paused. This fixes a secondary bug where `VPM.Property_PropertyChanged` was processing events (recalculating IsValid/IsSelfValid) during pause because VPM didn't know it was paused.

**Current behavior (bug):** When EPM is paused and a property fires PropertyChanged:
1. EPM.Property_PropertyChanged: `if (this.IsPaused)` -> EPM.IsPaused is true -> calls base
2. VPM.Property_PropertyChanged: `if (this.IsPaused)` -> VPM.IsPaused is false -> proceeds to recalculate IsValid/IsSelfValid

**Option C behavior (fixed):** When EPM is paused:
1. EPM.Property_PropertyChanged: `if (this.IsPaused)` -> VPM.IsPaused is true -> calls base
2. VPM.Property_PropertyChanged: `if (this.IsPaused)` -> VPM.IsPaused is true -> returns immediately

This is strictly better: no unnecessary recalculation during pause. The recalculation happens once at resume, which is more efficient.

### Call Chain Analysis With Option C

#### EntityBase.PauseAllActions Flow

```
EntityBase.PauseAllActions():
  1. base.PauseAllActions() -> ValidateBase.PauseAllActions():
     -> if (!this.IsPaused) -> TRUE (not yet paused)
     -> this.IsPaused = true  (ValidateBase.IsPaused)
     -> this.PropertyManager.PauseAllActions() -> EPM.PauseAllActions (override):
        -> if (!this.IsPaused) -> TRUE (VPM.IsPaused still false)
        -> base.PauseAllActions() -> VPM.PauseAllActions:
           -> if (!this.IsPaused) -> TRUE
           -> this.IsPaused = true  (VPM.IsPaused)
        -> foreach property: IsPaused = true
     -> return new Paused(this)
  2. this.PropertyManager.PauseAllActions() -> EPM.PauseAllActions (override):
     -> if (!this.IsPaused) -> FALSE (VPM.IsPaused already true) -> NO-OP
```

Result: ValidateBase.IsPaused = true, VPM.IsPaused = true, all properties paused. Correct.

#### EntityBase.ResumeAllActions Flow

```
EntityBase.ResumeAllActions():
  1. base.ResumeAllActions() -> ValidateBase.ResumeAllActions():
     -> if (this.IsPaused) -> TRUE
     -> this.IsPaused = false  (ValidateBase.IsPaused)
     -> this.PropertyManager.ResumeAllActions() -> EPM.ResumeAllActions (override):
        -> if (this.IsPaused) -> TRUE (VPM.IsPaused still true)
        -> base.ResumeAllActions() -> VPM.ResumeAllActions:
           -> if (this.IsPaused) -> TRUE
           -> this.IsPaused = false  (VPM.IsPaused)
           -> Recalculate IsValid, IsSelfValid, IsBusy (raise events if changed)
        -> foreach property: IsPaused = false
        -> Recalculate IsModified, IsSelfModified (raise events if changed)
     -> this.ResetMetaState()  -- captures CURRENT (now correct) state for all meta properties
  2. this.PropertyManager.ResumeAllActions() -> EPM.ResumeAllActions (override):
     -> if (this.IsPaused) -> FALSE (VPM.IsPaused already false) -> NO-OP
```

Result: All state correctly recalculated, MetaState captured, events raised for changes. Correct.

#### EntityBase.FactoryComplete(Create) Flow

```
EntityBase.FactoryComplete(Create):
  1. base.FactoryComplete(Create) -> ValidateBase.FactoryComplete -> this.ResumeAllActions():
     -> EntityBase.ResumeAllActions() (see flow above):
        All meta properties recalculated, MetaState captured
  2. MarkNew():
     -> this.IsNew = true
     -> (no CheckIfMetaPropertiesChanged call)
     -> MetaState.IsModified was captured as pre-MarkNew value
     -> Next event that triggers CheckIfMetaPropertiesChanged will detect the change
  3. this.ResumeAllActions() -> EntityBase.ResumeAllActions():
     -> ValidateBase.ResumeAllActions: IsPaused already false -> NO-OP
     -> EPM.ResumeAllActions: IsPaused already false -> NO-OP
```

This is CORRECT. After Create, IsNew=true makes IsModified=true. MetaState was captured before MarkNew, so it reflects IsModified=false. The mismatch will be detected on the next CheckIfMetaPropertiesChanged call (e.g., when the first property is set). This matches the existing behavior.

#### EntityBase.FactoryComplete(Fetch) Flow

```
EntityBase.FactoryComplete(Fetch):
  1. base.FactoryComplete(Fetch) -> this.ResumeAllActions():
     -> All meta properties recalculated, MetaState captured
     -> If child was modified during fetch: IsModified recalculated to true
     -> PropertyChanged("IsModified") raised from EPM
     -> _PropertyManager_PropertyChanged -> CheckIfMetaPropertiesChanged
     -> MetaState was captured AFTER recalculation (ResetMetaState in ValidateBase.ResumeAllActions)
        Wait -- ordering: EPM raises PropertyChanged -> ValidateBase._PropertyManager_PropertyChanged
        -> CheckIfMetaPropertiesChanged -> this compares MetaState (captured during ResetMetaState in
        ValidateBase.ResumeAllActions, which runs AFTER EPM.ResumeAllActions returns)
        Actually, let me be more precise:

     -> EPM.ResumeAllActions raises PropertyChanged("IsModified")
     -> _PropertyManager_PropertyChanged fires -> CheckIfMetaPropertiesChanged
     -> BUT: ResetMetaState hasn't been called yet (it runs after PropertyManager.ResumeAllActions returns)
     -> So MetaState still has the STALE values from the last ResetMetaState call (before pause)
     -> CheckIfMetaPropertiesChanged detects IsModified changed -> raises entity-level PropertyChanged
     -> Then ValidateBase.ResumeAllActions calls ResetMetaState -> captures correct values
  2. (no Fetch-specific state changes)
  3. this.ResumeAllActions() -> NO-OP
```

This is CORRECT. The PropertyChanged event from EPM triggers entity-level notification before ResetMetaState captures the new baseline. The entity correctly reports IsModified=true.

#### EntityBase.FactoryComplete(Insert/Update) Flow

```
EntityBase.FactoryComplete(Insert/Update):
  1. base.FactoryComplete -> this.ResumeAllActions():
     -> All meta properties recalculated, MetaState captured
  2. MarkUnmodified():
     -> PropertyManager.MarkSelfUnmodified()  -- clears all property IsSelfModified flags
     -> IsMarkedModified = false
     -> CheckIfMetaPropertiesChanged()  -- detects IsModified change, raises events, ResetMetaState
  3. MarkOld():
     -> IsNew = false
  4. this.ResumeAllActions() -> NO-OP
```

This is CORRECT. After Insert/Update, entity should be unmodified. MarkUnmodified's CheckIfMetaPropertiesChanged handles the state transition.

#### User PauseAllActions/ResumeAllActions (using pattern)

```
using (entity.PauseAllActions())
{
    entity.Name = "value1";  // Value changes, IsSelfModified stays false (EntityProperty.IsPaused=true)
    entity.Age = 42;         // Same
}
// Dispose -> ResumeAllActions():
//   EPM recalculates IsModified/IsSelfModified from properties -- still not modified (correct)
//   VPM recalculates IsValid -- unchanged (rules didn't run while paused)
```

This is CORRECT. The fix preserves batch-load semantics.

### Reentrancy and Double-Event Safety

The `RaisePropertyChanged` from the property manager triggers `_PropertyManager_PropertyChanged` on the entity, which calls `CheckIfMetaPropertiesChanged()`. This is NOT reentrant because:

1. `CheckIfMetaPropertiesChanged` is not async
2. It does not modify the property manager
3. It calls `RaisePropertyChanged` on the entity (not the PM), which fires `PropertyChanged` to external listeners
4. External listeners (UI bindings) do not modify entity state in their event handlers (standard pattern)

### What About IsBusy?

`IsBusy` in `ValidatePropertyManager` should be recalculated for completeness, though it's unlikely to be stale in practice (async operations would have completed by resume time). We recalculate it for defensive correctness, matching the pattern in the list base classes.

---

## Implementation Steps

1. **Refactor `EntityPropertyManager` to use `override` instead of `new`**
   - Remove `public bool IsPaused { get; private set; } = false;`
   - Change `PauseAllActions()` to `public override void PauseAllActions()` with `if (!this.IsPaused)` guard and `base.PauseAllActions()` call
   - Change `ResumeAllActions()` to `public override void ResumeAllActions()` with `if (this.IsPaused)` guard and `base.ResumeAllActions()` call
   - Verify `this.IsPaused` references in `CreateProperty<PV>` and `Property_PropertyChanged` now correctly read VPM's IsPaused (no code change needed, just confirm)

2. **Add recalculation to `ValidatePropertyManager.ResumeAllActions()`**
   - After `this.IsPaused = false`, recalculate `IsValid`, `IsSelfValid`, and `IsBusy` from property state
   - Raise `PropertyChanged` if values changed

3. **Add recalculation to `EntityPropertyManager.ResumeAllActions()`**
   - After `base.ResumeAllActions()` and unpausing individual properties
   - Recalculate `IsModified` and `IsSelfModified` from property state
   - Raise `PropertyChanged` if values changed

4. **Run all existing tests** -- verify no regressions from the override refactor

5. **Write red-green tests FIRST (before production code changes in steps 1-3)**

   Red-green testing approach: Write tests that expose the bug FIRST, verify they FAIL on the current code, THEN make production changes, THEN verify the tests PASS. This proves the fix actually fixes the bug rather than testing something that already worked.

   **Phase 3a: Write failing tests (RED)**
   - Run these tests against unmodified code to confirm they FAIL
   - Do NOT modify any existing tests

   **Phase 3b: Apply production code changes (steps 1-3)**

   **Phase 3c: Run all tests (GREEN)**
   - New tests should now pass
   - All existing tests must still pass
   - If any existing test fails: STOP and discuss (do NOT modify existing tests)

6. **Unit tests in `Neatoo.UnitTest/Unit/Core/EntityPropertyManagerTests.cs`** (red-green)
   - Test: EPM paused, child entity property becomes modified during pause, resume -> EPM.IsModified reflects child state. **Expected RED before fix:** EPM.IsModified stays false because EPM.ResumeAllActions does not recalculate.
   - Test: EPM paused, child entity property becomes invalid during pause, resume -> EPM.IsValid (via VPM) reflects child state. **Expected RED before fix:** VPM.IsValid may incorrectly show true (VPM event processing during pause is inconsistent due to the IsPaused bug).
   - Test: EPM paused, no changes, resume -> no events raised, values unchanged. **Expected GREEN before fix:** This is a safety test, not a bug-exposing test.
   - Test: VPM.Property_PropertyChanged correctly returns early when paused (secondary bug fix). **Expected RED before fix:** VPM processes events during EPM pause because VPM.IsPaused is never set.

7. **Unit tests in `Neatoo.UnitTest/Unit/Core/ValidatePropertyManagerTests.cs`** (red-green)
   - Test: VPM paused, property becomes invalid, resume -> IsValid recalculated. **Expected RED before fix:** VPM.ResumeAllActions does not recalculate.
   - Test: VPM paused, property becomes valid, resume -> IsValid recalculated. **Expected RED before fix:** Same reason.

8. **Optional integration test that reproduces the zTreatment scenario**
   - Parent entity (EntityBase) with child entity property
   - During parent's pause, child goes through independent lifecycle and becomes modified
   - After parent resume, parent.IsModified should be true
   - **Expected RED before fix**

9. **Verify all tests pass after production code changes**

---

## Acceptance Criteria

- [ ] `EntityPropertyManager` uses `override` for `PauseAllActions()` and `ResumeAllActions()` instead of implicit `new`
- [ ] `EntityPropertyManager.IsPaused` (hidden property) is removed; EPM uses VPM's `IsPaused`
- [ ] `ValidatePropertyManager.ResumeAllActions()` recalculates `IsValid`, `IsSelfValid`, `IsBusy`
- [ ] `EntityPropertyManager.ResumeAllActions()` calls `base.ResumeAllActions()` and recalculates `IsModified`, `IsSelfModified`
- [ ] VPM's `IsPaused` is correctly set to `true` during pause for EntityBase objects (secondary bug fix)
- [ ] VPM's `Property_PropertyChanged` correctly returns early when paused for EntityBase objects
- [ ] Parent entity correctly reports `IsModified=true` when child entity is modified during parent pause
- [ ] Parent entity correctly reports `IsValid=false` when child entity becomes invalid during parent pause
- [ ] No change in behavior for the `using (PauseAllActions())` pattern with batch loads
- [ ] No rule re-execution on resume (only cache recalculation)
- [ ] No double PropertyChanged events (check if value actually changed before raising)
- [ ] All existing tests pass
- [ ] PropertyChanged events fire for IsModified/IsValid changes detected during resume

---

## Dependencies

- None. This is a self-contained fix within the Neatoo framework.
- No RemoteFactory changes needed.
- No source generator changes needed.

---

## Risks / Considerations

### 1. Performance

Recalculating IsModified/IsValid on every resume adds O(n) iteration over properties. This is:
- Identical to what `OnDeserialized()` already does
- Identical to what `EntityListBase.ResumeAllActions()` already does
- Property count per entity is typically small (5-20 properties)
- Resume happens once per factory operation, not per property change
- **Acceptable cost.**

### 2. Secondary Bug Fix: VPM Event Processing During Pause

With Option C, VPM's `Property_PropertyChanged` will now correctly return early when paused for EntityBase objects. Previously it was processing events (recalculating IsValid/IsSelfValid) even during pause because VPM's `IsPaused` was never set. The recalculation was harmless in practice (entity-level events were suppressed by ValidateBase.IsPaused), but the unnecessary work was wasted. Any code that incidentally depended on VPM recalculating IsValid during pause will now get a slightly different behavior -- but the final values are the same because the recalculation now happens at resume instead.

### 3. Code That Depends on the Bug

Any code that relies on `IsModified=false` after fetch when a child IS actually modified would break. This is a bug fix, not a behavior change -- the code was always wrong, it just wasn't visible. The zTreatment workaround (`MarkModified()`) confirms this.

### 4. Double Calls in EntityBase.PauseAllActions/ResumeAllActions

With Option C, `EntityBase.PauseAllActions()` and `EntityBase.ResumeAllActions()` both make redundant calls to `this.PropertyManager.PauseAllActions()` / `this.PropertyManager.ResumeAllActions()`. These are harmless NO-OPs because VPM/EPM's guards prevent double execution. We leave them in place to minimize the scope of this change. They can be removed in a future cleanup.

### 5. IsBusy Recalculation

`IsBusy` in `ValidatePropertyManager` should be recalculated for completeness, but it's unlikely to be stale in practice. Async operations complete before the factory operation completes, so `IsBusy` should already be false at resume time. We recalculate it anyway for defensive correctness.

### 6. EPM.IsPaused Default Value

Current EPM `IsPaused` is initialized to `false`: `public bool IsPaused { get; private set; } = false;`. VPM's `IsPaused` default is also `false`: `public bool IsPaused { get; protected set; }` (C# default for bool). After removing EPM's IsPaused, the behavior is unchanged -- IsPaused defaults to false.

### 7. SetParent Is Safe With Option C

**Concern:** With Option C, VPM's `IsPaused` is correctly set to true during pause. VPM's `Property_PropertyChanged` now returns early during pause. Does this block SetParent calls on child entities during factory operations?

**Answer: No. SetParent is completely unaffected by Option C.**

SetParent flows through the **NeatooPropertyChanged** event chain, not the **PropertyChanged** event chain. These are two separate event systems, and only PropertyChanged has an IsPaused guard in VPM.

#### Complete SetParent Call Sites

1. **`ValidateProperty.LoadValue` (line 177-180):** Calls `SetParent(null)` directly on the OLD value being replaced. Not event-driven, not affected by pause.

2. **`ValidateProperty.HandleNonNullValue` (line 245-248):** Calls `SetParent(null)` directly on the OLD value being replaced. Not event-driven, not affected by pause.

3. **`ValidateProperty.LoadValue` / `HandleNonNullValue` -> NeatooPropertyChanged chain -> `ValidateBase._PropertyManager_NeatooPropertyChanged` (line 396-398):** Calls `SetParent(this)` on the NEW value. This is event-driven but goes through the NeatooPropertyChanged chain, which has NO IsPaused guard anywhere in the path.

4. **`ValidateBase.OnDeserialized` (line 535-540):** Explicit loop over properties calling `SetParent(this)`. Not event-driven.

5. **`ValidateListBase.SetParent` (line 114-126):** Explicit loop over items calling `SetParent(parent)`. Not event-driven.

6. **`ValidateListBase.InsertItem` (line 137-138):** Direct `SetParent(this.Parent)` call. Not event-driven.

7. **`ValidateListBase.SetItem` (line 217-218):** Direct `SetParent(this.Parent)` call. Not event-driven.

8. **`ValidateListBase.OnDeserialized` (line 310-312):** Explicit loop calling `SetParent(this.Parent)`. Not event-driven.

#### Why NeatooPropertyChanged Is Not Blocked

The NeatooPropertyChanged event chain from property to ValidateBase is:

```
ValidateProperty fires NeatooPropertyChanged (OnValueNeatooPropertyChanged, line 200/278/301)
  -> VPM._Property_NeatooPropertyChanged (line 81-84) -- NO IsPaused guard, pure pass-through
    -> ValidateBase._PropertyManager_NeatooPropertyChanged (line 392-408) -- SetParent called here
      -> ValidateBase.ChildNeatooPropertyChanged (line 374-390) -- IsPaused guard HERE, but only affects rules/bubbling
```

The IsPaused check that would suppress behavior is in `ChildNeatooPropertyChanged` (line 378), which runs AFTER `SetParent` has already been called (line 398). When paused, `ChildNeatooPropertyChanged` skips rule execution and event bubbling but SetParent has already happened.

#### What VPM's Property_PropertyChanged Guard Actually Blocks

The `Property_PropertyChanged` method (VPM line 137-179) that returns early when IsPaused handles:
- **IsBusy recalculation** (line 146)
- **IsValid recalculation** (line 153-157)
- **IsSelfValid recalculation** (line 166-170)
- **Pass-through PropertyChanged forwarding** (line 178)

None of these involve SetParent. They are purely meta-property caching operations that are now correctly deferred to ResumeAllActions with Option C's recalculation logic.

#### During Factory Operations (Create/Fetch)

When a child entity is assigned to a property during a [Create] or [Fetch] factory method:

1. The child is assigned via `LoadProperty` (generated code), which calls `ValidateProperty.LoadValue` or `EntityProperty.LoadValue`
2. `LoadValue` fires `NeatooPropertyChanged` with `ChangeReason.Load`
3. VPM's `_Property_NeatooPropertyChanged` passes it through (no IsPaused guard)
4. `ValidateBase._PropertyManager_NeatooPropertyChanged` calls `SetParent(this)` on the child
5. `ChildNeatooPropertyChanged` is then called -- it checks `!this.IsPaused` AND `eventArgs.OriginalEventArgs.Reason != ChangeReason.Load`, and since the reason IS Load, it skips rules/bubbling via BOTH guards

This means SetParent works correctly during pause regardless of Option C, because it flows through the NeatooPropertyChanged path which is completely unguarded for the SetParent step.

**Conclusion: Option C does not break SetParent in any scenario. No mitigation is needed.**

---

## Architecture Investigation: Option D Analysis

### User Hypothesis

> "The pause is we stop at the EntityBase or ValidateBase, but they ALWAYS listen to their properties."

The user proposed **Option D**: remove the `IsPaused` guard from property managers entirely so PM caches are always up-to-date, and let only the entity level handle pause semantics (suppressing rules, PropertyChanged to external listeners, NeatooPropertyChanged propagation).

### Investigation: What VPM.Property_PropertyChanged Does

When NOT paused (VPM lines 137-179):
1. Recalculates `IsBusy` from all properties (line 146)
2. Recalculates `IsValid` from all properties (lines 153-157)
3. Raises PM-level `PropertyChanged(nameof(IsValid))` if changed (line 161)
4. Recalculates `IsSelfValid` from all properties (lines 166-170)
5. Raises PM-level `PropertyChanged(nameof(IsSelfValid))` if changed (line 174)
6. **Passes through the PropertyChanged event** (line 178): `this.PropertyChanged?.Invoke(sender, e);`

When paused (lines 139-142):
- Returns immediately. No cache recalculation. No pass-through event.

The pass-through event at step 6 triggers `ValidateBase._PropertyManager_PropertyChanged` -> `CheckIfMetaPropertiesChanged()`.

### Investigation: What EPM.Property_PropertyChanged Does

When NOT paused (EPM lines 143-167):
1. Recalculates `IsModified` from all properties (lines 147-152)
2. Raises PM-level `RaisePropertyChanged(nameof(IsModified))` if changed
3. Recalculates `IsSelfModified` from all properties (lines 159-164)
4. Raises PM-level `RaisePropertyChanged(nameof(IsSelfModified))` if changed
5. Calls `base.Property_PropertyChanged(sender, e)` (line 167) -> VPM does steps 1-6 above

When paused (lines 137-141):
- Calls `base.Property_PropertyChanged(sender, e)` (VPM)
- Due to current bug, VPM.IsPaused is false, so VPM processes the event (recalculates IsValid/IsSelfValid, fires pass-through)
- The pass-through triggers `_PropertyManager_PropertyChanged` -> `CheckIfMetaPropertiesChanged()`

### Why Option D Fails: MetaState Tracking Problem

With Option D (no IsPaused guard in property managers), the following chain occurs during pause:

1. Property changes -> VPM.Property_PropertyChanged runs (no guard) -> recalculates IsValid -> fires pass-through PropertyChanged
2. Pass-through triggers `ValidateBase._PropertyManager_PropertyChanged` -> `CheckIfMetaPropertiesChanged()`
3. `EntityBase.CheckIfMetaPropertiesChanged()` checks `if (!this.IsPaused)` (line 228) -> paused, skips entity-level RaiseIfChanged
4. `base.CheckIfMetaPropertiesChanged()` runs: calls `RaiseIfChanged` for IsValid/IsSelfValid/IsBusy -> these call `ValidateBase.RaisePropertyChanged` -> guarded by `!this.IsPaused` -> suppressed. Good so far.
5. **`ResetMetaState()` runs unconditionally** (line 329) -> captures the CURRENT (now up-to-date) PM cache values

At resume time:
- PM caches are already correct (they were updated during pause)
- MetaState was ALSO updated to the correct values (via `ResetMetaState()` in step 5)
- **There is NO delta to detect** between MetaState and current values
- Entity-level `PropertyChanged("IsModified")` would NEVER fire
- External consumers would never be notified of the state change that happened during pause

This is a critical failure. The resume notification mechanism depends on a delta between stale MetaState and fresh PM caches. Option D eliminates both sides of the delta simultaneously.

### Why Option C Works: Deferred Update Creates Detectable Delta

With Option C:
- VPM.IsPaused is correctly set to true during pause
- VPM.Property_PropertyChanged returns early (line 141) -> no cache update, no pass-through
- MetaState IS still updated via the NeatooPropertyChanged path (`ChildNeatooPropertyChanged` else branch calls `ResetMetaState()`, line 388) -> captures STALE PM cache values
- At resume: PM caches are recalculated (fresh), MetaState holds stale values -> delta detected -> entity-level PropertyChanged fires

### Additional Option D Problem: Performance During Batch Loading

Even if the MetaState problem could be solved (by deferring ResetMetaState too), Option D has a secondary issue: during batch loading of N properties, every property change triggers O(n) recalculation across all properties. For a batch load of 20 properties, that is 20 unnecessary full recalculations. Option C defers to a single O(n) recalculation at resume.

### Additional Option D Problem: Scope Change for ValidateBase Objects

Option D would also change behavior for pure ValidateBase objects (non-EntityBase). Currently, VPM correctly pauses for ValidateBase objects. Removing the IsPaused guard would affect all ValidateBase objects, not just EntityBase objects with the known bug.

### Conclusion

**Option C is confirmed as the correct architecture.** Option D does not work because:
1. MetaState tracking resets alongside PM cache updates, eliminating the delta that resume needs to detect changes
2. Unnecessary O(n) recalculations during batch loading
3. Broader scope change affecting ValidateBase objects that currently work correctly

The user's intuition that "property managers should always keep caches up-to-date" is architecturally reasonable in principle, but it conflicts with the framework's MetaState change-detection mechanism, which requires a deferred update to create a detectable delta at resume.

### SetParent Verification (Confirmed Safe)

The SetParent analysis in the plan body (section "SetParent Is Safe With Option C") was independently verified against the source code. All claims are correct:

- VPM._Property_NeatooPropertyChanged (line 81-84): confirmed no IsPaused guard, pure pass-through
- ValidateBase._PropertyManager_NeatooPropertyChanged (line 392-408): confirmed SetParent at line 398 occurs BEFORE ChildNeatooPropertyChanged at line 407
- ChildNeatooPropertyChanged IsPaused guard (line 378): confirmed this runs AFTER SetParent has already been called
- LoadValue fires NeatooPropertyChanged with ChangeReason.Load (ValidateProperty line 200): confirmed
- EntityProperty.LoadValue fires PropertyChanged for IsSelfModified/IsModified AFTER base.LoadValue (which already triggered SetParent via NeatooPropertyChanged): confirmed

No SetParent calls are blocked by Option C's IsPaused guards.

---

## Architectural Verification

### Scope Table

| Component | Change Type | What Changes |
|-----------|------------|-------------|
| ValidatePropertyManager | Recalculation added | ResumeAllActions recalculates IsValid, IsSelfValid, IsBusy |
| EntityPropertyManager | Override refactor + recalculation | PauseAllActions/ResumeAllActions become overrides; IsPaused removed; ResumeAllActions recalculates IsModified, IsSelfModified |
| ValidateListBase | No change | Already fixed in v10.7.1 |
| EntityListBase | No change | Already fixed in v10.7.1 |
| ValidateBase | No change | Existing code works correctly with the refactored EPM |
| EntityBase | No change | Existing double-call pattern becomes redundant NO-OP (harmless) |

### Design Project Verification

**No Design project changes needed.** This fix is in runtime behavior (pause/resume state management), not API surface. The existing Design project code compiles and tests pass. The fix is verified by unit tests, not by Design project compilation.

### Breaking Changes

**No breaking changes to public API.** This fix corrects a bug where cached values were stale. All public APIs remain the same. The behavioral change is that:
1. Meta properties now correctly reflect child state after resume (bug fix)
2. VPM.Property_PropertyChanged correctly returns early during pause for EntityBase objects (secondary bug fix, invisible to external consumers)

### Codebase Analysis

Files examined during this review:

- `src/Neatoo/Internal/EntityPropertyManager.cs` -- EPM class declaration, PauseAllActions, ResumeAllActions, IsPaused, Property_PropertyChanged, OnDeserialized, CreateProperty
- `src/Neatoo/Internal/ValidatePropertyManager.cs` -- VPM PauseAllActions (virtual), ResumeAllActions (virtual), Property_PropertyChanged (virtual), IsPaused (protected set), OnDeserialized
- `src/Neatoo/ValidateBase.cs` -- PauseAllActions, ResumeAllActions, FactoryStart, FactoryComplete, CheckIfMetaPropertiesChanged, ResetMetaState, ChildNeatooPropertyChanged, _PropertyManager_PropertyChanged
- `src/Neatoo/EntityBase.cs` -- PauseAllActions (override), ResumeAllActions (override), FactoryComplete, IsModified, IsSelfModified, CheckIfMetaPropertiesChanged, MarkNew, MarkUnmodified, MarkOld
- `src/Neatoo/IValidatePropertyManager.cs` -- Interface declaring PauseAllActions, ResumeAllActions, IsPaused (covariant P)
- `src/Neatoo/IEntityPropertyManager.cs` -- Interface extending IValidatePropertyManager, does NOT redeclare PauseAllActions/ResumeAllActions
- `src/Neatoo/EntityListBase.cs` -- ResumeAllActions (already recalculates _cachedChildrenModified)
- `src/Neatoo/ValidateListBase.cs` -- ResumeAllActions (already recalculates _cachedIsValid and _cachedIsBusy)
- `src/Neatoo.UnitTest/Unit/Core/EntityPropertyManagerTests.cs` -- Existing pause/resume tests
- `src/Neatoo.UnitTest/Unit/Core/ValidatePropertyManagerTests.cs` -- Existing tests
- Git history: `ab5eead` (v10.7.1 caching commit) confirmed list bases were fixed but property managers were not

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Red-Green Tests (RED) | developer | Yes | Write tests first, verify they fail on current code | None |
| Phase 2: Override Refactor + Recalculation | developer | No | Small, focused change to 2 files; needs test context | Phase 1 |
| Phase 3: Verify GREEN | developer | No | Run all tests to confirm fix works | Phase 2 |

**Parallelizable phases:** None (each phase depends on the previous).

**Notes:** All three phases are small enough to handle in a single agent invocation. The developer should write tests first, confirm they fail, then apply production changes, then confirm all tests pass.

---

## Developer Review

### First Review (Concerns Raised)

**Status:** Concerns Raised
**Reviewed:** 2026-03-01

Three critical concerns were identified regarding interface dispatch analysis, double resume behavior, and missing IsPaused guards. The architect addressed all three in the revised design (Option C). See git history for the original review content.

### Architect Response to Developer Concerns

#### Concern 1: Interface Dispatch Analysis Was Wrong

**Confirmed correct.** The developer's finding that C# interface re-implementation causes ALL interface calls on EPM objects to dispatch to EPM's methods (never VPM's) was independently verified by the architect. The original plan's analysis was wrong. This revision adopts Option C per the developer's recommendation and the user's preference.

#### Concern 2: Double Resume Analysis Was Incorrect

**Confirmed correct.** Both ResumeAllActions calls in EntityBase.FactoryComplete dispatch to EPM. The first does the real work; the second is a NO-OP. The revised plan documents the correct flow. However, as the developer noted, the final result is still correct because MarkNew/MarkUnmodified call CheckIfMetaPropertiesChanged themselves.

#### Concern 3: EPM.ResumeAllActions Has No IsPaused Guard

**Addressed.** With Option C, EPM.ResumeAllActions uses `override` and includes the `if (this.IsPaused)` guard, consistent with VPM's pattern.

### Second Review (Approved)

**Status:** Approved
**Reviewed:** 2026-03-01

**Why This Plan Is Exceptionally Clear:**

All three previous concerns were correctly addressed. The revised design (Option C) is architecturally sound:
- Interface dispatch is fixed by using `override` instead of implicit `new`
- VPM's IsPaused (`protected set`) is accessible from EPM (derived class)
- IsPaused guards prevent double execution in the double-call pattern
- `base.ResumeAllActions()` chains correctly to VPM's virtual implementation
- Call chain traces for all scenarios (Create, Fetch, Insert/Update, using pattern) verified against source code
- Secondary bug (VPM processing events during EPM pause) correctly analyzed; fix falls out naturally from Option C
- Recalculation pattern matches existing OnDeserialized and list base class patterns
- Code changes are explicit with before/after blocks, no ambiguity

**Review Summary:**
- Files examined: EntityPropertyManager.cs, ValidatePropertyManager.cs, EntityBase.cs, ValidateBase.cs, IValidatePropertyManager.cs, IEntityPropertyManager.cs, EntityPropertyManagerTests.cs, ValidatePropertyManagerTests.cs (8 source + 2 test files)
- Questions checked: 16 of 16 (all satisfactory)
- Devil's advocate items: 3 generated, all addressed by the plan or verified as non-issues

---

## Implementation Contract

**Created:** 2026-03-01
**Approved by:** neatoo-developer

### Design Project Acceptance Criteria

N/A -- no Design project compilation changes. Fix is runtime behavior verified by unit tests.

### In Scope

Phase 1: Red-Green Tests (write FIRST, verify FAIL on current code)
- [ ] `src/Neatoo.UnitTest/Unit/Core/EntityPropertyManagerTests.cs`: Test EPM paused, child entity property becomes modified during pause, resume -> EPM.IsModified reflects child state. **Must FAIL before fix.**
- [ ] `src/Neatoo.UnitTest/Unit/Core/EntityPropertyManagerTests.cs`: Test EPM paused, child entity property becomes invalid during pause, resume -> EPM.IsValid reflects child state. **Must FAIL before fix.**
- [ ] `src/Neatoo.UnitTest/Unit/Core/EntityPropertyManagerTests.cs`: Test EPM paused, no changes during pause, resume -> no events raised, values unchanged. (Safety test, expected to pass before fix.)
- [ ] `src/Neatoo.UnitTest/Unit/Core/EntityPropertyManagerTests.cs`: Test VPM.Property_PropertyChanged correctly returns early when EPM paused (secondary bug fix). **Must FAIL before fix.**
- [ ] `src/Neatoo.UnitTest/Unit/Core/ValidatePropertyManagerTests.cs`: Test VPM paused, property becomes invalid during pause, resume -> IsValid recalculated. **Must FAIL before fix.**
- [ ] `src/Neatoo.UnitTest/Unit/Core/ValidatePropertyManagerTests.cs`: Test VPM paused, property becomes valid during pause, resume -> IsValid recalculated. **Must FAIL before fix.**
- [ ] Optional: Integration test for zTreatment scenario. **Must FAIL before fix.**
- [ ] Checkpoint: Run `dotnet test src/Neatoo.sln` -- new tests FAIL (confirming the bug exists), ALL existing tests still PASS.

Phase 2: Override Refactor (production code change 1)
- [ ] `src/Neatoo/Internal/EntityPropertyManager.cs`: Remove `public bool IsPaused { get; private set; } = false;` (line 105)
- [ ] `src/Neatoo/Internal/EntityPropertyManager.cs`: Change `PauseAllActions()` from implicit `new` to `override`, add `if (!this.IsPaused)` guard, add `base.PauseAllActions()` call before property loop
- [ ] `src/Neatoo/Internal/EntityPropertyManager.cs`: Change `ResumeAllActions()` from implicit `new` to `override`, add `if (this.IsPaused)` guard, add `base.ResumeAllActions()` call before property loop
- [ ] Checkpoint: `dotnet build src/Neatoo.sln` compiles, `dotnet test src/Neatoo.sln` -- all existing tests pass. New red-green tests may still fail (recalculation not added yet).

Phase 3: Add Recalculation (production code change 2)
- [ ] `src/Neatoo/Internal/ValidatePropertyManager.cs`: In `ResumeAllActions()`, after `this.IsPaused = false`, recalculate IsValid, IsSelfValid, IsBusy from property state. Raise PropertyChanged if values changed.
- [ ] `src/Neatoo/Internal/EntityPropertyManager.cs`: In `ResumeAllActions()`, after `base.ResumeAllActions()` and unpausing properties, recalculate IsModified, IsSelfModified from property state. Raise PropertyChanged if values changed.
- [ ] Checkpoint: `dotnet test src/Neatoo.sln` -- ALL tests pass including new red-green tests. This is the GREEN step.

### Explicitly Out of Scope

- EntityBase.PauseAllActions/ResumeAllActions cleanup (removing redundant double-calls) -- future task
- CreateProperty<PV> method hiding -- intentional, not a bug
- Removing or modifying any existing tests

### Verification Gates

1. After Phase 1 (red-green tests): New bug-exposing tests FAIL on current code. All existing tests still PASS. This confirms the bug exists and the tests are correctly written.
2. After Phase 2 (override refactor): Solution compiles. All existing tests pass. Some new tests may still fail (recalculation not added yet).
3. After Phase 3 (recalculation): ALL tests pass including new red-green tests. This is the GREEN confirmation.
4. Final: `dotnet test src/Neatoo.sln` -- zero failures.

### Stop Conditions

If any of these occur, STOP and report:
- New red-green tests PASS before the fix is applied (the test does not actually expose the bug -- rewrite the test)
- Out-of-scope existing test fails after any phase
- Architectural contradiction discovered (e.g., VPM's IsPaused is not accessible from EPM)
- Existing tests fail after override refactor (Phase 2) before recalculation is added
- Compile errors that suggest a design flaw (not a simple typo)

---

## Implementation Progress

**Started:** [date]
**Developer:** [agent name]

**[Milestone 1]:** [Name]
- [ ] Step 1
- [ ] Step 2
- [ ] **Verification**: [test results, evidence]

---

## Completion Evidence

[Developer fills this section, then sets status to "Awaiting Verification" and STOPS.]

**Reported:** [date]

- **Tests Passing:** [Output or summary]
- **Design Projects Compile:** [Yes/No/N/A]
- **All Contract Items:** [Confirmed 100% complete]

---

## Documentation

**Agent:** [documentation agent name, or "developer" if no documentation agent]
**Completed:** [date]

### Expected Deliverables

- [ ] No user-facing documentation changes needed (internal bug fix)
- [ ] Skill updates: No
- [ ] Sample updates: No

### Files Updated

[Documentation agent fills this after completing work]

---

## Architect Verification

[Architect fills this section after independently verifying the developer's work.]

**Verified:** [date]
**Verdict:** VERIFIED | SENT BACK

**Independent test results:**
- [Project/module 1]: [Build result]
- All tests: [X passed, Y failed]

**Design match:** [Does the implementation match the original plan?]

**Issues found:** [List any issues, or "None"]
