# The Flip: IsModified / IsSavable / Attach-Marking

**Plan #:** 004
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** Done (both gates closed — reviews/004-plan-review.md, 004-test-review.md, 004-code-review.md)
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
- **The entity-child-property channel must keep dirtying the parent.** It does so today
  through the weld (see Current State); parity across the flip is required, not optional —
  losing it is the same silent-data-loss shape as losing the list channel.
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
   to a live list, and apply the same marking when a child **entity** is assigned to a live
   parent property; leave the lazy-load path suppressed, leave paused population alone, and
   leave list-valued properties alone (their dirt already flows from their children).
4. Leave list-element replacement alone entirely — it has never dirtied the graph, an
   existing test pins that, and design.md never decided it. All of `SetItem` (incoming-item
   marking and identity, displaced-item disposition, missing guards) goes to ISNEW-009.
5. Write the why at each changed seam — the two questions, savable-vs-modified, and that
   dirt (not `IsNew`) is what aggregates.
6. Update the tests that pinned the welded semantics, distinguishing assertions that
   legitimately change from tests whose intent the flip would break.
7. Add coverage for the newly reachable states: rich create clean-and-savable, opt-in dirty
   create, attach-dirties-parent through both list and property, and the post-save and
   round-trip behavior of each.

---

## Acceptance

- [x] A created object — including one whose factory populated it and its children — reports
      not-modified while remaining savable, and saving it inserts `[integration]`
- [x] A `[Create]` that opts in with `MarkModified()` reports modified, and that survives a
      remote round trip `[integration]`
- [x] An unsaved-changes guard bound to modified-state stays quiet on a fresh create and
      speaks up after the first real edit `[unit]`
- [x] Attaching a new child to a live parent — via a list, via a single-entity child
      property, and via a list-valued child property — makes the parent modified and savable,
      and the child's insert is not skipped by modified-guarded cascades `[integration]`
- [x] Attaching a new child and then removing it returns the parent to clean — attach-marking
      is reversible, not sticky `[integration]`
- [x] Baseline population stays clean: factory-loaded and factory-created children produce a
      graph that reports not-modified `[integration]`
- [x] Lazy-loading a child does not dirty its parent `[integration]`
- [x] Post-save the whole graph is clean and a second save is still refused; a created object
      that is then deleted still routes correctly `[integration]`
- [x] Every test that pinned the welded semantics reports the new semantics, whether or not
      design.md named it — design.md's list is known incomplete (e.g.
      `Design.Tests/AggregateTests/DeletedListTests.IsModified_TrueWhenNewItemRemoved`, whose
      "New order is always modified" intent the flip deletes outright) `[unit]`
- [x] The save guard still reports the *accurate* failure reason for a new-but-busy entity —
      admitting `IsNew` must not make a busy object report `NotModified` `[unit]`
- [x] Build and both suites green `[explicit-skip: meta-bullet, gate run]`

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
- **Child-property assignment — CORRECTED by the plan review (veto B1).** The property never
  self-dirties when holding a Neatoo object (`Internal/EntityPropertyManager.cs:41-53`), but
  the **parent is dirtied today anyway**, through the child:
  `EntityProperty.IsModified => IsSelfModified || EntityChild?.IsModified` (`:56`), and
  `EntityPropertyManager.Property_PropertyChanged` recalculates from
  `PropertyBag.Any(p => p.Value.IsModified)` (`:167-176`), which feeds
  `EntityBase.IsModified`. A new child's `IsModified` is true **only via the weld** — so
  `parent.Child = childFactory.Create()` dirties a live parent today, and cutting the weld
  without property attach-marking would break it. This channel is **mandatory**, the same
  shape as the list case; it is not the "quirk fix" the first draft of this plan (and
  design.md) called it. The same chain runs on the paused path via
  `EntityPropertyManager.ResumeAllActions` (`:140-148`), which is why rich `[Create]` with a
  property-held child lands modified today.
- **List-valued child properties need no marking.** `EntityChild` is `IEntityMetaProperties`,
  which `EntityListBase` also implements — and a list has no `MarkModified`
  (`IsMarkedModified => false`, `EntityListBase.cs:76`). It does not need one: list dirt
  aggregates from children, which are attach-marked as they are added.
- **Mark placement is constrained.** `LazyLoadEntityProperty.OnPropertyChanged`
  (`Internal/LazyLoadEntityProperty.cs:213-233`) *calls* `base.OnPropertyChanged` and then
  undoes `IsSelfModified` — an undo written against `IsSelfModified` would not undo a mark
  placed on the child. The lazy path is additionally insulated because the generated lazy
  setter assigns via `LoadValue`, which raises no `Value` notification
  (`Internal/ValidateProperty.cs:218-220`). Both protections are properties of *this* call
  graph: the mark must go inside `EntityProperty.OnPropertyChanged`'s Value branch, not in
  `SetValue` or `HandleNonNullValue`.
- **`SetItem` today** (`EntityListBase.cs`) does cache arithmetic only — no identity, no
  marking, and the displaced item is dropped without `MarkDeleted` or `DeletedList` entry
  (silently orphaning its row). This plan marks only the incoming item; the displaced item's
  disposition is ISNEW-009.
- **Ordering/re-entrancy is bounded.** `MarkModified()` → `CheckIfMetaPropertiesChanged()`
  raises `PropertyChanged` *before* `base.InsertItem` subscribes the item, so for a fresh
  item the event has no subscribers (which is why the manual cache update at
  `EntityListBase.cs:265` exists). Newly reachable: an item that already has a `Parent`
  (intra-aggregate move, or re-add after `RemoveItem`, which unsubscribes but leaves
  `Parent`) now fires that upward notification for *new* items too. Existing pathway, one
  more object state — not new machinery.
- **Other generated consumer:** `Neatoo.BaseGenerator/Generators/MapperGenerator.cs:47` emits
  `if (this[nameof(P)].IsModified)` in `MapModifiedTo`. That is *property-level* `IsModified`,
  whose semantics the flip does not change for scalars; a mapper over an entity-child
  property would change behavior because property-level `IsModified` delegates to the child.
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

New tests in `Integration/Aggregates/SaveLifecycle/DecoupledSemanticsTests.cs` and
`Integration/Concepts/EntityBase/ChildPropertyAttachTests.cs` (the latter written **before**
the library edit, as a parity anchor for the previously-uncovered child-property channel —
it passed pre-flip via the weld and passes post-flip via attach-marking).

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| Created object incl. factory-built children: not modified, still savable | `[integration]` | `AggregateSaveLifecycleTests.RichCreate_Untouched_IsSavableFromIsNewAlone_AndSaveInserts`; `Design.Tests EntityBaseTests.Create_SetsIsModifiedFalse_ButStillSavable` | ✓ |
| `MarkModified()` opt-in create reports modified, survives a round trip | `[integration]` | `DecoupledSemanticsTests.CreateThatIsUserWork_OptsIntoModified_AndItSurvivesTheWire` (asserts `RemoteCallCount` advanced) | ✓ |
| Unsaved-changes guard quiet on fresh create, speaks after first edit | `[unit]` | `DecoupledSemanticsTests.UnsavedChangesGuard_QuietOnFreshCreate_SpeaksAfterFirstEdit`; `..._QuietImmediatelyAfterSave` (the originating regression) — tier note: written at `[integration]` because the guard's value comes from the real factory/save path, which is stricter than the declared tier | ✓ (stricter) |
| Attach via list / single-entity property / list-valued property dirties parent; insert not skipped | `[integration]` | `AggregateSaveLifecycleTests.FetchedRoot_AddOneNewChild_IsModifiedAndSavable_AndChildInserts`; `ChildPropertyAttachTests.AssignNewChildToCleanParent_DirtiesParent`; `..._AssignListValuedChildProperty_DirtIsCarriedByItsChildren` (assign-then-add) and `..._AssignFactoryPopulatedListToLiveParent_DirtiesParent` (populate-then-assign — the case the first implementation regressed; verified failing on revert) | ✓ |
| Replacing a list element with a new item dirties the list | `[unit]` | `EntityListBaseTests.SetItem_ReplaceWithNewItem_ListBecomesModified` (verified failing on revert) | ✓ |
| Attach-then-remove returns parent to clean (reversible, not sticky) | `[integration]` | `DecoupledSemanticsTests.AttachThenRemoveNewChild_ReturnsParentToClean`; `Design.Tests DeletedListTests.AddThenRemoveNewItem_LeavesOrderCleanButSavable` | ✓ |
| Baseline population stays clean | `[integration]` | `ListFactoryStateTests.FetchedGraph_IsCompletelyClean`; `ChildPropertyAttachTests.AssignChildDuringPausedFactoryOperation_LeavesParentClean`; `EntityListBaseTests.Add_WhenPaused_DoesNotMarkModified` | ✓ |
| Lazy-loading a child does not dirty its parent | `[integration]` | `DecoupledSemanticsTests.LazyLoadingAChild_DoesNotDirtyTheParent`; pre-existing `LazyLoadStatePropagationTests` (green throughout) | ✓ |
| Post-save clean, second save refused; created-then-deleted routes | `[integration]` | `AggregateSaveLifecycleTests.SavedAggregate_SecondSave_ThrowsNotModified`; `DecoupledSemanticsTests.CreatedThenDeleted_IsSavable_AndSaveDeletesNothing` (now actually saves) and `..._FetchedThenDeleted_Save_RoutesToDelete` | ✓ |
| Every weld-pinned test reports the new semantics | `[unit]` | Updated: `EntityBaseStateTests.IsModified_WhenIsNew_ReturnsFalse`, `.IsSavable_WhenNew_ReturnsTrue`, `.Scenario_NewEntityLifecycle`; `TwoContainerMetaStateTests.Create_TwoContainer_IsModified_ReturnsFalse`, `.Create_ServerSideOnly_IsModified_ReturnsFalse`; `RequiredDuringFactoryTests.RunRules_DuringFactoryInsert_...`; `Design.Tests` `EntityBaseTests` + `DeletedListTests`; samples `ApiReferenceSamples` (×2), `ChangeTrackingSamples` | ✓ |
| Save guard reports the accurate reason for a new-but-**busy** entity | `[unit]` | `EntityBaseStateTests.Save_WhenNewAndBusy_ThrowsIsBusy_NotNotModified` — the only case that makes the guard's `\|\| IsNew` term load-bearing; verified failing on revert. (An earlier evidence row reworded this bullet to "unsavable" and cited a test of *invalidity*, `DecoupledSemanticsTests.SaveGuard_ReportsAccurateReason_ForNewButInvalid`, which never reaches the line. That test is kept — it covers a different, real case.) | ✓ |
| Build + both suites green | `[explicit-skip]` | `reviews/004-*.log` — solution 2173 passed / 2 pre-existing skips; Design.Tests 116/116 | — |

---

## Plan Amendments

### 2026-08-21 — Attach-marking initially covered two of four channels (gate fixes)

- **Section affected:** Step 3, Step 4, Acceptance
- **Original said:** attach-marking on live list adds and new-child property assignment
  replaces every channel the removed `IsNew` term provided.
- **What changed:** two more channels were found regressed by the code-review gate and fixed
  in the same plan. (a) A **factory-populated list assigned to a live parent** no longer
  dirtied it — its children were added while paused, so they carry no mark, and post-flip
  their `IsNew` no longer makes them modified. An assigned list now marks its new children
  (a list cannot be marked itself). (b) **`SetItem` with a new item** no longer dirtied the
  list — the cache arithmetic reads `item.IsModified`, which the weld made true for any fresh
  item — so replacement on a clean root left it unsavable. `SetItem` now marks a new incoming
  item, reversing the earlier decision to drop it entirely (Amendment 2); everything
  save-side about `SetItem` remains ISNEW-009's.
- **Why:** both were regressions this plan introduced, not pre-existing gaps, and the earlier
  justification for excluding them was factually wrong in both cases. Recorded in
  `reviews/004-code-review.md` (V1, V2). Both fixes are pinned by tests verified to fail on
  revert.
- **Discovery Log link:** 2026-08-21 — ISNEW-004 (gate: two more weld channels)

### 2026-08-21 — Child-property marking scoped to NEW children (parity, not expansion)

- **Section affected:** Step 3, Constraints
- **Original said:** mark the assigned child when a child entity is assigned to a live parent
  property.
- **What changed:** the mark applies only when the assigned child `IsNew`.
- **Why:** marking every assignment turned six existing tests red — the entity-child
  *derivation* tests (`EntityPropertyManagerTests.IsModified_WithUnmodifiedEntityChild_ReturnsFalse`,
  `EntityPropertyTests.IsModified_WhenEntityChildIsNotModified_ReturnsFalse`, and the
  `EntityChild_*` scenarios). Their intent is the invariant "a property HOLDING an unmodified
  child is not modified", with assignment only as setup — a real invariant, not weld
  characterization. The plan review was explicit that this channel requires *parity*, and
  parity is exactly what the weld dirtied: new children only. design.md's migration bullet
  was corrected a second time to match.
- **Discovery Log link:** 2026-08-21 — ISNEW-004 (property-channel scope)

### 2026-08-21 — `SetItem` dropped from this plan entirely

- **Section affected:** Step 4, Acceptance
- **Original said:** mark the incoming item on list-element replacement.
- **What changed:** `SetItem` is untouched; a comment there records why and points at
  ISNEW-009.
- **Why:** marking broke `EntityListBaseTests.SetItem_ReplaceModifiedWithUnmodified_WhenOnlyModified_ListBecomesUnmodified`,
  whose intent is a deliberate cache invariant. Replacement has never dirtied the graph, and
  design.md never decided it — so changing it is a separate decision, not a consequence of
  the IsNew/IsModified split. This is the narrower half of the plan review's veto B2.
- **Discovery Log link:** 2026-08-21 — ISNEW-004 (property-channel scope)

### 2026-08-21 — `RemoveItem` now announces its modified-state change

- **Section affected:** Steps (attach-marking), Acceptance (reversibility bullet)
- **Original said:** nothing about list notifications.
- **What changed:** `EntityListBase.RemoveItem` calls `CheckIfMetaPropertiesChanged()` after
  recalculating its cache.
- **Why:** the reversibility acceptance bullet (added on the plan review's Pass A callout)
  failed and exposed a pre-existing bug: `base.RemoveItem` runs its meta-property check
  *before* `EntityListBase` updates `_cachedChildrenModified`, so the list's IsModified
  true→false transition was never announced and the parent's cached IsModified stayed true
  forever. Symptom: add a child, remove it, and the aggregate still claims unsaved changes
  with nothing to save. Fixing it was required by this plan's own acceptance; the broader
  list-notification hole stays with ISNEW-008.
- **Discovery Log link:** 2026-08-21 — ISNEW-004 (RemoveItem notification)
