# List Meta-State Baseline and Paused Delete

**Plan #:** 001
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md) (carved from ISNEW-008)
**Status:** Draft
**Last Updated:** 2026-08-21
**Plan-review opt-in:** TBD at draft
**Code-review opt-in:** Yes (library change to notification machinery)

---

## Scope

_Stub — Scope only; flesh out at Step 2. Queued from the ISNEW-003 code review, whose
findings are summarized in `reviews/003-test-review.md` and in the todo Discovery Log. Both
defects are pre-existing but live in the two methods ISNEW-003 now owns._

Two defects in `EntityListBase`'s factory-completion and removal paths:

1. **Stale meta-state baseline swallows the next notification.** On
   `FactoryComplete(Update)` the resume snapshots `EntityMetaState` while the DeletedList is
   still populated (baseline records modified = true), then the Update branch clears the
   DeletedList so the real value becomes false — with nothing to announce it or correct the
   baseline. The list then sits at actual false / baseline true, so the user's *next* child
   edit raises no `PropertyChanged`; the parent's cached `IsModified` never refreshes and the
   aggregate reports not-savable. Traced end to end by the reviewer through
   `ValidateProperty.PassThruValuePropertyChanged` → `EntityPropertyManager`. The suite
   misses it because the SaveLifecycle fixtures are `[Remote]`, so the post-save graph is
   deserialized and `OnDeserialized` rebuilds a correct baseline — the hole is open only on
   a **local / fat-client save**. Likely fix: `CheckIfMetaPropertiesChanged()` at the end of
   the Update branch instead of leaving the baseline where the resume put it. Needs a test
   that edits a child after a save that carried deletions, on a non-remote save.
2. **`Delete()` inside a paused window silently drops a child.** With `ContainingList` now
   set on fetched children (ISNEW-003), `EntityBase.Delete()` routes to
   `ContainingList.Remove(this)`, and the paused branch of `RemoveItem` neither marks the
   item deleted nor queues it — it just removes it. A widening of a hole that already
   existed for live-added items; no canonical flow reaches it today (no factory body calls
   `Delete()`), so it is a guard-the-seam fix rather than an observed failure.

Also fold in the ISNEW-003 test-review tech debt that lives in the same machinery: the
`HandlePropertyChanged` pause-guard asymmetry (load-bearing but unasserted — adding a guard
"for symmetry" would reopen the ISNEW-003 defect with a green suite) and
`EntityListBase.IsModified` raising no `PropertyChanged` at all (standing NOTE in
`EntityListBaseTests`; a real hole for Blazor bindings).

---

## Additional scope adopted at close-out (2026-08-21)

The close-out audit (veto V7) found five gate findings that had been recorded as "queued" but
traced to no Plan Index entry — they would have evaporated when this todo closed. Plus one
`MISSING` Test Evidence row accepted from ISNEW-004 (veto V6). All are folded in here because
they live in the same list/notification machinery this plan already owns, or in the same test
suites it will touch:

1. **`ResumeAllActions`' `if (IsPaused)` guard makes `FactoryComplete` a no-op on a
   never-paused list** — a live hole in the fix ISNEW-003 shipped. Several unit tests drive
   `FactoryComplete` without `FactoryStart` and therefore never reach the new recalculation
   path at all. This one has standalone merit and should be handled first.
   (`reviews/003-test-review.md`)
2. **The paused `InsertItem` branch skips the duplicate-add, busy-item and cross-aggregate
   guards** the live path enforces. Defensible for trusted input (fetch, deserialization),
   but unasserted in either direction. (`reviews/003-test-review.md`)
3. **`MockOrderRepository.GetItems` / `MockEmployeeRepository.GetAddresses` ignore their
   parent-id argument**, which blocks any per-parent child-loading test in Design.Tests.
   (`plans/006` Outcome)
4. **`SaveFailureReason.NoFactoryMethod` has no assertion anywhere in the solution** — one
   save-guard reason entirely uncovered. (`reviews/004-test-review.md`)
5. **`EntityParentChildFetchTests`' fixture cleans its own objects before asserting**, so it
   structurally cannot distinguish "the framework kept it clean" from "the test cleaned it" —
   the same vacuity shape the ISNEW-004 gate found in `ChildPropertyAttachTests`.
   (`reviews/004-test-review.md`)
6. **A parent-side lazy-load assertion.** ISNEW-004's "lazy-loading a child does not dirty its
   parent" bullet is an accepted `MISSING`: the behavior is correct and doubly protected, but
   the only candidate test (`LazyLoadStatePropagationTests.LazyLoadChild_InitialState_ParentNotModified`)
   asserts `child.IsModified` and never touches the parent, despite its name.

---

## Retirement / Carve-Out Note

Carved out of the ISNEW arc at its close-out (2026-08-21). This plan was written as
ISNEW-008 and never implemented; its provenance lines still cite ISNEW review records,
which remain the authoritative source for each finding.
