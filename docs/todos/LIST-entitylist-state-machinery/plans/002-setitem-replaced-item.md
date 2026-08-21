# SetItem: Identity and Persistence for the Replaced Item

**Plan #:** 002
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** Done
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
  worked around it by replacing rather than removing will see new DELETEs. **The release note must
  also cover whichever way Step 3's guard question lands**: adopting `InsertItem`'s
  aggregate-boundary guard would be a *second* save-side break (silent success → thrown exception),
  not covered by the DELETE-queueing framing alone.
- **RESOLVED (was blocking) — two sacred tests encoded the opposite expectation.** See
  `reviews/002-plan-review.md` Veto 1.
  `SetItem_ReplaceModifiedWithUnmodified_WhenOnlyModified_ListBecomesUnmodified`
  (`EntityListBaseTests.cs:1243-1265`) and `LargeList_SetItem_UpdatesCacheCorrectly` (`:1518-1556`)
  both replace a **persisted** item and assert the list ends up unmodified. Queueing the displaced
  item keeps `DeletedList.Any()` true, flipping both to failing. Escalated to the user per the
  global test-modification rule; **the user chose to proceed and update them.** See Amendment A1
  for the reasoning recorded in each test.
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
- The displaced item keeps its `ContainingList` until the save clears it, matching `RemoveItem`
  parity — it is **not** cleared immediately [unit], and **is** cleared by
  `FactoryComplete(Update)`'s existing `DeletedList` loop [integration]
- The guard decision from Step 3 is asserted in the direction chosen, with a stated reason [unit]
- A replacement that dirties the list announces `IsModified` [unit]
- The ISNEW-004 `IsNew` marking still holds [unit]
- Full solution suite green; release note written [unit]

---

## Plan Amendments

**A1 (Veto 1 resolved by the user — proceed).** The plan review found two passing tests asserting
the opposite of this plan's premise: `SetItem_ReplaceModifiedWithUnmodified_WhenOnlyModified_ListBecomesUnmodified`
and `LargeList_SetItem_UpdatesCacheCorrectly` both replace a **persisted** child and assert
`list.IsModified == false`. Escalated per the global test-modification rule. **The user chose to
proceed and update both tests.**

The reasoning, recorded in both tests: their assertion was only true because the displaced row was
silently orphaned. With a real pending deletion, a list reporting "not modified" is lying, and its
aggregate would refuse to save work that needs saving. The assertions were characterizing the
defect, not stating a contract. Both tests keep their original intent — *the children-modified
cache recalculates correctly* — now asserted directly via `list.All(i => !i.IsModified)`, plus the
new `DeletedList` term stated explicitly. Neither test lost an assertion; both gained two.

**A2 (Veto 2 fixed at draft).** Step 1 said "same disposition `RemoveItem` gives", but an
Acceptance bullet demanded the displaced item's `ContainingList` be cleared immediately —
unreachable under `RemoveItem` parity, which keeps it set until `FactoryComplete(Update)`. The
bullet was corrected before implementation.

**A3 (Step 3 guard decision — adopt them).** `SetItem`'s live branch now enforces `InsertItem`'s
duplicate, busy, and aggregate-boundary guards. A replacement is an add in every sense that
matters, and all three were violations `SetItem` simply did not check. The paused branch enforces
none, consistent with `InsertItem` and LIST-004. Release note extended to cover this second
behavior break, per the review's callout 3.

**A4 (self-replacement).** `list[i] = list[i]` is a no-op, not a removal — guarded with
`ReferenceEquals`, or the still-present item would be marked deleted and queued.

## Test Evidence

| Acceptance bullet | Test | Tier | Status |
|---|---|---|---|
| Replacing a persisted child queues it; a local save issues the DELETE | `EntityListBaseTests.SetItem_ReplacingPersistedChild_QueuesItForDeletion`; `LocalSaveLifecycleTests.ReplacingAPersistedChild_ThenSave_IssuesTheDelete` | unit + integration | Pinned — both fail on revert |
| Replacing a new child discards it without queueing | `SetItem_ReplacingNewChild_DiscardsItWithoutQueueingDeletion` | unit | Pinned |
| Incoming item receives `IsChild` and a working `ContainingList` | `SetItem_IncomingItem_ReceivesChildIdentity` — proves `ContainingList` via `Delete()` routing rather than an internal accessor | unit | Pinned |
| Displaced item keeps `ContainingList` until the save clears it | `RemoveItem` parity, unchanged by this plan; clearing is `FactoryComplete(Update)`'s existing `DeletedList` loop, covered by `ReplacingAPersistedChild_ThenSave_IssuesTheDelete` | integration | Pinned |
| Step 3's guard decision asserted | `SetItem_WhenLive_EnforcesTheSameGuardsAsAdd` (duplicate, busy); `RootPropertyTests.SetItem_ItemFromDifferentAggregate_Throws` (boundary — needs a fixture with real Roots, which `TestEntityList` has no parent to provide) | unit | Pinned |
| A replacement that dirties the list announces `IsModified` | `SetItem_ReplacingPersistedChild_AnnouncesIsModified` | unit | Pinned — fails on revert |
| The ISNEW-004 `IsNew` marking still holds | Pre-existing `SetItem_ReplaceUnmodifiedWithModified_ListBecomesModified` and the ISNEW-004 pin, both unmodified and passing | unit | Verified unmodified |
| Suite green; release note written | `src/Neatoo.sln` 1856 / 0 / 2 skipped; `Design.Tests` 130 / 0; `docs/release-notes/v0.32.0.md` | unit | Pinned |
| Paused branch queues nothing but still confers identity | `SetItem_WhenPaused_QueuesNothing` | unit | Pinned |
| Self-replacement is a no-op | `SetItem_ReplacingWithItself_IsANoOp` | unit | Pinned |

## Outcome

`SetItem` now composes `RemoveItem` and `InsertItem` rather than implementing neither. Eight new
tests, two existing tests updated with their reasoning recorded in-file, and a release note
covering both behavior breaks.

Revert verification was exact: gating out only the displaced-item disposition fails **five** tests
— the two updated ones and the three that depend on queueing — while identity, self-replacement,
paused-branch, and guard tests all keep passing, which is what makes them controls rather than
co-dependents.

Two process notes. The first revert attempt used `if (false)`, which fails the build under
`TreatWarningsAsErrors` via `CS0162`; `--no-build` then ran a **stale binary** and reported a green
suite. That is the third time in this arc a revert attempt produced a misleading result on the
first try — twice from an over-broad `sed`, once from a non-compiling revert. A revert must compile
and the build must be checked before trusting the run.

Second: the plan review found Veto 1 by reading `EntityListBaseTests.cs`, which the brief never
named. When a plan proposes a behavior change, the tests covering the changed method are where the
counter-evidence lives and should be a named code target.

