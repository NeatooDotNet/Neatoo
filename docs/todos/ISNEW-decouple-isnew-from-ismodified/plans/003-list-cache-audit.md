# List Meta-State Cache Correctness Across Factory Ops

**Plan #:** 003
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-21
**Plan-review opt-in:** TBD at draft
**Code-review opt-in:** TBD at draft

---

## Scope

_Stub — Scope only; flesh out at Step 2._

Make list state correct across factory operations — two required fixes (both library
changes contained to the list base classes), plus regression tests:

1. **Cache staleness:** `ValidateListBase.FactoryComplete` sets `IsPaused = false` directly,
   skipping the validity/busy cache recalculation `ResumeAllActions` performs — so a list
   populated while paused by its own factory op can carry stale `IsValid`/`IsBusy` (and
   `EntityListBase`'s modified cache) after the op completes.
2. **Paused-add child marking (upgraded to REQUIRED by the ISNEW-001 discovery, 2026-08-21):**
   `MarkAsChild`/`SetContainingList` run only in the un-paused add path, and the un-paused
   path is unusable for fetch (it `MarkModified()`s non-new items) — so no fetch shape today
   yields `IsChild=true` + clean state, and `item.Delete()` bypasses list routing for
   fetched children. Run these baseline-neutral calls for paused adds too, then lift the
   ISNEW-001 documentation caveats (OrderItem.cs / OrderItemList.cs notes citing ISNEW-003).
