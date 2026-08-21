# Test Review Record — ISNEW-004 — 2026-08-21

**Reviewer:** test-reviewer agent (deep budget). **Gate: PASSES after the fix loop.**

## must-cover findings and fixes

**MC1 — The `|| IsNew` term in the `Save()` guard was unreachable by every test in the
solution.** The reviewer enumerated the paths: reaching that line needs an entity that is
valid, non-child, and unsavable — i.e. either (a) neither modified nor new, or (b) busy. Case
(a) reports `NotModified` with or without the term and is well covered; only a **new,
unmodified, busy** entity distinguishes the two formulas, and nothing constructed one.
Deleting the term would have left the whole solution green while violating the plan's own
acceptance bullet. Compounding it, the Test Evidence row had been reworded from "new-but-busy"
to "new-but-unsavable" to fit the test that got written, instead of being marked `MISSING`.
**Fixed:** `EntityBaseStateTests.Save_WhenNewAndBusy_ThrowsIsBusy_NotNotModified` (with a
`MarkBusyForTest` helper added to the fixture), verified failing on revert. Evidence row
corrected.

**MC2 — The baseline-population constraint for the new child-property mark had no
non-vacuous assertion.** `AssignChildDuringPausedFactoryOperation_LeavesParentClean` was
vacuous twice: it asserted `IsSelfModified == false` one line after `MarkUnmodified()` (which
guarantees it), and `IsSelfModified` is the wrong property regardless — the mark lands on the
*child*, and a child's dirt can only surface as `parent.IsModified`, never as
`parent.IsSelfModified`. A mark leaking through the paused guard would not have been caught.
**Fixed:** asserts `child.IsModified` and `parent.IsModified` before any `Mark*` call.

## should-cover — all addressed

- **SC1** — `CreatedThenDeleted_*` asserted neither half of its name and its store-count
  assertion was guaranteed by `Reset()`. Fixed with the code review's V4 (see
  `004-code-review.md`): `Invoice` gained a `[Delete]`, the test now saves and asserts the
  short-circuit, and a companion test covers fetched-then-deleted.
- **SC2** — Re-attaching a removed new child is newly reachable (a removed item keeps its
  `ContainingList`, so the upward notification now fires for new items too) and had no
  modified-state assertion. The reversibility test gained a re-add step.
- **SC3** — The `MarkModified()` create opt-in was never carried through a `Save()`. If the
  flag failed to clear, every consumer following the new idiom would get a permanently dirty
  aggregate. The opt-in test now saves and asserts it clears.
- **SC4** — Two stale weld references fixed: the mechanism comment at
  `EntityListBaseStateTransitionTests:126`, and `TwoContainerMetaStateTests
  .Create_TwoContainer_IsSavable_ReturnsTrue`, which enumerated every `IsSavable` term
  *except* the modified/new one and so passed under both the old and new formula. It now
  asserts `IsNew` true and `IsModified` false.

## nice-to-have — dispositioned

- **NH1** (`MapModifiedTo` over an entity-child property) — accepted and queued; property-level
  `IsModified` semantics are unchanged for scalars, which is every current usage.
- **NH2** (build-then-assign list ordering) — this is exactly the case the code review's V1
  turned out to be, and it is now covered by
  `AssignFactoryPopulatedListToLiveParent_DirtiesParent`.
- **NH3/NH4** — evidence-row wording corrected; the `RemoveItem` fix is pinned transitively by
  a test verified to fail without it.

## Sacred tests

Twelve pre-existing tests modified; the reviewer found **no weakening** — each preserved or
strengthened its intent and carries a comment explaining the semantic change. Specifically
confirmed: the `ListFactoryStateTests` rename kept its clean-state assertions (and gained a
boundary proof), `RichCreate_Untouched_*` closes the ISNEW-003 `Lines.IsModified` baseline
row, and `DeletedListTests` — the one test whose original intent the flip deletes outright —
was named in Acceptance ahead of time and now asserts strictly more.

## Tech debt queued

`SaveFailureReason.NoFactoryMethod` has no assertion anywhere; `EntityParentChildFetchTests`'
fixture cleans its own objects before asserting, so it structurally cannot distinguish
"the framework kept it clean" from "the test cleaned it" — the same shape as MC2.

## Logs

`004-build.log` / `004-test.log` (solution 2178 passed / 2 pre-existing skips),
`004-design-build.log` / `004-design-test.log` (129/129). Re-run after the fix loop.
