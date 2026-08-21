# Code Review Record — ISNEW-004 — 2026-08-21

**Reviewer:** code-reviewer agent (opted-in, deep budget). Findings-only, no grade.

## Direction

Clean: the flip landed in the right three seams, and the reviewer walked every disjunct of
`IsSavable` against the four `Save()` guard branches, confirming each has a matching check in
an order that reports the true reason for new+child, new+invalid and new+busy. Dropping
`IsSelfModified` from the guard is safe (subsumed by `IsModified`). The `RemoveItem`
notification fix was verified as correctly placed and guarded — it cannot double-raise, fire
while paused, or break the false→true direction (`DeletedList.Add` precedes
`base.RemoveItem`, so that transition is still announced by the base call). The reviewer also
found the mark placement *better protected than its own comment claimed*: `EntityLazyLoad`
implements `IEntityMetaProperties` but not `IEntityBaseInternal`, so the pattern match cannot
match a lazy property even if a `Value` notification fired. That is now stated in the comment.

## Veto-tier findings — all fixed

**V1 — A factory-built child list assigned to a live parent no longer dirtied it.** The
justification for excluding lists ("list dirt aggregates from children, which are marked as
they are attached") holds only for **live** adds. A list built by its own factory operation
adds its children while paused, so they carry no mark; post-flip their `IsNew` no longer
makes them modified, so the list came out clean and assigning it to a live parent left the
parent clean and unsavable — the children's inserts would never run. Pre-flip the weld
prevented this. **Real regression introduced by this plan.**
**Fixed:** an assigned entity *list* now marks its new children (the list itself cannot be
marked — no `MarkModified`, `IsMarkedModified => false`). Pinned by
`ChildPropertyAttachTests.AssignFactoryPopulatedListToLiveParent_DirtiesParent`, verified
failing on revert.

**V2 — `SetItem` lost a channel, and the deferral comment was factually wrong.** Replacement
*did* dirty the graph for new items: the cache arithmetic reads `item.IsModified`, which the
weld made true for any fresh item. Post-flip `list[i] = newItem` on a clean fetched root left
it unsavable. The comment claimed "replacement has never dirtied the graph here, and an
existing test pins that" — but that test (`SetItem_ReplaceModifiedWithUnmodified_...`) builds
every participant with `CreateExistingItem()`, so it says nothing about new items.
**Real regression introduced by this plan.**
**Fixed:** `SetItem` marks a **new** incoming item (same parity scoping as the property
channel, which is why the existing-item test still passes). Comment rewritten to state the
truth. Pinned by `EntityListBaseTests.SetItem_ReplaceWithNewItem_ListBecomesModified`,
verified failing on revert. `SetItem`'s *other* defects (displaced item orphaned, no child
identity, no guards) remain ISNEW-009's, and that stub was corrected — it had claimed
ISNEW-004 covers the incoming item's mark, which Amendment 2 had disclaimed.

**V3 — Acceptance bullet 10 was uncovered and the Test Evidence row had been reworded** from
"new-but-**busy**" to "new-but-**unsavable**", citing a test of invalidity. `SaveFailureReason.IsBusy`
had no assertion anywhere in the solution. The behavior was correct by inspection, but this
was the one bullet written specifically because the flip could break it.
**Fixed:** `EntityBaseStateTests.Save_WhenNewAndBusy_ThrowsIsBusy_NotNotModified` — the only
case that makes the `|| IsNew` term in the guard load-bearing; verified failing on revert.
Evidence row corrected.

**V4 — `CreatedThenDeleted_*` was tautological.** It never asserted `IsSavable` despite its
name, never called `Save()`, and its store-count assertion was guaranteed by `Reset()`. The
reason it stopped short was itself the finding: `Invoice` had no `[Delete]`, so `Save()` would
have thrown — declining to call it was working around a production limitation in a test.
**Fixed:** `Invoice` gained a `[Delete]`; the test now asserts `IsSavable`, calls `Save()`,
and asserts the new-and-deleted short-circuit touches persistence in neither direction. A
companion test pins that a *fetched*-then-deleted root really is deleted.

## Callout-tier findings

- **C1** — `LazyLoadingAChild_DoesNotDirtyTheParent` lazy-loaded nothing (`Invoice.Lines` is a
  plain property). **Renamed** to `ReadingAFetchedGraph_DoesNotDirtyIt` with the real lazy
  coverage attributed to the pre-existing `LazyLoadStatePropagationTests`.
- **C2** — `AssignChildDuringPausedFactoryOperation_LeavesParentClean` asserted
  `IsSelfModified` immediately after `MarkUnmodified()` (guaranteed by its own setup) and on
  the wrong property besides. **Fixed:** asserts `child.IsModified` and `parent.IsModified`
  *before* any `Mark*` call.
- **C3** — A surviving comment documented the deleted weld chain. **Fixed.**
- **C4** — Dangling review-file references. **Fixed:** this file, `004-test-review.md`, and
  `004-plan-review.md` written; the `003-code-review.md` citation in the ISNEW-008 stub
  corrected to point at the record that exists.
- **C5** — `FactoryComplete_AfterPausedAddOfBusyItem_ListReportsBusy` is ISNEW-003's lane and
  arrived without an evidence row. Accepted; recorded in the ISNEW-003 review instead.

## Build & test after fixes

Solution 2178 passed / 2 pre-existing skips; Design.Tests 129/129; 0 build errors.
Every new regression test was verified by reverting its fix and confirming failure.
