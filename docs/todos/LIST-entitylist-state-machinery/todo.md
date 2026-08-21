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

- [ ] After a save that carried child deletions, editing a child again makes the aggregate
      savable — on a local/fat-client save, not only a remote one
- [ ] Replacing a persisted child in a list results in that child's row being deleted, not
      silently orphaned
- [ ] `Delete()` on a child inside a paused window either routes correctly or is prevented
- [ ] The list's `IsModified` transitions raise `PropertyChanged` so bindings and parents see them
- [ ] The pause-guard asymmetry in `HandlePropertyChanged` is asserted, so adding a guard
      "for symmetry" fails a test instead of silently reopening the ISNEW-003 defect
- [x] `FactoryComplete` on a never-paused list is either correct or explicitly dispositioned
      — LIST-001: correct for lists (unreachable canonically), pinned by
      `FactoryComplete_WhenNeverPaused_LeavesLiveMaintainedStateIntact`. The reachable
      entity-side variant is deferred to FABLE-001.
- [ ] The remaining test-infrastructure debt inherited from ISNEW is closed or accepted

## Out of Scope

- `IsNew`/`IsModified` semantics — settled by ISNEW and not reopened here
- The canonical aggregate lifecycle patterns in Design.Domain — settled by ISNEW-001/007

---

## Plan Index

| # | File | Title | Status |
|---|------|-------|--------|
| 001 | [001-resume-guard-never-paused-list](./plans/001-resume-guard-never-paused-list.md) | `ResumeAllActions` guard makes `FactoryComplete` a no-op on a never-paused list | Done (no library change) |
| 002 | [002-setitem-replaced-item](./plans/002-setitem-replaced-item.md) | SetItem: identity + replaced-item persistence | Draft (stub) |
| 003 | [003-metastate-baseline-and-notification](./plans/003-metastate-baseline-and-notification.md) | Stale meta-state baseline after a save carrying deletions; `IsModified` notification coverage | Draft (stub) |
| 004 | [004-paused-path-guards](./plans/004-paused-path-guards.md) | Paused `Delete()` seam; paused `InsertItem` guard dispositioning | Draft (stub) |
| 005 | [005-inherited-test-debt](./plans/005-inherited-test-debt.md) | Test-infrastructure debt inherited from ISNEW | Draft (stub) |

*Plans 001 and 002 were written as ISNEW-008 and ISNEW-009 and carry their original provenance
lines; cross-references to those IDs in ISNEW's records point here. Plan 001 was re-split at
its own Step 2 — see the Discovery Log entry below for what moved where.*

---

## Discovery Log

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
