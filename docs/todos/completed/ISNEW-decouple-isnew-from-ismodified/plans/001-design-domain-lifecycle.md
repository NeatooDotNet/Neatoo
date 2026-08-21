# Design.Domain OrderAggregate to Person-Canonical Lifecycle

**Plan #:** 001
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** Done (test-review gate closed, code-review vetoes verified closed — see reviews/001-*)
**Last Updated:** 2026-08-21
**Plan-review opt-in:** No (the canonical pattern was verified against the Person example and framework source in the 2026-08-20/21 design session, recorded in design.md; blast radius is the Design projects only)
**Code-review opt-in:** Yes (Design.Domain is the authoritative reference implementation — it must be *exactly* canonical, and ISNEW-002/ISNEW-004 build on it)

---

## Scope

Fix the Design.Domain OrderAggregate (Order, OrderItem, OrderItemList, and their Design.Tests)
so its factory lifecycle is faithful to actual framework behavior, following the Person
example's canonical pattern: children fetched through their own `[Fetch]` factory ops, child
persistence routed through per-item factory saves so post-save state is cleaned, and lifecycle
commentary that matches what the framework actually does. This plan does NOT touch Neatoo
library code, does NOT change IsModified/IsNew semantics (ISNEW-004), and does NOT add
Neatoo.UnitTest E2E coverage (ISNEW-002).

---

## Intent

Design.Domain is the documented source of truth, and today its flagship aggregate teaches
lifecycle patterns that are wrong: a Fetch that leaves every child `IsNew=true` (so the
documented Update dispatch would re-insert fetched items), an Update that never cleans child
state (so the root remains modified after save), and comments describing a
`FactoryComplete` cascade to children that does not exist. After this plan, a reader who
follows the OrderAggregate gets correct persistence state at every lifecycle stage, and
Design.Tests prove it. ISNEW-004 changes savability semantics on top of these flags — they must
be trustworthy first.

---

## Framework & Architectural Alignment

- **Person-example canonical lifecycle** (`src/Examples/Person`): the list's `[Fetch]`
  populates itself with items produced by the item factory's `[Fetch]` (adds happen while the
  list is paused by its own op); the list's `[Update]` iterates active + deleted items and
  routes each through the item factory's `Save`, giving every child its own
  `FactoryComplete(Insert/Update)` → `MarkUnmodified()` + `MarkOld()`; the parent delegates
  child persistence to the list factory.
- **Per-object factory lifecycle** — `FactoryStart`/`FactoryComplete` fire on the single
  factory target only; there is no graph cascade. Comments must reflect this.
- **Interface-first design** rules unchanged (internal concretes, public interfaces).
- Neatoo behavioral-design tables (plan-template-neatoo): n/a — no new behavioral properties.

---

## Constraints & Invariants

- No changes under `src/Neatoo/` in this plan.
- Existing Design.Tests keep their intent — extend or correct assertions to match *actual*
  framework behavior; never gut. Any existing test whose pinned expectation turns out to
  contradict real behavior is a discovery (stop and ask).
- Comments describe **today's** (pre-flip) semantics truthfully — e.g., Create still yields
  `IsModified=true` via the weld until ISNEW-004; that plan and ISNEW-005 sweep comments again.
- DeletedList lifecycle documentation stays consistent with `EntityListBase` actual behavior.
- The DDD documentation rules apply (terminology used freely, never explained).

---

## Steps

1. Give OrderItem and OrderItemList proper `[Fetch]` factory operations, and rework
   Order's `[Fetch]` to load items through them so fetched children carry correct
   persistence state (the Person list-fetch shape).
2. Rework Order's `[Update]` (and `[Insert]` where the same applies) to route child
   persistence through per-item factory saves so every child is cleaned by its own factory
   completion, and deleted-item processing is real rather than stubbed commentary.
3. Correct the lifecycle commentary across the three OrderAggregate files: remove the
   nonexistent FactoryComplete-cascade claims, fix the "state after Fetch/Save" annotations
   to verified behavior, and state that lifecycle hooks are per-factory-target.
4. Extend Design.Tests to pin the corrected lifecycle: post-fetch child state, post-save
   graph state for both insert and update paths, and deleted-item processing.
5. Sweep the rest of Design.Domain (base-class walkthroughs, gotchas) for the same cascade
   misconception and correct any instance found.

---

## Acceptance

- [x] After a factory Fetch of an order with items, every item reports `IsNew=false`,
      `IsModified=false`, and the aggregate reports `IsModified=false` `[integration]`
- [x] After `Save()` of a fetched order with a modified item and a newly added item, the
      whole graph reports `IsModified=false` and all items report `IsNew=false` `[integration]`
- [x] Removing an existing item and saving deletes it from the repository exactly once, and a
      subsequent save issues no further deletes (DeletedList cleared by the list's own
      factory completion) `[integration]`
- [x] Design.Domain lifecycle comments contain no claims contradicted by framework behavior
      (cascade claim removed; per-target lifecycle stated; **scoped by the 2026-08-21
      Amendment to the swept surface** — OrderAggregate, FetchPatterns, SavePatterns,
      AllBaseClasses, StateProperties, CommonGotchas; `Entities/` deferred to ISNEW-007)
      `[explicit-skip: prose accuracy — verified by the opted-in code review, not a test]`
- [x] Existing Design.Tests remain green with intent preserved `[explicit-skip: meta-bullet,
      full-suite run at the Step 5 gate]`

Added by the 2026-08-21 Amendment (sweep expanded the surface to two more demo aggregates):

- [x] After `SaveAggregateDemo` save (insert and update paths), every child route fires
      exactly once, generated Ids land on the entities, and the graph is clean `[integration]`
- [x] After `FetchWithChildrenDemo` fetch, children are old and clean `[integration]`

---

## Current State (Pre-Flight)

Walked during the 2026-08-20/21 design session (full narrative in
[../design.md](../design.md)):

- `Order.cs:109-144` — `[Fetch]` builds items via `itemFactory.Create()` + `LoadValue`, then
  `Items.Add(item)`. Item factory Create runs `FactoryComplete(Create)` → `MarkNew()`, so
  fetched items end `IsNew=true`; the comment at `Order.cs:143` claiming `IsNew=false` is
  wrong. The `Items = itemsFactory.Create()` list is already resumed when adds happen, so
  adds run the un-paused `InsertItem` branch (`MarkAsChild` fires — matching the `:139`
  comment — but item dirt-cache is set via the weld).
- `Order.cs:173-233` — `[Update]` writes the repository directly per item, never invokes the
  item factory; `ProcessDeletedItems` (`:221-233`) is stubbed commentary. Nothing cleans
  child state post-save; `OrderItem.cs:154-158` documents a FactoryComplete cascade
  ("For each item: MarkUnmodified()") that does not exist.
- `OrderItem.cs:49-62` — only `[Create]` ops; no `[Fetch]`. `OrderItemList.cs:32-36` — only
  an empty `[Create]`; no `[Fetch]`/`[Update]`.
- Canonical reference: `src/Examples/Person/Person.DomainModel/PersonPhoneList.cs` — list
  `[Fetch]` adds `personPhoneModelFactory.Fetch(entity)` while the list is paused by its own
  op; list `[Update]` iterates `this.Union(DeletedList)` and calls
  `personPhoneModelFactory.Save(model, entity)` per surviving item. Note its EF-entity-
  collection shape differs from Design's repository-tuple shape — adapt the *pattern*, not
  the signatures.
- Framework facts verified: generated factories invoke `FactoryStart`/`FactoryComplete` on
  the single target only (`RemoteFactory ClassFactoryRenderer.cs:666-704`);
  `EntityBase.FactoryComplete` Insert/Update → `MarkUnmodified()` + `MarkOld()`
  (`EntityBase.cs:557-588`); paused list adds skip `MarkAsChild`/`SetContainingList`/dirt
  cache (`EntityListBase.cs:198-267`) — so items fetched the canonical way will have
  `IsChild=false` today (pre-existing; ISNEW-003/ISNEW-004 territory, not this plan's).
- Tests: `Design.Tests/AggregateTests/OrderAggregateTests.cs:139-148` fetches but asserts
  only count; `DeletedListTests` exercise Create-only flows. DI:
  `Design.Tests/TestInfrastructure.cs:42,88` registers `MockOrderRepository`.
- Keyboard decision left open: whether Order's `[Fetch]` delegates to a list-factory
  `[Fetch]` (Person style) or keeps the loop in Order calling the item factory's `[Fetch]` —
  Person style preferred; pick whichever reads best as reference documentation.

---

## Test Evidence

All cited tests live in `src/Design/Design.Tests/AggregateTests/AggregateLifecycleTests.cs`
(new file, this plan). Tier note: Design.Tests run a single-container server scope — real
factories, real Save routing, stubbed repositories, no serialization boundary. The
two-container (client/server serialization) variants of these signals are ISNEW-002's scope.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| After Fetch, items old/clean, aggregate not modified | `[integration]` | `AggregateLifecycleTests.Fetch_ItemsAreOldAndClean_AggregateNotModified` | ✓ (single-container; two-container in ISNEW-002) |
| After Save (update path), whole graph clean | `[integration]` | `AggregateLifecycleTests.FetchModifyAddItem_Save_GraphIsCleanAndRoutingCorrect` | ✓ (single-container; two-container in ISNEW-002) |
| After Save (insert path), whole graph clean | `[integration]` | `AggregateLifecycleTests.CreateWithItems_Save_InsertsAllAndGraphIsClean` | ✓ (single-container; two-container in ISNEW-002) |
| Removed item deleted exactly once | `[integration]` | `AggregateLifecycleTests.RemoveExistingItem_Save_DeletesFromRepositoryExactlyOnce` | ✓ (single-container; two-container in ISNEW-002) |
| Lifecycle comments contain no contradicted claims | `[explicit-skip]` | Covered by opted-in code review | — |
| Existing Design.Tests green, intent preserved | `[explicit-skip]` | Full-suite run at gate (`reviews/001-test.log`) | — |

Added in the test-review loop (2026-08-21, addressing `reviews/001-test-review.md`
must-cover 1-2 and should-cover 3-5; tech debt queued as ISNEW-006):

| Behavior (from review findings) | Tier | Test method | Tier confirmed |
|---|---|---|---|
| SaveAggregateDemo insert path + Id writeback + graph clean | `[integration]` | `SaveAggregateLifecycleTests.CreateWithItems_Save_InsertsAllWithIdWriteback_AndGraphIsClean` | ✓ |
| SaveAggregateDemo fetch/modify/add/remove/save, all routes exactly once | `[integration]` | `SaveAggregateLifecycleTests.FetchModifyAddRemove_Save_RoutesAllPathsAndGraphIsClean` | ✓ |
| Generated-Id writeback (Order root + child) | `[integration]` | assertions added to both `AggregateLifecycleTests` save tests | ✓ |
| Unmodified-existing-child skip (exact UpdateItem count) | `[integration]` | tightened `CollectionAssert.AreEqual` in `FetchModifyAddItem_Save_...` | ✓ |
| FetchWithChildrenDemo children old + clean | `[integration]` | assertions added to `FetchTests.Fetch_WithChildren_LoadsChildCollection` | ✓ |
| Post-save item-count guards; second-save child delegation | `[integration]` | assertions added to `AggregateLifecycleTests` save/remove tests | ✓ |

---

## Plan Amendments

### 2026-08-21 — Step 5 sweep found two more broken demo aggregates

- **Section affected:** Step 5
- **Original said:** "Sweep the rest of Design.Domain ... correct any instance found" —
  expected comment-level fixes.
- **What changed:** `FactoryOperations/FetchPatterns.cs` and
  `FactoryOperations/SavePatterns.cs` *demonstrated* the same broken lifecycle as code
  (Create+LoadValue child fetch; direct-repository child writes; a phantom
  `GetDeletedItems()` helper), plus wrong `PauseAllActions` guidance. Both demo aggregates
  reworked to the canonical shape (child/list `[Fetch]`, list `[Update]` with per-item
  factory saves); the "Fetch with PauseAllActions" pattern replaced with a RETIRED /
  COMMON MISTAKE block (an explicit `using (PauseAllActions())` inside a factory method
  resumes EARLY on dispose — the op already runs paused); Pattern 1's "setters in Fetch
  mark modified" claim corrected (instance factory ops run paused; LoadValue is preferred
  because it is pause-independent and required in read-style `[Create]` constructors).
- **Why:** The flagship canonical fix cannot coexist with pattern files teaching the mistake.
- **Discovery Log link:** 2026-08-21 — ISNEW-001 (sweep entry)

### 2026-08-21 — Code-review vetoes: routing docs fixed; Entities sweep deferred to ISNEW-007

- **Section affected:** Step 5, Acceptance bullet #4
- **Original said:** The sweep would correct every instance of the cascade misconception
  found in Design.Domain.
- **What changed:** Two of the code review's veto findings were fixed in-plan
  (`SavePatterns.cs` stale "parent DeletedList cleared after child [Delete]" claim; the
  Save-routing pseudocode replaced with the actual generated ordering — IsDeleted first,
  then IsNew, IsModified never consulted — plus matching corrections in
  `AllBaseClasses.cs`). The third — `Entities/` (Employee/Address/AddressList), a whole
  additional broken demo aggregate including DID-NOT-DO blocks rejecting the now-canonical
  pattern — is a full aggregate rework and is **deferred to ISNEW-007** rather than absorbed.
  Acceptance bullet #4 is therefore scoped to the swept surface (OrderAggregate,
  FetchPatterns, SavePatterns, AllBaseClasses, StateProperties, CommonGotchas) with the
  Entities deferral recorded here and in the Discovery Log.
- **Why:** Silent absorption would hide scope; silent omission would make bullet #4 dishonest.
- **Discovery Log link:** 2026-08-21 — ISNEW-001 (code review: Entities aggregate + routing docs)

### 2026-08-21 — Fetched children lack IsChild/ContainingList (documented, not fixed)

- **Section affected:** Steps 1, 3; test assertions
- **Original said:** Fetch rework would leave children with fully correct state.
- **What changed:** Items added while the list is paused by its own `[Fetch]` skip
  `MarkAsChild`/`SetContainingList` (they run only in the un-paused add path) — and the
  un-paused path is unusable for fetch because it calls `MarkModified()` on non-new items.
  No fetch shape today produces `IsChild=true` + clean state. Documentation notes cite
  ISNEW-003; new tests assert IsNew/IsModified but not IsChild; guidance steers item removal
  through `list.Remove(item)` rather than `item.Delete()` until ISNEW-003 lands.
- **Why:** Library changes are out of this plan's scope; ISNEW-003 upgraded from "decide
  whether" to required.
- **Discovery Log link:** 2026-08-21 — ISNEW-001 (IsChild gap entry)

---

## Notes

- Comments written here get partially rewritten by ISNEW-004/ISNEW-005 (new semantics). Keep the
  lifecycle-mechanics commentary (this plan) separable from the semantics commentary (the
  flip) where practical.
- **Deliberate deviation from the Person reference** (code review callout d): the list
  `[Update]`s guard child saves with `else if (item.IsNew || item.IsModified)`;
  `PersonPhoneList` saves every surviving item unconditionally (EF change tracking makes
  that free; a repository write is not). The guard is pinned by the exact-count
  `UpdatedItemIds` assertion.
- Close-out audit note (code review callout e): Design.Tests is not part of
  `src/Neatoo.sln` — full coverage claims need both `001-build/test.log` AND
  `001-design-build/test.log`.
