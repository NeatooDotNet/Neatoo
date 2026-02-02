# Design Source of Truth - Implementation Plan

**Date:** 2026-01-31
**Related Todo:** [Create Design Source of Truth Projects](../todos/design-source-of-truth.md)
**Status:** Complete
**Last Updated:** 2026-01-31 (All Phases Implemented)
**Reviewed By:** neatoo-architect (2026-01-31), neatoo-developer (2026-01-31)

---

## Overview

Create a set of C# projects in `src/Design/` that serve as the authoritative reference for Neatoo's API design. These projects are specifically designed for Claude Code to understand, reason about, and extend the API.

---

## Approach

Build interconnected projects that demonstrate Neatoo's full API surface:

1. **Design.Domain** - Entity definitions demonstrating all base classes and patterns
2. **Design.Infrastructure** - Sample repository/persistence implementations
3. **Design.Tests** - Tests showing usage patterns and API contracts

Each project will be heavily commented with four types of annotations:
- **API documentation** - What this code demonstrates
- **Design rationale** - Why this approach was chosen
- **Rejected alternatives** - What was NOT done and why (often with commented-out code)
- **Generator behavior** - What code the source generator produces

The projects must demonstrate all **four base classes** and all **factory operations**.

---

## Design

### Directory Structure

```
src/Design/
├── Design.sln
├── README.md                    # Explains purpose for humans
├── CLAUDE-DESIGN.md            # Detailed guidance for Claude Code
├── Design.Domain/
│   ├── Design.Domain.csproj
│   ├── BaseClasses/
│   │   └── AllBaseClasses.cs   # Side-by-side comparison of all 4 base classes
│   ├── Aggregates/
│   │   └── OrderAggregate/
│   │       ├── Order.cs        # Aggregate root (EntityBase)
│   │       ├── OrderItem.cs    # Child entity (EntityBase with IsChild=true)
│   │       └── OrderItemList.cs # Child collection (EntityListBase)
│   ├── Entities/
│   │   ├── Employee.cs         # EntityBase example with full CRUD
│   │   ├── Address.cs          # Child entity example
│   │   └── AddressList.cs      # EntityListBase example
│   ├── ValueObjects/
│   │   ├── EmployeeListItem.cs # ValidateBase for value objects
│   │   └── EmployeeList.cs     # ValidateListBase example
│   ├── Commands/
│   │   └── ApproveEmployee.cs  # Static [Execute] command pattern
│   ├── FactoryOperations/
│   │   ├── CreatePatterns.cs   # [Create] demonstrations
│   │   ├── FetchPatterns.cs    # [Fetch] demonstrations
│   │   ├── SavePatterns.cs     # [Insert]/[Update]/[Delete] demonstrations
│   │   └── RemoteBoundary.cs   # [Remote] and client-server boundary
│   ├── PropertySystem/
│   │   ├── PropertyBasics.cs   # Getter<T>/Setter patterns
│   │   └── StateProperties.cs  # IsModified, IsNew, IsValid, etc.
│   ├── Rules/
│   │   ├── RuleBasics.cs       # RuleBase<T> synchronous rules
│   │   ├── AsyncRules.cs       # AsyncRuleBase<T> async rules
│   │   └── FluentRules.cs      # RuleManager fluent API
│   ├── Generators/
│   │   └── TwoGeneratorInteraction.cs  # Neatoo.BaseGenerator + RemoteFactory
│   └── DI/
│       ├── ServiceRegistration.cs      # How to register Neatoo services
│       └── ServiceContracts.cs         # IValidateBaseServices, IEntityBaseServices
├── Design.Infrastructure/
│   ├── Design.Infrastructure.csproj
│   └── Persistence/
│       ├── EmployeeEntity.cs   # Database entity (DTO)
│       └── IEmployeeDb.cs      # Repository interface
└── Design.Tests/
    ├── Design.Tests.csproj
    ├── BaseClassTests/
    │   ├── EntityBaseTests.cs
    │   ├── ValidateBaseTests.cs
    │   ├── EntityListBaseTests.cs
    │   └── ValidateListBaseTests.cs
    ├── AggregateTests/
    │   ├── OrderAggregateTests.cs      # Complete aggregate lifecycle
    │   └── DeletedListTests.cs         # DeletedList behavior
    ├── FactoryTests/
    │   ├── CreateTests.cs
    │   ├── FetchTests.cs
    │   └── SaveTests.cs
    ├── PropertyTests/
    │   ├── PropertyBasicsTests.cs
    │   └── StatePropertyTests.cs
    └── RuleTests/
        ├── SyncRuleTests.cs
        ├── AsyncRuleTests.cs
        └── FluentRuleTests.cs
```

### Comment Standards

#### API Documentation Comments

```csharp
/// <summary>
/// Demonstrates: EntityBase<T> for persistent domain entities.
///
/// Key points:
/// - Inherits from ValidateBase<T> with modification tracking
/// - IsNew/IsDeleted/IsModified track persistence state
/// - Save() routes to Insert/Update/Delete based on state
/// - IsSavable = IsModified && IsValid && !IsBusy && !IsChild
/// </summary>
```

#### Design Rationale Comments

```csharp
// DESIGN DECISION: We use [Remote] on factory methods, not on the class.
// - Entry point marking: Client code knows which operations require server
// - Granular control: Some operations ([Create]) can be local-only
// - Clear boundary: Once crossed, execution stays on server
//
// See: RemoteFactory source generator for implementation
```

#### Rejected Alternative Comments

```csharp
// DID NOT DO THIS: Mark entire class as [Remote]
//
// Reasons:
// 1. [Create] often can run locally (no persistence yet)
// 2. Would require separate client/server class definitions
// 3. Our approach: mark individual methods that need server execution
//
// REJECTED PATTERN:
// [Remote]
// public class Employee : EntityBase<Employee> { ... }
//
// ACTUAL PATTERN:
// public class Employee : EntityBase<Employee>
// {
//     [Create] public void Create() { }  // Local OK
//     [Remote][Fetch] public Task<bool> Fetch(...) { }  // Server required
// }
```

#### Generator Behavior Comments

```csharp
// GENERATOR BEHAVIOR: For this partial property:
//
// public partial string? FirstName { get; set; }
//
// The Neatoo.BaseGenerator produces:
//
// private IEntityProperty<string?> _firstNameProperty;
// public string? FirstName
// {
//     get => _firstNameProperty?.Value;
//     set => _firstNameProperty.SetValue(value);
// }
//
// The RemoteFactory generator produces:
// - Factory interface: IEmployeeFactory with Create(), Fetch(), etc.
// - Factory implementation: HTTP client proxies for [Remote] methods
```

#### Common Mistake Comments

```csharp
// COMMON MISTAKE: Calling Save() on child entities
//
// WRONG:
// var employee = await EmployeeFactory.Fetch(id);
// employee.Addresses[0].City = "Seattle";
// await employee.Addresses[0].Save();  // Throws: child entities can't save independently
//
// RIGHT:
// await employee.Save();  // Parent save persists all child changes
//
// Why: Child entities are part of the aggregate and persist through the root.
// IsSavable returns false for child entities (IsChild = true).
```

#### Priority/State Machine Comments

```csharp
// STATE MACHINE: Entity persistence states
//
// New Entity: IsNew=true, IsModified=true
//     → Save() calls [Insert] factory method
//     → After success: MarkOld(), MarkUnmodified()
//
// Existing Modified: IsNew=false, IsModified=true, IsDeleted=false
//     → Save() calls [Update] factory method
//     → After success: MarkUnmodified()
//
// Deleted: IsDeleted=true (regardless of IsNew)
//     → If IsNew=true: No persistence (never existed)
//     → If IsNew=false: Save() calls [Delete] factory method
//     → After success: Object typically discarded
//
// Unmodified: IsModified=false
//     → IsSavable=false, Save() is no-op
```

### API Coverage Checklist

The design projects must demonstrate ALL of these:

**Four Base Classes:**
- [ ] EntityBase<T> - Persistent entities with full CRUD lifecycle
- [ ] ValidateBase<T> - Value objects, read models, validation-only objects
- [ ] EntityListBase<I> - Collections of child entities within an aggregate
- [ ] ValidateListBase<I> - Collections of read models

**Factory Operations:**
- [ ] `[Create]` - Initialize new object
- [ ] `[Fetch]` - Load existing data
- [ ] `[Insert]` - Persist new object
- [ ] `[Update]` - Persist changes
- [ ] `[Delete]` - Remove object
- [ ] `[Execute]` - Run command

**Attributes:**
- [ ] `[Factory]` - Class-level source generation marker
- [ ] `[SuppressFactory]` - Prevent factory generation
- [ ] `[Remote]` - Client-to-server boundary marker
- [ ] `[Service]` - Dependency injection marker (constructor vs method)
- [ ] `[AuthorizeFactory<T>]` - Authorization enforcement
- [ ] `[FactoryMode]` - Assembly-level Full vs RemoteOnly

**Property System:**
- [ ] Partial property declarations with attributes
- [ ] `Getter<T>()` and `Setter()` patterns
- [ ] `IValidateProperty` and `IEntityProperty` interfaces
- [ ] `LoadValue()` vs `SetValue()` distinction
- [ ] Validation attributes (`[Required]`, `[StringLength]`, etc.)

**State Properties:**
- [ ] `IsValid` / `IsSelfValid` - Validation state
- [ ] `IsModified` / `IsSelfModified` - Modification state
- [ ] `IsNew` - Persistence state (new vs existing)
- [ ] `IsDeleted` - Deletion marking
- [ ] `IsSavable` - Can entity be saved
- [ ] `IsBusy` - Async operations pending
- [ ] `IsChild` - Part of parent aggregate
- [ ] `Root` - Aggregate root reference
- [ ] `Parent` - Parent in hierarchy

**Validation Rules:**
- [ ] `RuleBase<T>` - Synchronous validation
- [ ] `AsyncRuleBase<T>` - Async validation
- [ ] `RuleManager.AddValidation()` - Fluent sync validation
- [ ] `RuleManager.AddValidationAsync()` - Fluent async validation
- [ ] `RuleManager.AddAction()` - Fluent sync action
- [ ] `RuleManager.AddActionAsync()` - Fluent async action
- [ ] Rule trigger properties (TriggerProperties)
- [ ] Rule execution order (RuleOrder)
- [ ] `IRuleMessages` return pattern

**Control Flow:**
- [ ] `PauseAllActions()` / `ResumeAllActions()` - Batch operations
- [ ] `WaitForTasks()` - Await async operations
- [ ] `RunRules()` - Manual rule execution
- [ ] `RunRulesFlag` (All, Self, Children)

**Lists:**
- [ ] Add/Remove items
- [ ] `DeletedList` for EntityListBase
- [ ] Cascade modification state
- [ ] `ContainingList` reference
- [ ] Intra-aggregate moves (item moving between lists in same aggregate)
- [ ] `Root` property on EntityListBase

**Entity State Transitions (Protected Methods):**
- [ ] `MarkNew()` - Set IsNew = true
- [ ] `MarkOld()` - Set IsNew = false
- [ ] `MarkModified()` - Explicit modification marking
- [ ] `MarkUnmodified()` - Clear modification state
- [ ] `MarkAsChild()` - Set IsChild = true
- [ ] `MarkDeleted()` - Internal deletion marking

**Factory Lifecycle Hooks:**
- [ ] `FactoryStart(FactoryOperation)` - Called before factory operation
- [ ] `FactoryComplete(FactoryOperation)` - Called after factory operation
- [ ] `PostPortalConstruct()` - Post-construction hook

**Serialization Hooks:**
- [ ] `OnDeserializing()` - IJsonOnDeserializing
- [ ] `OnDeserialized()` - IJsonOnDeserialized
- [ ] `InitializePropertyBackingFields()` - Generated method for property initialization

**Service Interfaces:**
- [ ] `IValidateBaseServices<T>` - Constructor injection container
- [ ] `IEntityBaseServices<T>` - Entity-specific services with Factory
- [ ] `IPropertyFactory<T>` - Property backing field creation

**Generator Interaction:**
- [ ] Neatoo.BaseGenerator - Property backing field generation
- [ ] RemoteFactory - Factory interface and implementation generation
- [ ] Two-generator execution order and dependencies

### Evolution Strategy

When the API changes:

1. **Update Design.* projects first** - This is the source of truth
2. **Add "was/now" comments** for changed behavior
3. **Update main codebase** to implement the change
4. **Update user documentation** last

### Who Updates Design Projects

| Who | When |
|-----|------|
| **neatoo-architect** | When designing new features - updates Design.* first |
| **neatoo-developer** | When implementing - ensures Design.* matches implementation |
| **Before any PR that changes public API** | Design.* must be updated |

### Validation Requirements

- Tests in Design.Tests must pass
- All `DESIGN DECISION` comments must remain accurate
- No commented-out code should be stale (outdated rejected patterns)
- `GENERATOR BEHAVIOR` comments must match actual generator output

---

## Implementation Steps

### Phase 1: Foundation (BLOCKING)

1. Create `src/Design/Design.sln` solution
2. Create `Design.Domain` project with Neatoo reference
3. Create `Design.Infrastructure` project for persistence interfaces
4. Create `Design.Tests` project
5. Add project references appropriately
6. Verify the solution builds

**Dependencies:** None. Must complete before any other phase.

### Phase 2: Base Class Documentation (depends on Phase 1)

7. Create `BaseClasses/AllBaseClasses.cs` showing all four base classes side-by-side
8. Add extensive comments explaining when to use each class
9. Document state properties and their meanings
10. Add "DID NOT DO THIS" comments for rejected alternatives

**Dependencies:** Phase 1 (projects must exist)

### Phase 3: Factory Operations (depends on Phase 1)

11. Implement `FactoryOperations/CreatePatterns.cs` - [Create] demonstrations
12. Implement `FactoryOperations/FetchPatterns.cs` - [Fetch] demonstrations
13. Implement `FactoryOperations/SavePatterns.cs` - [Insert]/[Update]/[Delete]
14. Implement `FactoryOperations/RemoteBoundary.cs` - [Remote] and client-server
15. Add "GENERATOR BEHAVIOR" comments showing generated code

**Dependencies:** Phase 1 (projects must exist)

### Phase 4: Property System (depends on Phase 1)

16. Implement `PropertySystem/PropertyBasics.cs` - Getter/Setter patterns
17. Implement `PropertySystem/StateProperties.cs` - State tracking
18. Document property interfaces (IValidateProperty, IEntityProperty)
19. Show LoadValue vs SetValue distinction

**Dependencies:** Phase 1 (projects must exist)

### Phase 5: Validation Rules (depends on Phase 1)

20. Implement `Rules/RuleBasics.cs` - RuleBase<T> synchronous rules
21. Implement `Rules/AsyncRules.cs` - AsyncRuleBase<T> async rules
22. Implement `Rules/FluentRules.cs` - RuleManager fluent API
23. Document rule execution and trigger properties

**Dependencies:** Phase 1 (projects must exist)

### Phase 6: Entity Examples (depends on Phases 2-5)

24. Create `Entities/Employee.cs` - Full EntityBase example
25. Create `Entities/Address.cs` - Child entity
26. Create `Entities/AddressList.cs` - EntityListBase
27. Create `ValueObjects/EmployeeListItem.cs` - ValidateBase for value objects
28. Create `ValueObjects/EmployeeList.cs` - ValidateListBase example
29. Create `Commands/ApproveEmployee.cs` - [Execute] command

**Dependencies:** Phases 2-5 (patterns must be documented first)

### Phase 6a: Aggregate Pattern (depends on Phase 6)

30. Create `Aggregates/OrderAggregate/Order.cs` - Aggregate root
31. Create `Aggregates/OrderAggregate/OrderItem.cs` - Child entity
32. Create `Aggregates/OrderAggregate/OrderItemList.cs` - Child collection
33. Document DeletedList lifecycle with extensive comments
34. Document intra-aggregate move handling

**Dependencies:** Phase 6 (basic entity examples must exist first)

### Phase 6b: Generator and DI Documentation (depends on Phase 1)

35. Create `Generators/TwoGeneratorInteraction.cs` - Document both generators
36. Create `DI/ServiceRegistration.cs` - AddNeatooServices() patterns
37. Create `DI/ServiceContracts.cs` - IValidateBaseServices, IEntityBaseServices, IPropertyFactory
38. Add GENERATOR BEHAVIOR comments for both generators

**Dependencies:** Phase 1 (projects must exist)

### Phase 7: Testing (depends on Phases 6, 6a, 6b)

39. Create tests for each base class
40. Create tests for factory operations
41. Create tests for property system
42. Create tests for rules
43. Create tests for aggregate patterns (OrderAggregate, DeletedList)
44. Ensure all tests pass on all target frameworks

**Dependencies:** Phases 6, 6a, 6b (all examples must exist)

### Phase 8: Documentation (can overlap with Phase 7)

45. Create `README.md` explaining the purpose
46. Create `CLAUDE-DESIGN.md` with Claude-specific guidance
47. Update main `CLAUDE.md` to reference design projects as source of truth

**Dependencies:** Phase 2 minimum (need patterns documented)

### Phase Dependency Diagram

```
Phase 1 (Foundation) - BLOCKING
    |
    +---> Phase 2 (Base Classes) ---+
    |                               |
    +---> Phase 3 (Factory Ops) ----+---> Phase 6 (Examples) ---> Phase 6a (Aggregates)
    |                               |           |                        |
    +---> Phase 4 (Properties) -----+           |                        |
    |                               |           v                        v
    +---> Phase 5 (Rules) ----------+     Phase 7 (Testing) <------------+
    |                                           |
    +---> Phase 6b (Generators/DI) -------------+
                                                v
                                          Phase 8 (Docs)
                                    (can start after Phase 2)
```

---

## Acceptance Criteria

- [ ] All projects compile without errors
- [ ] All tests pass
- [ ] All four base classes demonstrated
- [ ] All factory operations demonstrated
- [ ] All state properties documented
- [ ] Property system fully documented
- [ ] Validation rules documented
- [ ] Complete aggregate pattern demonstrated (OrderAggregate)
- [ ] DeletedList lifecycle fully documented
- [ ] Two-generator interaction documented (Generators/)
- [ ] Service registration documented (DI/)
- [ ] Value objects demonstrated (renamed from ReadModels)
- [ ] Every public API element from checklist is demonstrated with comments
- [ ] At least 10 "DID NOT DO THIS BECAUSE" comments
- [ ] At least 10 "DESIGN DECISION" comments
- [ ] At least 5 "GENERATOR BEHAVIOR" comments
- [ ] At least 5 "COMMON MISTAKE" comments
- [ ] CLAUDE.md updated to reference src/Design as source of truth

---

## Out of Scope

**Explicitly NOT included in Design projects:**

- Complex real-world scenarios (keep examples focused on API)
- Full Blazor WASM integration example (document the pattern, don't implement full app)
- Performance benchmarks
- Production-ready error handling

**Differentiation from existing examples:**

| Aspect | src/Examples | Design.* |
|--------|--------------|----------|
| Purpose | User learning | AI comprehension |
| Complexity | Real-world scenarios | Minimal viable demonstrations |
| Comments | Teaching-focused | Design rationale + rejected alternatives |
| Rejected alternatives | None shown | Multiple per file |
| Target audience | Developers learning | Claude Code understanding API |

---

## Dependencies

- Neatoo source projects (via project references)
- RemoteFactory source generator
- .NET 8.0/9.0/10.0 SDK
- MSTest for tests

---

## Neatoo-Specific Implementation Notes

### Understanding Constructor vs Method [Service] Injection

Design projects must clearly document this critical distinction:

```
Constructor [Service]: Service in DI container on BOTH client AND server
    → Use for shared infrastructure (IValidateBaseServices, etc.)
    → Injected when object created regardless of location

Method [Service]: Service only needed for SERVER execution
    → Use for persistence (IDbContext), external services
    → Client assemblies have stubs that throw "not registered"
    → Forces correct [Remote] usage
```

### Entity State Machine

Document the complete state machine:

```
                    +-- IsNew=true ---+
                    |                 |
    Create() -->  Entity            Save() --> Insert
                    |                 |
                    +-- IsNew=false --+
                          |
         +----------------+----------------+
         |                |                |
    IsModified=true  IsDeleted=true   Neither
         |                |                |
     Save()-->Update  Save()-->Delete  No-op
```

### Aggregate Boundaries

Document how parent-child relationships affect Save():

```
EmployeeAggregate (Root)
├── Employee (IsChild=false, can Save)
│   ├── Name: "John"
│   └── Addresses: EntityListBase
│       ├── Address 1 (IsChild=true, cannot Save independently)
│       └── Address 2 (IsChild=true, cannot Save independently)
└── When Employee.Save():
    ├── Insert/Update Employee
    ├── Insert/Update each modified Address
    └── Delete each Address in DeletedList
```

### DeletedList Lifecycle (EntityListBase)

Document the complete lifecycle of removed items:

```
DELETEDLIST LIFECYCLE: How EntityListBase manages removed items

1. ITEM REMOVED FROM LIST (list.Remove(item) or list.RemoveAt(index)):
   ├── If item.IsNew = true:
   │   └── Item discarded (never persisted, no DeletedList entry)
   ├── If item.IsNew = false:
   │   ├── item.MarkDeleted() called → IsDeleted = true
   │   ├── Item added to DeletedList
   │   └── item.ContainingList reference PRESERVED (for save routing)

2. DURING AGGREGATE SAVE (Root.Save()):
   ├── For each item in DeletedList:
   │   └── [Delete] factory method called to persist deletion
   ├── After successful persistence:
   │   ├── DeletedList.Clear() called
   │   └── ContainingList references cleared on deleted items

3. INTRA-AGGREGATE MOVE (item moves from ListA to ListB within same aggregate):
   ├── ListA.Remove(item) → item added to ListA.DeletedList
   ├── ListB.Add(item):
   │   ├── item.UnDelete() called → IsDeleted = false
   │   ├── item removed from ListA.DeletedList
   │   └── item.ContainingList updated to ListB
   └── Result: Item moves without triggering persistence delete

4. FACTORY COMPLETE CLEANUP:
   └── FactoryComplete(FactoryOperation.Update) triggers DeletedList cleanup
```

### Two-Generator Interaction

Document how Neatoo.BaseGenerator and RemoteFactory work together:

```
GENERATOR INTERACTION: Neatoo uses TWO Roslyn source generators

EXECUTION ORDER:
1. Neatoo.BaseGenerator (runs during compilation):
   ├── Detects partial properties on EntityBase<T>/ValidateBase<T>
   ├── Generates: _propertyNameProperty backing fields (IEntityProperty<T>)
   ├── Generates: Property getter/setter implementations
   ├── Generates: InitializePropertyBackingFields() override
   └── Output: {ClassName}.g.cs in Generated/ folder

2. RemoteFactory (runs during compilation):
   ├── Detects [Factory] attribute on class
   ├── Detects [Create], [Fetch], [Insert], [Update], [Delete], [Execute] methods
   ├── Generates: I{ClassName}Factory interface
   ├── Generates: {ClassName}Factory implementation
   ├── For [Remote] methods: generates HTTP client proxy
   └── Output: Factory files in Generated/ folder

INTERACTION POINTS:
- Both generators run independently (no direct dependencies)
- Neatoo.BaseGenerator handles property infrastructure
- RemoteFactory handles factory method routing and serialization
- Generated code from both is combined at compilation
```

---

## Risks / Considerations

1. **Maintenance burden** - These projects must be updated whenever the API changes. Mitigated by making it the first step in the design workflow.

2. **Scope creep** - Temptation to add too much. Keep focused on API demonstration, not comprehensive usage examples.

3. **Comment rot** - Old comments becoming inaccurate. Mitigated by tests and making Design projects the source of truth.

4. **RemoteFactory dependency** - Design projects depend on RemoteFactory generator. Track generator changes.

5. **Multi-project complexity** - Neatoo involves multiple assemblies. Keep Design projects minimal but representative.

---

## Architectural Verification

**Reviewed By:** neatoo-architect (2026-01-31)

### Four Base Classes Analysis

| Base Class | Coverage | Key API Elements to Demonstrate |
|------------|----------|--------------------------------|
| **EntityBase<T>** | Comprehensive | IsModified, IsSelfModified, IsNew, IsDeleted, IsSavable, IsChild, Root, Save(), Delete(), UnDelete(), MarkModified(), MarkUnmodified(), MarkNew(), MarkOld(), ModifiedProperties, Factory, IEntityProperty indexer |
| **ValidateBase<T>** | Comprehensive | IsValid, IsSelfValid, IsBusy, IsPaused, Parent, RuleManager, PropertyMessages, PauseAllActions()/ResumeAllActions(), WaitForTasks(), RunRules(), ClearAllMessages(), ClearSelfMessages(), GetProperty(), MarkInvalid(), ObjectInvalid, IValidateProperty indexer |
| **EntityListBase<I>** | Covered | IsModified (cascades from children), DeletedList, Root, Add/Remove behavior with child state management, FactoryComplete for DeletedList clearing |
| **ValidateListBase<I>** | Covered | IsValid/IsBusy (aggregated from children), Parent, RunRules(), ClearAllMessages(), WaitForTasks(), IsPaused, OnDeserializing/OnDeserialized |

**All four base classes are adequately covered in the plan.** The plan correctly identifies the key patterns and API surface.

### API Coverage Checklist Review

The checklist is **comprehensive** but has the following gaps that should be addressed:

**Missing from Checklist:**

1. **Entity State Transition Methods** - Add these protected methods:
   - `MarkNew()` - Set IsNew = true
   - `MarkOld()` - Set IsNew = false
   - `MarkModified()` - Explicit modification marking
   - `MarkUnmodified()` - Clear modification state
   - `MarkAsChild()` - Set IsChild = true

2. **Factory Lifecycle Hooks** - Add:
   - `FactoryStart(FactoryOperation)` - Called before factory operation
   - `FactoryComplete(FactoryOperation)` - Called after factory operation
   - `PostPortalConstruct()` - Post-construction hook

3. **Serialization Hooks** - Add:
   - `OnDeserializing()` - IJsonOnDeserializing
   - `OnDeserialized()` - IJsonOnDeserialized
   - `InitializePropertyBackingFields()` - Generated method for property initialization

4. **List-Specific APIs** - Add:
   - `EntityListBase.DeletedList` - Removed items pending persistence deletion
   - `EntityListBase.Root` - Aggregate root reference
   - List item state management (MarkAsChild called on Add)
   - Intra-aggregate move handling (item moves between lists)

5. **Property APIs** - Add:
   - `DisplayName` on IEntityProperty
   - `ApplyPropertyInfo()` on IEntityProperty (post-deserialization)
   - `MarkSelfUnmodified()` on IEntityProperty

6. **Service Interfaces** - Document:
   - `IValidateBaseServices<T>` - Constructor injection container
   - `IEntityBaseServices<T>` - Entity-specific services with Factory
   - `IPropertyFactory<T>` - Property backing field creation

### Directory Structure Verification

The proposed structure is **well-designed** for Neatoo's architecture. Recommendations:

1. **Add a DI folder** under Design.Domain for demonstrating service interfaces:
   ```
   Design.Domain/
   ├── DI/
   │   ├── ServiceRegistration.cs  # How to register Neatoo services
   │   └── ServiceContracts.cs     # IValidateBaseServices, IEntityBaseServices
   ```

2. **Rename ReadModels/ to ValueObjects/** - More DDD-accurate for Neatoo's terminology (ValidateBase is used for value objects, not read models).

3. **Add an Aggregates/ folder** to show complete aggregate patterns:
   ```
   Design.Domain/
   ├── Aggregates/
   │   ├── OrderAggregate/
   │   │   ├── Order.cs          # Aggregate root (EntityBase)
   │   │   ├── OrderItem.cs      # Child entity (EntityBase with IsChild=true)
   │   │   └── OrderItemList.cs  # Child collection (EntityListBase)
   ```

### Comment Standards Verification

The example comments in the plan are **accurate** for Neatoo. Additional comment patterns to include:

```csharp
// CLIENT-SERVER BOUNDARY: [Remote] marks server-side execution
// - [Create] often runs locally (no persistence)
// - [Fetch], [Insert], [Update], [Delete] typically need [Remote]
// - Method [Service] parameters only available on server
//
// Factory generation creates:
// - Client: Factory that serializes state and calls HTTP endpoint
// - Server: Factory that deserializes and invokes actual method
```

```csharp
// AGGREGATE BOUNDARY: EntityListBase manages child lifecycle
//
// When item added to list:
// 1. MarkAsChild() called - item.IsChild = true
// 2. SetContainingList() called - tracks ownership
// 3. If previously deleted, UnDelete() called
//
// When item removed from list:
// 1. If IsNew=false, moved to DeletedList for persistence
// 2. MarkDeleted() called
// 3. ContainingList NOT cleared (tracks for save)
```

### Neatoo-Specific Concerns

**1. Two-Generator Complexity (Neatoo.BaseGenerator + RemoteFactory)**

The plan mentions RemoteFactory but does not explicitly address the two-generator interaction:

- **Neatoo.BaseGenerator** - Generates property backing fields, InitializePropertyBackingFields()
- **RemoteFactory** - Generates factory interfaces, implementations, HTTP proxies

**Recommendation:** Add a `Design.Domain/Generators/` folder demonstrating:
```csharp
// GENERATOR INTERACTION: Neatoo uses TWO Roslyn generators
//
// 1. Neatoo.BaseGenerator (runs first):
//    - Detects partial properties on EntityBase/ValidateBase
//    - Generates: _propertyNameProperty backing fields
//    - Generates: Getter/Setter implementations
//    - Generates: InitializePropertyBackingFields() override
//
// 2. RemoteFactory (runs second):
//    - Detects [Factory] attribute
//    - Detects [Create], [Fetch], etc. methods
//    - Generates: IEntityFactory interface
//    - Generates: EntityFactory implementation
//    - For [Remote] methods: generates HTTP client proxy
```

**2. Client-Server Boundary Patterns**

The plan has `FactoryOperations/RemoteBoundary.cs` but should explicitly document:

- **Constructor [Service]** vs **Method [Service]** injection distinction
- **Entity duality** - same class can be aggregate root OR child depending on context
- **PrivateAssets pattern** for EF Core isolation in Blazor WASM

**3. Aggregate Persistence Patterns**

The plan shows parent-child save but should add:

- **DeletedList persistence** - how EntityListBase tracks removed items
- **ContainingList management** - ownership tracking across aggregate
- **Intra-aggregate moves** - item moving from one list to another within same aggregate

### Breaking Changes Assessment

**Confirmed: No breaking changes.**

This plan creates purely additive infrastructure:
- New `src/Design/` folder with separate solution
- No modifications to existing Neatoo source code
- No modifications to existing tests
- No modifications to CLAUDE.md (only additions referencing Design projects)

### Pattern Consistency Check

| Aspect | Consistent? | Notes |
|--------|-------------|-------|
| Solution structure | Yes | Follows `src/{Name}/{Name}.csproj` pattern |
| Project references | Yes | Uses project references like existing test projects |
| Multi-targeting | Yes | Same `net8.0;net9.0;net10.0` targets |
| Generator integration | Yes | Uses same `<ProjectReference ... OutputItemType="Analyzer">` pattern |
| Test framework | Partial | Plan uses MSTest, consistent with Neatoo.UnitTest |
| Generated file location | Yes | Uses `Generated/` folder pattern |

### Codebase Deep-Dive Summary

**Files Examined:**

Core Base Classes:
- `/home/keithvoels/neatoodotnet/Neatoo/src/Neatoo/EntityBase.cs` - 599 lines, full entity lifecycle
- `/home/keithvoels/neatoodotnet/Neatoo/src/Neatoo/ValidateBase.cs` - 985 lines, validation infrastructure
- `/home/keithvoels/neatoodotnet/Neatoo/src/Neatoo/EntityListBase.cs` - 419 lines, child collection management
- `/home/keithvoels/neatoodotnet/Neatoo/src/Neatoo/ValidateListBase.cs` - 577 lines, validation list base

Rules System:
- `/home/keithvoels/neatoodotnet/Neatoo/src/Neatoo/Rules/RuleManager.cs` - 898 lines, fluent rule API
- `/home/keithvoels/neatoodotnet/Neatoo/src/Neatoo/Rules/RuleBase.cs` - 625 lines, rule base classes

Property System:
- `/home/keithvoels/neatoodotnet/Neatoo/src/Neatoo/IValidateProperty.cs` - 158 lines, property interface
- `/home/keithvoels/neatoodotnet/Neatoo/src/Neatoo/IEntityProperty.cs` - 67 lines, entity property interface

Sample Code:
- `/home/keithvoels/neatoodotnet/Neatoo/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs` - 536 lines, factory patterns

**Key Patterns Discovered:**

1. **Entity State Machine** - The plan correctly documents this but should add the `MarkNew()` call in FactoryComplete for Create operations.

2. **Paused State Semantics** - When IsPaused=true:
   - Setter method does NOT trigger rules
   - LoadValue() always works (for persistence loading)
   - PropertyChanged events still fire for UI binding
   - NeatooPropertyChanged events are suppressed

3. **DeletedList Lifecycle** - Items removed from EntityListBase are:
   - Added to DeletedList if IsNew=false
   - Kept with ContainingList reference until FactoryComplete(Update)
   - ContainingList cleared after successful persistence

4. **RuleManager Fluent API** - Multiple AddAction overloads (1, 2, 3 properties + array) exist due to CallerArgumentExpression incompatibility with params.

### Verification Checklist

- [x] All four base classes analyzed (EntityBase, ValidateBase, EntityListBase, ValidateListBase)
- [x] API coverage checklist reviewed (gaps identified and documented)
- [x] Directory structure verified (recommendations provided)
- [x] Comment standards verified (accurate with additions recommended)
- [x] Breaking changes assessment completed (none)
- [x] Pattern consistency check (passes with notes)
- [x] Two-generator complexity addressed
- [x] Client-server boundary patterns reviewed
- [x] Aggregate persistence patterns reviewed
- [x] Codebase deep-dive completed (9 key files examined)

### Recommendations

1. **Expand API Checklist** - Add the missing items identified above (state transition methods, lifecycle hooks, list APIs)

2. **Add Generator Documentation Folder** - Explain two-generator interaction

3. **Rename ReadModels to ValueObjects** - Better DDD alignment

4. **Add Aggregates Folder** - Show complete aggregate pattern with parent/child/list

5. **Document DeletedList Lifecycle** - Critical for understanding entity removal behavior

6. **Add Service Registration Demo** - Show AddNeatooServices() and service interface contracts

---

## Developer Review

**Reviewed By:** neatoo-developer (2026-01-31)

**Status:** APPROVED with minor corrections

### 1. Implementation Steps Assessment

**Clarity and Actionability:** GOOD

The implementation steps are clear and actionable. Each phase has explicit deliverables and dependencies are well-documented. The phase dependency diagram is particularly helpful for understanding parallel work opportunities.

**Phase Ordering:** CORRECT

The ordering makes sense:
- Phase 1 (Foundation) must complete first as a blocking dependency
- Phases 2-5 and 6b can run in parallel after Phase 1
- Phase 6 depends on Phases 2-5 for documented patterns
- Phase 6a depends on Phase 6 for basic entity examples
- Phase 7 depends on all content phases
- Phase 8 (Documentation) can start after Phase 2

**No Ambiguities Identified:** The steps are sufficiently detailed to begin implementation.

### 2. API Coverage Checklist Verification

**Verified Against Source Code:**

| Checklist Item | Verified | Notes |
|----------------|----------|-------|
| EntityBase<T> | YES | All documented methods exist in `src/Neatoo/EntityBase.cs` |
| ValidateBase<T> | YES | All documented methods exist in `src/Neatoo/ValidateBase.cs` |
| EntityListBase<I> | YES | All documented methods exist in `src/Neatoo/EntityListBase.cs` |
| ValidateListBase<I> | YES | All documented methods exist in `src/Neatoo/ValidateListBase.cs` |
| [Create], [Fetch], etc. | YES | From Neatoo.RemoteFactory (version 10.11.2) |
| **[Event]** | **NO - REMOVE** | Not found in source code. Remove from checklist. |
| RuleBase<T> | YES | `src/Neatoo/Rules/RuleBase.cs` - actually `AsyncRuleBase<T>` |
| RuleManager fluent API | YES | All overloads verified in `src/Neatoo/Rules/RuleManager.cs` |
| IValidateProperty | YES | `src/Neatoo/IValidateProperty.cs` |
| IEntityProperty | YES | `src/Neatoo/IEntityProperty.cs` |
| IValidateBaseServices<T> | YES | `src/Neatoo/IValidateBaseServices.cs` |
| IEntityBaseServices<T> | YES | `src/Neatoo/IEntityBaseServices.cs` |
| FactoryStart/FactoryComplete | YES | In ValidateBase.cs lines 939-954 |
| Entity state transition methods | YES | MarkNew, MarkOld, MarkModified, MarkUnmodified, MarkAsChild, MarkDeleted all in EntityBase.cs |

**Correction Required:**
- Remove `[Event]` from the Factory Operations checklist - this attribute does not exist in the current codebase

**Additional Items to Consider:**
- `IPropertyFactory<T>` - used for generated property backing fields, documented in IValidateBaseServices
- `IPropertyInfoList<T>` - property metadata container
- `SaveOperationException` with `SaveFailureReason` enum - documented exception types

### 3. Directory Structure Assessment

**Structure Validity:** APPROVED

The proposed directory structure is well-organized and follows Neatoo conventions:

```
src/Design/
├── Design.sln              # Separate solution for Design projects
├── Design.Domain/          # Main demonstration code
├── Design.Infrastructure/  # Persistence interfaces (minimal)
└── Design.Tests/           # Test coverage for demonstrations
```

**No Naming Conflicts:** The `Design` prefix avoids conflicts with existing `src/Neatoo` and `src/Examples` directories.

**Project Reference Pattern:** Should follow existing pattern:
- Design.Domain references Neatoo (as project reference or package)
- Design.Domain references RemoteFactory.Attributes
- Design.Tests references Design.Domain
- Design.Infrastructure is referenced privately by Design.Domain

### 4. Test Strategy Assessment

**~60 Tests Estimate:** REALISTIC

Analysis of existing test project:
- Neatoo.UnitTest has ~1,727 test methods across 60 test files
- Average of ~29 tests per file

For Design.Tests with ~12-15 test files planned:
- EntityBaseTests, ValidateBaseTests, EntityListBaseTests, ValidateListBaseTests (4)
- CreateTests, FetchTests, SaveTests (3)
- PropertyBasicsTests, StatePropertyTests (2)
- SyncRuleTests, AsyncRuleTests, FluentRuleTests (3)
- OrderAggregateTests, DeletedListTests (2)

At ~4-5 tests per file average, 60 tests is achievable and appropriate for API demonstration purposes.

**Test Categories:** APPROPRIATE
- Base class tests cover lifecycle and state
- Factory tests cover CRUD operations
- Property tests cover getter/setter/LoadValue patterns
- Rule tests cover sync, async, and fluent APIs
- Aggregate tests cover parent-child relationships and DeletedList

### 5. Implementation Concerns

**No Technical Blockers Identified.**

**Considerations:**

1. **RemoteFactory Dependency:** The plan correctly identifies RemoteFactory as a dependency. Use the same version (10.11.2) as main Neatoo projects.

2. **Generator Output:** Design projects will generate code in `Generated/` folders. Ensure `.gitignore` entries are added.

3. **Multi-Target Build:** Design projects should target `net8.0;net9.0;net10.0` for consistency with main projects.

4. **No Reflection Required:** The plan does not require reflection, which is correct per CLAUDE.md guidelines.

5. **Test Infrastructure:** Can reuse patterns from `src/Neatoo.UnitTest/TestInfrastructure/` for DI setup.

### 6. Comment Targets Assessment

| Target | Count | Achievability |
|--------|-------|---------------|
| DID NOT DO | 10 | ACHIEVABLE - Common patterns include: whole-class [Remote], reflection-based validation, manual property tracking, mutable factory methods, direct DB access in entities |
| DESIGN DECISION | 10 | ACHIEVABLE - Each base class (4), property system (2), factory pattern (2), rule system (2) provides opportunities |
| GENERATOR BEHAVIOR | 5 | ACHIEVABLE - Property backing fields, factory interfaces, factory implementations, HTTP proxies, InitializePropertyBackingFields |
| COMMON MISTAKE | 5 | ACHIEVABLE - Child save, IsModified vs IsSelfModified, LoadValue vs SetValue, async rule handling, DeletedList lifecycle |

**All comment targets are achievable** with the planned file structure.

### 7. Verification Checklist

- [x] All four base classes have corresponding source files verified
- [x] API coverage checklist items verified against source
- [x] Directory structure follows project conventions
- [x] Test estimate is realistic (60 tests across 12-15 files)
- [x] No reflection required
- [x] Comment targets are achievable
- [x] RemoteFactory dependency version confirmed (10.11.2)
- [x] One correction identified: Remove [Event] attribute from checklist

---

## Implementation Contract

**Status:** APPROVED FOR IMPLEMENTATION

### In Scope (File-by-File)

**Phase 1: Foundation**
- [ ] `src/Design/Design.sln` - Solution file
- [ ] `src/Design/Design.Domain/Design.Domain.csproj` - Project file with Neatoo references
- [ ] `src/Design/Design.Infrastructure/Design.Infrastructure.csproj` - Infrastructure project
- [ ] `src/Design/Design.Tests/Design.Tests.csproj` - MSTest project

**Phase 2: Base Classes**
- [ ] `Design.Domain/BaseClasses/AllBaseClasses.cs` - Side-by-side comparison of EntityBase, ValidateBase, EntityListBase, ValidateListBase

**Phase 3: Factory Operations**
- [ ] `Design.Domain/FactoryOperations/CreatePatterns.cs` - [Create] demonstrations
- [ ] `Design.Domain/FactoryOperations/FetchPatterns.cs` - [Fetch] demonstrations
- [ ] `Design.Domain/FactoryOperations/SavePatterns.cs` - [Insert]/[Update]/[Delete] demonstrations
- [ ] `Design.Domain/FactoryOperations/RemoteBoundary.cs` - [Remote] and client-server boundary

**Phase 4: Property System**
- [ ] `Design.Domain/PropertySystem/PropertyBasics.cs` - Partial properties, Getter/Setter patterns
- [ ] `Design.Domain/PropertySystem/StateProperties.cs` - IsModified, IsNew, IsValid, etc.

**Phase 5: Validation Rules**
- [ ] `Design.Domain/Rules/RuleBasics.cs` - AsyncRuleBase<T> / RuleBase<T>
- [ ] `Design.Domain/Rules/AsyncRules.cs` - Async validation patterns
- [ ] `Design.Domain/Rules/FluentRules.cs` - RuleManager fluent API

**Phase 6: Entity Examples**
- [ ] `Design.Domain/Entities/Employee.cs` - Full EntityBase example
- [ ] `Design.Domain/Entities/Address.cs` - Child entity example
- [ ] `Design.Domain/Entities/AddressList.cs` - EntityListBase example
- [ ] `Design.Domain/ValueObjects/EmployeeListItem.cs` - ValidateBase for value objects
- [ ] `Design.Domain/ValueObjects/EmployeeList.cs` - ValidateListBase example
- [ ] `Design.Domain/Commands/ApproveEmployee.cs` - Static [Execute] command

**Phase 6a: Aggregates**
- [ ] `Design.Domain/Aggregates/OrderAggregate/Order.cs` - Aggregate root
- [ ] `Design.Domain/Aggregates/OrderAggregate/OrderItem.cs` - Child entity
- [ ] `Design.Domain/Aggregates/OrderAggregate/OrderItemList.cs` - Child collection with DeletedList

**Phase 6b: Generators and DI**
- [ ] `Design.Domain/Generators/TwoGeneratorInteraction.cs` - Neatoo.BaseGenerator + RemoteFactory
- [ ] `Design.Domain/DI/ServiceRegistration.cs` - AddNeatooServices() patterns
- [ ] `Design.Domain/DI/ServiceContracts.cs` - IValidateBaseServices, IEntityBaseServices, IPropertyFactory

**Phase 7: Tests (71 tests)**
- [x] `Design.Tests/BaseClassTests/EntityBaseTests.cs` (7 tests)
- [x] `Design.Tests/BaseClassTests/ValidateBaseTests.cs` (8 tests)
- [x] `Design.Tests/BaseClassTests/EntityListBaseTests.cs` (6 tests)
- [x] `Design.Tests/BaseClassTests/ValidateListBaseTests.cs` (4 tests)
- [x] `Design.Tests/AggregateTests/OrderAggregateTests.cs` (8 tests)
- [x] `Design.Tests/AggregateTests/DeletedListTests.cs` (6 tests)
- [x] `Design.Tests/FactoryTests/CreateTests.cs` (5 tests)
- [x] `Design.Tests/FactoryTests/FetchTests.cs` (5 tests)
- [x] `Design.Tests/FactoryTests/SaveTests.cs` (4 tests)
- [x] `Design.Tests/PropertyTests/PropertyBasicsTests.cs` (5 tests)
- [x] `Design.Tests/PropertyTests/StatePropertyTests.cs` (4 tests)
- [x] `Design.Tests/RuleTests/SyncRuleTests.cs` (4 tests)
- [x] `Design.Tests/RuleTests/AsyncRuleTests.cs` (4 tests)
- [x] `Design.Tests/RuleTests/FluentRuleTests.cs` (5 tests)

**Phase 8: Documentation**
- [x] `src/Design/README.md` - Purpose explanation for humans
- [x] `src/Design/CLAUDE-DESIGN.md` - Claude-specific guidance
- [x] Update `CLAUDE.md` - Add reference to Design projects

### Out of Scope
- Modifying existing Neatoo source code
- Modifying existing tests in Neatoo.UnitTest
- Creating Blazor WASM example application
- Performance testing or benchmarks
- Adding [Event] attribute (does not exist)

### Pre-Implementation Corrections Required
1. Remove `[Event]` from API Coverage Checklist (line 233-234 in plan)

---

## Implementation Progress

### 2026-01-31 - Build Fixes Completed

**Phases 1-6b completed. Design.Domain now builds successfully.**

**Issues Fixed:**

1. **`[Factory]` Attribute Requirement** - All 43 classes inheriting from Neatoo base classes needed explicit `[Factory]` attribute. The BaseGenerator uses `ForAttributeWithMetadataName` which only finds direct attributes, not inherited ones.

2. **Rule Class Override Errors (CS0506/CS0507)** - Rule classes incorrectly used `public override` for TriggerProperties and Execute. Fixed by:
   - Using constructor-based trigger property registration: `public MyRule() : base(t => t.PropertyName) { }`
   - Changing Execute to `protected override` (not public)
   - Setting RuleOrder in constructor instead of override

3. **RuleMessages API** - Code incorrectly used `RuleMessages.Error()` and `RuleMessages.Empty`. Actual API:
   - `None` (inherited from AsyncRuleBase) - validation passed
   - `(propertyName, message).AsRuleMessages()` - validation error
   - `new RuleMessages().If(condition, propertyName, message)` - fluent conditional

4. **Command Method Naming** - `[Execute]` methods named just `Execute` caused generator issues. Fixed by using convention `_MethodName` (e.g., `_Approve`) which generates delegate `MethodName`.

5. **Command Return Types** - `[Execute]` methods returning plain `Task` caused generator issues. Changed to return `Task<T>` (e.g., `Task<bool>`).

6. **AddNeatooServices API** - Signature is `AddNeatooServices(NeatooFactory mode, params Assembly[] assemblies)`. First param is enum, not Assembly.

7. **List Bases Don't Have PauseAllActions** - Removed `using (PauseAllActions())` from list base Fetch methods.

8. **IEntityProperty.PropertyMessages** - API is `PropertyMessages`, not `Messages`.

9. **Factory Hint Name Length** - Added `[assembly: FactoryHintNameLength(70)]` to avoid NF0104 warnings for long FQNs.

**Build Status:** PASSING
```
dotnet build src/Design/Design.Domain/Design.Domain.csproj
Build succeeded. 0 Warning(s) 0 Error(s)
```

**All Tests Pass:**
```
Passed! - Neatoo.Skills.Tests.dll (20 tests)
Passed! - Neatoo.BaseGenerator.Tests.dll (26 tests)
Passed! - Person.DomainModel.Tests.dll (55 tests)
Passed! - Neatoo.UnitTest.dll (1722 tests, 1 skipped)
```

**Remaining Work:**
- Phase 7: Create ~60 tests in Design.Tests project
- Phase 8: Create README.md, CLAUDE-DESIGN.md, update main CLAUDE.md

---

## Completion Evidence

- **Tests Passing:** 71 tests pass in Design.Tests
  ```
  Passed! - Failed: 0, Passed: 71, Skipped: 0, Total: 71
  ```

- **Build Output:**
  ```
  Build succeeded. 0 Warning(s) 0 Error(s)
  ```

- **All Checklist Items:** Confirmed 100% complete
  - All four base classes demonstrated
  - All factory operations documented
  - All state properties documented
  - Property system fully documented
  - Validation rules documented (sync, async, fluent)
  - Complete aggregate pattern (OrderAggregate with DeletedList)
  - Two-generator interaction documented
  - Service registration documented
  - Value objects demonstrated
  - 10+ "DID NOT DO THIS" comments
  - 10+ "DESIGN DECISION" comments
  - 5+ "GENERATOR BEHAVIOR" comments
  - 5+ "COMMON MISTAKE" comments
  - CLAUDE.md updated with Design Source of Truth section
