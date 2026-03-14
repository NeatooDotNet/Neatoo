# LazyLoad Auto-Trigger on Value Access

**Status:** Complete
**Priority:** High
**Created:** 2026-03-13
**Last Updated:** 2026-03-13

---

## Problem

`LazyLoad<T>.Value` currently returns `null` without triggering the fetch. The only way to kick off the load is an explicit `await lazyLoad` or `await lazyLoad.LoadAsync()`. This means Razor databinding can't "just work" — developers must add manual `await` boilerplate to initiate the load, which breaks the databinding-driven pattern Neatoo is designed around.

In zTreatment, Claude consistently says it can't use pure databinding for LazyLoad properties and always adds an explicit "await kickoff" step.

## Solution

Make `LazyLoad<T>.Value` getter auto-trigger `LoadAsync()` as fire-and-forget when accessed and the load hasn't started. Only the `Value` getter triggers — `IsLoading` and `IsLoaded` remain pure state checks with no side effects, so developers can inspect state without accidentally starting a load.

The existing `PropertyChanged` events (`IsLoading`, `IsLoaded`, `Value`) already fire during `LoadAsyncCore()`, so Blazor will re-render automatically when the load completes.

**Design decision:** Only `Value` triggers the load — accessing `IsLoaded`/`IsLoading` does not. This lets developers check "is it loaded?" without the side effect of starting a load.

Target Razor pattern:
```razor
@{
    var orderLines = Model.OrderLines.Value; // Kicks off load if needed
}
@if (Model.OrderLines.IsLoading)
{
    <LoadingSpinner />
}
else if (Model.OrderLines.IsLoaded)
{
    <OrderLinesList Items="@orderLines" />
}
```

---

## Clarifications

[Architect comprehension check Q&A from Step 2.]

---

## Requirements Review

**Reviewer:** neatoo-requirements-reviewer
**Reviewed:** 2026-03-13
**Verdict:** APPROVED

### Relevant Requirements Found

**Source 1: Design project (Design.Domain)**

1. **Design decision: Value never triggers load (WILL BE SUPERSEDED)**
   `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 8-9 state:
   > DESIGN DECISION: LazyLoad<T> is declared as a regular property because: It wraps a child entity/value, not a scalar property value; It has its own lifecycle (IsLoaded, IsLoading, LoadAsync)
   This design decision describes *why* LazyLoad is a regular property, not a partial property. It does not constrain how `Value` behaves. However, the same file does not explicitly state "Value never triggers load" -- that principle lives in code comments and the skill file (see below).

2. **STATE PROPAGATION contract**
   `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 30-35 document that the parent entity includes LazyLoad children in its IsModified, IsValid, and IsBusy calculations. The proposed change *reinforces* this contract: when `Value` triggers a fire-and-forget load, `IsBusy` on LazyLoad becomes true (line 243 of LazyLoad.cs: `IsBusy => IsLoading || ...`), which cascades to the parent via `IsAnyLazyLoadChildBusy()` in `ValidateBase.cs` line 174. This is correct and desirable.

3. **SUBSCRIPTION LIFECYCLE contract**
   `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 38-43: parent subscribes at FactoryComplete() and OnDeserialized(). The proposed change does not alter subscription behavior. PropertyChanged events from LoadAsyncCore() will propagate correctly through existing subscriptions.

**Source 2: Unit tests (behavioral contracts)**

4. **Value_BeforeLoad_ReturnsNull** -- `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` line 17-28.
   Contract: WHEN LazyLoad created with loader AND Value accessed before LoadAsync, THEN Value is null.
   **This test will need updating.** After the change, accessing `Value` triggers fire-and-forget loading. The `Value` getter will still return null *synchronously* on first access (the load is async), so this test may still pass depending on implementation. But the *semantic intent* of the test changes: it currently asserts that Value access has no side effects. See Contradictions below.

5. **IsLoading_DuringLoad_ReturnsTrue** -- lines 57-83.
   Contract: WHEN LoadAsync called, THEN IsLoading is true until load completes.
   The proposed change does not conflict; it adds another trigger path to the same LoadAsync/LoadAsyncCore flow.

6. **LoadAsync_CalledConcurrently_OnlyLoadsOnce** -- lines 100-129.
   Contract: WHEN multiple LoadAsync calls concurrent, THEN only one actual load executes.
   The proposed change must respect this: multiple accesses to `Value` must share the same load task. The existing `_loadLock` and `_loadTask` in `LoadAsync()` already handle this.

7. **LoadAsync_WhenAlreadyLoaded_ReturnsImmediately** -- lines 131-149.
   Contract: WHEN already loaded, THEN LoadAsync returns cached value, no re-load.
   The proposed `Value` getter must check `_isLoaded` before triggering. The existing `LoadAsync()` already does this (line 188: `if (_isLoaded) return Task.FromResult(_value)`).

8. **LoadAsync_RaisesPropertyChangedForAllStateProperties** -- lines 177-192.
   Contract: WHEN load completes, THEN PropertyChanged fires for Value, IsLoaded, IsLoading.
   The proposed change uses the same LoadAsyncCore path, so PropertyChanged events fire identically. Blazor re-rendering will work as expected.

9. **IsBusy_WhenLoading_ReturnsTrue** -- lines 209-223.
   Contract: WHEN loading in progress, THEN IsBusy is true.
   The proposed change creates a new code path where loading starts from the getter. IsBusy must be true during this load. Since the same LoadAsyncCore is used, this is satisfied.

10. **LoadAsync_OnFailure_SetsErrorState** -- lines 151-175.
    Contract: WHEN loader throws, THEN HasLoadError=true, LoadError has message, IsLoaded=false.
    The proposed fire-and-forget path creates a critical concern: if the load fails, the exception is thrown inside LoadAsyncCore but the caller of `Value` (the getter) has no mechanism to observe it. The exception could become an unobserved task exception. See Gaps below.

**Source 3: Integration tests**

11. **WaitForTasks after LazyLoad trigger** -- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashTests.cs`.
    Contract: WHEN AddActionAsync handler awaits LazyChild, THEN WaitForTasks surfaces the result.
    The proposed change adds a *second* trigger path (Value getter) alongside the existing explicit-await path. Both share the same LoadAsync/LoadAsyncCore, so WaitForTasks can still surface the load task. However, the fire-and-forget load from the Value getter must register the task with the parent's WaitForTasks mechanism for this to work. See Gaps below.

12. **LazyLoadStatePropagation tests** -- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs`.
    These tests use pre-loaded LazyLoad (constructor with value). The proposed change only affects unloaded LazyLoad instances, so these tests are not impacted.

**Source 4: Code comments in framework source**

13. **XML doc: "Key principle: Accessing Value never triggers a load"**
    `src/Neatoo/LazyLoad.cs` line 27: `/// Key principle: Accessing <see cref="Value"/> never triggers a load - it returns current state only.`
    This is the most explicit statement of the current contract. The todo intentionally reverses this principle.

14. **XML doc on Value property: "Never triggers a load"**
    `src/Neatoo/LazyLoad.cs` line 140: `/// Never triggers a load - use <c>await</c> or <see cref="LoadAsync"/> to load.`
    Same as above -- must be updated.

**Source 5: Skill documentation**

15. **Skill: "Explicit loading only"**
    `skills/neatoo/references/lazy-loading.md` line 7: `- **Explicit loading only** -- Value returns current state (null if not loaded). Use await or LoadAsync() to load.`
    This behavioral contract must be updated to reflect the new auto-trigger behavior.

16. **Skill: State properties table**
    `skills/neatoo/references/lazy-loading.md` line 145: `| Value | T? | Current value. null if not yet loaded. Never triggers a load. |`
    Must be updated.

**Source 6: Release notes and completed todos**

17. **v0.11.0 release notes: "Value never triggers a load"**
    `docs/release-notes/v0.11.0.md` line 35: `- .Value property never triggers a load - always returns current state`
    This was a marketed feature of the v0.11.0 release.

18. **Completed todo: lazy-loading-v2-design.md**
    `docs/todos/completed/lazy-loading-v2-design.md` line 59: `1. **No magic.** Accessing .Value never triggers a load - it returns current state.`
    This was the *first* design principle of LazyLoad v2. The todo labeled "no magic" was a deliberate rejection of the fire-and-forget-on-access pattern that existed in the *old* `ValidateProperty<T>.OnLoad` system (documented in `docs/todos/completed/property-backing-fields.md` lines 92-97).

**Source 7: Historical context -- the old system DID have fire-and-forget on access**

19. **Old property-backing-fields design**
    `docs/todos/completed/property-backing-fields.md` lines 92-94: The old `Property<T>` had exactly this behavior: `On getter access: if Value == null && OnLoad != null && _onLoadTask == null --> fire-and-forget load`. LazyLoad v2 was designed to *replace* this with explicit loading. The todo proposes reverting to a conceptually similar approach, but now on `LazyLoad<T>` rather than `Property<T>`.

20. **AsyncTasks design rationale -- fire-and-forget with rendezvous**
    `docs/todos/completed/async-tasks-design-rationale.md` documents Neatoo's established pattern: property setters trigger fire-and-forget async work, and `WaitForTasks()` is the rendezvous point. The proposed `Value` getter auto-trigger follows this same pattern.

### Gaps

**Gap 1: Unobserved task exception handling**
When `Value` triggers a fire-and-forget `LoadAsync()`, the returned task is discarded in the getter. If the loader throws, this becomes an unobserved task exception. The existing test `LoadAsync_OnFailure_SetsErrorState` shows that exceptions propagate, but in the fire-and-forget path there is no caller to catch them. The architect must decide: (a) catch and swallow exceptions in the fire-and-forget path, relying on `HasLoadError`/`IsValid=false` for error surfacing, or (b) register the load task with the parent entity's `RunningTasks`/`WaitForTasks` so exceptions surface at the rendezvous point.

**Gap 2: WaitForTasks integration for auto-triggered loads**
Currently `LazyLoad<T>.WaitForTasks()` returns `_loadTask` if it exists and is not completed (line 264). The parent's `WaitForTasks` calls `PropertyManager.WaitForTasks()` but does NOT call `WaitForTasks()` on LazyLoad children. It only checks `IsBusy` via polling. If the auto-triggered load is fire-and-forget from the getter, does the parent's `WaitForTasks()` wait for it? Currently NO -- the parent only awaits `RunningTasks` and `PropertyManager` tasks. The auto-triggered load would only surface through `IsBusy` polling (which has no await). The architect must determine whether auto-triggered loads need to be registered with the parent's async task tracking.

**Gap 3: Serialization edge case -- deserialized LazyLoad with no loader**
When `LazyLoad<T>` is deserialized via the parameterless JSON constructor (`new LazyLoad()`), `_loader` is null. The current `LoadAsync()` throws `InvalidOperationException` when `_loader == null`. If the `Value` getter auto-triggers, accessing `Value` on a deserialized-without-loader instance would trigger a throw from a property getter -- a side effect that callers don't expect. The existing test `FatClientLazyLoad_PostDeserialization_LoadAsync_Throws` verifies this throw, but it's for explicit LoadAsync calls. The auto-trigger must guard against `_loader == null`.

**Gap 4: No existing test for auto-trigger behavior**
There are no tests that verify "accessing Value triggers load." New tests are needed to cover:
- Value access on unloaded instance triggers load
- Value access on already-loaded instance does not re-trigger
- Value access on instance with no loader (deserialized) does not throw
- Concurrent Value accesses share one load
- IsBusy is true on parent after Value access triggers load
- Value access during an in-progress load does not start a second load

### Contradictions

**Contradiction 1: "Value never triggers a load" -- INTENTIONAL REVERSAL (not a veto)**

The proposed change intentionally reverses the "no magic" design principle established in LazyLoad v2. This is documented in 7 locations:

| Source | File | Content |
|--------|------|---------|
| Code XML doc | `src/Neatoo/LazyLoad.cs:27` | "Key principle: Accessing Value never triggers a load" |
| Code XML doc | `src/Neatoo/LazyLoad.cs:140` | "Never triggers a load" |
| Skill | `skills/neatoo/references/lazy-loading.md:3` | "Loading is always explicit -- accessing Value never triggers a load" |
| Skill | `skills/neatoo/references/lazy-loading.md:145` | "Never triggers a load" |
| Release notes | `docs/release-notes/v0.11.0.md:35` | ".Value property never triggers a load" |
| Completed todo | `docs/todos/completed/lazy-loading-v2-design.md:59` | "No magic. Accessing .Value never triggers a load" |
| Completed todo | `docs/todos/completed/lazy-loading-v2-design.md:28` | "Current value, NEVER triggers load" |

This is NOT a veto because the todo explicitly proposes changing this behavior based on real-world usage feedback (Claude consistently adds boilerplate in zTreatment). The owner has confirmed the design intent. However, all 7 locations must be updated during implementation to maintain consistency.

**Contradiction 2: Test Value_BeforeLoad_ReturnsNull semantic intent**

`src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` `Value_BeforeLoad_ReturnsNull()` was written to verify that `Value` has no side effects. After the change, `Value` will still return null synchronously (the load is async), so the assertion may still pass. But the test's semantic intent changes -- it no longer verifies "no side effect." The test should be updated or split: one test for "Value returns null synchronously before load completes" and another for "Value access triggers load."

### Recommendations for Architect

1. **Error handling in fire-and-forget path (Gap 1):** The `Value` getter must not let exceptions escape from the auto-triggered `LoadAsync()`. Catch the task's exception and rely on `HasLoadError`/`IsValid=false` for error surfacing. This aligns with how Neatoo handles async rule exceptions -- they surface through validation state, not by crashing the caller.

2. **Guard against null loader (Gap 3):** The auto-trigger in the `Value` getter must check `_loader != null` before calling `LoadAsync()`. If `_loader` is null (deserialized instance without constructor-created loader), `Value` should return null without triggering -- the existing behavior. This preserves the serialization contract tested by `FatClientLazyLoad_PostDeserialization_LoadAsync_Throws`.

3. **WaitForTasks integration (Gap 2):** Consider whether auto-triggered loads should register with the parent's `RunningTasks`. The existing `AsyncTasks` + `WaitForTasks` pattern (documented in `docs/todos/completed/async-tasks-design-rationale.md`) is the established rendezvous mechanism. If auto-triggered loads bypass it, `WaitForTasks()` may complete before the load finishes. This is acceptable if the UI pattern is pure databinding (Blazor re-renders on PropertyChanged), but problematic if imperative code calls `WaitForTasks()` expecting all LazyLoad loads to be done.

4. **Update all 7 documentation locations (Contradiction 1):** The code comments, skill docs, and release notes that say "Value never triggers a load" must be updated. The architect should include documentation updates in the plan.

5. **Verify Design.Domain sample file:** `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` does not contain executable lazy-load-on-access samples. It should be reviewed and potentially updated to reflect the new behavior.

6. **Test the IsBusy cascading path:** After the change, accessing `.Value` on an unloaded LazyLoad starts a load, making `LazyLoad.IsBusy=true`, which cascades to `ValidateBase.IsBusy=true` via `IsAnyLazyLoadChildBusy()`. This is the correct behavior, but the cascading path should be tested to confirm the parent's `IsBusy` becomes true after a child's `Value` is accessed.

7. **Precedent: the old system worked this way.** The old `Property<T>.OnLoad` system (removed in LazyLoad v2) had fire-and-forget-on-access. That system worked in practice; the v2 redesign removed it for "no magic" clarity, but real-world usage showed the explicitness creates friction. The proposed change is a return to the proven pattern, but on a dedicated wrapper type (`LazyLoad<T>`) rather than on every property.

---

## Plans

- [LazyLoad Auto-Trigger Plan](../plans/lazyload-auto-trigger.md)

---

## Tasks

- [x] Architect comprehension check (Step 2)
- [x] Business requirements review (Step 3)
- [x] Architect plan creation & design (Step 4)
- [x] Developer review (Step 5)
- [x] Implementation (Step 7)
- [x] Verification (Step 8) — VERIFIED + REQUIREMENTS SATISFIED
- [x] Documentation (Step 9) — Requirements Documented
- [x] Completion (Step 10)

---

## Progress Log

### 2026-03-13
- Created todo from user feedback about LazyLoad properties not working well in Razor pages
- User confirmed: only `Value` getter should trigger load; `IsLoading`/`IsLoaded` remain side-effect-free
- Core file: `src/Neatoo/LazyLoad.cs`
- Requirements review completed -- APPROVED with 4 gaps and 2 intentional contradictions
- Architect plan created at `docs/plans/lazyload-auto-trigger.md` -- 12 business rules, 13 test scenarios, 3-phase implementation
- **Scope expansion:** User requested WaitForTasks integration with LazyLoad children be included in this plan (was originally out of scope). Plan updated: added 3 new business rules (13-15), 4 new test scenarios (14-17), `ValidateBase.WaitForTasks()` now awaits LazyLoad children via new `WaitForLazyLoadChildren()` method. Plan now has 15 business rules and 17 test scenarios.
- **Developer review approved.** All 15 business rules traced through implementation paths. All 17 test scenarios verified. Two non-blocking concerns noted: (1) WaitForLazyLoadChildren exception handling for faulted LazyLoad children -- recommend try-catch to swallow, consistent with fire-and-forget design. (2) Pre-existing inconsistency in WaitForTasks(CancellationToken) not awaiting PropertyManager -- out of scope. Plan status set to "Ready for Implementation".

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: PASS (zero warnings, zero errors)
- Tests: PASS (2111 tests, 0 failures)

---

## Results / Conclusions

- `LazyLoad<T>.Value` getter now auto-triggers `LoadAsync()` fire-and-forget, enabling Razor databinding without `await` boilerplate
- `ValidateBase.WaitForTasks()` now awaits LazyLoad children (scoped in during review)
- 18 new tests (12 unit + 6 integration) covering auto-trigger, concurrency, error handling, and parent state propagation
- All 7 documentation locations updated from "Value never triggers a load" to reflect new behavior
- Version bumped to 0.21.0
- Separate todo created for pre-existing `WaitForTasks(CancellationToken)` gap: `docs/todos/waitfortasks-cancellation-missing-propertymanager.md`
- Workflow improvement: tightened developer/documenter boundary in project-todos skill and neatoo-developer agent
