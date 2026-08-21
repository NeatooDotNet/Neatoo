# Paused-Path Guards: `Delete()` and `InsertItem`

**Plan #:** 004
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md) (re-split out of the original LIST-001)
**Status:** Done
**Last Updated:** 2026-08-21
**Plan-review opt-in:** No — no direction question remains; both seams are diagnosed and the
choice between "route it" and "refuse it" is argued in Steps and settled by the reachability
facts already in hand.
**Code-review opt-in:** Yes — changes `Delete()` routing, which is save-side behavior.

---

## Scope

Two paused-branch seams in `EntityListBase`, neither reached by a canonical flow today. The first
loses data silently and is fixed; the second is a set of skipped guards that get dispositioned
and asserted rather than changed.

1. **`Delete()` inside a paused window silently drops a child.** `EntityBase.Delete()` delegates
   to `ContainingList.Remove(this)`, and `RemoveItem`'s paused branch neither marks the item
   deleted nor queues it in `DeletedList` — it just removes it, so no DELETE is ever issued and
   the row is orphaned. ISNEW-003 widened this by setting `ContainingList` on fetched children.
2. **The paused `InsertItem` branch skips the duplicate-add, busy-item, and cross-aggregate
   guards** the live path enforces. Disposition each as intentional-and-asserted or a hole.

This plan does **not** touch `SetItem` (LIST-002) or the inherited test debt (LIST-005), and does
not change what `RemoveItem` does on its *live* branch.

## Intent

Deleting a child never silently loses the deletion, whatever window it happens in. The paused
insert path's skipped guards stop being an open question: each is either enforced or asserted as
deliberate, so a future reader does not have to guess which.

## Framework & Architectural Alignment

- The paused branch of `RemoveItem` skipping the delete-queue is **correct for its own purpose**:
  during a fetch or deserialization, removals are baseline construction, not user deletions.
  Queueing those would manufacture phantom DELETEs. The fix therefore does not change
  `RemoveItem`; it changes what `Delete()` — an explicit statement of intent — does when it finds
  itself in that window.
- `IEntityListBaseInternal` is the existing seam for framework-only access to a list. Exposing
  pause state there keeps it off the public `IEntityListBase` surface, where it would invite
  consumers to branch on it.
- Cross-aggregate protection is deliberately skipped while paused so deserialization can
  reassemble a graph — already pinned by
  `RootPropertyTests.AddToList_WhenPaused_SkipsCrossAggregateCheck`.

## Constraints & Invariants

- `RemoveItem`'s paused branch keeps its current behavior; `list.Remove(item)` during a factory
  operation stays baseline construction.
- The public `IEntityListBase` surface does not grow. Pause state is framework-internal.
- Existing tests are sacred — in particular `AddToList_WhenPaused_SkipsCrossAggregateCheck`,
  which pins a skip this plan must not "fix".
- No canonical flow calls `Delete()` inside a factory body today, so this is guard-the-seam work:
  it must not change behavior for any flow that exists.

## Current State

Walked 2026-08-21.

- `EntityBase.Delete()` (`src/Neatoo/EntityBase.cs:411-421`): if `ContainingList != null`,
  delegates to `ContainingList.Remove(this)` and returns; otherwise `MarkDeleted()`.
- `EntityListBase.RemoveItem` (`:310-348`): the mark-deleted-and-queue work is entirely inside
  `if (!this.IsPaused)`. When paused, `base.RemoveItem(index)` runs alone.
- `RemoveItem` deliberately uses `((IEntityBaseInternal)item).MarkDeleted()` rather than
  `item.Delete()`, with the comment "to avoid recursion with Delete()" — so the framework itself
  never routes through `Delete()`, which is why nothing hits this today.
- `IsPaused` is a public property on `ValidateListBase` (`:97`) but is **not** on
  `IValidateListBase` / `IEntityListBase`. `EntityBase` holds its list as `IEntityListBase?`
  (`:243`) and therefore cannot see pause state today.
- `IEntityListBaseInternal` (`src/Neatoo/InternalInterfaces.cs:151-165`) already carries
  `DeletedList` and `RemoveFromDeletedList` — the natural home.

## Steps

1. Give framework code a way to see a list's pause state without widening the public surface.
2. Make `Delete()` preserve the deletion when its containing list is paused, rather than
   delegating to a removal that discards it. Marking in place is preferred over throwing: it
   keeps the entity's intent recorded, and the canonical list `[Update]` pattern
   (`this.Union(DeletedList)` filtered on `IsDeleted`) already persists a marked-deleted item
   that is still in the list.
3. Confirm the live path is untouched — `Delete()` outside a paused window still routes through
   the list exactly as before.
4. Disposition the paused `InsertItem` guards: assert the duplicate-add and busy-item skips in
   whichever direction the code actually behaves, with comments stating why the skip is
   acceptable for trusted factory input.
5. Verify by revert; full-suite run.

## Acceptance

- `Delete()` on a child whose list is paused records the deletion instead of discarding it, so a
  subsequent save still issues the DELETE [unit]
- `Delete()` on a child whose list is **not** paused behaves exactly as before — routed through
  the list, queued in `DeletedList` [unit]
- `Delete()` on a parentless entity still marks it deleted [unit]
- The paused `InsertItem` duplicate-add and busy-item skips are asserted in the direction the
  code actually behaves, each with a stated reason [unit]
- `AddToList_WhenPaused_SkipsCrossAggregateCheck` still passes unmodified [unit]
- Full solution suite green, no existing test's intent weakened [unit]

---

## Plan Amendments

**A1 (Step 2, marked in place rather than throwing).** Both options were live at draft. Marking
in place won because it preserves the caller's intent and composes with the canonical list
`[Update]` loop, which iterates `this.Union(DeletedList)` and filters on `IsDeleted` — so a
marked-deleted item still in the list is persisted correctly without any change to consumer code.
Throwing would have been safe (nothing calls this today) but would convert a recoverable
situation into a hard failure for a caller doing something reasonable.

**A2 (Step 1, internal rather than public).** `IsPaused` went on `IEntityListBaseInternal`, not
`IEntityListBase`. Pause state is a framework implementation detail, and putting it on the public
interface would invite consumers to branch on it — which is the sort of thing that turns an
internal lifecycle detail into a compatibility constraint.

**A3 (Step 4, both guards asserted, not just one).** The draft acceptance named the duplicate-add
*and* busy-item skips. The busy case needed a `MarkBusyForTest` helper on the test item
(mirroring the one `IEntityPerson` already exposes) — additive, no existing helper changed — so
both are asserted rather than one being quietly dropped.

## Test Evidence

| Acceptance bullet | Test | Tier | Status |
|---|---|---|---|
| `Delete()` on a child of a paused list records the deletion | `EntityListBaseTests.Delete_WhenListPaused_RecordsTheDeletionInsteadOfDiscardingIt` | unit | Pinned — sole failure on revert |
| `Delete()` on a child of a live list behaves exactly as before | `EntityListBaseTests.Delete_WhenListIsLive_StillRoutesThroughTheList` | unit | Pinned — passes on revert, confirming the live path is untouched |
| `Delete()` on a parentless entity still marks it deleted | `EntityListBaseTests.Delete_WhenParentless_MarksDeleted` | unit | Pinned — passes on revert |
| Paused `InsertItem` duplicate-add skip asserted, with reason | `Add_WhenPaused_AllowsDuplicate_UnlikeTheLivePath`, paired with `Add_WhenLive_RejectsDuplicate` | unit | Pinned |
| Paused `InsertItem` busy-item skip asserted, with reason | `Add_WhenPaused_AllowsBusyItem_UnlikeTheLivePath`, paired with `Add_WhenLive_RejectsBusyItem` | unit | Pinned |
| `AddToList_WhenPaused_SkipsCrossAggregateCheck` still passes unmodified | Pre-existing `RootPropertyTests` test; not touched by this plan's diff | unit | Verified unmodified |
| Suite green, no intent weakened | Solution run: 1845 passed / 0 failed / 2 skipped, plus Samples 254, BaseGenerator 42, Person.DomainModel 55 | unit | Pinned |

## Outcome

`Delete()` no longer silently discards a deletion when its containing list is paused. The change
is deliberately narrow: `RemoveItem`'s paused branch is untouched, so `list.Remove(item)` during a
factory operation remains baseline construction rather than a user deletion — the distinction the
whole fix rests on.

Revert verification came back exact: reverting `Delete()` to unconditional delegation fails
**one** test, and the live-path and parentless tests keep passing — which is the evidence that
those two are genuine controls on "the live path is unchanged" rather than tests that happen to
travel with the fix.

The paused `InsertItem` skips are now recorded decisions rather than open questions. Each is
asserted in the direction the code actually behaves and paired with its live-path counterpart, so
the asymmetry reads as deliberate; if anyone later makes the paused branch enforce these, a test
fails and the decision gets revisited on purpose instead of by accident.
