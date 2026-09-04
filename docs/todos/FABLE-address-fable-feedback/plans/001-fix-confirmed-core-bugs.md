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

## Scope added (2026-08-22) — `RemoveItem`/`SetItem` ordering characteristics (reorder together)

Three related items from the LIST arc's code reviews and close-out audit
(`LIST-entitylist-state-machinery/reviews/002-code-review.md`, `004-code-review.md`,
`close-out-audit.md` C1). All three were **traced to correct end states** — no data is at risk
today — and all three live in the same `MarkDeleted`-before-`base` shape that `RemoveItem` and
`SetItem` share, which is why they are one work item, not three: fixing any of them properly means
reordering both methods together.

1. **Mid-mutation notification.** In both `RemoveItem` (`EntityListBase.cs`, `MarkDeleted` at the
   top of the live branch) and `SetItem`, `MarkDeleted()` on the outgoing item fires **before**
   `base.RemoveItem`/`base.SetItem` — which is where `ValidateListBase` unsubscribes that item's
   handlers. The mark can therefore raise a list-level `PropertyChanged(IsModified)` mid-mutation:
   before the slot is swapped/removed, before `DeletedList.Add` runs, and (for `SetItem`) before
   the incoming item has identity. A synchronous consumer reacting to that notification observes
   the old item still physically present and `DeletedList` empty despite `IsDeleted == true`. End
   state verified correct on every branch; the window is the issue.

2. **Transient cache pollution.** In `SetItem`, `oldWasModified` is captured **before**
   `MarkDeleted()` runs, so replacing an unmodified-persisted item with another
   unmodified-persisted item skips both branches of the end-of-method recalculation and leaves
   `_cachedChildrenModified` `true` from the transient `MarkDeleted`-triggered flip — violating
   that field's own doc comment ("children only, not `DeletedList`"). Not reachable as a wrong
   *public* `IsModified` today, because `DeletedList.Any()` masks it until `FactoryComplete(Update)`
   fully recalculates. The one unruled-out path: moving the item to a *different* list in the same
   aggregate while it still sits in this list's `DeletedList` — `RemoveFromDeletedList` on the old
   list does no cache recalculation and no notification, so the pollution would persist unmasked
   once that `DeletedList` empties.

3. **Stale `ContainingList` on the displaced/removed item, paused branch.** Neither `RemoveItem`'s
   nor `SetItem`'s paused branch touches the outgoing item, so it keeps a `ContainingList` pointing
   at a list it is no longer in. A later `Delete()` on it routes through `DeleteChild` on that
   list — recording a deletion with no persistence consequence, silently lost. Confirmed by the
   LIST-004 review that the same failure mode existed identically on the live path before the LIST
   arc (`Collection<T>.Remove` on an absent item is a no-op), so this is long-standing, not new.

Constraint to carry into the fix: LIST-003's Discovery Log records that the current notification
safety during nested saves depends on `EntityBase.FactoryComplete`'s pause → resume →
`MarkUnmodified` ordering. Any reordering here must re-check that interaction — the trace is in
`LIST-entitylist-state-machinery/reviews/003-code-review.md` Callout 1.

## Scope added (2026-08-21) — entity-property assignment confers no child identity

Found during LIST-005; recorded here rather than fixed there, because it is entity-property
behavior rather than list state machinery.

Assigning a child entity to a parent's entity property does **not** set `IsChild` or
`ContainingList` on that child. `EntityParentChildFetchTests` has to call `child.MarkAsChild()`
by hand after `parent.Child = child`, and that call is still there for exactly this reason.

This is the one attach channel ISNEW-003 did not unify. ISNEW-003 gave child identity to the list
channels (`InsertItem`, both branches) and LIST-002 added the last list channel (`SetItem`, both
branches) — so a child that joins via a *list* is now always a full member, while a child assigned
to an entity *property* is not. Without identity, save routing and `Delete()` do not work on it.

Whether that asymmetry is a defect or a deliberate boundary is the open question: it may be
legitimate for a property-held child to derive its identity differently. Decide it explicitly
rather than leaving it as a hand-written line in a test fixture.

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
