# Internal Factory Methods

**Status:** Complete
**Priority:** High
**Created:** 2026-03-07
**Last Updated:** 2026-03-07

---

## Problem

Neatoo's child entity factory methods ([Insert], [Update], [Delete]) are currently `public`, meaning they appear on the generated factory interface and are visible to client code. Child entity factory methods are only called within the aggregate on the server — they should not be callable from the client.

## Solution

Make child entity factory methods `internal` to take advantage of RemoteFactory 0.20.0's internal factory method visibility feature:

- `internal` factory methods get `IsServerRuntime` guards (server-only, trimmable)
- When all methods on a class are `internal`, the generated factory interface becomes `internal` (hidden from client DI)
- This aligns with Neatoo's existing pattern of `internal` entity classes with matched `public` interfaces

Reference: RemoteFactory v0.17.0 release notes — "Internal Factory Visibility & IL Trimming"

---

## Clarifications

---

## Requirements Review

**Reviewer:** neatoo-requirements-reviewer
**Reviewed:** 2026-03-07
**Verdict:** APPROVED (with constraints)

### Relevant Requirements Found

**Source 1: Design Projects (Design.Domain / Design.Tests)**

1. **Child entities do NOT have [Remote] on persistence methods (DESIGN DECISION).**
   `src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItem.cs` lines 88-106: "DID NOT DO THIS: Give child entities independent [Remote] persistence. ... Child entities are part of the aggregate - persistence goes through root." This means child [Insert]/[Update]/[Delete] methods are already intended to be server-only. Making them `internal` formalizes this intent at the language level.

2. **Child entities DO have [Create] methods that run locally.**
   `src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItem.cs` lines 49-57: OrderItem has two `[Create]` overloads (parameterless and with product data). These are called on the client side (e.g., `itemFactory.Create("Widget", 5, 10.00m)`) before `order.Items.Add(item)`. The [Create] methods must remain `public` to keep the factory interface public so client code can call them.

3. **Child entities sometimes have [Fetch] methods without [Remote].**
   `src/Design/Design.Domain/CommonGotchas.cs` lines 194-199: Gotcha2Item has `[Fetch] public void Fetch(int id)` without [Remote]. This is used by the parent list factory (`Gotcha2ItemList.FetchForParent`) to create child items during fetch. Similarly, `src/Examples/Person/Person.DomainModel/PersonPhone.cs` line 61-65: `[Fetch] public void Fetch(PersonPhoneEntity personPhoneEntity)`. These child [Fetch] methods are also server-only and could be made internal.

4. **Parent factory methods inject child factory interfaces as [Service] parameters.**
   `src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs` lines 101, 113-114: Order's `[Create]` injects `[Service] IOrderItemListFactory` and `[Service] IOrderItemFactory`. Order's `[Remote][Fetch]` injects both child factory interfaces. These parent methods are `public`. If the child factory interface becomes `internal`, this would trigger CS0051 ("inconsistent accessibility: parameter type is less accessible than method"). The RemoteFactory skill explicitly documents this constraint: `~/.claude/skills/RemoteFactory/references/class-factory.md` lines 319: "CS0051 constraint: When a generated factory interface is internal, it cannot be used as a [Service] parameter type in a public method on another class."

5. **The "Entity Duality" pattern requires public factory methods.**
   `src/Design/Design.Domain/FactoryOperations/RemoteBoundary.cs` lines 224-295: DualUseEntity demonstrates an entity that can be an aggregate root OR a child. Its [Remote][Fetch]/[Insert]/[Update]/[Delete] methods are `public` because when used as a root, they are called through the factory interface from the client. Similarly, `src/Design/Design.Domain/Entities/Address.cs` lines 97-127 shows Address with `[Remote][Insert]`/`[Update]`/`[Delete]` that exist "for when Address is used as an aggregate root (entity duality - same class, different contexts)." Making these internal would break the root usage path.

6. **The cascade save pattern uses child factory interfaces injected into parent [Insert]/[Update] methods.**
   `src/samples/EntitiesSamples.cs` lines 315, 332: `[Service] IEntitiesCascadeItemFactory itemFactory` in parent's `[Insert]` and `[Update]`. `src/Examples/Person/Person.DomainModel/Person.cs` lines 87, 111: `[Service] IPersonPhoneListFactory personPhoneModelListFactory` in Person's `[Remote][Insert]` and `[Remote][Update]`. The parent's methods are `public`. If the child's factory interface becomes `internal`, the parent cannot accept it as a `[Service]` parameter in a `public` method.

7. **[Remote] on internal methods is a diagnostic error (NF0105).**
   `~/.claude/skills/RemoteFactory/references/anti-patterns.md` lines 263-285: Anti-pattern #11 explicitly states `[Remote]` and `internal` are contradictory. The generator emits NF0105 diagnostic. This confirms: child methods that are made `internal` must NOT have `[Remote]`.

8. **RemoteFactory skill Quick Decisions table endorses internal for child entity methods.**
   `~/.claude/skills/RemoteFactory/SKILL.md` line 38: "Should child entity methods be internal? Yes - server-only, trimmable, invisible to client."

9. **Behavioral contracts from Design.Tests remain unaffected.**
   WHEN OrderItem is created and added to Order's Items, THEN item.IsChild==true (`src/Design/Design.Tests/AggregateTests/OrderAggregateTests.cs` AddItem_ItemBecomesChild). WHEN IOrderItem is accessed through IEntityBase, THEN IsSavable and Save() are not available (`src/Design/Design.Tests/AggregateTests/EntityRootInterfaceTests.cs` ChildInterface_DoesNotExposeIsSavable). These behavioral contracts are about the entity interfaces, not factory visibility, and are unaffected by this change.

10. **IL trimming supports the internal pattern.**
    `~/.claude/skills/RemoteFactory/references/trimming.md` lines 33-34: "internal (no [Remote]): Yes [guarded]. Trimmed on client. Server-only." This confirms internal factory methods get `IsServerRuntime` guards and are trimmable.

**Source 2: Framework Source Comments (src/Neatoo/)**

No DESIGN DECISION markers in `src/Neatoo/` relate directly to factory method visibility. The framework source does not constrain this change.

**Source 3: User-Facing Documentation (docs/)**

`docs/release-notes/v0.17.0.md`: The IEntityRoot release removed IsSavable from child entity interfaces. The design philosophy of preventing client access to child persistence operations is the same motivation behind this todo. No contradictions.

**Source 4: Skills**

`~/.claude/skills/neatoo/SKILL.md` line 97: IsSavable formula includes `!IsChild`. `~/.claude/skills/neatoo/references/entities.md` "Aggregate Save Cascading" section: Parent's [Insert]/[Update] must call `childFactory.SaveAsync()` on children, demonstrating the pattern where parent public methods inject child factory interfaces. `~/.claude/skills/neatoo/references/pitfalls.md`: "Adding [Remote] to child entity factory methods: Unnecessary - child methods are called from server. Only use [Remote] on aggregate root entry points." This aligns with making child methods internal.

### Gaps

1. **No existing requirement defines which factory methods MUST remain `public` on child entities vs. which CAN become `internal`.** The Design project shows child entities with `[Create]` (public, local) and persistence methods (could be internal), but there is no explicit documented rule about the boundary. The architect must establish clear criteria.

2. **No existing test verifies factory interface visibility.** Design.Tests tests resolve factory interfaces from DI (`GetRequiredService<IOrderItemFactory>()`) but do not assert visibility. After this change, child factories whose interfaces become internal would no longer be resolvable from test code via `GetRequiredService<IChildFactory>()` if the test assembly cannot see internal types. The architect must address test accessibility (e.g., `InternalsVisibleTo`).

3. **No documented pattern for the "mixed visibility" case** where a child entity has BOTH `public` [Create] methods AND `internal` [Insert]/[Update]/[Delete] methods. The RemoteFactory skill documents the "all internal" case (factory interface becomes internal) but not the "mixed" case where some methods are public and some are internal.

### Contradictions

None found. The proposal aligns with all existing requirements. The critical CS0051 constraint is already documented in the RemoteFactory skill and is a technical constraint to navigate, not a contradiction with the proposal's intent.

### Recommendations for Architect

1. **Separate the scope into two categories:**
   - **Pure child entities** (only used as children, never as roots): OrderItem, SaveDemoItem, Gotcha2Item, Gotcha5Child. These can have [Insert]/[Update]/[Delete] made `internal`. But note: if [Create] stays `public`, the factory interface stays `public` (mixed visibility). Only if ALL methods are internal does the interface become internal.
   - **Dual-use entities** (can be root or child): DualUseEntity, Address. These MUST keep [Insert]/[Update]/[Delete] as `public` with `[Remote]` because they need the factory interface accessible to clients when used as aggregate roots.

2. **Address the CS0051 constraint for the cascade save pattern.** Parent entities' `[Create]`/`[Fetch]` methods inject child factory interfaces via `[Service]`. If the child factory interface becomes `internal`, the parent's `public` method cannot reference it. Two approaches:
   - Keep child factory interfaces `public` (mixed visibility: some methods public, some internal). Verify RemoteFactory 0.20.0 supports this and that internal methods get the `IsServerRuntime` guard while public methods do not.
   - Make the parent methods that inject child factories also `internal`. But this is only viable if those parent methods are already server-only (have `[Remote]`). Check: Order.Create (no [Remote]) injects IOrderItemListFactory -- this parent method MUST stay public and local.

3. **Verify against these Design project files after implementation:**
   - `src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItem.cs` -- child [Create] stays public, [Insert]/[Update]/[Delete] become internal (if applicable)
   - `src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs` -- parent methods must still compile when injecting child factory interfaces
   - `src/Design/Design.Domain/FactoryOperations/RemoteBoundary.cs` (DualUseEntity) -- must NOT change, methods stay public with [Remote]
   - `src/Design/Design.Domain/Entities/Address.cs` -- must NOT change, dual-use entity
   - `src/Design/Design.Domain/FactoryOperations/SavePatterns.cs` (SaveAggregateDemo) -- parent Insert injects ISaveDemoItemFactory; test CS0051
   - `src/Design/Design.Domain/FactoryOperations/FetchPatterns.cs` (FetchDemoItem) -- child with [Remote][Insert/Update/Delete]; these have [Remote] so making them internal would trigger NF0105. Must remove [Remote] first.
   - `src/Examples/Person/Person.DomainModel/PersonPhone.cs` -- child entity in real example; Person.cs injects IPersonPhoneListFactory in public [Remote] methods

4. **Note the "OrderItem has no [Insert]/[Update]/[Delete]" observation.** In the Design project's OrderAggregate, OrderItem does NOT have its own [Insert]/[Update]/[Delete] methods -- the parent Order handles all child persistence inline. Only the factory generates `IOrderItemFactory` with `Create()` methods. This means for OrderItem specifically, there are no persistence methods to make internal. The affected entities are those that follow the cascade save pattern with `childFactory.SaveAsync()` (FetchDemoItem, Gotcha2Item, Gotcha5Child, SaveDemoItem in Design; PersonPhone in Examples; EntitiesCascadeItem in samples).

5. **For child entities whose [Insert]/[Update]/[Delete] currently have [Remote], remove [Remote] first** before making them internal. `[Remote]` + `internal` = NF0105 diagnostic error. Affected: FetchDemoItem (has `[Remote][Insert/Update/Delete]`), Address (dual-use -- do not change).

6. **Update the RemoteFactory skill** (`~/.claude/skills/RemoteFactory/references/class-factory.md`) and Neatoo skill (`~/.claude/skills/neatoo/references/entities.md`, `pitfalls.md`) to document the new pattern after implementation.

---

## Plans

- [Internal Factory Methods Design](../plans/internal-factory-methods-design.md)

---

## Tasks

- [x] Architect comprehension check
- [x] Business requirements review
- [x] Architect plan creation & design
- [x] Developer review
- [x] Implementation
- [x] Verification
- [x] Documentation
- [x] Completion

---

## Progress Log

### 2026-03-07
- Created todo after upgrading RemoteFactory from 0.16.1 to 0.20.0
- Build and all tests pass with the upgrade
- Architect comprehension check: Ready (no questions)
- Business requirements review: APPROVED with constraints
- Architect plan created: docs/plans/internal-factory-methods-design.md
- Developer review: Concerns raised (3 concerns, 1 blocking)
- Developer re-review: All 3 concerns resolved by user clarifications. Approved with implementation contract.
- Implementation Phase 1 (RemoteFactory 0.20.0): Partial — Design.Domain child entities Insert/Update/Delete made internal. Fetch and PersonPhone/PersonPhoneList deferred due to RemoteFactory 0.20.0 removing internal methods from generated interfaces.
- RemoteFactory 0.20.1 published: fixes interface limitation — internal methods emitted as `internal` interface members.
- Implementation Phase 2 (RemoteFactory 0.20.1): Full — ALL child entity factory methods made internal (Fetch/Insert/Update/Delete on Gotcha2Item, Gotcha5Child, FetchDemoItem, Gotcha2ItemList, PersonPhone, PersonPhoneList).
- KnockOff test fix: Converted `IPersonPhoneListFactory` from inline stub to standalone `internal` stub.
- Full solution: 0 build errors, 2083 tests passed.
- Architect verification: VERIFIED. Independent build (0 errors, 0 warnings) and test run (2083 passed, 1 skipped, 0 failed). All implementation matches design. Two minor observations noted (Completion Evidence narrative inaccuracy about Gotcha2Item Fetch visibility; LocalFetch guard difference between Design.Domain and Person.DomainModel factories). Neither is blocking.
- Requirements verification: REQUIREMENTS SATISFIED. All 20 requirements traced, zero contradictions, zero unintended side effects.
- Requirements documentation: Updated `skills/neatoo/references/entities.md` with "Child Entity Factory Method Visibility" section. No developer deliverables needed.
- Todo completed.

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors, 0 warnings
- Tests: 2083 passed, 1 skipped (pre-existing), 0 failed

---

## Results / Conclusions

All child entity factory methods ([Fetch], [Insert], [Update], [Delete]) made `internal` on pure child entities across Design.Domain and Person example. [Create] methods remain `public`, keeping factory interfaces public (mixed visibility). PersonPhoneList's factory interface became fully `internal` (no public methods). Generated code shows IsServerRuntime guards on all internal method implementations, enabling IL trimming on the client. Dual-use entities (Address, DualUseEntity) and root entities were not changed. RemoteFactory upgraded from 0.16.1 to 0.20.1 to support internal interface members.
