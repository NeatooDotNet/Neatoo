# Compiled fluency-size aggregate in Design.Domain

**Plan #:** 005
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-5, AC-11
**Status:** Done
**Last Updated:** 2026-10-16
**Plan-review opt-in:** No (the shape is the architecture page's; no public framework API changes)
**Code-review opt-in:** Yes (the example every skill quotes; doctrine must be exact)
**Branch:** fci-005-fluency-size-example
**PR:** —

---

## Scope

A Design.Domain aggregate the size of zTreatment's fluency tab, built rules-first in a domain of its own: a root holding approved minutes and a child list; a slider-shaped fact per child; a fit rule across siblings that converges on the equality short-circuit; the scoped anchor a gesture sets for one fit; a lock that is an intent written as an ordered write; an admission fact on a meta trigger; a next-mark fact the UI writes. Every rule and verb uses the awaited `SetX`. Design.Tests pin each behaviour. AC-11 (an open finishing inside a non-remote seam) is Optional and not built here.

## Intent

- The three skills quote one compiled aggregate for facts, consequences and intents, instead of `Approve` alone.
- Each shape the architecture names has a region and a test.

## Framework & Architectural Alignment

- VisionAlignment statement 8 and D27–D30. Interface-first, partial interfaces for `SetX`. Parent writes children through the child's method; a child reaches its root through `Parent`.
- Persistence is out of this example: the plan is created, edited and its rules exercised; the save path is WorkOrder's and Order's.

## Constraints & Invariants

- Zero NEATOO011/012 in Design.Domain; no `LoadValue`; no rule suspended for a verb.
- Every region compiles and is pinned by a test.

## Steps

1. Interfaces naming each member as fact, consequence or intent.
2. Root: the fit, planned minutes, validations, admission on `IsValid`, the anchor, `AddInterval`, `Approve`.
3. Child: the slider fact, next-mark facts, `SlideMinutes` with the anchor, `LockMinutes` as an ordered write.
4. Tests per behaviour, flip-checked.

## Acceptance

- [x] The fit shares approved minutes by effort and the cascade converges `[integration]`
- [x] Writing the effort slider refits; a nudge writes the next mark `[integration]`
- [x] A slide holds its interval for one fit, and the next fact refits everything `[integration]`
- [x] A lock holds its minutes, the fit shares the rest, and unlocking is a plain write `[integration]`
- [x] Admission follows validity through an `IsValid` trigger `[integration]`
- [x] Approve stamps then sets status, and admission has followed when it returns `[integration]`
- [x] Adding or removing an interval refits through the list's `Count` `[integration]`
- [x] Build and all tests green; zero NEATOO011/012 in Design `[explicit-skip: meta-bullet]`

## Current State (Pre-Flight)

- Design.Domain has OrderAggregate and WorkOrderAggregate; WorkOrder triggers on child paths (`t => t.Tasks![0].Cost`) and uses the four-trigger array form. `AddValidation` takes one trigger, so a validation over several facts is a `RuleBase` class.
- A list raises `NeatooPropertyChanged("Count")` on add and remove (`ValidateListBase.cs:158,195,276`), so `t => t.Intervals!.Count` triggers the root.
- Design.Tests build aggregates from `DesignTestServices.GetScope()` with real Neatoo services in Server mode.

## Test Evidence

Flip checks, each restored and the suite green after:
- The fit ignoring the anchor failed the slide test.
- `LockMinutes` writing minutes before the lock failed the lock test and the admission test that uses it.
- `CanApprove` without its `IsValid` trigger failed the admission test.
- The fit without its `Count` trigger failed the add and the remove tests (`reviews/005-flip-count-trigger.log`).
- `AnchorFit` not restoring the previous anchor failed only the nest test (`reviews/005-flip-anchor-restore.log`).
- `Interval.Create` without `RunRules` failed only the blank-name test (`reviews/005-flip-create-rules.log`).
- The fit without its every-interval-held guard failed only the all-locked test (`reviews/005-flip-all-locked.log`).

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Fit shares by effort, converges | Required | `[integration]` | `Design.Tests.AggregateTests.TrainingPlanTests.Fit_SharesTheApprovedMinutesByEffort`, `.EveryIntervalLocked_TheFitHasNothingToShare` | ✓ |
| Slider refits; nudge writes next mark | Required | `[integration]` | `TrainingPlanTests.EffortSlider_IsAFact_TheFitFollowsWhoeverWroteIt`, `.NextMark_IsAFactTheUiWritesBack`, `.NextMark_StopsAtTheEnds_AndEffortOutsideTheRangeIsInvalid` | ✓ |
| Slide anchors one fit; anchor evaporates | Required | `[integration]` | `TrainingPlanTests.SlideMinutes_FitsTheOthersAroundIt_AndTheAnchorEvaporates`, `.AnchorScopes_Nest_AndAnInnerScopeRestoresTheOuterAnchor`, `.AnchorScope_EndsWhenTheGestureThrows` | ✓ |
| Lock holds; unlock is a plain write | Required | `[integration]` | `TrainingPlanTests.LockMinutes_LocksThenSets_AndTheFitSharesTheRest` | ✓ |
| Admission follows validity via IsValid | Required | `[integration]` | `TrainingPlanTests.CanApprove_FollowsValidity`, `.ApprovedMinutesMustBePositive_AndAdmissionFollows`, `.NewIntervalWithABlankName_EntersInvalid_AndAdmissionFollows` | ✓ |
| Approve stamps then status | Required | `[integration]` | `TrainingPlanTests.Approve_StampsThenSetsStatus_AndAdmissionFollows` | ✓ |
| Add/remove refits via Count | Required | `[integration]` | `TrainingPlanTests.AddingAnInterval_IsACountChange_TheFitFollowsEvenWhenNoOtherFactChanges`, `.RemovingAnInterval_IsACountChange_TheFitFollows` | ✓ |
| Build and tests green; no NEATOO011/012 | Required | `[explicit-skip]` | `reviews/005-build.log`, `005-design-build.log` (0 NEATOO011/012), `005-test.log` (all assemblies under the hang detector), `005-race-25-runs.log` | ✓ |

## Gate Record

- **Test review round 1:** CONCERNS, all fixed.
- **Test review round 2:** APPROVED; its one open callout, the all-locked test not pinning its guard, was fixed and flip-checked. See `reviews/005-test-review.md`.
- **Code review:** APPROVED with four callouts. Three fixed; the fourth is a framework decision parked for Keith. See `reviews/005-code-review.md`.

## Plan Amendments

- A race in the changed-meta-property list under concurrent writes, which the full suite exposed in 1 of 25 runs, was fixed on this branch as a todo punchlist row; it is FCI-004's code, AC-4.
- `Interval.Create` takes the effort and runs its own rules, so `AddInterval` is one change and one fit (code review callout 1 and 2).
