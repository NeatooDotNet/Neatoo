# E2E Aggregate Save Lifecycle Integration Tests

**Plan #:** 002
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** Done (test-review gate closed — see reviews/002-test-review.md)
**Last Updated:** 2026-08-21
**Plan-review opt-in:** No (test-only plan against the framework's existing two-container harness; no library or public-API change)
**Code-review opt-in:** No (test-only; the test-review gate is the appropriate check)

---

## Scope

Build the end-to-end aggregate save safety net in Neatoo.UnitTest that ISNEW-004's semantic
flip will be verified against: a purpose-built aggregate (root + child list + child, backed
by an in-memory store) exercised through the real two-container client/server harness, with
tests covering create→save, fetch→modify→save, child add/remove/delete, and post-save graph
state on the client side of the serialization boundary. Assertions pin **current**
(pre-flip) semantics; ISNEW-004 updates the ones the flip changes. Does NOT change library
code, and does NOT re-cover what Design.Tests already pins single-container (ISNEW-001/007).

---

## Intent

The suite has effectively no end-to-end `Save()` coverage: exactly one test file calls
`.Save(`, and list `FactoryComplete(Update)` behavior is only ever *simulated* by calling it
by hand. That means the flip in ISNEW-004 — which rewires savability and the child-dirt
channel — would land on unverified ground, and a regression in aggregate save would surface
in consumer apps rather than in CI. After this plan, save is exercised the way applications
actually use it: a real `Save()` on a root that crosses the wire, with the returned graph's
state asserted on the client.

---

## Framework & Architectural Alignment

- Canonical aggregate lifecycle as established in ISNEW-001 (list `[Fetch]` via item
  `[Fetch]`; list `[Update]` routing per-item factory saves; root delegates to list factory).
- Two-container harness: `ClientServerTestBase` / `ClientServerContainers` — real
  serialization across a simulated remote boundary, server-side factory execution.
- Neatoo testing philosophy: real Neatoo classes throughout; only the persistence store is
  a test double.
- Interface-first: root interface extends `IEntityRoot`, child extends `IEntityBase`, list
  parameterized on the child interface.

---

## Constraints & Invariants

- No changes under `src/Neatoo/`.
- Assertions describe **current** semantics (Create ⇒ IsModified=true via the weld). Where a
  bullet's expected value is one the flip will change, the test must be written so ISNEW-004
  updates an assertion rather than restructuring the test.
- Existing tests keep their intent; the new aggregate is additive and self-contained.
- The store must be resettable per test — the two containers are static/shared, so leaked
  state between tests would produce order-dependent failures.
- `[Remote]` on the root's persistence operations so the save genuinely crosses the boundary.

---

## Steps

1. Add a self-contained test aggregate (root, child, child list) to the integration test
   project with the canonical lifecycle wired to an in-memory store, remote-capable so its
   saves cross the client/server boundary.
2. Give the store per-test reset and enough recording to assert which persistence operations
   fired, mirroring the recording-mock approach proven in ISNEW-001/007.
3. Cover the create→insert path end to end: client creates, populates children, saves, and
   the returned graph is asserted for persistence state and generated identity.
4. Cover the fetch→modify→save path end to end, including a child added, a child modified,
   and a child removed in the same save.
5. Cover the save-guard behaviors that ISNEW-004 will change (unmodified root rejected;
   savability of a fresh create), so the flip has a before/after anchor.
6. Cover post-save graph cleanliness on the CLIENT side specifically — the state that
   crossed the wire, not just the server's copy.

---

## Acceptance

- [x] A created aggregate with children, saved across the two-container boundary, comes back
      with the root and every child persisted, old, and clean, with generated identities
      landing on the client-side entities `[integration]`
- [x] A fetched aggregate modified with a child added, a child changed, and a child removed,
      saved across the boundary, routes each persistence operation exactly once and comes
      back clean `[integration]`
- [x] Saving an unmodified fetched aggregate is rejected by the save guard (records the
      pre-flip behavior ISNEW-004 changes) `[integration]`
- [x] A freshly created, untouched aggregate is savable and its save inserts (records the
      pre-flip behavior ISNEW-004 preserves through a different mechanism) `[integration]`
- [x] Removing a child that was never persisted causes no delete operation `[integration]`
- [x] Build and full solution suite green `[explicit-skip: meta-bullet, gate run]`

---

## Current State (Pre-Flight)

Walked 2026-08-21 before the first edit:

- **Harness exists and is adequate.** `TestInfrastructure/IntegrationTestBase.cs:260-338`
  defines `ClientServerTestBase` with `InitializeScopes()`, `GetClientService<T>()`,
  `GetServerService<T>()`. `ClientServerContainer.cs:80-115` builds two static containers
  (server = `NeatooFactory.Server`, client = `NeatooFactory.Remote`) and wires the client's
  `ServerServiceProvider` to the server scope; `MakeRemoteDelegateRequest` (`:16-72`) mimics
  the full HTTP path including double JSON round-trips. Scopes are per-test; containers are
  static and shared.
- **DI registration is by naming convention** — `AutoRegisterAssemblyTypes` (`:118-140`) maps
  `IFoo` → `Foo` transiently for every class in the test assembly. A test repository will be
  auto-registered if named to match, but **transient**, so the store itself must be static
  (which also makes it visible to both containers, correctly mimicking one database).
- **Coverage gap confirmed.** `.Save(` appears in exactly one Neatoo.UnitTest file
  (`Unit/Core/EntityBaseStateTests.cs`, and only for the three guard-exception paths).
  `Unit/Core/EntityListBaseTests.cs` calls `list.FactoryComplete(FactoryOperation.Update)`
  directly to "simulate save completion" — no factory, no round trip.
- **No existing test aggregate fits.** `Integration/Aggregates/Person` has `[Fetch]`-only
  entities (`PersonEntityBase.cs:44`); `Concepts/Serialization/EntityObject.cs:84-90` has a
  combined `[Update]`/`[Insert]` that mutates `Name` and no child persistence. Neither
  exercises the canonical list lifecycle, so ISNEW-002 adds a purpose-built aggregate rather
  than retrofitting these (retrofitting would also violate the sacred-tests rule).
- **Two-container meta-state assertions already exist** for flat entities in
  `Integration/Concepts/Serialization/TwoContainerMetaStateTests.cs` — the new tests
  complement them at aggregate scope and should not duplicate them.

---

## Test Evidence

All cited tests live in
`src/Neatoo.UnitTest/Integration/Aggregates/SaveLifecycle/AggregateSaveLifecycleTests.cs`
(new, this plan), running on `ClientServerTestBase` — real two-container round trip, all
assertions against the client's post-save copy.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| Created aggregate + children saved across boundary, old/clean with Ids | `[integration]` | `AggregateSaveLifecycleTests.CreateWithLines_Save_InsertsGraph_ClientCopyIsOldAndClean` | ✓ |
| Fetch + modify/add/remove, each route once, comes back clean | `[integration]` | `AggregateSaveLifecycleTests.FetchModifyAddRemove_Save_RoutesEachPathOnce_ClientCopyIsClean` | ✓ |
| Unmodified fetched aggregate rejected by save guard | `[integration]` | `AggregateSaveLifecycleTests.FetchedUnmodified_Save_ThrowsNotModified` | ✓ |
| Fresh create is savable and inserts | `[integration]` | `AggregateSaveLifecycleTests.RichCreate_Untouched_IsSavableFromIsNewAlone_AndSaveInserts` | ✓ |
| Removing a never-persisted child issues no delete | `[integration]` | `AggregateSaveLifecycleTests.RemoveNeverPersistedLine_Save_IssuesNoDelete` | ✓ |
| Build + full suite green | `[explicit-skip]` | Gate logs `reviews/002-*.log` (2153 sln + 113 Design, 2 pre-existing skips) | — |

Added in the test-review loop (2026-08-21, closing `reviews/002-test-review.md` must-covers
1-3 and should-covers 4-6):

| Behavior (from review findings) | Tier | Test method | Tier confirmed |
|---|---|---|---|
| The save genuinely crosses the wire (client holds deserialized instances) | `[integration]` | `AreNotSame` assertions in `CreateWithLines_Save_...` and `FetchModifyAddRemove_Save_...` | ✓ |
| Savability of an untouched create comes from `IsNew` alone, not property dirt | `[integration]` | `RichCreate_Untouched_IsSavableFromIsNewAlone_AndSaveInserts` (new `[Create]` overload populates inside the paused op) | ✓ |
| A user-attached new child alone makes a clean fetched parent modified + savable, and its insert is not skipped | `[integration]` | `FetchedRoot_AddOneNewChild_IsModifiedAndSavable_AndChildInserts` | ✓ |
| Removal alone makes a clean fetched parent modified + savable | `[integration]` | `FetchedRoot_RemoveOneChild_IsModifiedAndSavable_AndChildDeletes` | ✓ |
| Second save on a saved graph is refused (no duplicate insert) | `[integration]` | `SavedAggregate_SecondSave_ThrowsNotModified` | ✓ |
| Pre-save created-aggregate state; factory-populated children insert | `[integration]` | assertions in `CreateWithLines_Save_...` and `RichCreate_Untouched_...` | ✓ |
| Update-path insert Id writeback reaches the client copy | `[integration]` | assertion added to `FetchModifyAddRemove_Save_...` | ✓ |

Extra coverage beyond the bullets: `FetchModifyRoot_Save_UpdatesHeader` pins the root-header
write path and the unmodified-children skip in the positive direction.

---

## Plan Amendments

### 2026-08-21 — Rich-create overload added so savability could be pinned honestly

- **Section affected:** Step 5, Acceptance bullet 4
- **Original said:** cover "savability of a fresh create" as a pre-flip anchor.
- **What changed:** The aggregate gained a rich `[Create]` overload (`CreateForCustomer`,
  plus `InvoiceLineList.CreateWithStandardLines`) that populates the root and its children
  *inside* the paused factory operation. The test was renamed accordingly.
- **Why:** The first version set `Customer` with a setter before asserting, so `IsSavable`
  was delivered by ordinary property dirt — it would have passed post-flip even if the new
  `|| IsNew` term in `IsSavable` were forgotten, which is the exact regression the bullet
  exists to catch (`reviews/002-test-review.md` must-cover 2). `Customer` is `[Required]`,
  so an untouched plain create is invalid and unsavable; only a paused rich create yields a
  valid aggregate with zero property dirt. Bonus: it also exercises the
  factory-populated-children case design.md names as the motivating scenario.
- **Discovery Log link:** 2026-08-21 — ISNEW-002 (test review)

### 2026-08-21 — Boundary proof added; static store made the round trip unverifiable

- **Section affected:** Constraints (the `[Remote]` bullet), Acceptance
- **Original said:** `[Remote]` on the root's persistence operations "so the save genuinely
  crosses the boundary" — treated as guaranteed by construction.
- **What changed:** `AreNotSame` assertions on the post-save root and a child now prove the
  client holds deserialized instances.
- **Why:** Because `SaveLifecycleStore` is static and visible to both containers, every test
  would have passed identically with the operations running in-process — dropping `[Remote]`
  broke no assertion. The flip depends on state (`IsMarkedModified`) riding the wire, so an
  unproven boundary would have hidden serialization regressions.
- **Discovery Log link:** 2026-08-21 — ISNEW-002 (test review)
