# LIST — Close-Out Audit (Step 7)

**Date:** 2026-08-21
**Reviewer:** `code-reviewer` (close-out mode)
**Object:** the whole arc — 7 commits, 29 files, PR #83
**Verdict:** **CONCERNS** — two veto-tier findings, three callouts. Both vetoes were
process/container; the library diff traced clean.

---

## Acceptance criteria

All **7 traced and confirmed**, each against the actual test body rather than its name. The
auditor explicitly verified that LIST-001's self-downgraded Test Evidence row ("NOT pinned") is
*accurate rather than over-modest*, and that the removed `EntityListBaseTests` NOTE text is gone
repo-wide.

## V1 — three declared code-review gates never ran (process failure)

`plans/001`, `002` and `004` each declared `Code-review opt-in: Yes`. Only `003-code-review.md`
existed. No Skipped Steps entry recorded the gap. **The gate was declared and then silently
skipped** — exactly the drift these gates exist to catch.

The auditor's argument for why it mattered was concrete rather than procedural: LIST-003's code
review earned its keep by hand-tracing a notification re-entrancy path and finding an undocumented
ordering dependency — and LIST-002 turned out to carry a directly analogous one that nobody had
recorded (see C1), on a **breaking** save-side change.

**Disposition:**
- **LIST-002 and LIST-004 code reviews run retroactively before merge** — both changed save-side
  library code. Findings in `002-code-review.md` and `004-code-review.md`.
- **LIST-001's recorded as an accepted skip** in the todo's Skipped Steps, with the reason: it
  changed no production code at all, so the opt-in had no subject by the time the plan closed.

## V2 — dangling cross-container link (fixed)

`docs/todos/completed/ISNEW-.../todo.md:77` — the ISNEW-008 Retired tombstone pointed at
`plans/001-metastate-baseline-and-paused-delete.md`, the stub this arc deleted during LIST-001's
re-split. **Fixed:** the tombstone now names all four successors (LIST-001/003/004/005), and LIST's
own Plan Index note no longer claims a clean one-to-one for ISNEW-008 — because there isn't one.

## C1 — `MarkDeleted()` fires before the displaced item is unsubscribed (recorded)

In `SetItem`, `MarkDeleted()` runs before `base.SetItem`, which is where
`ValidateListBase.SetItem` unsubscribes the displaced item's handlers. The mark can therefore raise
a list-level `IsModified` notification **mid-mutation** — before the slot is swapped and before
`DeletedList.Add` runs — during which a synchronous consumer would observe the old item still in
the collection and `DeletedList` still empty, despite `IsDeleted == true`.

The auditor traced the end state as correct on every branch, and found the identical
`MarkDeleted`-before-`base` shape already present, unremarked, in `RemoveItem`. **Inherited
characteristic, not a regression LIST-002 introduced.**

**Disposition: recorded in the Discovery Log, not changed.** The arc's own standard is to write
ordering dependencies down (see the LIST-003 gate entry); changing this one would mean reordering
`RemoveItem` too, which is outside this todo.

## C2 — untraced deferral (fixed)

LIST-005's Outcome flagged that entity-property assignment confers no child identity — but in prose
only, with no Index row, Discovery Log entry, or sibling scope. Unlike the parallel
`PauseAllActions` finding, which was properly routed to FABLE-001. **Fixed:** added to FABLE-001's
scope, with the framing that this is the one attach channel ISNEW-003 and LIST-002 did not unify,
and that whether the asymmetry is a defect or a deliberate boundary is the open question.

## C3 — no canonical final logs (fixed)

The auditor had to infer the final state from the chronologically last per-plan logs. **Fixed:**
`reviews/final-build.log` and `reviews/final-test.log` now hold a single clean run of **both** test
projects — `src/Neatoo.sln` **1856 / 0 / 2 skipped** and `Design.Tests` **130 / 0** — so no future
audit has to sort by timestamp.

## Deferred work carrying forward

| # | Item | Where it lives now |
|---|---|---|
| 1 | `PauseAllActions()` is not re-entrant (entity side) | `FABLE-001` scope — verified present with accurate citations |
| 2 | LIST-003's safety depends on `EntityBase.FactoryComplete`'s pause→resume→`MarkUnmodified` ordering | LIST Discovery Log + `003-code-review.md` Callout 1 |
| 3 | `MarkDeleted`-before-unsubscribe ordering in `SetItem` **and** pre-existing `RemoveItem` | LIST Discovery Log (C1 above) |
| 4 | Entity-property assignment confers no child identity | `FABLE-001` scope (C2 above) |
| 5 | LIST-001's unrun code review | Accepted skip, recorded in Skipped Steps with reason (V1 above) |

## Read report

Read beyond the brief: all three list/entity library files in full (to trace C1 by hand rather than
trust the plan), ISNEW's completed `todo.md` (where V2 surfaced), FABLE-001 (to verify both
deferral citations), `v0.32.0.md` plus four sibling release notes for link-convention, and **every
cited test method's body** rather than just its existence. Named but unused: none.

**Calibration note:** the auditor observed that the actual work order (001 → 003 → 004 → 005 →
gates → 002) differs from plan numbering — not a finding, but worth knowing that plan numbers in
this container are not a chronology.
