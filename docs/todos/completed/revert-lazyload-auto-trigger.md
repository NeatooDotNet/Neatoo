# Revert LazyLoad Auto-Trigger on .Value Access

**Status:** Complete
**Priority:** High
**Created:** 2026-03-15
**Last Updated:** 2026-03-15 (Developer Review)

---

## Problem

The auto-trigger on `.Value` access (introduced in v0.21.0) has an unintended side effect in real applications. In zTreatment, convenience accessors that read `.Value` (e.g., `hub.Treatment`, `hub.SymptomsAssessment`) inadvertently trigger LazyLoad loading when accessed by Neatoo's serialization pipeline, computed property evaluation chains, or test assertions. The intent was deferred loading, but `.Value` collapses two distinct intents into one accessor: "give me what's loaded" vs "load it if needed."

## Solution

1. Remove the auto-trigger from `.Value` — make it a purely passive read (returns current value or null if not loaded)
2. Keep `LoadAsync()` returning `Task<T?>` — consumers use this for explicit loading in imperative contexts (tests, domain logic, `OnInitializedAsync`)
3. Remove `GetAwaiter()` if it exists (since `await lazyLoad` would bypass the explicit `LoadAsync()` pattern)
4. Update skills and documentation to show the two patterns: `LoadAsync()` for imperative code, `.Value` for binding

### Prior art research

Frameworks that make property access trigger loading (EF Core, CSLA, System.Lazy) all suffer from the same problem. The NotifyTask<T> pattern (Stephen Cleary / MvvmCross) separates load trigger from value read and uses PropertyChanged for the loading→loaded transition — this maps directly to Neatoo's existing LazyLoad infrastructure.

### Related completed todos

- [LazyLoad auto-trigger on Value access (v0.21.0)](completed/lazyload-auto-trigger-on-value-access.md) — the feature being reverted
- [Generate LazyLoad registration (v0.22.0)](completed/generate-lazyload-registration.md) — recent LazyLoad PropertyManager unification

---

## Clarifications

Architect confirmed understanding with no questions. Key implications noted:
- `TriggerLoadAsync()` becomes dead code and should be removed
- `WaitForTasks` integration for LazyLoad children should be evaluated (still valuable for explicit `LoadAsync()` calls)
- Tests from v0.21.0 that test auto-trigger behavior need updating/removal
- Tests and samples using `await lazyLoad` (GetAwaiter) syntax need changing to `await lazyLoad.LoadAsync()`

---

## Requirements Review

**Reviewer:** neatoo-requirements-reviewer
**Reviewed:** 2026-03-15
**Verdict:** APPROVED

### Relevant Requirements Found

**Source 1: Design project (Design.Domain)**

1. **DESIGN DECISION: Value auto-triggers fire-and-forget load (WILL BE SUPERSEDED)**
   `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 19-24: "DESIGN DECISION: Accessing Value auto-triggers a fire-and-forget load when the value hasn't been loaded, no load is in progress, and a loader delegate is present." This design decision was added in v0.21.0 and is the exact behavior the todo proposes to revert.

2. **DESIGN DECISION: WaitForTasks awaits in-progress LazyLoad children**
   `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 26-30: "ValidateBase.WaitForTasks() awaits in-progress LazyLoad children via PropertyManager.WaitForTasks(). WaitForTasks does NOT trigger loads on unaccessed LazyLoad children (uses BoxedValue, not Value getter)." This contract remains valid after the revert -- WaitForTasks should still await loads that were explicitly triggered by `LoadAsync()`. The "(uses BoxedValue, not Value getter)" note becomes less critical after the revert since Value will no longer trigger loads, but the BoxedValue pattern is still needed for the look-through property subclasses.

3. **DESIGN DECISION: LazyLoad<T> is a partial property**
   `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 9-14. Unaffected by the proposed change -- the partial property pattern and generator behavior remain identical.

4. **STATE PROPAGATION contract**
   `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 52-60: LazyLoad property subclasses delegate IsValid, IsBusy, IsModified, WaitForTasks, RunRules, PropertyMessages, and ClearAllMessages to the inner entity. Unaffected -- the look-through property subclass infrastructure stays the same.

5. **SERIALIZATION contract**
   `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 42-50: Value and IsLoaded are serialized; loader delegate is not. The current auto-trigger creates an implicit dependency: `[JsonInclude]` on `.Value` means System.Text.Json calls the `.Value` getter during serialization of `LazyLoad<T>`. With auto-trigger, this triggers a load during serialization. The revert eliminates this implicit side effect.

**Source 2: Unit tests (behavioral contracts)**

6. **Value_BeforeLoad_ReturnsNullSynchronously** -- `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` line 17-34.
   Contract: WHEN LazyLoad created with async loader AND Value accessed, THEN Value is null synchronously. The test currently observes that the load fires in the background (arranges a `TaskCompletionSource` to control completion). After the revert, Value returns null with no side effect at all. The test assertion still passes, but the cleanup (`continueLoad.SetResult()`) becomes unnecessary. This test should be simplified.

7. **Await_LoadsValue (uses GetAwaiter)** -- `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` lines 91-104.
   Contract: WHEN `await lazyLoad` used, THEN value is loaded. This test uses `GetAwaiter()` which the todo proposes to remove. **This test must be updated or removed.**

8. **LoadAsync_CalledConcurrently_OnlyLoadsOnce** -- lines 107-135.
   Contract: WHEN multiple LoadAsync calls concurrent, THEN only one actual load executes. Unaffected -- LoadAsync behavior is unchanged.

9. **LoadAsync_WhenAlreadyLoaded_ReturnsImmediately** -- lines 138-155.
   Contract: WHEN already loaded, THEN LoadAsync returns cached value. Unaffected.

10. **LoadAsync_OnFailure_SetsErrorState** -- lines 158-181.
    Contract: WHEN loader throws via LoadAsync, THEN HasLoadError=true, LoadError has message, IsLoaded=false. Note line 180: `Assert.IsNull(lazyLoad.Value)` -- after the revert, accessing `.Value` on a faulted instance returns null with no side effects. Unaffected.

11. **LoadAsync_RaisesPropertyChangedForAllStateProperties** -- lines 184-198.
    Contract: WHEN load completes, THEN PropertyChanged fires for Value, IsLoaded, IsLoading. Unaffected -- PropertyChanged events come from `LoadAsyncCore()`.

12. **IsBusy_WhenLoading_ReturnsTrue** -- lines 214-229.
    Contract: WHEN loading in progress, THEN IsBusy is true. Unaffected.

13. **Auto-trigger test region** -- lines 244-504: 12 tests covering auto-trigger behavior.
    All of these tests were added in v0.21.0 specifically for auto-trigger. **All must be updated or removed.** Key tests and their disposition:
    - `ValueAccess_TriggersFireAndForgetLoad` (line 247) -- remove or rewrite to assert no load
    - `ValueAccess_AutoTrigger_CompletesSuccessfully` (line 273) -- remove
    - `ValueAccess_AlreadyLoaded_ReturnsCachedValue` (line 294) -- keep (Value on pre-loaded instance returns cached value, still valid)
    - `ValueAccess_DuringLoad_DoesNotStartSecondLoad` (line 317) -- simplify (Value during load still returns null)
    - `ValueAccess_NoLoader_ReturnsNullWithoutException` (line 346) -- keep (still valid: Value on no-loader instance returns null)
    - `ValueAccess_AutoTrigger_LoadFailure_SetsErrorState` (line 362) -- remove auto-trigger part
    - `ValueAccess_ConcurrentAccess_SharesOneLoad` (line 391) -- remove (no auto-trigger means no concurrent load from Value)
    - `IsLoadingAccess_DoesNotTriggerLoad` (line 414) -- keep
    - `IsLoadedAccess_DoesNotTriggerLoad` (line 433) -- keep
    - `ExplicitLoadAsync_StillWorksIdentically` (line 453) -- keep
    - `ExplicitLoadAsync_OnFailure_StillPropagatesException` (line 469) -- keep
    - `WaitForTasks_AfterAutoTrigger_AwaitsLoad` (line 482) -- rewrite to use explicit LoadAsync

14. **IsValid_WhenHasLoadError_ReturnsFalse** -- lines 231-242.
    Uses `lazyLoad.LoadAsync().GetAwaiter().GetResult()` to synchronously trigger a load failure. The `GetAwaiter()` call here is on the Task returned by `LoadAsync()`, not on `LazyLoad<T>` itself. Unaffected by removing `LazyLoad<T>.GetAwaiter()`.

**Source 3: Integration tests**

15. **LazyLoadStatePropagationTests** -- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs`.
    `LazyLoadChild_*` tests (lines 34-75) use pre-loaded LazyLoad. Unaffected.

16. **LazyLoadAutoTriggerPropagationTests** -- same file, lines 82-238. 6 tests:
    - `ParentIsBusy_AfterAutoTriggeredChildLoad` (line 99) -- accesses `.Value` to trigger load. **Must be rewritten to use `LoadAsync()` instead.**
    - `ParentIsValid_AfterAutoTriggeredChildLoadFailure` (line 124) -- accesses `.Value` to trigger load. **Must be rewritten.**
    - `ParentWaitForTasks_AwaitsAutoTriggeredLazyLoadChild` (line 151) -- accesses `.Value` to trigger load. **Must be rewritten.**
    - `ParentWaitForTasksWithToken_AwaitsAutoTriggeredLazyLoadChild` (line 177) -- same pattern. **Must be rewritten.**
    - `ParentWaitForTasks_PreLoadedChild_CompletesImmediately` (line 203) -- pre-loaded. Unaffected.
    - `ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger` (line 221) -- verifies WaitForTasks does NOT trigger loads. Still valid and important.

17. **WaitForTasksLazyLoadCrashTests** -- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashTests.cs`.
    `WaitForTasks_AddActionAsync_AwaitsLazyLoad_WithRemoteFetch_Crashes` (line 36) -- tests that `AddActionAsync` handler awaits `LazyChild` via `await parent.LazyChild` (GetAwaiter). **Must be updated to use `await parent.LazyChild.LoadAsync()` instead.**

18. **WaitForTasksLazyLoadCrashEntity** -- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs` line 83: `var child = await parent.LazyChild;` uses GetAwaiter. **Must be updated to `await parent.LazyChild.LoadAsync()`.**

19. **FatClientLazyLoadTests** -- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientLazyLoadTests.cs`.
    Pre-loaded and unloaded round-trip tests. Lines 99-115: `FatClientLazyLoad_Unloaded_RoundTrip` accesses `deserialized.LazyDescription.Value` on an unloaded deserialized instance. Currently, this would NOT trigger a load because the deserialized instance (via `JsonSerializer.Deserialize<>`, not the Neatoo converter) uses the parameterless constructor (`_loader = null`). After the revert, this remains safe. Unaffected.

20. **TwoContainerLazyLoadTests** -- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/TwoContainerLazyLoadTests.cs`.
    Pre-loaded round-trip. Unaffected.

**Source 4: Samples and Examples**

21. **LazyLoadSamples.cs** -- `src/samples/LazyLoadSamples.cs` line 74: `var child = await parent.LazyChild;` uses GetAwaiter. **Must be updated to `await parent.LazyChild.LoadAsync()`.**

22. **Person.DomainModel Person.cs** -- `src/Examples/Person/Person.DomainModel/Person.cs` lines 114 and 144: `var phoneList = await this.PersonPhoneList;` uses GetAwaiter. **Must be updated to `await this.PersonPhoneList.LoadAsync()`.**

23. **PersonIntegrationTests.cs** -- `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs` lines 184 and 224: `var phoneList = await result.PersonPhoneList;` and `var fetchedPhoneList = await result.PersonPhoneList;` use GetAwaiter. **Must be updated.**

**Source 5: Skill documentation**

24. **Skill: "Auto-trigger on Value access"**
    `skills/neatoo/references/lazy-loading.md` line 7: "Auto-trigger on Value access -- Accessing Value triggers a fire-and-forget load..." This must be reverted to describe passive Value behavior.

25. **Skill: State properties table**
    `skills/neatoo/references/lazy-loading.md` line 166: "Value | T? | Current value. null if not yet loaded. Auto-triggers fire-and-forget load when unloaded and loader is present." Must be updated.

26. **Skill: Loading section**
    `skills/neatoo/references/lazy-loading.md` lines 132-143: Shows three loading patterns including `var value = lazy.Value;` auto-trigger and `var value = await lazy;` GetAwaiter. Both must be removed; only `await lazy.LoadAsync()` remains.

27. **Skill: WaitForTasks integration**
    `skills/neatoo/references/lazy-loading.md` lines 148-158: Shows `entity.OrderLines.Value` triggering load, then `WaitForTasks()` awaiting it. This pattern is no longer valid -- must be updated to show explicit `LoadAsync()`.

28. **Skill: UI Binding section**
    `skills/neatoo/references/lazy-loading.md` lines 207-229: Shows Blazor pattern using `Model.OrderLines.Value` to auto-trigger. Must be rewritten.

**Source 6: Code comments in framework source**

29. **XML doc: "Key principle: auto-triggers fire-and-forget load"**
    `src/Neatoo/LazyLoad.cs` lines 22-33: Class-level doc describes auto-trigger behavior. Must be updated to describe passive Value.

30. **XML doc on Value property**
    `src/Neatoo/LazyLoad.cs` lines 176-181: "Auto-triggers a fire-and-forget LoadAsync when the value has not been loaded..." Must be updated.

31. **LazyLoadPropertyHelper "Uses BoxedValue to avoid triggering auto-load"**
    `src/Neatoo/Internal/LazyLoadValidateProperty.cs` lines 30, 40, 50, 120 and `src/Neatoo/Internal/LazyLoadEntityProperty.cs` line 38: Comments explain using BoxedValue "to avoid triggering auto-load on the LazyLoad.Value getter." After the revert, `.Value` no longer triggers a load, but BoxedValue remains the correct access path for look-through subclasses because it returns the raw `_value` without going through the public `Value` property. Comments should be updated to reflect the new rationale (direct field access, not auto-load avoidance).

32. **ValidateBase.WaitForTasks note about PropertyManager**
    `src/Neatoo/ValidateBase.cs` lines 588-590: "Note: This method does NOT call PropertyManager.WaitForTasks(). This is a pre-existing gap that predates the LazyLoad unification." This gap applies equally regardless of auto-trigger. Unaffected.

**Source 7: Release notes**

33. **v0.21.0 release notes**
    `docs/release-notes/v0.21.0.md` -- entire release is about auto-trigger. A new release note for the revert should reference this.

34. **v0.11.0 release notes (already updated)**
    `docs/release-notes/v0.11.0.md` line 35: Already strikethrough'd the original "Value never triggers a load" text and added a note about v0.21.0 change. Must be un-strikethrough'd or updated again.

35. **v0.22.0 release notes**
    `docs/release-notes/v0.22.0.md` -- LazyLoad PropertyManager unification. Unaffected by auto-trigger revert.

**Source 8: Historical design decisions**

36. **LazyLoad v2 design: "No magic"**
    `docs/todos/completed/lazy-loading-v2-design.md` line 28: `Value` described as "Current value, NEVER triggers load". Line 59: "No magic. Accessing .Value never triggers a load." The proposed revert restores this original design principle. This supports the todo.

37. **AsyncTasks design rationale: fire-and-forget with rendezvous**
    `docs/todos/completed/async-tasks-design-rationale.md`: Documents Neatoo's pattern of fire-and-forget from property setters with WaitForTasks as rendezvous. The auto-trigger extended this pattern to property getters, which is unprecedented in the framework. The revert removes this extension and keeps fire-and-forget limited to property setters (rules).

### Gaps

**Gap 1: No existing test for "Value access does NOT trigger load"**
After the revert, the original v2 principle is restored: Value is passive. There should be an explicit test asserting: WHEN Value accessed on unloaded LazyLoad with a loader, THEN no load is triggered (IsLoading remains false, loader not invoked). Several auto-trigger tests can be repurposed for this.

**Gap 2: UI loading pattern without auto-trigger**
The todo proposes removing auto-trigger but does not specify how Blazor components should initiate loading. The v0.21.0 release notes documented that auto-trigger eliminated manual `await` boilerplate in Razor pages. With the revert, developers must explicitly call `LoadAsync()` -- likely in `OnInitializedAsync()` or via an `AddActionAsync` rule. The skill documentation and Blazor section must provide clear patterns for this.

**Gap 3: GetAwaiter removal impact on IValidateProperty**
`src/Neatoo/IValidateProperty.cs` line 84 has a `GetAwaiter()` default interface method: `TaskAwaiter GetAwaiter() => Task.GetAwaiter();`. This is on `IValidateProperty`, not on `LazyLoad<T>`. The todo proposes removing `GetAwaiter()` from `LazyLoad<T>` only. The `IValidateProperty.GetAwaiter()` is a separate concern and should not be affected. The architect should verify this distinction.

**Gap 4: Serialization-triggered load scenario**
The todo's problem statement mentions serialization pipeline triggering loads. The `[JsonInclude]` on `LazyLoad<T>.Value` means System.Text.Json reads the `.Value` getter during serialization. With auto-trigger, this silently starts a load. After the revert, this access returns null harmlessly. However, the LazyLoad property subclasses already use `BoxedValue` (not `.Value`) for their look-through access. The architect should verify that the Neatoo JSON converter path (`NeatooBaseJsonTypeConverter` line 398: `property.GetValue(value)` gets the `LazyLoad<T>` instance, then `JsonSerializer.Serialize` serializes it via STJ which reads `.Value`) is confirmed safe after the revert.

### Contradictions

**No contradictions found that would warrant a VETO.**

The todo reverses the v0.21.0 design decision ("Value auto-triggers a fire-and-forget load") and restores the v2 design principle ("No magic. Accessing .Value never triggers a load"). This is an intentional reversal motivated by real-world usage feedback, not an accidental contradiction. The original v2 design supports this direction.

The v0.21.0 auto-trigger review itself noted (requirement 19 in `docs/todos/completed/lazyload-auto-trigger-on-value-access.md`): "The old Property<T>.OnLoad system had exactly this behavior... LazyLoad v2 was designed to replace this with explicit loading." The current todo completes the round-trip back to explicit loading based on evidence that the auto-trigger caused the same problems the old system did.

### Recommendations for Architect

1. **Comprehensive GetAwaiter removal audit.** The `await lazyLoad` pattern is used in 6 locations across the codebase:
   - `src/samples/LazyLoadSamples.cs` line 74
   - `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` line 99 (test `Await_LoadsValue`)
   - `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` line 636 (test `Factory_Create_WithLoader_CreatesLazyLoad`)
   - `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs` line 83
   - `src/Examples/Person/Person.DomainModel/Person.cs` lines 114 and 144
   - `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs` lines 184 and 224
   All must be changed to `await x.LoadAsync()`.

2. **TriggerLoadAsync removal.** `TriggerLoadAsync()` at `src/Neatoo/LazyLoad.cs` lines 291-303 is the fire-and-forget wrapper used only by the Value getter. With auto-trigger removed, it becomes dead code and should be removed.

3. **Test updates are substantial but mechanical.** 12 unit tests (auto-trigger region) and 4 integration tests reference auto-trigger behavior. Most can be repurposed: change "Value triggers load" assertions to "Value does NOT trigger load" assertions, and move load triggering to explicit `LoadAsync()` calls.

4. **Preserve WaitForTasks integration.** The WaitForTasks-awaits-LazyLoad-children feature (v0.21.0 scope expansion) is independent of auto-trigger and should survive the revert. `PropertyManager.WaitForTasks()` iterates busy properties and awaits them. A `LazyLoad` child that has an in-progress `LoadAsync()` (explicitly triggered) will still be IsBusy=true and will still be awaited. The test `ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger` (line 221) remains critical -- it verifies WaitForTasks does NOT trigger loads.

5. **Update Design.Domain file.** `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 19-24 must be reverted from "auto-triggers" back to "never triggers" or equivalent.

6. **Update all documentation locations.** The auto-trigger text appears in 10+ locations (Design.Domain comments, LazyLoad.cs XML docs, skill references, release notes, Blazor sample). Plan should enumerate all updates.

7. **BoxedValue comments.** After the revert, the comments saying "Uses BoxedValue to avoid triggering auto-load" in `LazyLoadValidateProperty.cs` and `LazyLoadEntityProperty.cs` should be updated. BoxedValue is still the right access pattern (it provides the raw field value without going through the public getter), but the rationale shifts from "avoid auto-load trigger" to "direct internal access to the backing value."

8. **New release note.** The revert warrants a new release note (breaking behavioral change). The migration guide should reference the v0.21.0 migration guide and reverse it.

---

## Plans

- [Revert LazyLoad Auto-Trigger Plan](../plans/revert-lazyload-auto-trigger.md)

---

## Tasks

- [x] Architect comprehension check (Step 2)
- [x] Business requirements review (Step 3) — APPROVED, no contradictions
- [x] Architect plan creation & design (Step 4)
- [x] Developer review (Step 5) -- APPROVED with non-blocking concerns (BoxedValue comments added to scope)
- [x] Implementation (Step 7) — all 3 phases complete, 0 test failures
- [x] Verification (Step 8) — Architect VERIFIED, Requirements SATISFIED
- [x] Documentation (Step 9) — skills updated, release notes v0.23.0 written, version bumped

---

## Progress Log

### 2026-03-15
- Created todo from zTreatment feedback about auto-trigger side effects
- Researched how other frameworks handle lazy loading (EF Core, CSLA, NotifyTask, React Suspense, AsyncLazy)
- User decided: `.Value` becomes passive, `LoadAsync()` returns `Task<T?>`, remove `GetAwaiter()`
- Architect comprehension check: ready, no questions
- Requirements review: APPROVED — 37 requirements traced, 4 gaps identified, 0 contradictions
- Architect plan created at `docs/plans/revert-lazyload-auto-trigger.md` — 17 business rules, 16 test scenarios, 3-phase implementation
- Developer review: APPROVED — all 17 business rules traced through implementation, 1 non-blocking concern (BoxedValue comments added to Phase 3 scope). Implementation contract created.
- Implementation: all 3 phases complete. 10 files modified, 5 auto-trigger tests deleted, 0 test failures (2112 passed)
- Architect verification: VERIFIED — independent build/test confirmed 0 errors, 0 failures
- Requirements verification: REQUIREMENTS SATISFIED — 29/37 requirements satisfied in source, 8 deferred to Step 9 (documentation)
- Documentation: skills updated (neatoo lazy-loading.md, neatoo SKILL.md, mudneatoo SKILL.md), release notes v0.23.0 written, version bumped to 0.23.0, release notes index updated

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors (architect independently verified)
- Tests: 0 failures, 2112 passed, 1 skipped (architect independently verified)

---

## Results / Conclusions

The auto-trigger on `.Value` access (v0.21.0) was reverted based on real-world feedback from zTreatment. The core issue: `.Value` collapsed "give me what's loaded" and "load it if needed" into a single accessor, causing unintended loads from serialization, computed properties, and convenience accessors.

The solution separates the two intents: `.Value` is a passive read (returns current value or null), `LoadAsync()` is the explicit load trigger. This aligns with the NotifyTask<T> pattern and restores the original v2 "no magic" design principle.

Key decisions:
- `LoadAsync()` returns `Task<T?>` (useful in tests and domain logic)
- `GetAwaiter()` removed (forces explicit `LoadAsync()` calls)
- WaitForTasks integration preserved (awaits explicitly-triggered loads)
- Version bumped to 0.23.0 (breaking change)
