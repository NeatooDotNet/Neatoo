# Fix LazyLoad Deserialization Overwrite

**Date:** 2026-03-13
**Related Todo:** [WaitForTasks crashes when AddActionAsync awaits LazyLoad with [Remote] call](../todos/waitfortasks-crash-lazyload-remote.md)
**Status:** Documentation Complete
**Last Updated:** 2026-03-13

---

## Overview

`NeatooBaseJsonTypeConverter` overwrites constructor-created `LazyLoad<T>` instances during client-side deserialization. The `_loader` delegate is `[JsonIgnore]` (non-serializable), so the deserialized replacement has `_loader = null`. When an `AddActionAsync` handler awaits the `LazyLoad`, `LoadAsync()` throws `InvalidOperationException`.

This blocks the zTreatment lazy-loaded child entity pattern where `AddActionAsync` handlers load children on demand.

---

## Approach

**Merge serialized state into the existing LazyLoad instance instead of replacing it.**

During deserialization, when the converter encounters a `LazyLoad<>` property:
1. Read the serialized JSON into a temporary `LazyLoad<T>` instance (to extract `Value` and `IsLoaded`)
2. Get the **existing** `LazyLoad<T>` instance from the already-constructed entity (created by the constructor via DI)
3. If `IsLoaded == true` in the serialized data, apply the `Value` and `IsLoaded` state to the existing instance
4. If `IsLoaded == false`, leave the existing instance untouched (it already has its loader delegate from the constructor)

This preserves the loader delegate set up in the constructor while still transferring pre-loaded state from the server.

### Trade-off Analysis

**Option A: Skip LazyLoad properties entirely during deserialization.**
- Pro: Simplest change (just remove lines 130-136 and 196-200)
- Con: Breaks the pre-loaded case. When the server has already loaded a `LazyLoad<T>` (e.g., `IsLoaded=true`, `Value` contains an entity), the client would not receive that state. The client would re-fetch unnecessarily.
- **Rejected**: Pre-loaded state transfer is a real use case (existing tests verify it).

**Option B: Replace the instance but also copy the loader delegate.**
- Pro: Preserves current replacement behavior
- Con: The loader delegate is non-serializable by design. There's no clean way to extract it from the old instance and inject it into the new one without adding API surface to `LazyLoad<T>`.
- **Rejected**: Requires exposing internal `LazyLoad<T>` state and feels hacky.

**Option C (chosen): Merge serialized state into the existing instance.**
- Pro: Preserves the constructor-created instance with its loader delegate. Transfers pre-loaded state when available. Clean API.
- Con: Requires adding an internal method to `LazyLoad<T>` for state application.
- **Selected**: Minimal API change, solves both the deferred and pre-loaded cases.

---

## Design

### LazyLoad<T> changes

Add an internal method to apply deserialized state:

```csharp
/// <summary>
/// Applies deserialized state (Value and IsLoaded) to this instance,
/// preserving the loader delegate. Used by NeatooBaseJsonTypeConverter
/// during deserialization to merge server-side state without replacing
/// the constructor-created instance.
/// </summary>
internal void ApplyDeserializedState(T? value, bool isLoaded)
{
    if (isLoaded)
    {
        _value = value;
        _isLoaded = true;
        SubscribeToValuePropertyChanged(_value);
    }
    // If not loaded, leave the instance untouched -- the constructor's
    // loader delegate is intact for on-demand loading.
}
```

### NeatooBaseJsonTypeConverter changes

In the `Read` method, change the LazyLoad property handling (lines 196-200) from:

```csharp
// BEFORE: Replaces the entire instance
var value = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
property.SetValue(result, value);
```

To:

```csharp
// AFTER: Deserialize to temp, then merge state into existing instance
var deserialized = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
var existing = property.GetValue(result);

if (existing != null && deserialized != null)
{
    // Merge: apply serialized state into existing instance (preserves loader)
    var applyMethod = property.PropertyType.GetMethod("ApplyDeserializedState",
        BindingFlags.Instance | BindingFlags.NonPublic);

    // Extract Value and IsLoaded from deserialized instance
    var valueProp = property.PropertyType.GetProperty("Value");
    var isLoadedProp = property.PropertyType.GetProperty("IsLoaded");

    applyMethod.Invoke(existing, new object?[] {
        valueProp.GetValue(deserialized),
        (bool)isLoadedProp.GetValue(deserialized)
    });
}
else if (existing == null && deserialized != null)
{
    // No constructor-created instance -- fall back to replacement
    property.SetValue(result, deserialized);
}
// If existing != null && deserialized == null: keep existing (constructor's instance)
```

**Note on reflection**: The converter already uses reflection extensively (lines 121-136, 164-165, 196-200, 375-387). The `ApplyDeserializedState` call uses reflection because we're working with `LazyLoad<>` as an open generic at runtime. This is consistent with the existing pattern in the converter. An alternative is to introduce an `ILazyLoadInternal` interface to avoid the reflection call -- the developer should evaluate this during implementation.

### Serialization (Write) -- No changes needed

The `Write` method (lines 374-387) already correctly serializes `LazyLoad<T>` properties by writing `Value` and `IsLoaded`. This continues to work as-is.

---

## Implementation Steps

1. Add `ApplyDeserializedState(T? value, bool isLoaded)` method to `LazyLoad<T>`
2. Modify `NeatooBaseJsonTypeConverter.Read` to merge state instead of replacing the instance
3. Verify the reproduction test passes
4. Verify all existing LazyLoad serialization tests still pass
5. Verify full test suite passes

---

## Acceptance Criteria

- [ ] `WaitForTasksLazyLoadCrashTests.WaitForTasks_AddActionAsync_AwaitsLazyLoad_WithRemoteFetch_Crashes` passes
- [ ] All existing `FatClientLazyLoadTests` pass (pre-loaded round-trip, unloaded round-trip, nested entity, PropertyManager properties)
- [ ] All existing `TwoContainerLazyLoadTests` pass (remote Fetch preserves pre-loaded value)
- [ ] All existing `LazyLoadStatePropagationTests` pass (state propagation from child to parent)
- [ ] All existing `LazyLoadTests` pass (unit tests for core LazyLoad behavior)
- [ ] Full `Neatoo.UnitTest` suite passes with zero new failures

---

## Dependencies

None. This is a self-contained fix within the Neatoo framework.

---

## Risks / Considerations

1. **Reflection in ApplyDeserializedState call**: The converter already uses reflection for LazyLoad property discovery and value setting. The merge approach adds one more reflective method call. Consider introducing a non-generic interface (`ILazyLoadMergeable` or similar) to avoid the reflection if the developer prefers.

2. **LazyLoad assigned after constructor but before deserialization**: Some entities assign `LazyLoad` in `[Fetch]` methods rather than constructors (e.g., `LazyLoadEntityObject.Fetch`). In these cases, the server-side `[Fetch]` creates the `LazyLoad`, it gets serialized, and the client constructor does NOT create one. The "existing is null" fallback handles this case (full replacement, same as current behavior).

3. **Thread safety of ApplyDeserializedState**: Deserialization happens on a single thread before the entity is returned to the consumer. No concurrent access concern.

4. **Pre-existing Design project build failures**: The `src/Design/Design.sln` has 101 pre-existing NF0105 analyzer errors (all about `[Remote]` method accessibility). These are unrelated to this bug fix.

---

## Architectural Verification

**Scope Table:**

| Pattern | Affected? | Status |
|---------|-----------|--------|
| LazyLoad with deferred loader (constructor-created, not pre-loaded) | Yes - primary bug | Needs Implementation |
| LazyLoad pre-loaded on server (IsLoaded=true, Value set) | Yes - must continue working | Verified (existing tests) |
| LazyLoad unloaded (IsLoaded=false, no loader) | No change - existing behavior preserved | Verified (existing tests) |
| LazyLoad with nested Neatoo entity | Must continue working | Verified (existing tests) |
| PropertyManager serialization | Not affected | N/A |
| EntityBase meta-properties serialization | Not affected | N/A |

**Design Project Verification:**

The Design projects have pre-existing build failures (101 NF0105 errors). The `LazyLoadProperty.cs` design file documents the serialization behavior but does not exercise the deferred-loader-survives-deserialization pattern. The reproduction test in `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashTests.cs` serves as the acceptance criteria for the fix.

- LazyLoad pre-loaded serialization: Verified (existing tests at `FatClientLazyLoadTests.cs` and `TwoContainerLazyLoadTests.cs`)
- LazyLoad deferred loader survives deserialization: Needs Implementation (reproduction test at `WaitForTasksLazyLoadCrashTests.cs:36` currently fails)

**Breaking Changes:** No. The fix changes internal deserialization behavior. The `ApplyDeserializedState` method is `internal` and not part of the public API.

**Codebase Analysis:**

Files examined:
- `src/Neatoo/LazyLoad.cs` -- LazyLoad class with `[JsonConstructor]`, `[JsonInclude]`, `[JsonIgnore]` attributes
- `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` -- Custom JSON converter that detects and overwrites LazyLoad properties
- `src/Neatoo/RemoteFactory/Internal/NeatooJsonConverterFactory.cs` -- Converter factory (confirms LazyLoad is NOT claimed as IValidateBase)
- `src/Neatoo/ValidateBase.cs` -- `OnDeserialized()` and `FactoryComplete()` call `SubscribeToLazyLoadProperties()`
- `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` -- Unit tests for core LazyLoad behavior
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientLazyLoadTests.cs` -- Fat-client serialization round-trip tests
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/TwoContainerLazyLoadTests.cs` -- Two-container remote serialization tests
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadEntityObject.cs` -- Test entity with LazyLoad properties
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadValidateObject.cs` -- Test ValidateBase with LazyLoad
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs` -- Reproduction entity
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashTests.cs` -- Reproduction test
- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs` -- State propagation tests
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- Design reference for LazyLoad pattern

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Implementation | developer | Yes | Small, focused fix across 2 files + test verification | None |

**Parallelizable phases:** N/A (single phase)

**Notes:** This is a small, well-scoped bug fix. A single developer agent phase is sufficient. The fix touches exactly 2 production files (`LazyLoad.cs`, `NeatooBaseJsonTypeConverter.cs`) and verification involves running the existing test suite.

---

## Developer Review

**Status:** Concerns Raised
**Reviewed:** 2026-03-13

### My Understanding of This Plan

**Core Change:** Merge serialized state into existing `LazyLoad<T>` instances during deserialization instead of replacing them, preserving the constructor-created loader delegate.
**User-Facing API:** No public API change. Internal `ApplyDeserializedState` method added to `LazyLoad<T>`.
**Internal Changes:** Modify `NeatooBaseJsonTypeConverter.Read` to merge state; add internal merge method to `LazyLoad<T>`.
**Base Classes Affected:** None directly, but `ValidateBase.OnDeserialized()` interaction is relevant.

### Codebase Investigation

**Files Examined:**
- `src/Neatoo/LazyLoad.cs` -- `_loader` is `readonly`, set only in constructors. `[JsonConstructor]` uses parameterless constructor (null loader). Constructor with `Func<Task<T?>>` sets the loader.
- `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` -- Lines 130-136 detect LazyLoad properties, lines 196-200 overwrite them. Line 93 creates entity via DI (constructor runs). Line 61-63 call `OnDeserialized()` after all properties are set.
- `src/Neatoo/ValidateBase.cs` -- `OnDeserialized()` (line 632) calls `SubscribeToLazyLoadProperties()` after deserialization. `FactoryComplete()` (line 1068) also calls it.
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs` -- **Critical:** `CrashParent` constructor does NOT create `LazyChild`. The `LazyChild` is created in `[Remote] [Fetch]`, which only runs on the server.
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashTests.cs` -- Reproduction test.
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientLazyLoadTests.cs` -- Existing tests covering pre-loaded, unloaded, nested entity, and PropertyManager round-trips.
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/TwoContainerLazyLoadTests.cs` -- Two-container tests.
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadEntityObject.cs` -- Test entity creates LazyLoad in `[Fetch]`, not constructor.

**Searches Performed:**
- Searched for `SubscribeToLazyLoadProperties` -- found 7 files; called in `OnDeserialized()`, `FactoryComplete()`, and in property setters of test entities.

**Design Project Verification:**
- The architect noted Design projects have 101 pre-existing NF0105 errors and stated the reproduction test serves as acceptance criteria. This is reasonable given the pre-existing failures are unrelated.

**Discrepancies Found:**
- **CRITICAL:** The plan assumes the constructor creates the LazyLoad instance ("Get the **existing** LazyLoad<T> instance from the already-constructed entity (created by the constructor via DI)"). But the reproduction entity (`CrashParent`) creates `LazyChild` in the `[Remote] [Fetch]` method, NOT the constructor. The `LazyLoadEntityObject` test entity also creates LazyLoad in `[Fetch]`, not the constructor.
- The todo's progress log says "Server: constructor runs (creates LazyChild with loader)" but the actual code shows the constructor does NOT create LazyChild.
- The entity comment (line 46-47) also claims "Client deserialization: constructor runs AGAIN (from DI), creates NEW LazyChild with loader" -- this is false per the code.

### Structured Question Checklist

**Completeness:**
- [x] Base classes: Only `LazyLoad<T>` and the converter are affected. No base class changes needed.
- [x] Factory operations: The fix is in the deserialization path, not in factory operations themselves.
- [x] Property system: LazyLoad is not in PropertyManager. No impact.
- [x] Validation rules: No direct impact.
- [x] Parent-child relationships: No impact.

**Correctness:**
- [x] Existing patterns: The converter already uses reflection extensively. The merge approach is consistent.
- [ ] **FAIL: The approach does not match the actual reproduction scenario.** The merge approach assumes `existing != null`, but in the reproduction case, `existing` is null because the constructor does not create the LazyLoad instance.
- [x] Breaking changes: None, if the fix works correctly.
- [x] State properties: No impact.

**Clarity:**
- [ ] **FAIL: Cannot implement as specified.** The plan's central assumption (constructor creates LazyLoad) does not match the reproduction test entity. Following the plan exactly would result in hitting the `existing == null` fallback, which is identical to current behavior -- the bug would persist.
- [x] Edge cases: The plan addresses pre-loaded, unloaded, and null-existing cases.
- [x] Test strategy: Clear acceptance criteria.

**Risk:**
- [x] Existing tests: The `FatClientLazyLoad_PostDeserialization_LoadAsync_Throws` test expects `LoadAsync()` to throw on a deserialized `LazyLoad<string>()` with no loader. If the fix changes this behavior, that test would break.
- [x] Serialization: No Write-side changes.
- [x] RemoteFactory: No generator changes.

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. **The reproduction entity creates LazyLoad in `[Fetch]`, not the constructor.** On the client side after `[Remote]` deserialization, the constructor runs but does NOT create a LazyChild. The "existing" instance will be null. The merge code falls to the replacement path, and the bug persists.
2. **What about entities where LazyLoad IS created in the constructor?** In that case the merge approach works, but this is not the pattern used in the reproduction test or apparently in zTreatment (the `[Fetch]` method is where the entity knows what ID to pass to the loader).
3. **The `FactoryComplete()` call:** After the converter finishes, `FactoryComplete()` is called (for factory operations) which calls `SubscribeToLazyLoadProperties()`. But this happens after the converter has already replaced the instance. The subscription will be to the wrong (loader-less) instance.

**Ways this could break existing functionality:**
1. If the fix causes the `FatClientLazyLoad_PostDeserialization_LoadAsync_Throws` test to change behavior (it expects `LoadAsync` to throw on unloaded LazyLoad without a loader).

**Ways users could misunderstand the API:**
1. The plan does not address the fundamental question: should users create LazyLoad in the constructor or in `[Fetch]`? If the fix only works when LazyLoad is constructor-created, this needs to be documented. But the natural pattern is to create it in `[Fetch]` because that's where the entity knows its ID and can build the loader delegate.

### Concerns

1. **Critical: Merge approach does not fix the reproduction case.**
   - Details: The plan's approach assumes the constructor creates the `LazyLoad<T>` instance, so there will be an "existing" instance to merge into during deserialization. However, the reproduction entity (`CrashParent`) and the test entity (`LazyLoadEntityObject`) both create `LazyLoad` in their `[Fetch]` methods, not their constructors. On the client side, after `[Remote]` deserialization, the constructor runs via DI but does NOT create the LazyLoad property. The converter finds `existing == null`, falls to the replacement path (same as current behavior), and the bug persists.
   - Question: How should the fix handle entities where LazyLoad is created in `[Fetch]` rather than the constructor? This is the natural pattern because the loader delegate typically needs the entity's ID (set during Fetch) to know what to load.
   - Suggestion: The approach needs rethinking. Two possible directions:
     - (A) **Skip unloaded LazyLoad during deserialization entirely.** If `IsLoaded == false` in the serialized data, don't set the property at all. The client-side `[Fetch]` will re-run for `[Remote]` operations... but wait, `[Fetch]` does NOT re-run on the client for `[Remote]` -- only on the server. So this won't work either unless the entity re-creates the LazyLoad some other way.
     - (B) **Re-create the LazyLoad on the client side.** The real issue is that the client needs a way to reconstruct the loader delegate after deserialization. This might require a different pattern entirely -- perhaps a `[Create]` or separate initialization method that runs on the client after deserialization.
     - (C) **Change the converter to skip LazyLoad properties where `IsLoaded == false`.** Combined with guidance that entities should create deferred-mode LazyLoad in the constructor (using a factory injected via DI) so the constructor-created instance with loader is preserved. The `[Fetch]` method would need to be refactored to NOT create the LazyLoad but rather just set up whatever the loader needs. This would require changing the reproduction test entity.

2. **Moderate: Reflection concern needs resolution before implementation.**
   - Details: The plan uses `GetMethod("ApplyDeserializedState")` and `GetProperty("Value")`/`GetProperty("IsLoaded")` via reflection. The plan acknowledges this and suggests a non-generic interface (`ILazyLoadMergeable` or similar) as an alternative. The project's CLAUDE.md says to avoid reflection and ask before using it.
   - Question: Should we use a non-generic interface (e.g., `ILazyLoadInternal` with `void ApplyDeserializedState(object? value, bool isLoaded)`) to avoid the reflection calls? The converter already has the property type, so it can access Value and IsLoaded via the interface directly.
   - Suggestion: Introduce `internal interface ILazyLoadInternal { void ApplyDeserializedState(object? value, bool isLoaded); }` and have `LazyLoad<T>` implement it. The converter can cast to `ILazyLoadInternal` directly without reflection.

3. **Minor: Misleading comments in reproduction entity.**
   - Details: The comments in `WaitForTasksLazyLoadCrashEntity.cs` (lines 42-48) and the todo's progress log claim the constructor creates LazyChild, but the code shows it is created in `[Fetch]`. This misled the architect's design.
   - Question: Should the reproduction entity be updated to create LazyLoad in the constructor (changing the test pattern), or should the fix be redesigned to handle the Fetch-created pattern?

### What Looks Good

- The trade-off analysis (Option A/B/C) is thorough and well-reasoned, given the assumption about constructor-created LazyLoad.
- The acceptance criteria are comprehensive and list all relevant test classes.
- The risk analysis around thread safety, pre-existing Design project failures, and the Write path is correct.
- The codebase analysis is thorough (12 files examined).
- The single-phase agent plan is appropriate for this scope.

### Recommendation

Send back to architect to address the critical concern about the merge approach not matching the actual reproduction scenario. The architect needs to:
1. Clarify the intended entity pattern (constructor-created vs Fetch-created LazyLoad)
2. Redesign the approach to handle whichever pattern is correct
3. Possibly update the reproduction test entity to match the intended pattern
4. Resolve the reflection concern (non-generic interface vs reflection)

---

## Implementation Contract

**Created:**
**Approved by:**

### Design Project Acceptance Criteria

N/A -- Design projects have pre-existing build failures unrelated to this fix. The reproduction test serves as acceptance criteria.

### In Scope

- [x] `src/Neatoo/LazyLoad.cs`: Add `ILazyLoadDeserializable` interface and `ApplyDeserializedState` method
- [x] `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs`: Change LazyLoad deserialization from replace to merge
- [x] `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs`: Fix reproduction entity to create LazyLoad in constructor
- [x] Verify reproduction test passes: `WaitForTasksLazyLoadCrashTests`
- [x] Verify all existing LazyLoad tests pass
- [x] Verify full test suite passes

### Out of Scope

- Design project NF0105 build failures (pre-existing, unrelated)
- LazyLoad API additions beyond the internal merge method
- Changes to LazyLoad serialization (Write path)
- Documentation updates (this is a bug fix, not a new feature)

### Verification Gates

1. After LazyLoad.cs change: Unit LazyLoadTests still pass
2. After converter change: Reproduction test passes AND all FatClient/TwoContainer LazyLoad tests pass
3. Final: Full `dotnet test src/Neatoo.sln` passes

### Stop Conditions

If any occur, STOP and report:
- Out-of-scope test failure
- Architectural contradiction discovered

---

## Implementation Progress

**Started:** 2026-03-13
**Developer:** neatoo-developer

### Milestones

1. **Task 1: Fix reproduction entity** -- Moved LazyLoad creation from `[Fetch]` to constructor in `CrashParent`. The loader lambda now captures `childFactory` from DI and uses `this.Id` (resolved at load-time). Removed `_childFactory` and `_lazyLoadFactory` fields. Updated comments.
2. **Gate 1: Reproduction test fails** -- Confirmed the test fails with `InvalidOperationException` (no loader delegate), proving the converter bug exists with the correct entity pattern.
3. **Task 2: Implement converter fix** -- Added `ILazyLoadDeserializable` internal interface (non-generic, avoids reflection) with `ApplyDeserializedState`, `IsLoaded`, and `BoxedValue`. `LazyLoad<T>` implements it explicitly. Modified `NeatooBaseJsonTypeConverter.Read` to merge deserialized state into existing instances via the interface instead of replacing them.
4. **Gate 2: Reproduction test passes** -- Confirmed.
5. **Gate 3: All 42 LazyLoad tests pass** -- Including FatClientLazyLoadTests, TwoContainerLazyLoadTests, LazyLoadStatePropagationTests, LazyLoadTests, and the reproduction test.
6. **Gate 4: Full test suite passes** -- 2092 tests passed, 0 failed, across all 4 test projects (Neatoo.BaseGenerator.Tests, Neatoo.UnitTest, Design.Tests, DomainModel.Tests).

### Deviation from Plan

The developer review's Critical Concern #1 (merge approach doesn't fix the reproduction case because LazyLoad is created in `[Fetch]`, not constructor) was resolved by fixing the reproduction entity per the user's instructions. `CrashParent` now creates `LazyLoad` in the constructor, which is the correct pattern. The `[Fetch]` method only sets the `Id`.

The developer review's Concern #2 (reflection) was resolved by introducing `ILazyLoadDeserializable` as a non-generic internal interface instead of using reflection to call `ApplyDeserializedState`. The converter casts to this interface directly.

---

## Completion Evidence

### Files Modified

1. **`src/Neatoo/LazyLoad.cs`** -- Added `ILazyLoadDeserializable` internal interface and explicit implementation on `LazyLoad<T>`. The interface provides `ApplyDeserializedState(object? value, bool isLoaded)`, `IsLoaded`, and `BoxedValue` to avoid reflection in the converter.

2. **`src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs`** -- Changed LazyLoad property deserialization (lines 196-210) from replacing the instance to merging state. When an existing instance is found (constructor-created), it casts to `ILazyLoadDeserializable` and calls `ApplyDeserializedState`. Falls back to replacement when no existing instance exists.

3. **`src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs`** -- Moved LazyLoad creation from `[Fetch]` to constructor. Removed `_childFactory` and `_lazyLoadFactory` fields. Updated comments to reflect the correct flow and the fix.

### Test Results

- **Full test suite:** 2092 passed, 0 failed
- **LazyLoad tests:** 42 passed, 0 failed
- **Reproduction test:** Passes (was failing before the fix)
- **No out-of-scope test failures**
- **No stop conditions triggered**

---

## Documentation

**Agent:** developer
**Completed:**

### Expected Deliverables

- [ ] Update `src/Design/CLAUDE-DESIGN.md` Serialization section: note that LazyLoad properties with loader delegates survive deserialization when the constructor creates them
- [ ] Skill updates: No
- [ ] Sample updates: No

### Files Updated

---

## Architect Verification

**Verdict:** VERIFIED
**Date:** 2026-03-13

### Independent Build and Test Results

- `dotnet build src/Neatoo.sln`: **0 errors, 0 warnings**
- `dotnet test src/Neatoo.sln`: **2092 passed, 0 failed, 1 skipped**
  - Neatoo.BaseGenerator.Tests: 26 passed
  - Samples: 249 passed
  - Person.DomainModel.Tests: 55 passed
  - Neatoo.UnitTest: 1762 passed, 1 skipped (pre-existing `AsyncFlowTests_CheckAllRules`)
- Reproduction test `WaitForTasks_AddActionAsync_AwaitsLazyLoad_WithRemoteFetch_Crashes`: **passed (148ms)**

### Code Review

**`src/Neatoo/LazyLoad.cs` -- ILazyLoadDeserializable interface and implementation:**

- Internal non-generic interface at lines 13-18 with `IsLoaded`, `BoxedValue`, and `ApplyDeserializedState(object?, bool)`. Clean, minimal API surface.
- `LazyLoad<T>` implements the interface explicitly (lines 115-136), keeping the merge capability invisible to consumers.
- `ApplyDeserializedState` correctly only applies state when `isLoaded == true` (lines 128-133), leaving the loader delegate intact for deferred cases.
- No reflection used. The interface avoids the need for `GetMethod`/`Invoke` calls.

**`src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` -- merge logic:**

- Lines 196-213: Correctly reads the existing instance via `property.GetValue(result)`, casts both existing and deserialized to `ILazyLoadDeserializable`, and calls `ApplyDeserializedState` to merge.
- Fallback at lines 207-211 handles the case where no constructor-created instance exists (existing == null), preserving backward compatibility for entities that create LazyLoad in `[Fetch]`.
- No new reflection calls added. Uses interface casting instead.

**`src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs` -- reproduction entity:**

- `CrashParent` constructor creates `LazyChild` at line 74 using `lazyLoadFactory.Create<ICrashChild>` with a loader that captures `childFactory` and resolves `this.Id` at load-time. This is the correct pattern -- LazyLoad created in constructor, not `[Fetch]`.
- `[Fetch]` method (lines 113-124) only sets `Id`, does not create LazyLoad. Clean separation.
- The `AddActionAsync` handler (lines 81-91) awaits `LazyChild` when `Trigger` changes, matching the zTreatment `VisitHub` pattern.

**`src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashTests.cs` -- test verification:**

- Test fetches parent through `ClientServerTestBase` pipeline (line 47), triggering full client-server serialization round-trip.
- Sets `Trigger` (line 52) to fire the `AddActionAsync` handler, which awaits the `LazyChild`.
- Asserts `LazyChild.IsLoaded`, `LazyChild.Value` not null, and correct `Data` value (lines 58-63).
- This exercises the exact bug scenario: constructor-created LazyLoad survives deserialization with loader intact.

### Both Cases Verified

1. **Deferred LazyLoad (constructor-created, not pre-loaded):** Loader survives deserialization because `ApplyDeserializedState` is a no-op when `isLoaded == false`. Reproduction test confirms.
2. **Pre-loaded LazyLoad (IsLoaded=true, Value set on server):** State transfers via `ApplyDeserializedState` setting `_value` and `_isLoaded = true`. All existing `FatClientLazyLoadTests` and `TwoContainerLazyLoadTests` pass, confirming no regression.

### No Reflection Added

The developer correctly addressed Concern #2 from the developer review by introducing `ILazyLoadDeserializable` as a non-generic internal interface. The converter casts to this interface directly -- no `GetMethod`, `MethodInfo.Invoke`, or other reflection calls were added.

