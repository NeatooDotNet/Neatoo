# Entities Demo Aggregate (Employee/Address) to Canonical Lifecycle

**Plan #:** 007
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** Done (both gates closed — see reviews/007-*)
**Last Updated:** 2026-08-21
**Plan-review opt-in:** No (same work-shape as ISNEW-001, whose canonical target was verified against the Person example and framework source; blast radius is Design projects only)
**Code-review opt-in:** Yes (Design.Domain is the authoritative reference; this file set currently teaches rules that contradict ISNEW-001's)

---

## Scope

Rework the Entities demo aggregate (`Employee`, `Address`, `AddressList`) to the canonical
factory lifecycle established by ISNEW-001, and rewrite the design-narrative blocks that
currently reject that pattern. Add lifecycle test coverage in the `AggregateLifecycleTests`
style, including the test infrastructure this aggregate has never had. Does NOT touch Neatoo
library code, does NOT change IsModified/IsNew semantics (ISNEW-004).

---

## Intent

Design.Domain is the authoritative reference, and it currently contains two file sets
teaching opposite rules about the same lifecycle: OrderAggregate/FetchPatterns/SavePatterns
now demonstrate list-`[Fetch]` and per-item factory saves, while Entities demonstrates
Create+LoadValue child loading, direct-repository child writes, a phantom deleted-items
helper, and DID-NOT-DO blocks explicitly rejecting the canonical shape. After this plan a
reader gets one consistent answer regardless of which file they open, and the Employee
aggregate's persistence state is correct at every lifecycle stage.

---

## Framework & Architectural Alignment

- Person-example canonical lifecycle, as applied in ISNEW-001: list `[Fetch]` populates via
  the item factory's `[Fetch]` (paused adds); list `[Update]` routes surviving children
  through the item factory's `Save` and deletes removed ones; the root delegates child
  persistence to the list factory in both `[Insert]` and `[Update]`.
- Per-object factory lifecycle — hooks fire on the single factory target; no graph cascade.
- Interface-first design; DDD documentation conventions.

---

## Constraints & Invariants

- No changes under `src/Neatoo/`.
- Existing Design.Tests keep their intent (the aggregate currently has none of its own).
- Comments describe **current** (pre-flip) semantics truthfully; ISNEW-004/005 sweep again.
- `Address`'s dual-role commentary (child vs. standalone root) must survive the rework —
  the entity-duality point it teaches is legitimate and unrelated to the lifecycle defect.
- The Employee aggregate keeps its own repository interface shape (tuple-returning), like
  OrderAggregate; no EF-style entity collections.

---

## Steps

1. Give `Address` a `[Fetch]` and parent-scoped local `[Insert]`/`[Update]` so children load
   and persist through their own factory, preserving the entity-duality commentary about its
   standalone-root role.
2. Give `AddressList` a `[Fetch]` that populates via the address factory, and an `[Update]`
   that deletes removed children and routes survivors through per-item factory saves.
3. Rework `Employee`'s `[Fetch]`, `[Insert]`, and `[Update]` to delegate child loading and
   persistence to the list factory; remove the phantom deleted-items helper.
4. Rewrite the design-narrative blocks that reject the canonical pattern (`AddressList`'s
   no-Fetch / no-persistence DID-NOT-DO blocks, `Address`'s no-Fetch block) so they teach
   the canonical shape, and correct every lifecycle claim contradicted by framework behavior.
5. Add test infrastructure for this aggregate (recording repository mocks, DI registration)
   and lifecycle tests covering fetch state, both save paths, and child delete routing.

---

## Acceptance

- [x] After a factory Fetch of an employee with addresses, every address reports
      `IsNew=false` / `IsModified=false` and the aggregate reports `IsModified=false`
      `[integration]`
- [x] After `Save()` of a fetched employee with a modified address, an added address, and a
      removed address, each route fires exactly once and the whole graph is clean
      `[integration]`
- [x] After `Save()` of a created employee with addresses, all children are inserted with
      generated Ids landing on the entities, and the graph is old and clean `[integration]`
- [x] No file in `Design.Domain/Entities/` teaches a lifecycle rule contradicted by framework
      behavior or by the canonical pattern in OrderAggregate `[explicit-skip: prose
      consistency — verified by the opted-in code review, not a test]`
- [x] Build and full Design.Tests suite green `[explicit-skip: meta-bullet, gate run]`

---

## Current State (Pre-Flight)

Walked 2026-08-21 before the first edit:

- `Employee.cs:124-158` — `[Fetch]` wraps its body in `using (PauseAllActions())` (resumes
  early on dispose; the op already runs paused) and builds addresses via
  `addressFactory.Create()` + `LoadValue` + `Add`, so fetched addresses end `IsNew=true`.
  The `:157` comment claims the opposite. Adds happen on a resumed list, so `MarkAsChild`
  does fire (matching `:154`).
- `Employee.cs:166-184` / `:192-224` — Insert and Update write child rows directly via the
  repository; no child factory involvement, so nothing marks children unmodified/old.
  `:221` calls `ProcessDeletedAddresses`, whose body (`:226-231`) is comment-only — deleted
  addresses are never persisted as deletions. `:223` claims `FactoryComplete(Update)` clears
  the DeletedList (it does not — `AddressList` is never a factory target).
- `Address.cs:79-95` — DID-NOT-DO block rejecting child `[Fetch]`. `:97-127` — `[Remote]`
  `[Insert]/[Update]/[Delete]` exist only for the standalone-root role, taking
  `IAddressOnlyRepository`; the entity-duality point at `:103-104` is worth keeping.
  `:146-149` repeats the false cascade claim.
- `AddressList.cs:49-70` — DID-NOT-DO blocks rejecting list `[Fetch]` and list persistence
  ops (both now canonical). `:89-94` repeats the false FactoryComplete-clears claim.
  `:144-145` documents the duplicated-aggregate-name exception message (tracked in
  ISNEW-006).
- Tests/DI: the aggregate has **no** test coverage and **no** repository registration —
  `IEmployeeRepository` and `IAddressOnlyRepository` appear nowhere in Design.Tests, so
  test infrastructure must be added before any test can resolve the factories.
- Interfaces (`IEntityInterfaces.cs:19,37,51`) are already correct: `IEmployee : IEntityRoot`,
  `IAddress : IEntityBase`, `IAddressList : IEntityListBase<IAddress>`.

---

## Test Evidence

All cited tests live in
`src/Design/Design.Tests/AggregateTests/EmployeeAggregateLifecycleTests.cs` (new file, this
plan). Tier note: Design.Tests run a single-container server scope — real factories, real
Save routing, stubbed repositories, no serialization boundary (two-container variants are
ISNEW-002's scope).

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| Fetched addresses old + clean, aggregate not modified | `[integration]` | `EmployeeAggregateLifecycleTests.Fetch_AddressesAreOldAndClean_AggregateNotModified` | ✓ |
| Modify/add/remove then Save — each route once, graph clean | `[integration]` | `EmployeeAggregateLifecycleTests.FetchModifyAddRemove_Save_RoutesAllPathsAndGraphIsClean` | ✓ |
| Create with addresses then Save — inserts + Id writeback, graph old/clean | `[integration]` | `EmployeeAggregateLifecycleTests.CreateWithAddresses_Save_InsertsAllWithIdWriteback_AndGraphIsClean` | ✓ |
| No Entities file teaches a contradicted rule | `[explicit-skip]` | Covered by opted-in code review | — |
| Build + Design.Tests green | `[explicit-skip]` | Gate logs `reviews/007-*.log` (113/113) | — |

---

## Plan Amendments

### 2026-08-21 — The "preserve Address's dual-role commentary" constraint was wrong

- **Section affected:** Constraints & Invariants (the `Address` dual-role bullet), Step 1
- **Original said:** Address's entity-duality commentary "must survive the rework — the
  point it teaches is legitimate and unrelated to the lifecycle defect."
- **What changed:** The standalone-root role was **removed entirely** — its `[Remote]`
  operations, `IAddressOnlyRepository`, and the mock — and replaced with a
  `NO STANDALONE-ROOT OPERATIONS` rejected-pattern block. The duality teaching moved to
  `FactoryOperations/RemoteBoundary.cs`, reframed around the remote/local boundary.
- **Why:** The code review (`reviews/007-code-review.md`, vetoes 1-2) established the
  premise was false on two counts. (a) The role was unreachable: `IAddress : IEntityBase` is
  how the framework declares "child, not root", so no consumer can save it as one — the
  generated root-role Save threw `NotImplementedException` for insert/update. (b) Worse, it
  was actively harmful: any parent-less `[Remote]` operation makes the generator emit a
  **public** `Save(IAddress target)`, letting a consumer persist or delete a child outside
  the aggregate's save flow — a boundary hole the canonical `IOrderItemFactory` does not
  have. Mid-loop I first tried the opposite fix (completing the role with Fetch/Insert/
  Update), which would have widened the hole; reverted before it landed.
- **Discovery Log link:** 2026-08-21 — ISNEW-007 (code review: Address standalone role)
