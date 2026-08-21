# Paused-Path Guards: `Delete()` and `InsertItem`

**Plan #:** 004
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md) (re-split out of the original LIST-001)
**Status:** Draft
**Last Updated:** 2026-08-21
**Plan-review opt-in:** TBD at draft
**Code-review opt-in:** Yes — save-routing seam.

---

## Scope

_Stub — Scope only; flesh out at Step 2._

Two paused-branch seams in `EntityListBase`, neither reached by a canonical flow today, both
guard-the-seam work rather than observed failures:

1. **`Delete()` inside a paused window silently drops a child.** With `ContainingList` now set
   on fetched children (ISNEW-003), `EntityBase.Delete()` routes to
   `ContainingList.Remove(this)`, and the paused branch of `RemoveItem` neither marks the item
   deleted nor queues it in `DeletedList` — it just removes it, so no DELETE is ever issued.
   This widens a hole that already existed for live-added items. No factory body calls
   `Delete()` today, which is why nothing fails. Decide whether the paused path routes the
   deletion correctly or refuses it outright; a refusal is a legitimate answer for a seam whose
   only callers would be framework-internal.

2. **The paused `InsertItem` branch skips the duplicate-add, busy-item, and cross-aggregate
   guards** the live path enforces. Defensible for trusted input (fetch, deserialization) —
   and `RootPropertyTests.AddToList_WhenPaused_SkipsCrossAggregateCheck` already pins the
   cross-aggregate skip deliberately — but the duplicate and busy skips are unasserted in
   either direction. Disposition each: intentional (assert it, with the reason) or a hole
   (close it).

Provenance: ISNEW `reviews/003-test-review.md`.
