# LIST-004 — Code Review (Step 5, per-plan, findings-only)

**Date:** 2026-08-21 (run retroactively — see close-out audit V1)
**Reviewer:** `code-reviewer`
**Object:** commit `fdbbe93`
**Budget:** deep
**Outcome:** **One veto-tier finding — a real defect in the fix. Fixed.**

---

## The finding: marking in place never rejoined the cleanup contract

The reviewer confirmed the headline symptom was genuinely fixed — a paused `Delete()` no longer
discards the deletion, and it verified against the two real list `[Update]` methods in the repo
*plus* the authoritative `Design.Domain/FactoryOperations/SavePatterns.cs:356-369` pattern that
`this.Union(DeletedList)` filtered on `IsDeleted` does pick up a marked-in-place item and issue the
DELETE.

Then it asked the question the plan never did: **what happens after that save?**

Traced mechanically, and independently re-verified before acting:

- Paused `Delete()` set `IsDeleted = true` and left the child a **live member of the list** — by
  design, asserted by the plan's own test.
- It was **never added to `DeletedList`**. The only cleanup path that exists —
  `FactoryComplete(Update)` clearing `ContainingList` and draining the queue — iterates
  `DeletedList`, so it never touched this child. Nothing else removed it either.
- `EntityBase.IsSelfModified` includes `|| this.IsDeleted` (`EntityBase.cs:187`), so the child is
  `IsModified` **forever** — nothing clears `IsDeleted` automatically.
- `ResumeAllActions` recalculates `_cachedChildrenModified = this.Any(c => c.IsModified)`, which
  keeps finding it, so `list.IsModified` is stuck `true` and bubbles to the root as unsaved work
  that does not exist.

Two concrete consequences: a `[Fetch]` that deletes a child mid-load — the exact shape of the
plan's own pinned test — produces a **freshly loaded aggregate reporting `IsModified = true`; and
the `Union(DeletedList)`/`IsDeleted` loop **re-issues the DELETE on every subsequent save, forever**.

The doc comment the plan added ("a marked-deleted item still in the list is persisted correctly")
was true for exactly one save cycle and overclaimed past it. No test in the diff called
`FactoryComplete(Update)` after a paused delete, which is why nothing caught it.

## Disposition — fixed, not accepted

Confirmed empirically before changing anything: the missing test
(`Delete_WhenListPaused_ThenSave_LeavesTheListClean`) was written first and **failed** on
`list.Contains(doomed)`, exactly as the reviewer predicted.

The fix replaces the `IsPaused`-peeking approach entirely. `IEntityListBaseInternal.IsPaused` is
gone — it existed only so `EntityBase.Delete()` could branch — and is replaced by
`DeleteChild(IEntityBase)`, which puts the decision on the list that owns what leaving it means:

- while paused and the child was persisted: mark deleted and queue it (the work `RemoveItem`'s
  paused branch deliberately skips);
- while live: leave that to `RemoveItem`, so nothing is done twice;
- **always remove the child**, which is what rejoins the cleanup contract.

`RemoveItem`'s paused branch is still untouched — `list.Remove(item)` during a fetch remains
baseline construction.

Revert verification of the corrected fix: reverting `Delete()` to an unconditional `Remove` fails
**exactly two** tests — the original recording test and the new round-trip test — while the
live-path, parentless, and guard tests keep passing.

## Callouts

**C1 — stale `ContainingList` on a displaced item (LIST-002's, not this plan's).** `SetItem`'s
paused branch confers `ContainingList` on the incoming item but never clears it on the *displaced*
one, so that item keeps a reference to a list it is no longer in; a later `Delete()` on it records
a deletion with no persistence consequence. The reviewer confirmed this failure mode **already
existed identically** for the live path before LIST-004 (`Collection<T>.Remove` on an absent item
is a no-op), so it is a pre-existing `SetItem` gap rather than something this plan introduced.
Recorded in the Discovery Log and carried forward.

**C2 — no test-gate artifact for plan 004.** Correct: the per-plan test gate never ran for LIST-004
either. The reviewer independently spot-checked all seven Test Evidence rows against the actual
test bodies and found them honest — cited tests exist and assert what they claim, and the untouched
`AddToList_WhenPaused_SkipsCrossAggregateCheck` is confirmed absent from the diff. Recorded in
Skipped Steps rather than letting this code review stand in for a gate it is not.

## Verified, no findings

- `IEntityListBaseInternal` is implemented only by `EntityListBase<I>`; the explicit implementation
  reads a non-virtual auto-property, so no recursion or shadowing risk.
- `InsertItem` guard dispositioning is honestly asserted in both directions with paired live/paused
  tests.
- Release note matches the code.
- Logs check out: build succeeded; 1845/0/2 at that commit; the revert log shows exactly one
  failure with the live-path and parentless controls passing.
