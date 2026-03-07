# Plan: Fix IsValid/IsSavable Stale Cache During RunRules() in Factory Operations

**Date:** 2026-03-03
**Related Todo:** [IsValid Stale Cache During RunRules](../todos/isvalid-stale-cache-during-runrules.md)
**Status:** Documentation Complete
**Last Updated:** 2026-03-03

---

## Overview

`ValidatePropertyManager.IsValid` is a cached property that only recalculates when `Property_PropertyChanged` fires while `IsPaused == false`, or when `ResumeAllActions()` is called. When `RunRules()` is called explicitly during a paused state (inside factory operations), rules set error messages on properties, but the cache is never updated. This causes `IsValid` and `IsSavable` to return stale `true` values.

## Approach

Add a method to `ValidatePropertyManager` that recalculates the cached `IsValid` and `IsSelfValid` from actual property state. Call this from both `RunRules` overloads in `ValidateBase` after rule execution completes.

This is intentionally minimal -- we do not change the paused event suppression behavior (which is correct for preventing cascading rules and UI events). We only ensure that after an explicit `RunRules()` call, the cached values reflect reality.

## Design

### Change 1: Add `RecalculateValidity()` to `ValidatePropertyManager`

**File:** `src/Neatoo/Internal/ValidatePropertyManager.cs`

Add a new public method that recalculates the cached `IsValid` and `IsSelfValid` properties from the current property state, without raising events or requiring `IsPaused == false`:

```csharp
/// <summary>
/// Recalculates the cached IsValid and IsSelfValid values from current property state.
/// Called after explicit RunRules() to ensure caches are accurate regardless of paused state.
/// Does not raise PropertyChanged events (caller is responsible for meta-state notifications).
/// </summary>
public void RecalculateValidity()
{
    this.IsValid = !this.PropertyBag.Any(p => !p.Value.IsValid);
    this.IsSelfValid = !this.PropertyBag.Any(p => !p.Value.IsSelfValid);
}
```

This is the same logic used in `ResumeAllActions()` (lines 307, 314) and `OnDeserialized()` (lines 236-237), extracted into a reusable method. The method does NOT raise `PropertyChanged` events because:
- During paused state, events should remain suppressed
- The caller (`ValidateBase.RunRules`) handles meta-state change detection via `CheckIfMetaPropertiesChanged()`

### Change 2: Call `RecalculateValidity()` from `ValidateBase.RunRules(RunRulesFlag)`

**File:** `src/Neatoo/ValidateBase.cs`

After rules execute (and before the method returns), call `PropertyManager.RecalculateValidity()`:

```csharp
public virtual async Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null)
{
    if (runRules == Neatoo.RunRulesFlag.All)
    {
        this.ClearAllMessages();
    }

    try
    {
        // Run child property rules unless only Self flag is set
        if (runRules != RunRulesFlag.Self)
        {
            await this.PropertyManager.RunRules(runRules, token);
        }

        await this.RuleManager.RunRules(runRules, token);
        await this.RunningTasks.AllDone;
    }
    catch (OperationCanceledException)
    {
        this.MarkInvalid("Validation cancelled");
        throw;
    }
    finally
    {
        // Recalculate cached validity after rules execute.
        // During factory operations (IsPaused=true), Property_PropertyChanged
        // skips recalculation, leaving the cache stale.
        this.PropertyManager.RecalculateValidity();
    }
}
```

Note: The full-sweep `RunRules(RunRulesFlag)` currently does NOT have a `finally` block with `CheckIfMetaPropertiesChanged()`. This is a secondary issue -- the method should have it for consistency with `RunRules(string)`, but that is outside the scope of this fix. The `RecalculateValidity()` call ensures the cache is correct regardless.

### Change 3: Call `RecalculateValidity()` from `ValidateBase.RunRules(string)`

**File:** `src/Neatoo/ValidateBase.cs`

The property-name-triggered `RunRules(string)` already has `CheckIfMetaPropertiesChanged()` in its finally block, but that reads the stale cached value. Add `RecalculateValidity()` before `CheckIfMetaPropertiesChanged()`:

```csharp
public virtual async Task RunRules(string propertyName, CancellationToken? token = null)
{
    try
    {
        await this.RuleManager.RunRules(propertyName, token);
    }
    catch (OperationCanceledException)
    {
        this.MarkInvalid("Validation cancelled");
        throw;
    }
    finally
    {
        // Recalculate cached validity after rules execute.
        // During factory operations (IsPaused=true), Property_PropertyChanged
        // skips recalculation, leaving the cache stale.
        this.PropertyManager.RecalculateValidity();
        this.CheckIfMetaPropertiesChanged();
    }
}
```

### Change 4: Add `RecalculateValidity()` to `IValidatePropertyManager<P>` interface

**File:** `src/Neatoo/IValidatePropertyManager.cs`

The interface needs the new method so `ValidateBase` can call it through the `PropertyManager` property:

```csharp
/// <summary>
/// Recalculates the cached IsValid and IsSelfValid values from current property state.
/// Called after explicit RunRules() to ensure caches are accurate regardless of paused state.
/// </summary>
void RecalculateValidity();
```

### Optional Refactoring: DRY up `ResumeAllActions` and `OnDeserialized`

`ResumeAllActions()` and `OnDeserialized()` both contain the same recalculation logic. They could be refactored to call `RecalculateValidity()` instead. This is optional but improves consistency:

```csharp
public virtual void ResumeAllActions()
{
    if (this.IsPaused)
    {
        this.IsPaused = false;

        var wasValid = this.IsValid;
        var wasSelfValid = this.IsSelfValid;

        RecalculateValidity();

        if (wasValid != this.IsValid)
        {
            RaisePropertyChanged(nameof(IsValid));
        }
        if (wasSelfValid != this.IsSelfValid)
        {
            RaisePropertyChanged(nameof(IsSelfValid));
        }

        this.IsBusy = this.PropertyBag.Any(p => p.Value.IsBusy);
    }
}
```

## Implementation Steps

1. Add `RecalculateValidity()` to the `IValidatePropertyManager<P>` interface
2. Implement `RecalculateValidity()` in `ValidatePropertyManager<P>`
3. Add `this.PropertyManager.RecalculateValidity()` to `ValidateBase.RunRules(RunRulesFlag)` in a finally block
4. Add `this.PropertyManager.RecalculateValidity()` to `ValidateBase.RunRules(string)` before `CheckIfMetaPropertiesChanged()`
5. (Optional) Refactor `ResumeAllActions()` and `OnDeserialized()` to use `RecalculateValidity()`
6. Add Design project acceptance test
7. Run all existing tests

## Acceptance Criteria

1. After `await RunRules()` inside `[Insert]`, `IsValid` returns `false` when Required fields are empty
2. After `await RunRules()` inside `[Insert]`, `IsSavable` returns `false` when entity is invalid
3. After `await RunRules()` inside `[Insert]`, `IsValid` returns `true` when all rules pass
4. Existing tests pass without modification
5. `ResumeAllActions()` still correctly recalculates (no regression)

## Dependencies

None. This is a framework-internal change with no RemoteFactory impact.

## Risks

- **Low:** The `RecalculateValidity()` call adds a LINQ `.Any()` scan over the property bag after every `RunRules()`. This is the same cost already incurred in `ResumeAllActions()` and `Property_PropertyChanged`. For typical entity sizes (5-20 properties), this is negligible.
- **Low:** Calling `RecalculateValidity()` when not paused is redundant but harmless -- the cache is already correct.

---

## Architectural Verification

### Affected Base Classes

| Base Class | Affected? | Notes |
|---|---|---|
| `ValidateBase<T>` | Yes | Both `RunRules` overloads need the fix |
| `EntityBase<T>` | Indirectly | `IsSavable` delegates to `IsValid` which delegates to `PropertyManager.IsValid` |
| `EntityListBase<I>` | No | Lists delegate validity to children |
| `ValidateListBase<I>` | No | Lists delegate validity to children |

### Affected Factory Operations

| Operation | Affected? | Notes |
|---|---|---|
| `[Create]` | Potentially | If user calls `RunRules()` in Create (uncommon) |
| `[Fetch]` | Potentially | If user calls `RunRules()` in Fetch (uncommon) |
| `[Insert]` | Yes | Primary bug scenario -- Person example uses this pattern |
| `[Update]` | Yes | Person example uses this pattern |
| `[Delete]` | Potentially | If user calls `RunRules()` in Delete (uncommon) |
| `[Execute]` | No | Static commands, not entity lifecycle |

### Design Project Verification

Tests already exist at `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/RequiredDuringFactoryTests.cs` that reproduce the exact bug. Current test results (pre-fix):

| Test | Status | Notes |
|---|---|---|
| `RunRules_NotPaused_RequiredFieldsEmpty_IsValidFalse` | PASS | Baseline: rules work when not paused |
| `RunRules_DuringFactoryInsert_RequiredFieldsEmpty_IsValidFalse` | **FAIL** | Core bug: IsValid stale while paused |
| `RunRules_DuringFactoryInsert_RequiredFieldsEmpty_IsSelfValidFalse` | **FAIL** | Same bug for IsSelfValid |
| `RunRules_DuringFactoryInsert_RequiredFieldEmpty_IsSavableFalse` | **FAIL** | Real-world scenario with IsSavable |
| `RunRules_DuringFactoryUpdate_RequiredFieldsEmpty_IsValidFalse` | **FAIL** | Same bug during Update |
| `RunRules_AfterFactoryComplete_RequiredFieldsEmpty_IsValidFalse` | PASS | ResumeAllActions recalculates correctly |
| `RunRules_DuringFactoryInsert_RequiredFieldsSet_IsValidTrue` | PASS | Valid case stays valid |

**Acceptance criteria:** All 7 tests must pass after the fix. The 4 currently-failing tests ARE the acceptance criteria.

### Breaking Changes

None. `RecalculateValidity()` is additive. The behavioral change (cached values updated sooner) is a bug fix, not a breaking change.

### Pattern Consistency

The recalculation logic (`!PropertyBag.Any(p => !p.Value.IsValid)`) is already used in three places:
- `Property_PropertyChanged` (line 156)
- `ResumeAllActions` (line 307)
- `OnDeserialized` (line 236)

Adding `RecalculateValidity()` as a shared method improves consistency.

### Test Strategy

| Category | Test | Description |
|---|---|---|
| Unit/ | `ValidatePropertyManager_RecalculateValidity_UpdatesCachedIsValid` | Directly test the new method |
| Integration/Concepts/ | `RunRulesWhilePaused_RecalculatesCachedValidity` | Test RunRules during paused state |
| Design.Tests/ | `RunRulesInInsert_RequiredFieldEmpty_IsValidFalse` | End-to-end factory operation test |

### Edge Cases

1. **RunRules called when not paused:** `RecalculateValidity()` is redundant but harmless
2. **RunRules called multiple times while paused:** Each call recalculates, all correct
3. **No properties in bag:** `PropertyBag.Any()` returns false, `IsValid` stays true -- correct
4. **Child ValidateBase properties:** `ValidateProperty.IsValid` checks child's validity, so the cascade works correctly
5. **Cancellation during RunRules:** The `OperationCanceledException` catch calls `MarkInvalid` which sets ObjectInvalid, then finally block recalculates -- correct

### Files Examined

- `src/Neatoo/ValidateBase.cs` -- RunRules overloads, FactoryStart/FactoryComplete, PauseAllActions/ResumeAllActions, IsValid delegation
- `src/Neatoo/Internal/ValidatePropertyManager.cs` -- Cached IsValid/IsSelfValid, Property_PropertyChanged with IsPaused guard, ResumeAllActions recalculation
- `src/Neatoo/Internal/ValidateProperty.cs` -- SetMessagesForRule fires PropertyChanged, IsValid computed from RuleMessages
- `src/Neatoo/EntityBase.cs` -- IsSavable = IsModified && IsValid && !IsBusy && !IsChild
- `src/Neatoo/Rules/Rules/RequiredRule.cs` -- RequiredRule implementation
- `src/Neatoo/Rules/Rules/AllRequiredRulesExecuted.cs` -- Sets ObjectInvalid when required rules not executed
- `src/Neatoo/Rules/RuleManager.cs` -- SetMessagesForRule call site
- `src/Design/Design.Domain/FactoryOperations/SavePatterns.cs` -- SaveDemo entity (Design project)
- `src/Design/Design.Tests/FactoryTests/SaveTests.cs` -- Existing save tests
- `src/Examples/Person/Person.DomainModel/Person.cs` -- Person Insert/Update with RunRules pattern
- `docs/todos/completed/fetch-rules-paused-verification.md` -- Confirms factory lifecycle
- `docs/todos/business-rules-in-factory-methods-antipattern.md` -- Documents the RunRules-in-factory pattern

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-03

### My Understanding

**Core Change:** Add `RecalculateValidity()` to `ValidatePropertyManager` and call it from both `ValidateBase.RunRules` overloads in `finally` blocks, ensuring `IsValid`/`IsSelfValid` caches are accurate after explicit `RunRules()` calls regardless of paused state.

**User-Facing Impact:** `IsValid`, `IsSelfValid`, and `IsSavable` return correct values immediately after `RunRules()` inside factory operations.

### Codebase Investigation

**Files Examined:**
- `src/Neatoo/Internal/ValidatePropertyManager.cs` -- Confirmed cached `IsValid`/`IsSelfValid` fields (initialized `true`), `Property_PropertyChanged` early exit when paused (line 139), identical recalculation logic in `ResumeAllActions()` (lines 307, 314) and `OnDeserialized()` (lines 236-237)
- `src/Neatoo/ValidateBase.cs` -- Confirmed `RunRules(RunRulesFlag)` (line 851) has NO `finally` block. Confirmed `RunRules(string)` (line 814) has `CheckIfMetaPropertiesChanged()` in finally. Confirmed `IsValid` delegates to `PropertyManager.IsValid` (line 277)
- `src/Neatoo/EntityBase.cs` -- Confirmed `IsSavable` (line 153) reads `this.IsValid`. Confirmed `CheckIfMetaPropertiesChanged()` override has `IsPaused` guard (line 228)
- `src/Neatoo/IValidatePropertyManager.cs` -- No `RecalculateValidity()` currently exists
- `src/Neatoo/ValidateListBase.cs` -- Lists delegate `RunRules()` to children, have separate cache
- `src/Neatoo/EntityListBase.cs` -- Inherits from ValidateListBase, no RunRules override
- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/RequiredDuringFactoryTests.cs` -- All 7 tests confirmed, 4 fail/3 pass
- `src/Design/Design.Domain/FactoryOperations/SavePatterns.cs` -- No RunRules-during-factory pattern
- `src/Design/Design.Tests/FactoryTests/SaveTests.cs` -- No coverage of this scenario

**Design Project Verification:**
- The architect used existing failing unit tests as acceptance criteria instead of Design project compilation evidence
- Acceptable because: (a) runtime cache staleness cannot be demonstrated at compile time, (b) 4 concrete failing tests reproduce the exact bug

**Discrepancies Found:** None

### Concerns (Minor, Non-Blocking)

1. **Test strategy clarity:** Plan's Test Strategy table lists 3 new tests but also says the 4 existing tests ARE the acceptance criteria. Recommendation: use 4 existing failing tests as primary acceptance criteria, add unit test for `RecalculateValidity()` directly, skip redundant integration/design tests.

2. **Optional refactoring scope:** DRY refactoring of `ResumeAllActions`/`OnDeserialized` should be explicitly in-scope (4 lines of change, low risk, removes duplication) or explicitly excluded.

3. **Missing `CheckIfMetaPropertiesChanged` in `RunRules(RunRulesFlag)`:** Verified this is a pre-existing issue unrelated to this fix. During paused state, events are suppressed anyway. After `ResumeAllActions()`, the transition is handled correctly by `PropertyManager.ResumeAllActions()` firing `PropertyChanged` -> `_PropertyManager_PropertyChanged` -> `CheckIfMetaPropertiesChanged()`.

### What Looks Good

- Root cause analysis is thorough and accurate against source code
- Fix reuses existing logic (same LINQ expression in 3 places already)
- Fix placed in `finally` blocks for correctness on both success and failure paths
- Every base class, factory operation, and edge case addressed
- No breaking changes, no serialization impact, no RemoteFactory impact

### Verdict

**Approved.** This plan is exceptionally clear -- the bug is precisely reproduced with 4 committed failing tests, the fix reuses existing logic, and every claim in the plan was verified against the source code.

## Implementation Contract

**Created:** 2026-03-03
**Approved by:** neatoo-developer

### Acceptance Criteria (Failing Tests)

These 4 tests currently fail and must pass after the fix:

- [x] `RequiredDuringFactoryTests.RunRules_DuringFactoryInsert_RequiredFieldsEmpty_IsValidFalse`
- [x] `RequiredDuringFactoryTests.RunRules_DuringFactoryInsert_RequiredFieldsEmpty_IsSelfValidFalse`
- [x] `RequiredDuringFactoryTests.RunRules_DuringFactoryInsert_RequiredFieldEmpty_IsSavableFalse`
- [x] `RequiredDuringFactoryTests.RunRules_DuringFactoryUpdate_RequiredFieldsEmpty_IsValidFalse`

These 3 tests currently pass and must continue to pass:

- [x] `RequiredDuringFactoryTests.RunRules_NotPaused_RequiredFieldsEmpty_IsValidFalse`
- [x] `RequiredDuringFactoryTests.RunRules_AfterFactoryComplete_RequiredFieldsEmpty_IsValidFalse`
- [x] `RequiredDuringFactoryTests.RunRules_DuringFactoryInsert_RequiredFieldsSet_IsValidTrue`

### In Scope

1. [x] Add `void RecalculateValidity()` to `IValidatePropertyManager<P>` interface (`src/Neatoo/IValidatePropertyManager.cs`)
2. [x] Implement `RecalculateValidity()` in `ValidatePropertyManager<P>` (`src/Neatoo/Internal/ValidatePropertyManager.cs`)
3. [x] Add `this.PropertyManager.RecalculateValidity()` in `finally` block of `ValidateBase.RunRules(RunRulesFlag)` (`src/Neatoo/ValidateBase.cs`)
4. [x] Add `this.PropertyManager.RecalculateValidity()` before `CheckIfMetaPropertiesChanged()` in `ValidateBase.RunRules(string)` (`src/Neatoo/ValidateBase.cs`)
5. [x] Refactor `ResumeAllActions()` in `ValidatePropertyManager` to use `RecalculateValidity()` (DRY)
6. [x] Refactor `OnDeserialized()` in `ValidatePropertyManager` to use `RecalculateValidity()` (DRY)
7. [x] Add unit test: `ValidatePropertyManager_RecalculateValidity_UpdatesCachedValues` testing the method directly (4 tests added)
8. [x] **Checkpoint:** Run `RequiredDuringFactoryTests` -- all 7 pass
9. [x] **Checkpoint:** Run full test suite -- 1743 passed, 0 failed, 1 skipped (pre-existing)
10. [x] **Checkpoint:** `dotnet build src/Design/Design.sln` -- succeeded

### Explicitly Out of Scope

- Adding `CheckIfMetaPropertiesChanged()` to `RunRules(RunRulesFlag)` overload (pre-existing issue, separate todo)
- Redundant integration test or Design.Tests test (existing 7 tests sufficient)
- Any changes to `ValidateListBase`, `EntityListBase`, or list caching behavior

### Verification Gates

1. After items 1-4: Run `RequiredDuringFactoryTests` -- all 7 tests pass
2. After items 5-6: Run `RequiredDuringFactoryTests` again -- no regression from refactoring
3. After item 7: New unit test passes
4. Final: Full test suite passes, Design.sln builds

### Stop Conditions

If any of these occur, STOP and report:
- Any out-of-scope test starts failing
- `ResumeAllActions()` behavior changes after DRY refactoring (regression)
- Architectural contradiction discovered in the cache invalidation flow
- Code does not compile

## Implementation Progress

- 2026-03-03: Implementation started
- 2026-03-03: Items 1-4 complete (interface, implementation, both RunRules call sites). Verification gate 1: All 7 RequiredDuringFactoryTests pass.
- 2026-03-03: Items 5-6 complete (DRY refactoring of ResumeAllActions and OnDeserialized). No regression.
- 2026-03-03: Item 7 complete (4 unit tests for RecalculateValidity). All pass.
- 2026-03-03: Final verification: All 1743 unit tests pass, 245 samples pass, 84 Design.Tests pass, Design.sln builds clean.

## Completion Evidence

### Files Modified

**Production code (3 files):**
- `src/Neatoo/IValidatePropertyManager.cs` -- Added `void RecalculateValidity()` to interface
- `src/Neatoo/Internal/ValidatePropertyManager.cs` -- Added `RecalculateValidity()` implementation, refactored `ResumeAllActions()` and `OnDeserialized()` to use it
- `src/Neatoo/ValidateBase.cs` -- Added `this.PropertyManager.RecalculateValidity()` in finally blocks of both `RunRules` overloads

**Test code (1 file):**
- `src/Neatoo.UnitTest/Unit/Core/ValidatePropertyManagerTests.cs` -- Added 4 `RecalculateValidity` unit tests in new `#region RecalculateValidity Tests`

### Test Results

**RequiredDuringFactoryTests (acceptance criteria):** 7/7 passed (4 previously-failing now pass)
```
Passed!  - Failed:     0, Passed:     7, Skipped:     0, Total:     7
```

**Full test suite (Neatoo.UnitTest):** 1743 passed, 0 failed, 1 skipped (pre-existing)
```
Passed!  - Failed:     0, Passed:  1743, Skipped:     1, Total:  1744
```

**Samples:** 245 passed, 0 failed
```
Passed!  - Failed:     0, Passed:   245, Skipped:     0, Total:   245
```

**Design.Tests:** 84 passed, 0 failed
```
Passed!  - Failed:     0, Passed:    84, Skipped:     0, Total:    84
```

**Design.sln build:** Succeeded, 0 errors, 0 warnings

### No Stop Conditions Triggered

- No out-of-scope tests failed
- ResumeAllActions() behavior unchanged after DRY refactoring (verified by full test suite)
- No architectural contradictions discovered
- All code compiles cleanly

## Documentation

### Expected Deliverables

- Update `docs/todos/business-rules-in-factory-methods-antipattern.md` if needed (the pattern it recommends now works correctly)
- No public API documentation changes needed (behavioral bug fix)
- Release notes entry for the fix

### Completed

*To be filled by documentation agent.*

## Architect Verification

**Verified:** 2026-03-03
**Verdict:** VERIFIED

### Independent Test Results

All builds and tests were run independently by the architect. No developer-reported results were trusted without re-running.

**RequiredDuringFactoryTests (7 acceptance criteria tests):**
```
Passed!  - Failed:     0, Passed:     7, Skipped:     0, Total:     7
```
All 4 previously-failing tests now pass:
- `RunRules_DuringFactoryInsert_RequiredFieldsEmpty_IsValidFalse` -- PASS
- `RunRules_DuringFactoryInsert_RequiredFieldsEmpty_IsSelfValidFalse` -- PASS
- `RunRules_DuringFactoryInsert_RequiredFieldEmpty_IsSavableFalse` -- PASS
- `RunRules_DuringFactoryUpdate_RequiredFieldsEmpty_IsValidFalse` -- PASS

All 3 previously-passing tests still pass (no regression):
- `RunRules_NotPaused_RequiredFieldsEmpty_IsValidFalse` -- PASS
- `RunRules_AfterFactoryComplete_RequiredFieldsEmpty_IsValidFalse` -- PASS
- `RunRules_DuringFactoryInsert_RequiredFieldsSet_IsValidTrue` -- PASS

**RecalculateValidity unit tests (4 new tests):**
```
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4
```

**Full Neatoo.UnitTest suite:**
```
Passed!  - Failed:     0, Passed:  1743, Skipped:     1, Total:  1744
```
The 1 skip is `AsyncFlowTests_CheckAllRules` (pre-existing).

**Design.Tests:**
```
Passed!  - Failed:     0, Passed:    84, Skipped:     0, Total:    84
```

**Design.sln build:**
```
Build succeeded. 0 Warning(s), 0 Error(s)
```

**Person.DomainModel.Tests (directly affected by this fix):**
```
Passed!  - Failed:     0, Passed:    55, Skipped:     0, Total:    55
```

### Code Review Against Plan

**Change 1: `RecalculateValidity()` added to `IValidatePropertyManager<P>` interface** (`src/Neatoo/IValidatePropertyManager.cs`)
- Matches plan: `void RecalculateValidity()` with correct XML doc comment
- Added at end of interface, clean placement

**Change 2: `RecalculateValidity()` implemented in `ValidatePropertyManager<P>`** (`src/Neatoo/Internal/ValidatePropertyManager.cs`)
- Matches plan exactly: same LINQ expressions (`!PropertyBag.Any(p => !p.Value.IsValid)` and `!PropertyBag.Any(p => !p.Value.IsSelfValid)`)
- Correct XML doc comment including the note about not raising PropertyChanged events
- Method is `public` as required by the interface

**Change 3: `RunRules(string)` in `ValidateBase.cs`**
- `RecalculateValidity()` call added in `finally` block, BEFORE `CheckIfMetaPropertiesChanged()`
- Matches plan design exactly -- ensures cache is fresh before meta-state change detection reads it

**Change 4: `RunRules(RunRulesFlag)` in `ValidateBase.cs`**
- `RecalculateValidity()` call added in a new `finally` block
- Matches plan design -- this overload previously had NO finally block

**Change 5 (DRY): `OnDeserialized()` in `ValidatePropertyManager.cs`**
- Previous inline LINQ replaced with `RecalculateValidity()` call
- Diff confirms exact 2-line removal, 1-line addition -- behavior preserved

**Change 6 (DRY): `ResumeAllActions()` in `ValidatePropertyManager.cs`**
- Previous inline LINQ replaced with `RecalculateValidity()` call
- `wasValid` and `wasSelfValid` captures moved together before the call for clarity
- Conditional `RaisePropertyChanged` calls preserved -- behavior identical
- Diff confirms clean refactoring with no logic changes

### Scope Verification

**Modified files (4 total):**
1. `src/Neatoo/IValidatePropertyManager.cs` -- Interface addition only
2. `src/Neatoo/Internal/ValidatePropertyManager.cs` -- New method + DRY refactoring
3. `src/Neatoo/ValidateBase.cs` -- Two `RecalculateValidity()` calls in finally blocks
4. `src/Neatoo.UnitTest/Unit/Core/ValidatePropertyManagerTests.cs` -- 4 new unit tests in `#region RecalculateValidity Tests`

**No out-of-scope modifications:** `git status` confirms only these 4 files are modified. No out-of-scope tests were touched.

### Documentation Status

No public API documentation changes needed -- this is a behavioral bug fix. The `RecalculateValidity()` method is on a public interface but is framework-internal infrastructure. Release notes entry should be created when the version is bumped.
