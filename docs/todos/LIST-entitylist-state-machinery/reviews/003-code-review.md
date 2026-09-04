# LIST-003 — Code Review (Step 5, per-plan, findings-only)

**Date:** 2026-08-21
**Reviewer:** `code-reviewer`
**Object:** commit `35e5d1b`
**Budget:** deep
**Outcome:** **No veto-tier findings.** Two callouts, both adopted as records rather than fixes.

---

## Direction

Sound. The change reuses the same seam `RemoveItem` already uses for the identical class of bug
(`EntityListBase.cs:346`), rather than introducing a new notification mechanism, and is correctly
scoped inside `if (factoryOperation == FactoryOperation.Update)`. Nothing to redirect.

## Callout 1 — the paused-parent re-entrancy path (traced, benign, now recorded)

The reviewer traced the re-entrancy question by hand instead of accepting the plan's framing, and
found that the new announcement **does** reach a parent mid-save — harmlessly, but for reasons this
plan depends on without having said so:

- The **sync** path is blocked. `PropertyChanged` bubbles list → `ValidateProperty.PassThruValuePropertyChanged`
  → `PropertyManager.Property_PropertyChanged`, which early-returns on
  `if (this.IsPaused)` (`Internal/ValidatePropertyManager.cs:147`) — gated on the *parent's*
  `PropertyManager.IsPaused`, still `true` for the whole nested `Update` call. The parent is never
  reached synchronously mid-save.
- The **async** `NeatooPropertyChanged` path is **not** gated the same way
  (`Internal/ValidatePropertyManager.cs:82-85` forwards unconditionally), landing in
  `ValidateBase.ChildNeatooPropertyChanged` (`ValidateBase.cs:381-397`). Because the parent is
  paused it takes the `else` branch — a bare `ResetMetaState()` on the parent, not a re-entrant
  call into `FactoryComplete`. That snapshot is overwritten moments later by the parent's own
  `FactoryComplete(Update)`. Net: no lost notification, no double-fire, no stack growth.

**This is safe only because of the specific pause → resume → `MarkUnmodified` ordering in
`EntityBase.FactoryComplete`**, which LIST-003 correctly left untouched but never recorded as a
dependency. **Disposition: adopted as a Discovery Log entry**, so whoever next changes that
ordering knows to re-check this interaction. Not a fix; the plan stands as Done.

## Callout 2 — revert-log provenance (informational, corrected)

`003-revert-unit.log` shows `Total: 1838` while `003-test.log` shows `Total: 1840` — a two-test
gap exactly equal to the two `LocalSaveLifecycleTests`. The reviewer's inference is **correct**:
the unit revert was run before the integration fixture existed, and the integration revert was run
afterwards against a filtered run (`Total: 2`). The load-bearing claim — one unit failure and one
integration failure, both the defect tests — is corroborated by the actual failing test names and
assertion messages in both logs, so Test Evidence honesty is intact. **Disposition: the plan's
Test Evidence row now states that the two revert runs happened at different tree states**, rather
than implying a single run.

## Other brief questions — verified, no findings

- **Timing / storms:** fires once per `FactoryComplete(Update)`, fixed O(1) comparisons, after the
  persistence work is already complete.
- **Silence invariant:** `grep` across `src/Neatoo`, `src/Design`, `src/Examples` found **no**
  subclass overriding `FactoryComplete` — `EntityListBase<I>` is the only implementation. Generated
  list factories only ever pass `Create`/`Fetch`/`Update` to a list; child Insert-vs-Update is
  routed *inside* the list's own `[Update]`, never as a list-level `FactoryOperation.Insert`. So
  Fetch/Create genuinely cannot reach the branch.
- **`IsSelfModified` always false-vs-false:** harmless, and not an unanticipated context — the same
  seam is already called from five other sites. Side benefit: reusing the whole seam also closes
  the same silent-baseline-rebase class for `IsValid`/`IsSelfValid`/`IsBusy` after Update.
- **Fixture quality:** interface-first confirmed (`ILocalOrder : IEntityRoot`,
  `ILocalOrderLine : IEntityBase`, `ILocalOrderLineList : IEntityListBase<ILocalOrderLine>`,
  concretes `internal partial`). `LocalSaveStore` follows the existing `SaveLifecycleStore` idiom;
  no `[assembly: Parallelize]` anywhere in the project.

## Read report

Read beyond the brief: `ValidateBase.cs`, `ValidateListBase.cs`, `EntityBase.cs`,
`Internal/ValidatePropertyManager.cs`, `Internal/ValidateProperty.cs`, and generated factory code
(`OrderFactory.g.cs`, `OrderItemListFactory.g.cs`) — all needed to answer the re-entrancy and
ordering questions by trace rather than by trusting the plan. Also both revert logs, which is what
surfaced Callout 2.

**Brief calibration for next time:** the internal property-manager files
(`Internal/ValidatePropertyManager.cs`, `Internal/ValidateProperty.cs`) hold the actual pause gates
and could not have been skipped — name them as code targets when asking a re-entrancy question.
