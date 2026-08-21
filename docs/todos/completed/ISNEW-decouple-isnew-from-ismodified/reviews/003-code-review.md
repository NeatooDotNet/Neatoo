# Code Review Record — ISNEW-003 — 2026-08-21

**Reviewer:** code-reviewer agent (opted-in, deep budget). Findings-only, no grade.

> **Record note (written 2026-08-21 during the close-out audit, veto V5).** This review ran
> at the time and produced the findings below, but the record file was never written — only
> its downstream consequences were (the ISNEW-008 stub, an ISNEW-006 entry, and a one-line
> summary in the plan header). The audit correctly flagged that three documents cited a file
> that did not exist, and that the plan's header called the review "clean" when it had
> produced five callouts, two of which spawned a whole plan. Reconstructed here from those
> preserved records rather than from the reviewer's original message; the findings and their
> dispositions are accurate, the wording is not verbatim.

## Direction

**No veto-tier findings.** Both fixes land in the right place: routing
`ValidateListBase.FactoryComplete` through `ResumeAllActions()` collapses a second, weaker
resume into the framework's single one (copying the shape `OnDeserialized` already used), and
the child-identity split in `EntityListBase.InsertItem` draws the line exactly where the plan
said — `MarkAsChild`/`SetContainingList` cross to the paused branch, `MarkModified` and the
dirt cache stay live-only. The reviewer verified the "baseline-neutral" claim the whole fix
rests on rather than accepting it: `IsChild` is a plain auto-property, `SetContainingList`
sets a field, neither raises a notification or touches `IsSelfModified`, and neither is
overridden anywhere in the repo.

Ordering was checked explicitly: `base.FactoryComplete` resolving to
`EntityListBase.ResumeAllActions` before the Update-only cleanup does **not** suppress a
notification consumers previously received — neither the pre- nor post-change path calls
`CheckIfMetaPropertiesChanged()` inside `FactoryComplete`, so the DeletedList-clearing
`IsModified` flip was silent before and after.

## Callout-tier findings and dispositions

1. **`FactoryComplete(Update)` leaves the meta-state baseline stale.** The resume snapshots
   `EntityMetaState` while the DeletedList is still populated (baseline records modified =
   true), then the Update branch clears it — leaving actual-false / baseline-true, so the
   user's *next* child edit raises nothing and the parent's cached `IsModified` never
   refreshes. Traced end to end through `ValidateProperty.PassThruValuePropertyChanged` into
   `EntityPropertyManager`. Pre-existing, but this plan now owns the method. Only reachable
   on a **local/fat-client save** — remote saves mask it because `OnDeserialized` rebuilds a
   correct baseline. **Queued to ISNEW-008.**
2. **The `[Create]` path's list `IsModified` changed as an unstated side effect.** The
   recalculation now runs on every factory operation, so a rich `[Create]` with children went
   from reporting false (stale cache that never ran) to true (real computation, children new
   and welded). Direction right, self-corrects at the flip — but the ISNEW-004 target used to
   be produced *by accident*. **Recorded**; ISNEW-003's test review then made it must-cover
   and it is pinned by the `Lines.IsModified` row in `AggregateSaveLifecycleTests`.
3. **`Delete()` inside a paused window silently drops a child.** With `ContainingList` now set
   on fetched children, `EntityBase.Delete()` routes to `ContainingList.Remove(this)`, and the
   paused `RemoveItem` branch neither marks deleted nor queues. A widening of a hole that
   already existed for live-added items; no canonical flow reaches it today. **Queued to
   ISNEW-008.**
4. **The fix incidentally repairs remove-then-re-add of a fetched child.** The re-add path
   only clears `DeletedList` when the item has a `ContainingList`; before this plan a fetched
   child removed and re-added stayed in `DeletedList` *and* the collection, so the next save
   would both update and delete it. A real bug fixed for free. **Regression test added in
   ISNEW-006** (`ListFactoryStateTests.FetchedChild_RemovedThenReAdded_LeavesDeletedList`).
5. **`SetItem` applies no child identity in either branch**, and the replaced item keeps
   `IsChild`/`ContainingList` while never reaching `DeletedList`. Confirming rather than
   reporting — the plan correctly did not touch it. **Queued to ISNEW-009.**

Doc nit: the `OrderItem.cs` WRONG/RIGHT pair opened with identical lines. **Fixed in ISNEW-006.**

## Acceptance bullet 6 (caveat removal) — verified

`grep -rn "ISNEW-003" src --include=*.cs` outside the test project returns nothing, and each
of the three rewritten Design.Domain blocks was checked against the code: the dirt/identity
split, the Parent/Root timing, and the "ContainingList cannot be serialized, so the paused add
re-establishes it" claim all match. The last was confirmed at the source —
`NeatooListBaseJsonTypeConverter` writes active and DeletedList items into `$items` and reads
them all back through `list.Add` while paused, which is what makes the paused branch the only
place `ContainingList` can be restored.

## Sacred tests

All three inverted tests pulled from the prior commit and compared: each had exactly one
assertion, each comment described the mechanism rather than a requirement, and no setup, edge
case, or additional assertion was dropped. The new `Add_WhenPaused_DoesNotMarkModified` covers
the other half of the contract on a clean item.

## Logs

`003-build.log` / `003-test.log` and `003-design-build.log` / `003-design-test.log`. **These
archived logs predate the plan's test-review fix loop** (they show 2160 and 113); the final
state after that loop is captured in `reviews/final-test.log` (2178) and
`final-design-test.log` (129).
