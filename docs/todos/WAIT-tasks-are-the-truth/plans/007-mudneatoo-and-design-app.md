# MudNeatoo and Design.App on the new tracking

**Plan #:** 007
**Date:** 2026-11-24
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-8
**Status:** In Progress
**Last Updated:** 2026-11-24
**Plan-review opt-in:** No (the criterion states the behavior; no design is added)
**Code-review opt-in:** No (no product code changes; tests only)
**Branch:** wait-007-mudneatoo-and-design-app
**PR:** —

---

## Scope

Proves the reported case at the component level: a MudNeatoo input writes a property whose async rule is in flight, a handler awaits `WaitForTasks()` on the entity, and it completes when the rule does. It checks the MudNeatoo inputs and the Design.App pages and ViewModels for any removed member, and checks the inputs' busy display against the new busy state. It adds no new MudNeatoo feature.

---

## Intent

- A page that binds MudNeatoo inputs to an entity and awaits `WaitForTasks()` in its save handler does not freeze, whether the input writes through `EntityProperty.SetValue` or through its `Setter`.
- A rule that throws after a UI edit reaches the entity's wait once, and the entity recovers: the next edit is waited for (D34).

---

## Framework & Architectural Alignment

- D32: the owner registers. An input calls `IEntityProperty.SetValue` and registers nothing; the property reports to its entity. This is the write path the earlier design never tracked (analysis defect D2).
- Tests render the real components with bUnit over a real `EntityBase`; nothing of Neatoo is mocked.

---

## Constraints & Invariants

- No MudNeatoo component and no Design.App file changes unless one uses a removed member.
- The existing `SetterTests` are not changed.

---

## Steps

1. Tests that render an input over a property with a gated async rule, raise the wrapped MudBlazor component's change without awaiting it, and await the entity's wait.
2. Check MudNeatoo and Design.App for removed members and for how they read busy.

---

## Acceptance

- [x] A write through a MudNeatoo input followed by `WaitForTasks()` on the entity completes when the rule does, and not before `[integration]`
- [x] The same on a root, for an input bound to its child's property `[integration]`
- [x] The same when the input writes through its `Setter` `[integration]`
- [x] The input is disabled while its rule runs and enabled once the entity's wait completes `[integration]`
- [x] A UI edit whose rule throws is reported by the entity's wait once, and the next edit is waited for `[integration]`
- [x] Build and tests green `[explicit-skip: meta-bullet]`

---

## Current State (Pre-Flight)

- All eleven MudNeatoo inputs write the same way: `await EntityProperty.SetValue(value)`, or `await Setter(value)` when a `Setter` is given (one call of each per component, grepped). Each binds `Disabled` to `EntityProperty.IsBusy` and re-renders on the property's `IsBusy` notification. Seven of them await `EntityProperty.WaitForTasks()` in their validation callback.
- Nothing in `src/Neatoo.Blazor.MudNeatoo` or `src/Design/Design.App` uses a member this todo removed (`IsSelfBusy`, `IsMarkedBusy`, `AddMarkedBusy`, `RemoveMarkedBusy`, the public `AddChildTask`). The only hits in `src` outside the framework are stale generated files in ignored, uncompiled `Generated/` folders.
- The three Design.App save handlers are `WaitForTasks()`, then `IsSavable`, then `Save()`. They need no change.
- `SetterTests` already pins, per input, that a change without a `Setter` writes through the property.

---

## Punchlist

---

## Test Evidence

All in `Neatoo.Blazor.MudNeatoo.Tests.InputWriteIsWaitedForTests`, over the new fixture `AsyncRuleTarget`.

Red run: the same test files against `main` (b9bdabb, the 0.36.1 code), one test per run with a 30-second hang timeout (`reviews/007-red-main.log`). Three tests hang the test host there: the plain input write, the child's input and the throwing rule. That is the freeze of #141 through a MudNeatoo input, now measured; before, it was read from the code. The `Setter` test and the disabled test pass on `main`: a write through `Set{Name}` was already registered by the generated code, and the property's busy flag already drove the display. Those two hold behavior fixed across the redesign.

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Input write, then the entity's wait | Required | `[integration]` | `AWriteThroughAnInput_ThenWaitForTasksOnTheEntity_CompletesWhenTheRuleDoes` (hangs on `main`) | ✓ |
| A child's input, the root's wait | Required | `[integration]` | `AWriteThroughAnInputOnAChild_ThenWaitForTasksOnTheRoot_CompletesWhenTheChildsRuleDoes` (hangs on `main`) | ✓ |
| Through the `Setter` | Required | `[integration]` | `AWriteThroughAnInputsSetter_ThenWaitForTasksOnTheEntity_CompletesWhenTheRuleDoes` | ✓ |
| Disabled while the rule runs | Required | `[integration]` | `TheInput_IsDisabledWhileItsRuleRuns_AndEnabledWhenTheEntitysWaitCompletes` | ✓ |
| A throwing rule is reported once | Required | `[integration]` | `AWriteThroughAnInput_WhoseRuleThrows_IsReportedByTheEntitysWaitOnce_AndTheEntityRecovers` (hangs on `main`) | ✓ |
| Build and tests green | Required | `[explicit-skip]` | `reviews/007-build.log`, `007-test.log` (2,109 passed, 2 skipped, 0 failed), `007-design-build.log`, `007-design-test.log` (235 passed) | n/a |

No existing test was changed. The MudNeatoo suite ran 20 more times with 0 failures.

The tests use the text field. The other ten inputs share its write path, and `SetterTests` pins that path for each of them.

---

## Gate Record

- Round 1 (2026-11-24): test review APPROVED, no veto, no callout — `reviews/007-test-review.md`. It confirmed that all eleven inputs share the text field's write path and busy display, that "not before" is asserted, and that the tier is `[integration]`. Its two tidy-up notes were taken: the throwing test now asserts the exact exception type and that the change and the wait report the same exception, and the fixture's summary no longer says "no factory". Its theoretical line also narrowed this plan's Intent: no test shows what the renderer does with a fault that leaves an input's handler, so the Intent now claims only that the entity recovers. Full suite rerun after those edits. The gate is closed.
---

## Plan Amendments

### 2026-11-24 — Nothing to move in MudNeatoo or Design.App

- **Section affected:** Scope, Step 2
- **Original said:** move the MudNeatoo inputs and the Design.App pages and ViewModels off any removed member.
- **What changed:** no product file changes. The plan is tests only.
- **Why:** nothing there uses a removed member, and the inputs read busy from `EntityProperty.IsBusy`, which still means "this property has work in flight".
- **Discovery Log:** n/a (no decision changed)

---

## Notes

- The bullets are tagged `[integration]`, not `[ui]`: they render components in bUnit and do not drive a browser, and the project declares no use-case catalogue.
