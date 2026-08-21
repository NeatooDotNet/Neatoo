# The Flip: IsModified / IsSavable / Attach-Marking

**Plan #:** 004
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** In Progress
**Last Updated:** 2026-08-21
**Plan-review opt-in:** Yes (breaking public-API semantics change at the framework's core; every consumer's save and unsaved-changes logic depends on it)
**Code-review opt-in:** Yes (library change to the state machinery the whole framework rests on)

---

## Scope

Land the decided semantic change from [design.md](../design.md): `IsModified` stops meaning
"or new", savability admits new objects directly, dirty-on-create becomes opt-in through the
existing `MarkModified()`, and attaching a child to a live graph marks it — replacing the
job the removed term was secretly doing. Update the tests that pinned the old semantics, add
coverage for the new states, and write the "why" at the changed seams in code. Does NOT
touch documentation outside code comments (ISNEW-005) and does NOT change `IsDeleted`
semantics or factory save routing.

---

## Intent

Today the framework cannot express "new but untouched": `IsModified` is a live derivation
that includes `IsNew`, so the state is unreachable rather than merely unset. That forces
every unsaved-changes guard bound to `IsModified` to cry wolf on freshly created objects —
including immediately after a save that re-derives them — and pushes applications into
per-callsite hand-splits like `IsNew ? HasData : IsModified`.

After this plan the two questions are answered separately: `IsModified` means "differs from
the baseline the factory operation left", `IsNew` means "persistence doesn't know this yet",
and savability needs either. A `[Create]` whose result *is* the user's work opts in with
`MarkModified()`. Dirt — never `IsNew` — continues to aggregate up the graph, which is why
attaching a child must mark it.

---

## Framework & Architectural Alignment

- The `MarkXXX` vocabulary is the whole surface: `MarkNew`/`MarkOld` for routing state,
  `MarkModified`/`MarkUnmodified` for dirt. No new state, no new API, no configuration knob.
- Per-object routing state vs aggregating state — the same split CSLA's `ITrackStatus` makes
  (verified in design.md): `IsNew`/`IsDeleted` never propagate; `IsModified`/`IsValid`/
  `IsBusy` do.
- Attach-marking rides the framework's existing dirt channel and the list/property machinery
  ISNEW-003 just made correct; it is the removal of an exemption, not a new mechanism.
- Existing serialization contract: `IsMarkedModified` already crosses the wire, so opt-in
  create dirt and attach marks survive a round trip with no converter change.

---

## Constraints & Invariants

- Factory save routing is untouched — it dispatches on `IsDeleted`/`IsNew` only.
- `IsDeleted` keeps its place in both `IsModified` and `IsSelfModified`.
- A deleted-and-new object must not attempt to delete a row that was never written (the
  generated routing short-circuits this only when a `[Delete]` exists in the signature group
  — see the ISNEW-007 finding).
- Baseline population must stay clean: paused adds and factory-op writes must not mark.
- LazyLoad must stay clean: loading a lazy child is a load, not user work.
- Post-save the whole graph must still come back clean, and a second save must still be
  refused.
- The ISNEW-002/003 safety net must go green with only *assertion-value* edits where the
  flip genuinely changes an answer — any test needing restructuring is a signal the change
  is wrong, not that the test is.

---

## Steps

1. Remove the `IsNew` term from the modified derivation, and let savability and the save
   guard admit new objects directly.
2. Keep `MarkNew` pure routing state — nothing about dirt.
3. Remove the exemption that stops newly-created items being marked when they are attached
   to a live list, and apply the same marking when a child entity is assigned to a live
   parent property; leave the lazy-load path suppressed and leave paused population alone.
4. Cover the replaced-item path on lists so a swap behaves like a remove plus an attach.
5. Write the why at each changed seam — the two questions, savable-vs-modified, and that
   dirt (not `IsNew`) is what aggregates.
6. Update the tests that pinned the welded semantics, distinguishing assertions that
   legitimately change from tests whose intent the flip would break.
7. Add coverage for the newly reachable states: rich create clean-and-savable, opt-in dirty
   create, attach-dirties-parent through both list and property, and the post-save and
   round-trip behavior of each.

---

## Acceptance

- [ ] A created object — including one whose factory populated it and its children — reports
      not-modified while remaining savable, and saving it inserts `[integration]`
- [ ] A `[Create]` that opts in with `MarkModified()` reports modified, and that survives a
      remote round trip `[integration]`
- [ ] An unsaved-changes guard bound to modified-state stays quiet on a fresh create and
      speaks up after the first real edit `[unit]`
- [ ] Attaching a new child to a live parent — via a list and via a child property — makes
      the parent modified and savable, and the child's insert is not skipped by
      modified-guarded cascades `[integration]`
- [ ] Baseline population stays clean: factory-loaded and factory-created children produce a
      graph that reports not-modified `[integration]`
- [ ] Lazy-loading a child does not dirty its parent `[integration]`
- [ ] Post-save the whole graph is clean and a second save is still refused; a created object
      that is then deleted still routes correctly `[integration]`
- [ ] The pinned tests named in design.md report the new semantics `[unit]`
- [ ] Build and both suites green `[explicit-skip: meta-bullet, gate run]`

---

## Current State (Pre-Flight)

Walked 2026-08-21 before the first edit (line numbers as of this branch):

- **The three seams to change.** `EntityBase.cs:159` `IsModified => PropertyManager.IsModified
  || IsDeleted || IsNew || IsSelfModified`; `:174` `IsSavable => IsModified && IsValid &&
  !IsBusy && !IsChild`; `:450` the `Save()` guard's `NotModified` branch tests
  `!(IsModified || IsSelfModified)` — the `IsSelfModified` half is already redundant.
  `MarkNew` (`:330-333`) is already pure.
- **Attach exemption.** `EntityListBase.InsertItem` un-paused branch,
  `EntityListBase.cs:242-245`: `if (!item.IsNew) { itemInternal.MarkModified(); }` — the
  `!IsNew` guard is the exemption to remove. `SetItem` (`:315-340`) marks nothing today.
  The paused branch (post-ISNEW-003) applies identity only — that must stay.
- **Child-property assignment.** `EntityProperty.OnPropertyChanged`
  (`Internal/EntityPropertyManager.cs:41-53`) sets `IsSelfModified = true && EntityChild ==
  null`, i.e. deliberately never self-dirties when holding a Neatoo object — so assigning a
  child entity to a parent property dirties nothing today.
  `LazyLoadEntityProperty.OnPropertyChanged` (`Internal/LazyLoadEntityProperty.cs:213-233`)
  already suppresses and actively undoes `IsSelfModified`, so the lazy path is insulated.
- **What the removed term is doing today** (must be replaced, not just deleted): the only
  upward channel is `child.IsNew → child.IsModified → list cache → EntityProperty →
  PropertyManager → parent.IsModified`. `EntityListBase.IsNew => false` (`:84`) and
  `EntityBase.IsNew` (`:180`) is a plain flag — `IsNew` itself never aggregates.
- **Serialization.** `IsMarkedModified` is on `IEntityMetaProperties` and round-trips via the
  converter's reflection loop (`NeatooBaseJsonTypeConverter.cs:414-427`); `IsNew` likewise.
  No converter change needed.
- **Generated routing** (verified in ISNEW-007 against emitted factories): `IsDeleted` is
  tested first, then `IsNew`, else Update; `IsModified` is never consulted. A group that has
  a `[Delete]` short-circuits new-and-deleted to a no-op; a group without one throws
  `NotImplementedException` on the deleted branch. So the flip cannot change routing, but the
  created-then-deleted path is worth an explicit test.
- **Blast radius in tests.** 17 files contain `Assert.IsTrue(....IsModified)`. Most are
  property-dirt cases and unaffected; the ones that change are those where dirt came *only*
  from `IsNew`. design.md names the specific pinned tests
  (`EntityBaseStateTests.IsModified_WhenIsNew_ReturnsTrue`, three in
  `TwoContainerMetaStateTests`, `EntityListBaseStateTransitionTests.Add_NewItem_...`, and the
  `api.md` sample in `src/samples/ApiReferenceSamples.cs`).
- **Safety net in place.** ISNEW-002's `AggregateSaveLifecycleTests` (9 tests) and ISNEW-003's
  `ListFactoryStateTests` (5) exercise the exact paths this plan rewires, including a
  boundary proof, an isolated attach case, and a rich create whose savability is pinned to
  the `IsNew` term alone — that last one is the before/after anchor for this flip.

---

## Test Evidence

_Filled after implementation, before the gate._

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| | | | |

---

## Plan Amendments

_None yet._
