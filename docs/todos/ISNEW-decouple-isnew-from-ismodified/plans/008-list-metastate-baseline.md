# List Meta-State Baseline and Paused Delete

**Plan #:** 008
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-21
**Plan-review opt-in:** TBD at draft
**Code-review opt-in:** Yes (library change to notification machinery)

---

## Scope

_Stub — Scope only; flesh out at Step 2. Queued from the ISNEW-003 code review
(`reviews/003-code-review.md`, callouts 1 and 3), which found both defects pre-existing but
living in the two methods ISNEW-003 now owns._

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
