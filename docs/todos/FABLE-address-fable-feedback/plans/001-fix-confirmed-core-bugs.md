# Fix Confirmed + High-Confidence Core Defects

**Plan #:** 001
**Date:** 2026-08-11
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-11
**Plan-review opt-in:** (declared at draft time)
**Code-review opt-in:** (declared at draft time)

---

## Scope

Work through the "Verified" and "High-confidence — core" defects in FableFeedback.md Appendix A: the `IsBusy` operator-precedence bug, post-deserialization double event subscription (entity and list), the list converter's missing `$ref` emission, the `AsyncTasks` completion race and unobservable async-rule exceptions, per-invocation `expression.Compile()` in trigger properties, the shadowed-static `CreateProperty` hazard, the mutable shared `RuleBase.None`, the internal `PropertyReadOnlyException`, and the dead/inert surface (`RunRulesFlag`, no-op generated `GetRuleId`, unreachable generator paths). Each item is re-verified against current code before fixing; each fix gets a regression test. This plan does NOT touch MudNeatoo (plan 004) or RemoteFactory (plan 003), and does not revisit the `NoWarn` policy beyond what individual fixes require.

## Scope added (2026-08-21) — `PauseAllActions()` is not re-entrant

Found and **verified** during LIST-001; recorded here rather than fixed there, because it is
entity-side and outside that todo's list-machinery goal.

`ValidateBase.PauseAllActions()` (`ValidateBase.cs:780-789`) skips the pause when the object is
already paused, but still returns a `Paused` disposable whose `Dispose()` calls
`ResumeAllActions()` **unconditionally** (`:750-753`). Two consequences:

1. Two nested `using (x.PauseAllActions())` blocks — the pattern the XML docs demonstrate — leave
   the object un-paused as soon as the *inner* block exits, silently un-batching the outer one.
2. The documented batch-update idiom used **inside a factory method** ends the factory's pause
   scope early. `FactoryComplete` then finds the object un-paused, and its `ResumeAllActions()`
   hits the `if (IsPaused)` guard and does nothing — skipping `PropertyManager.ResumeAllActions()`
   and `ResetMetaState()`, leaving a stale meta-state baseline.

Verified by probe: after `FactoryStart(Fetch)` and a nested `using (entity.PauseAllActions())`,
`IsPaused` was already `False` on exit from the block and remained `False` through
`FactoryComplete`. A depth counter (or a disposable that only resumes if it was the one that
paused) is the obvious shape, but the fix is this plan's call to make.

Related: the same `if (IsPaused)` guard on the **list** side was examined by
[LIST-001](../../LIST-entitylist-state-machinery/plans/001-resume-guard-never-paused-list.md) and
dispositioned as correct — lists expose no `PauseAllActions()`, so they cannot reach this.

## Scope removed (2026-08-21)

**The list `FactoryComplete` stale-cache hole is done — do not re-fund it.** FableFeedback.md:118
states the defect as "`ValidateListBase.FactoryComplete:575` sets `IsPaused = false` directly
instead of `ResumeAllActions()` — stale `IsValid` cache after a paused Fetch." ISNEW-003 fixed
exactly that and shipped it in 0.31.0; `ValidateListBase.cs:579-582` now calls
`ResumeAllActions()`, pinned by
`EntityListBaseTests.FactoryComplete_AfterPausedAddOfModifiedItem_ListReportsModified`.

What remains is a *sequel*, not this item: `ResumeAllActions`' own `if (IsPaused)` guard
(`ValidateListBase.cs:546`) makes that new call a no-op on a never-paused list. That is owned by
[LIST-001](../../LIST-entitylist-state-machinery/plans/001-resume-guard-never-paused-list.md),
along with the rest of the `EntityListBase` state machinery. FableFeedback.md itself is left
unedited — it is a record of what the assessor said, not a live work list.
