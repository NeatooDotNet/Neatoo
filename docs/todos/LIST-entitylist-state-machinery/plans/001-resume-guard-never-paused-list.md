# The `ResumeAllActions` Guard and the Never-Paused List

**Plan #:** 001
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md) (carved from ISNEW-008; re-split at Step 2)
**Status:** Done
**Last Updated:** 2026-08-21
**Plan-review opt-in:** No — the seam and its reachability are already diagnosed by the ISNEW-003
test review; this plan's risk is *false coverage*, which the Step 5 test gate is the right tool for.
**Code-review opt-in:** Yes — touches lifecycle machinery on the resume path.

---

## Scope

`FactoryComplete` routes through `ResumeAllActions()`, whose entire body sits behind
`if (IsPaused)`. On a list that was never paused, `FactoryComplete` therefore does nothing at
all — no cache recalculation, no meta-state baseline reset. This plan determines whether that
no-op is correct or a live hole, dispositions it explicitly, and closes the false coverage that
currently hides the question: a large number of unit tests drive `FactoryComplete` with no
preceding `FactoryStart`, so they never enter the guarded body and never exercise the fix
ISNEW-003 shipped.

This plan does **not** touch the meta-state baseline defect after a save carrying deletions
(LIST-003), the paused `Delete()` seam (LIST-004), or `SetItem` (LIST-002). It does not change
what `FactoryComplete(Update)` does with `DeletedList`.

## Intent

After this plan, a list that completes a factory operation has correct cached state and a
correct meta-state baseline, and it does not matter to the caller whether the list was paused
on the way in. Whichever way the guard is dispositioned, the suite proves the ISNEW-003
recalculation path actually runs — removing that fix must turn the suite red.

## Framework & Architectural Alignment

- The pause/resume lifecycle is the framework's mechanism for suppressing rules and
  notifications during factory operations; `FactoryStart`/`FactoryComplete` are its public
  bookends and fire on the single factory target only, with no graph cascade.
- `ResumeAllActions` is the sanctioned recalculation seam — ISNEW-003 deliberately routed
  `FactoryComplete` through it rather than duplicating recalculation logic. Any fix here
  preserves that single-seam property instead of re-scattering recalculation.
- Caches (`_cachedChildrenModified`, `_cachedIsValid`, `_cachedIsBusy`) are maintained
  incrementally by the live `HandlePropertyChanged` path and repaired wholesale on resume.
  The guard question is exactly: is the live path's maintenance sufficient when no pause
  ever happened?

## Constraints & Invariants

- `IsNew`/`IsModified` semantics settled by ISNEW are not reopened.
- Existing tests are sacred. The tests this plan repairs are ones whose *stated intent* is to
  cover the recalculation path; repairing them must preserve that intent, not weaken it. Any
  test whose intent turns out to be "characterize the never-paused no-op" is left alone and
  cited as the disposition.
- `FactoryComplete(Update)`'s `DeletedList` handling stays as-is — it is LIST-003's subject.
- Resume must remain silent: repairing a baseline on resume announces nothing, because the
  post-factory state *is* the new baseline. Raising notifications from the resume path is out
  of scope and would contradict the Fetch lifecycle.

## Current State

Walked 2026-08-21 against the branch point (`main` @ `18ef4c4`, Neatoo 0.31.0).

- `ValidateListBase.ResumeAllActions` (`src/Neatoo/ValidateListBase.cs:544-556`) — whole body
  behind `if (this.IsPaused)`: sets `IsPaused = false`, recalculates `_cachedIsValid` and
  `_cachedIsBusy`, then `ResetMetaState()`.
- `ValidateListBase.FactoryComplete` (`:579-582`) — now `this.ResumeAllActions();`. This *is*
  ISNEW-003's shipped fix and satisfies FableFeedback.md:118.
- `EntityListBase.ResumeAllActions` (`src/Neatoo/EntityListBase.cs:467-476`) — recalculates
  `_cachedChildrenModified` inside its own `if (this.IsPaused)`, then calls base. Ordering is
  correct: the entity-level recalculation lands before base's `ResetMetaState()` snapshots it.
- `EntityListBase.FactoryComplete` (`:444-461`) — calls base first, then on `Update` clears
  `DeletedList` and recalculates `_cachedChildrenModified` **directly**, outside the resume path.
- `ValidateBase.ResumeAllActions` (`src/Neatoo/ValidateBase.cs:798-806`) — same guard shape on
  the entity side; noted for symmetry, not necessarily in scope.
- False coverage, confirmed: in `src/Neatoo.UnitTest/Unit/Core/EntityListBaseTests.cs`,
  `FactoryComplete` is driven with no preceding `FactoryStart` at lines 519, 536, 553, 790,
  858, 956, 1169, 1285. Only the ISNEW-003 regression test (357/361) pairs them.
  `FactoryComplete_Update_RecalculatesCache` (`:938`) is the sharpest case — it is named for
  the recalculation yet passes via the *direct* recalculation at `EntityListBase.cs:459`,
  never entering `ResumeAllActions`' guarded body.

## Steps

1. Establish reachability: determine whether a list can receive `FactoryComplete` without a
   preceding `FactoryStart` in any canonical flow — generated factories, nested/child list
   population, and the deserialization path. Record the answer in the Discovery Log; it decides
   step 2.
2. Disposition the guard. If canonical flows always pause first, the no-op is correct and gets
   an explicit characterization test naming *why*. If any canonical flow reaches
   `FactoryComplete` unpaused, make completion recalculate regardless of pause state, keeping
   `ResumeAllActions` the single recalculation seam.
3. Decide the same question for `ValidateBase.ResumeAllActions`' identical guard — fix or
   explicitly defer with a reason; do not leave it unexamined.
4. Repair the false coverage: make the tests whose intent is the recalculation path drive a
   real `FactoryStart`/`FactoryComplete` pair, so they enter the body they claim to cover.
5. Prove the coverage is real: revert ISNEW-003's `ResumeAllActions()` call in
   `ValidateListBase.FactoryComplete` against a *compiling* tree and confirm the suite goes red;
   restore, and record which tests failed.
6. Full-suite run; no pre-existing test loses its original intent.

## Acceptance

- A list paused via `FactoryStart`, mutated while paused, then completed via `FactoryComplete`
  reports correct `IsValid`, `IsBusy`, and `IsModified` [unit]
- The never-paused `FactoryComplete` case has an explicit assertion of the dispositioned
  behavior, whose comment states the reason rather than merely restating the code [unit]
- Reverting `ValidateListBase.FactoryComplete`'s call to `ResumeAllActions()` fails at least one
  test that is *not* the ISNEW-003 regression test — proving coverage broadened beyond the
  single pinning test [unit]
- `ValidateBase.ResumeAllActions`' guard is either covered by an equivalent assertion or carries
  a recorded deferral with a reason [explicit-skip: recorded in the plan Outcome if deferred]
- Full solution suite green, with no existing test's intent weakened [unit]

---

## Plan Amendments

**A1 (Step 1-2, disposition decided).** The guard is **correct** and the never-paused
`FactoryComplete` no-op is safe for lists. Reachability came back negative on three independent
legs: `ValidateListBase`/`EntityListBase` expose no `PauseAllActions()` of their own, so
`FactoryStart` is a list's only pause channel; `IsPaused` is a plain per-object flag that does not
cascade from a parent; and the generated factories always emit `FactoryStart` and
`FactoryComplete` as a pair inside one try block (verified in `InvoiceLineListFactory.g.cs`, 4
occurrences of each). On a never-paused list the un-paused `InsertItem` branch
(`ValidateListBase.cs:146`) and the **unguarded** `CheckIfMetaPropertiesChanged()` that closes
`HandlePropertyChanged` (`:407`) have already maintained both the caches and the baseline. No
library change was needed; the disposition is pinned by a test instead.

**A2 (Step 4-5, premise corrected — the plan was wrong).** Step 4 assumed ISNEW-003's fix was
uncovered because so many tests call `FactoryComplete` without `FactoryStart`. The revert run
disproved it: reverting `ValidateListBase.FactoryComplete` to `IsPaused = false` fails **three**
pre-existing tests spanning all three meta properties (modified, invalid, busy) — see the todo
Discovery Log for names. The unpaired tests are not false coverage; they exercise
`EntityListBase.FactoryComplete(Update)`'s *direct* recalculation, a different and legitimate
path. **Step 4 therefore did not repair any existing test** — doing so would have been gutting
tests to fit a mistaken premise. One test was still worth adding, at the `ValidateListBase` tier:
all three existing pins run through `EntityListBase`, leaving the base class that actually
contains the fixed code without direct coverage.

**A3 (Step 3, entity-side guard — deferred, not skipped).** The identical guard **is** reachable
on `ValidateBase`/`EntityBase` because `PauseAllActions()` is not re-entrant. Verified by probe;
full mechanism and evidence in the todo Discovery Log. Deferred out of LIST as entity-side work
and added to FABLE-001's scope with provenance, per the standing directive to record
out-of-scope discoveries rather than widen the plan.

## Test Evidence

| Acceptance bullet | Test | Tier | Status |
|---|---|---|---|
| Paused list, mutated while paused, completes with correct `IsValid`/`IsBusy`/`IsModified` | `ValidateListBaseTests.FactoryComplete_AfterPausedAddOfInvalidItem_ValidateListReportsInvalid` (new); pre-existing `EntityListBaseStateTransitionTests.FactoryComplete_AfterPausedAddOfInvalidItem_ListReportsInvalid` / `...AddOfBusyItem_ListReportsBusy`; `EntityListBaseTests.FactoryComplete_AfterPausedAddOfModifiedItem_ListReportsModified` | unit | Pinned |
| Never-paused `FactoryComplete` has an explicit assertion of the dispositioned behavior, with the reason | `ValidateListBaseTests.FactoryComplete_WhenNeverPaused_LeavesLiveMaintainedStateIntact` (new) | unit | **NOT pinned — documented, not regression-proof.** The test gate proved it would stay green if the guard were deleted outright: the recalculation the guard skips computes exactly the value live maintenance already produced, so guarded and unguarded agree by construction. It is a documentation anchor plus a corruption detector, and its comment now says so. The plan's Constraints explicitly sanctioned a characterization test here, so this is an accepted trade-off — but the original "Pinned" label overstated it |
| Reverting the fix fails a test that is *not* the ISNEW-003 regression test | Revert run 2026-08-21: 4 failures across 3 distinct test methods in 3 classes, only one of which is the ISNEW-003 pin | unit | Pinned |
| `ValidateBase.ResumeAllActions`' guard covered or deferred with a reason | Deferred — see A3 and FABLE-001 | explicit-skip | Deferred, traced |
| Full suite green, no existing test's intent weakened | Solution run 2026-08-21: 1832 passed / 0 failed / 2 skipped (`Neatoo.UnitTest`), plus Samples 254, BaseGenerator 42, Person.DomainModel 55. No existing test modified. | unit | Pinned |

## Outcome

Delivered as a **no-library-change plan**, which was the honest result rather than the expected
one. The guard the plan was written to fix turned out to be correct for lists, and the false
coverage it was written to close turned out not to exist. Two tests added (+2 net: 1830 → 1832);
zero existing tests modified, so no test lost its original intent.

The plan still earned its place three ways: it converted an unexamined guard into a
dispositioned one with the reasoning recorded at the seam; it added `ValidateListBase`-tier
coverage where the fixed code actually lives; and it surfaced a **verified, previously unknown
defect** — the non-re-entrant `PauseAllActions()` — now owned by FABLE-001.

A note for LIST-003, which inherits the sharpest thing found here:
`EntityListBaseTests.FactoryComplete_Update_RecalculatesCache` (`:938`) drives an Update
completion on a list it un-pauses by hand, asserts `IsModified` flips to false, and is sitting
directly on top of LIST-003's stale-baseline defect without seeing it — because it asserts the
*value* and never the *notification*. It is the closest existing test to that bug and the natural
place to start.

---

## Retirement / Carve-Out Note

Carved out of the ISNEW arc at its close-out (2026-08-21) as ISNEW-008, then re-split at this
plan's Step 2. The meta-state baseline defect, paused `Delete()`, and inherited test debt that
were folded into the original stub now live in LIST-003, LIST-004, and LIST-005 respectively.
ISNEW's `reviews/003-test-review.md` remains the authoritative source for the original finding.
