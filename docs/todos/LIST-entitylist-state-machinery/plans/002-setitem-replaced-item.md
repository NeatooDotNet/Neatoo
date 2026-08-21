# SetItem: Identity and Persistence for the Replaced Item

**Plan #:** 002
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-21
**Plan-review opt-in:** **Yes** — changes save-side behavior for existing consumers: a row that
survives today starts being deleted. Reviewed before implementation.
**Code-review opt-in:** Yes — save routing.

---

## Scope

Decide and implement what `EntityListBase.SetItem` does with the **displaced** item when a list
element is replaced (`list[0] = replacement`), and give the **incoming** item the child identity
it currently never receives.

Today `SetItem` performs cache arithmetic and one narrow mark. Relative to `InsertItem`'s live
branch it does no duplicate check, no busy check, no aggregate-boundary check, no `UnDelete`, and
sets no child identity. Relative to `RemoveItem` it drops the displaced item without `MarkDeleted`
and without queueing it in `DeletedList`. The consequence is that **replacing a persisted child
silently orphans its row** — no DELETE is ever issued, and the child also keeps a stale
`ContainingList` pointing at a list it is no longer in.

This plan does **not** revisit the `item.IsNew` marking already in `SetItem` — that channel was an
ISNEW-004 regression, fixed and pinned there (`reviews/004-code-review.md` V2).

## Intent

`list[i] = replacement` behaves like the remove-plus-insert a reader would expect it to be: the
displaced child's row is deleted rather than orphaned, and the incoming child is a full member of
the aggregate rather than an item that happens to be in a collection.

## Framework & Architectural Alignment

- `RemoveItem` and `InsertItem` already define what leaving and joining a list mean. `SetItem`
  should compose those two meanings rather than invent a third — the bug is precisely that it
  currently implements neither.
- Child identity (`MarkAsChild` + `SetContainingList`) is what makes save routing and `Delete()`
  work; ISNEW-003 established that it must be set on *every* channel by which a child joins a
  list, and `SetItem` is the one channel that still does not.
- Deletion is queue-then-persist: `MarkDeleted` plus a `DeletedList` entry, drained by the list's
  `[Update]`. A displaced persisted child needs both.
- The paused branch stays trusted input, consistent with LIST-004's disposition.

## Constraints & Invariants

- **This is an observable behavior change for existing consumers** and needs its own release
  note: a persisted child that is replaced will now be DELETEd where previously its row survived.
  The old behavior is not defensible — it is an orphaned row nobody asked for — but consumers who
  worked around it by replacing rather than removing will see new DELETEs.
- New (never-persisted) displaced items must **not** be queued for deletion; they are discarded,
  matching `RemoveItem`'s `if (!item.IsNew)` rule.
- `RemoveItem` and `InsertItem` behavior is unchanged.
- Existing tests are sacred, including the ISNEW-004 pin on the `IsNew` marking.

## Current State

Walked 2026-08-21. `EntityListBase.SetItem` (`src/Neatoo/EntityListBase.cs:361-410`):

- Live branch: captures `oldWasModified`, then marks the **incoming** item modified if
  `item.IsNew` (the ISNEW-004 fix, carrying a comment that explicitly defers everything else to
  this plan).
- `base.SetItem(index, item)`.
- Live branch again: cache arithmetic on `_cachedChildrenModified` from `item.IsModified` and
  `oldWasModified`.
- The displaced item is never touched: no `MarkDeleted`, no `DeletedList` entry, no
  `SetContainingList(null)`.
- The incoming item never receives `MarkAsChild()` or `SetContainingList(this)` on **either**
  branch — unlike `InsertItem`, which sets both on both branches after ISNEW-003.
- No duplicate / busy / aggregate-boundary guard on either branch.

## Steps

1. Give the displaced item the same disposition `RemoveItem` gives a removed one: mark deleted and
   queue for persistence deletion when it was persisted; discard it when it was new.
2. Give the incoming item the child identity `InsertItem` gives, on both branches.
3. Decide the guard question explicitly: whether `SetItem`'s live branch adopts `InsertItem`'s
   duplicate / busy / aggregate-boundary checks, or deliberately does not. Record the reasoning
   either way; an unasserted skip is what this todo exists to eliminate.
4. Announce correctly — a replacement that changes the list's `IsModified` must raise, consistent
   with LIST-003.
5. Verify by revert; full-suite run; write the release note for the behavior change.

## Acceptance

- Replacing a **persisted** child queues that child for deletion, and a subsequent local save
  issues the DELETE [integration]
- Replacing a **new** child discards it without queueing a deletion [unit]
- The incoming item receives `IsChild` and a `ContainingList` pointing at the list [unit]
- The displaced item's `ContainingList` no longer points at a list it is not in [unit]
- The guard decision from Step 3 is asserted in the direction chosen, with a stated reason [unit]
- A replacement that dirties the list announces `IsModified` [unit]
- The ISNEW-004 `IsNew` marking still holds [unit]
- Full solution suite green; release note written [unit]
