# EntityListBase State Machinery — Notification, Replacement, and Paused-Path Defects

**ID:** LIST (assigned 2026-08-21; unique across active and completed todos; registered in `docs/todos/_ids.md`. Plans referenced as `LIST-NNN`.)
**Type:** Bug
**Status:** In Progress
**Priority:** Medium
**Created:** 2026-08-21
**Last Updated:** 2026-08-21

**Sibling of:** [ISNEW](../ISNEW-decouple-isnew-from-ismodified/todo.md) — every item here was
found by an ISNEW gate but does not advance that todo's Goal, so it was carved out at close
rather than dropped or left hanging in a completed container.

---

## Goal

Close the defects in `EntityListBase` / `ValidateListBase` state machinery that the ISNEW arc
surfaced but deliberately did not fix: a stale meta-state baseline that swallows the user's
next edit notification after a save carrying deletions, a `SetItem` that silently orphans the
row it replaces, a paused `Delete()` that drops a child, and a set of guards and notifications
that are load-bearing but asserted nowhere. Two of these are user-visible bugs (items 1 and 5
in the deferred table below); the rest are holes that let a future change reopen a fixed
defect with a green suite.

## Acceptance Criteria

- [x] After a save that carried child deletions, editing a child again makes the aggregate
      savable — on a local/fat-client save, not only a remote one — LIST-003, pinned at both
      tiers; the new `LocalSaveLifecycle` fixture is the local save path the suite lacked
- [x] Replacing a persisted child in a list results in that child's row being deleted, not
      silently orphaned — LIST-002, pinned at both tiers. Breaking; released in 0.32.0. Two
      pre-existing tests asserted the opposite and were updated by user decision (plan review
      Veto 1), keeping their original cache-recalculation intent
- [x] `Delete()` on a child inside a paused window either routes correctly or is prevented
      — LIST-004: records the deletion in place rather than delegating into a paused
      `RemoveItem` that would discard it
- [x] The list's `IsModified` transitions raise `PropertyChanged` so bindings and parents see them
      — LIST-003; the standing NOTE claiming they do not was **disproven** by the control test
      and removed, replaced by the four tests it had deferred
- [x] The pause-guard asymmetry in `HandlePropertyChanged` is asserted, so adding a guard
      "for symmetry" fails a test instead of silently reopening the ISNEW-003 defect
      — LIST-003, `HandlePropertyChanged_MetaCheckIsNotPauseGuarded`
- [x] `FactoryComplete` on a never-paused list is either correct or explicitly dispositioned
      — LIST-001: correct for lists (unreachable canonically), pinned by
      `FactoryComplete_WhenNeverPaused_LeavesLiveMaintainedStateIntact`. The reachable
      entity-side variant is deferred to FABLE-001.
- [x] The remaining test-infrastructure debt inherited from ISNEW is closed or accepted
      — LIST-005; one of the four (`NoFactoryMethod`) was already covered, so the finding
      was recorded as stale rather than duplicated

## Out of Scope

- `IsNew`/`IsModified` semantics — settled by ISNEW and not reopened here
- The canonical aggregate lifecycle patterns in Design.Domain — settled by ISNEW-001/007

---

## Plan Index

| # | File | Title | Status |
|---|------|-------|--------|
| 001 | [001-resume-guard-never-paused-list](./plans/001-resume-guard-never-paused-list.md) | `ResumeAllActions` guard makes `FactoryComplete` a no-op on a never-paused list | Done (no library change) |
| 002 | [002-setitem-replaced-item](./plans/002-setitem-replaced-item.md) | SetItem: identity + replaced-item persistence | Done |
| 003 | [003-metastate-baseline-and-notification](./plans/003-metastate-baseline-and-notification.md) | Stale meta-state baseline after a save carrying deletions; `IsModified` notification coverage | Done |
| 004 | [004-paused-path-guards](./plans/004-paused-path-guards.md) | Paused `Delete()` seam; paused `InsertItem` guard dispositioning | Done |
| 005 | [005-inherited-test-debt](./plans/005-inherited-test-debt.md) | Test-infrastructure debt inherited from ISNEW | Done |

*Plans 001 and 002 were written as ISNEW-008 and ISNEW-009 and carry their original provenance
lines. ISNEW-009 → LIST-002 is a clean one-to-one; **ISNEW-008 is not** — it was re-split at
LIST-001's own Step 2 into 001, 003, 004 and 005, so ISNEW's tombstone for it points at all four
rather than at a single successor. See the Discovery Log entry below for what moved where.*

---

## Discovery Log

### 2026-08-21 — LIST-002 code review (run retroactively): silent data loss via the missing re-add step

- **Finding (veto-tier, and correct):** `InsertItem`'s live branch has a re-add / intra-aggregate-move
  step — `RemoveFromDeletedList` on the incoming item's old list, then `UnDelete()` if it was
  flagged (`EntityListBase.cs:268-277`). **`SetItem`'s live branch had no equivalent.** So
  `list[i] = item` where `item` was sitting in `this.DeletedList` left it swapped into the live
  slot, still queued, still `IsDeleted`. The canonical `[Update]` loop filters
  `this.Union(DeletedList)` on `IsDeleted`, so **the next save would DELETE a row the collection
  shows as live** — silent data loss.
- **Why nothing caught it:** none of LIST-002's eight tests, and no Acceptance bullet, covered
  "replace with an item currently awaiting deletion." It was literally question 5 in the review
  brief and the plan had never answered it.
- **Verified before acting:** the new test was written first and failed on `a.IsDeleted`.
- **Decision — fixed.** `SetItem`'s live branch now mirrors `InsertItem`, placed **before** the
  displaced item is queued so resurrecting the incoming item and queueing the outgoing one do not
  interfere. Reverting just that step fails exactly the one new test.
- **Finding (callout, carried):** because `oldWasModified` is captured *before* `MarkDeleted()`,
  replacing an unmodified-persisted item with another can leave `_cachedChildrenModified` `true`
  from the transient flip — contradicting its own doc comment. Not reachable as a wrong *public*
  `IsModified` today, because `DeletedList.Any()` masks it until `FactoryComplete(Update)`
  recalculates; the one unruled-out path is moving the item to a different list in the same
  aggregate while still queued here, since `RemoveFromDeletedList` neither recalculates nor
  notifies. Carried to whatever eventually reorders `RemoveItem`/`SetItem` together.
- **Finding (unrelated, worth knowing):** `ValidateBaseAsyncTests.ValidateBaseAsync_Child_IsBusy`
  is **flaky** — it failed once in a full run, then passed 3/3 in isolation and on a full re-run. It
  asserts an `IsBusy` `PropertyChanged` arrived, which is a timing race, and it touches no list
  code. Consistent with the `AsyncTasks` completion race FableFeedback already records
  (`AsyncTasks.cs:141-159`, `SetResult` outside the lock). Not caused by this arc; recorded so it
  is not mistaken for a regression later.
- **Follow-up:** FABLE-001 (the `AsyncTasks` race); the `_cachedChildrenModified` nuance needs an
  owner alongside the `RemoveItem`/`SetItem` reordering

### 2026-08-21 — LIST-004 code review (run retroactively): the fix was half a fix

- **Finding (veto-tier, and correct):** LIST-004's "mark in place" solution fixed the silent
  discard but **never rejoined the framework's cleanup contract**. The child was marked
  `IsDeleted` and left a live member of the list, never queued in `DeletedList`. But
  `FactoryComplete(Update)`'s cleanup iterates `DeletedList`, so it never touched the child;
  `EntityBase.IsSelfModified` includes `|| IsDeleted` (`EntityBase.cs:187`), so the child was
  `IsModified` **forever**; and `ResumeAllActions` recalculates
  `_cachedChildrenModified = this.Any(c => c.IsModified)`, which kept finding it. Net: a
  **freshly fetched aggregate reporting unsaved work that does not exist**, and a canonical
  `[Update]` loop **re-issuing the DELETE on every subsequent save**.
- **Why nothing caught it:** no test in the diff called `FactoryComplete(Update)` after a paused
  delete. The three `Delete_When...` tests exercised `Fetch` or never completed at all. The plan
  asked "does the DELETE fire?" and never asked "and then what?".
- **Verified before acting:** the missing test was written first and failed exactly as predicted,
  on `list.Contains(doomed)`.
- **Decision — fixed, not accepted.** `IEntityListBaseInternal.IsPaused` is **removed** (it existed
  only so `Delete()` could peek at pause state) and replaced by `DeleteChild(IEntityBase)`, which
  puts the decision on the list: mark and queue while paused, defer to `RemoveItem` while live, and
  **always remove**, so the existing cleanup drains it. `RemoveItem`'s paused branch is still
  untouched. Reverting the corrected fix fails exactly two tests — the recording test and the new
  round-trip test — with all controls passing.
- **Finding (callout, carried):** `SetItem`'s paused branch confers `ContainingList` on the
  incoming item but never clears it on the **displaced** one, so that item keeps a reference to a
  list it is not in; a later `Delete()` on it would record a deletion with no persistence
  consequence. The reviewer confirmed this failure mode already existed identically on the live
  path before this arc (`Collection<T>.Remove` on an absent item is a no-op), so it is a
  pre-existing `SetItem` gap. Carried forward, not fixed here.
- **Follow-up:** the displaced-item `ContainingList` gap — needs an owner if it is ever to be closed

### 2026-08-21 — Close-out audit: a missed gate, a dangling link, and a second ordering characteristic

- **Finding (process failure, V1):** three plans declared `Code-review opt-in: Yes` — 001, 002 and
  004 — and **only LIST-003's code review actually ran.** No `001/002/004-code-review.md` existed,
  and no Skipped Steps entry recorded the gap. The gate was declared and then silently skipped,
  which is precisely the drift these gates exist to catch. The audit's argument for why it matters
  is concrete: LIST-003's code review earned its keep by hand-tracing a notification re-entrancy
  path and finding an undocumented ordering dependency, and LIST-002 (breaking, save-side) turned
  out to have a directly analogous one nobody had recorded.
- **Decision:** run the two missing reviews that touched save-side library code — LIST-002 and
  LIST-004 — before merge. **LIST-001's is recorded as an accepted skip:** it changed no production
  code at all (its own Outcome and the audit both confirm "no-library-change plan"), so a code
  review of a test-only diff would be ceremony. Recorded here rather than left implicit.
- **Finding (ordering, C1 — inherited, not new):** in `SetItem`, `MarkDeleted()` on the displaced
  item fires **before** `base.SetItem`, which is where `ValidateListBase.SetItem` unsubscribes that
  item's handlers. So the mark can raise a list-level `IsModified` notification mid-mutation —
  before the slot is swapped and before `DeletedList.Add` runs — during which a synchronous
  consumer would see the old item still in the collection and `DeletedList` still empty despite
  `IsDeleted == true`. The audit traced the end state as correct on every branch, and the same
  `MarkDeleted`-before-`base` shape already exists unremarked in `RemoveItem`, so this is an
  inherited characteristic rather than a regression LIST-002 introduced.
- **Decision:** recorded, not changed — the arc's own standard (see the LIST-003 entry below) is to
  write down ordering dependencies rather than leave them implicit. Changing it would mean
  reordering `RemoveItem` too, which is out of scope here.
- **Finding (V2, cross-container):** ISNEW's completed container had a Retired tombstone linking to
  `plans/001-metastate-baseline-and-paused-delete.md`, the stub this arc deleted during LIST-001's
  re-split. **Fixed:** the tombstone now points at all four successors, and LIST's own Index note
  no longer claims a clean one-to-one for ISNEW-008.
- **Finding (C2):** LIST-005's Outcome flagged that entity-property assignment confers no child
  identity, but only in prose — no Index row, no Discovery Log entry, no sibling scope. An untraced
  deferral is work evaporating. **Fixed:** added to FABLE-001's scope alongside the
  `PauseAllActions` finding it sits next to in spirit.
- **Follow-up:** FABLE-001

### 2026-08-21 — LIST-003 gates: an undocumented ordering dependency, traced

- **Finding (from the LIST-003 code review, traced not assumed):** the list-level `IsModified`
  announcement LIST-003 added **does** reach a parent during a nested aggregate save — harmlessly,
  but by a route worth recording. The **sync** `PropertyChanged` path is blocked: it bubbles to
  `PropertyManager.Property_PropertyChanged`, which early-returns on `if (this.IsPaused)`
  (`Internal/ValidatePropertyManager.cs:147`) — gated on the *parent's* `PropertyManager.IsPaused`,
  still true for the whole nested `Update`. The **async** `NeatooPropertyChanged` path is *not*
  gated that way (`:82-85` forwards unconditionally) and lands in
  `ValidateBase.ChildNeatooPropertyChanged` (`ValidateBase.cs:381-397`); because the parent is
  paused it takes the `else` branch — a bare `ResetMetaState()`, not a re-entrant `FactoryComplete`
  — and that snapshot is overwritten moments later by the parent's own `FactoryComplete(Update)`.
  Net: no lost notification, no double-fire, no unbounded recursion.
- **Why it matters:** this is safe **only** because of the pause → resume → `MarkUnmodified`
  ordering in `EntityBase.FactoryComplete`. LIST-003 depends on that ordering and did not say so.
  Anyone changing it — or doing further paused-path work — must re-check this interaction.
- **Decision:** recorded rather than fixed; nothing is wrong today. Both LIST-003 gates returned
  **no must-cover and no veto-tier findings**. Their two lesser findings were adopted: a
  `FactoryComplete_Create_AnnouncesNothing` test (the Acceptance bullet named `Create` but only
  `Fetch` had a test, and "structurally unreachable" is a property of the current guard, not of
  the design), and a `DeletedCount == 0` assertion for symmetry with the control.
- **Correction to the record:** `reviews/003-revert-unit.log` (`Total: 1838`) and
  `003-test.log` (`Total: 1840`) differ by exactly the two `LocalSaveLifecycleTests`, because the
  unit revert ran **before** the integration fixture existed and the integration revert ran after,
  filtered. The claim of one unit + one integration failure holds — both logs name the failing
  tests and their assertion messages — but the two runs were at different tree states, which the
  Test Evidence row now says outright.
- **Follow-up:** LIST-004 (paused-path work), and any future change to `EntityBase.FactoryComplete`

### 2026-08-21 — LIST-001 findings: coverage premise corrected; entity-side pause-scope defect (out of scope)

- **Finding (premise corrected):** the ISNEW-003 test review's "false coverage" concern was
  **overstated**, and the revert run proved it. Reverting `ValidateListBase.FactoryComplete` to
  `IsPaused = false` fails **three** pre-existing tests, covering all three meta properties:
  `EntityListBaseTests.FactoryComplete_AfterPausedAddOfModifiedItem_ListReportsModified`, and
  `EntityListBaseStateTransitionTests.FactoryComplete_AfterPausedAddOfInvalidItem_ListReportsInvalid`
  / `...AddOfBusyItem_ListReportsBusy`. The fix was well pinned. What is true is the narrower
  claim: many tests drive `FactoryComplete` with no `FactoryStart` — but those exercise
  `EntityListBase.FactoryComplete(Update)`'s *direct* recalculation, which is legitimate
  coverage of a different path, not a hole.
- **Decision:** LIST-001's Step 4 shrinks accordingly (see the plan's Amendments). One test was
  still worth adding at the `ValidateListBase` tier, where the broken code actually lives —
  all three pre-existing pins go through `EntityListBase`.
- **Finding (guard disposition):** the never-paused `FactoryComplete` no-op is **correct for
  lists**. Lists expose no `PauseAllActions()`, `IsPaused` does not cascade from a parent, and
  generated factories always emit `FactoryStart`/`FactoryComplete` as a pair in one try block —
  so the case is unreachable from a canonical list flow. On a never-paused list the live
  `InsertItem` branch and the unguarded `CheckIfMetaPropertiesChanged` have already maintained
  caches and baseline. Pinned by
  `ValidateListBaseTests.FactoryComplete_WhenNeverPaused_LeavesLiveMaintainedStateIntact`.
- **Finding (OUT OF SCOPE — entity side, verified):** the same guard **is** reachable on
  `ValidateBase`/`EntityBase`, because `PauseAllActions()` is **not re-entrant**
  (`ValidateBase.cs:780-789`): when the object is already paused it skips the pause but *still*
  returns a `Paused` disposable whose `Dispose()` calls `ResumeAllActions()` unconditionally
  (`:750-753`). So user code doing the documented `using (entity.PauseAllActions()) { ... }`
  batch update **inside a factory method** ends the factory's pause scope early; `FactoryComplete`
  then finds the object un-paused and its `ResumeAllActions()` is a no-op, skipping
  `PropertyManager.ResumeAllActions()` and `ResetMetaState()`. Verified with a throw-away probe:
  `IsPaused` was `False` immediately after the nested `using` block and stayed `False` through
  `FactoryComplete`. The same non-re-entrancy breaks two plain nested `using` blocks with no
  factory involved.
- **Decision:** not fixed here — it is entity-side, outside this todo's Goal of list state
  machinery, and the standing directive is to record out-of-scope discoveries rather than widen
  the plan. Added to FABLE-001's scope, which already owns core defects of this kind.
- **Follow-up:** FABLE-001

### 2026-08-21 — LIST-001 re-split at Step 2; FABLE overlap resolved

- **Finding (re-split):** LIST-001 as carved held two core defects plus six folded-in items —
  well past one deliverable. Its own stub already said the `ResumeAllActions` guard item "has
  standalone merit and should be handled first."
- **Finding (theme):** the standing NOTE in `EntityListBaseTests` (~line 820) claiming
  "`EntityListBase.IsModified` is computed (uses `Any()`) and does not raise `PropertyChanged`"
  is **stale**. `_cachedChildrenModified` is a cache, and `EntityListBase.CheckIfMetaPropertiesChanged`
  does `RaiseIfChanged(..., nameof(IsModified))`. The machinery exists; the open question is
  whether it *fires correctly* — which is the meta-state baseline defect. So the NOTE and the
  baseline bug are one theme, not two, and travel together into LIST-003.
- **Decision:** Re-split into 001 (resume guard, foundational), 003 (baseline + notification),
  004 (paused-path guards), 005 (inherited test debt). LIST-002 unchanged.
- **Finding (FABLE overlap):** FableFeedback.md:118 states the defect as
  "`ValidateListBase.FactoryComplete:575` sets `IsPaused = false` directly instead of
  `ResumeAllActions()`" — which **ISNEW-003 already fixed and shipped in 0.31.0**
  (`ValidateListBase.cs:579-582` now calls `ResumeAllActions()`). FABLE-001's scope line is
  therefore satisfied. What remains is the *sequel*: `ResumeAllActions`' `if (IsPaused)` guard
  (`ValidateListBase.cs:546`) makes the new call a no-op on a never-paused list.
- **Decision:** LIST-001 owns the sequel; FABLE-001's scope line struck with a pointer here so
  the work is not funded twice.
- **Index changes:** LIST-003, LIST-004, LIST-005 created. The stub
  `plans/001-metastate-baseline-and-paused-delete.md` was deleted under the workflow's
  stub-deletion rule — created this same working session, never got past its Scope paragraph,
  never implemented against — and replaced by `plans/001-resume-guard-never-paused-list.md`.
  Every item it held is now owned by 001, 003, 004, or 005; nothing was dropped.
- **Follow-up:** LIST-001

### 2026-08-21 — LIST (carve-out from ISNEW)
- **Finding:** ISNEW closed with two Draft plans in its container. They do not advance ISNEW's
  Goal (which is met and fully accepted), and the workflow requires every plan in a completing
  todo to be terminal.
- **Decision:** Re-split — carved both plans into this sibling todo, renumbered 008→001 and
  009→002; ISNEW's Index rows are Retired tombstones pointing here.
- **Index changes:** LIST-001 and LIST-002 created from ISNEW-008 and ISNEW-009.
- **Follow-up:** LIST-001

---

## Skipped Steps

- Step 1 reconnaissance — unnecessary; every item arrived with a gate's diagnosis, file
  citation, and reachability analysis attached (see the ISNEW review records).
- **LIST-001's code review — skipped deliberately, recorded late.** The plan declared
  `Code-review opt-in: Yes` when it was expected to touch lifecycle machinery on the resume path.
  It ended up changing **no production code at all** (the guard was dispositioned correct; the diff
  is two tests plus markdown), so the opt-in no longer had a subject. Recorded here because the
  close-out audit correctly flagged that it — along with LIST-002's and LIST-004's — had gone
  missing with no note. Those two were run retroactively before merge; this one was not, on the
  reasoning above.

---

## Sibling Todos

- [ISNEW — Decouple IsNew from IsModified](../ISNEW-decouple-isnew-from-ismodified/todo.md) —
  the arc that surfaced all of this work. Its `reviews/003-code-review.md`,
  `reviews/003-test-review.md`, and `reviews/004-test-review.md` hold the original findings.

---

## Close-Out Audit

_Not yet run._

## Docs & Retro

_Filled at Step 8._

## Results / Conclusions

_Filled at Step 8._
