# SetItem: Identity and Persistence for the Replaced Item

**Plan #:** 009
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-21
**Plan-review opt-in:** Yes (changes save-side behavior for existing consumers)
**Code-review opt-in:** Yes

---

## Scope

_Stub — Scope only; flesh out at Step 2. Carved out of ISNEW-004 by that plan's review
(`reviews/004-plan-review.md`, veto B2), which found the original Step 4 wording
("a swap behaves like a remove plus an attach") silently deciding a persistence question
design.md had explicitly left open — inside a plan whose Constraints open with "factory save
routing is untouched."_

Decide and implement what `EntityListBase.SetItem` does with the **displaced** item when a
list element is replaced (`list[0] = replacement`). Today `SetItem` performs cache arithmetic
only: relative to `InsertItem`'s live branch it does no duplicate check, busy check,
aggregate-boundary check, `UnDelete`, or child identity; relative to `RemoveItem` it drops
the displaced item without `MarkDeleted` and without queueing it in `DeletedList`. The
consequence is that replacing a persisted child **silently orphans its row** — no DELETE is
ever issued.

Making the displaced item behave like a removal is a new observable save-side behavior for
existing consumers (a row that used to survive starts being deleted), which is why it needs
its own plan, its own review, and its own release note rather than riding along with the
semantic flip. The decision should also cover whether `SetItem` picks up the live branch's
guards (duplicate / busy / aggregate boundary), since a swap currently bypasses all of them.

ISNEW-004 marks a **new** incoming item — that channel was a regression it introduced and
fixed (see `reviews/004-code-review.md` V2). Everything else about `SetItem` belongs to this
plan: the displaced item's disposition, the missing guards, and the incoming item's child
identity (`MarkAsChild`/`SetContainingList`), which is still absent on both branches.
