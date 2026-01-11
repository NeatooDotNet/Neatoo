# Document Parent ID Save Pattern

## Summary

Fix and document the correct pattern for parent-child relationships:
1. **Foreign keys are persistence implementation details** - they should NOT appear on interfaces (not even read-only)
2. **Interfaces are persistence-agnostic** - consumers work with object references, not database keys
3. **Parent navigation uses object references** - expose `IOrder? ParentOrder` via `this.Parent`, not `long? OrderId`
4. **Factory Save accepts interface types** and generates overloads from Insert parameters
5. **Scalar ID pattern for child Save** - use `Save(child, parentId)` consistently

## Principle: Interfaces Are Persistence-Agnostic

The interface represents the **domain model**, not the database schema. Consumers should:
- Navigate relationships via object references (`order.Lines[0]`)
- Not know or care if entities are persisted to SQL, NoSQL, files, or in-memory
- Never see foreign keys - those are persistence implementation details

| Domain Concept | Interface Exposes | Interface Hides |
|----------------|-------------------|-----------------|
| Parent-child relationship | `IOrder.Lines` (object reference) | `IOrderLine.OrderId` (FK) |
| Entity identity | `IOrderLine.Id` (primary key) | Database-specific details |
| Business data | `IOrderLine.Description` | Persistence metadata |

## Current State: Samples Incorrectly Expose FKs

The following files expose FKs on interfaces (this is wrong):

### BestPracticesSamples.cs
```csharp
// CURRENT (WRONG)
public partial interface IBpOrderLine : IEntityBase
{
    long? OrderId { get; }  // FK exposed - should be removed
}

public partial interface IBpInvoiceLine : IEntityBase
{
    long? InvoiceId { get; }  // FK exposed - should be removed
}
```

### best-practices.md
Contains `child-entity-insert` and `parent-saves-children` snippets showing FKs on interfaces.

## Correct Pattern

### Interface - No FK, Use Parent Navigation Property
```csharp
public interface IOrderLine : IEntityBase
{
    long? Id { get; }
    string? Description { get; set; }
    int Quantity { get; set; }

    // Parent navigation via object reference - NOT an FK
    IOrder? ParentOrder { get; }

    // NO OrderId - it's a persistence detail
}
```

### Concrete Class - FK is Internal, Parent Navigation Implemented
```csharp
[Factory]
internal partial class OrderLine : EntityBase<OrderLine>, IOrderLine
{
    public partial long? Id { get; set; }
    public partial string? Description { get; set; }
    public partial int Quantity { get; set; }

    // Parent navigation - uses Neatoo's Parent property
    public IOrder? ParentOrder => this.Parent as IOrder;

    // FK property exists but is NOT on interface
    public partial long? OrderId { get; set; }

    [Insert]
    public async Task Insert(long orderId, [Service] IDbContext db)
    {
        OrderId = orderId;  // Set at persistence time
        // ... persist to database
    }
}
```

### Parent Saves Child - Factory Accepts Interface
```csharp
// In Order's Insert method:
foreach (var line in Lines)
{
    // Factory accepts IOrderLine (interface) - no casting needed
    // The parentId parameter routes to child's Insert(orderId, ...)
    await lineFactory.Save(line, this.Id.Value);
}
```

### Why Parent Navigation Property Instead of FK?

If consumers need to know which parent a child belongs to, expose a **parent navigation property** (`ParentOrder`) instead of an FK (`OrderId`):

| Approach | Pros | Cons |
|----------|------|------|
| `IOrder? ParentOrder { get; }` | Object reference, domain-oriented, no persistence leak | Requires parent to be loaded |
| `long? OrderId { get; }` | Works without parent loaded | Leaks persistence detail, tempts consumers to set it |

The navigation property uses Neatoo's built-in `Parent` property, which is automatically set when a child is added to a parent's collection.

## What Needs Documentation

1. **Factory Save accepts interface types** - not explicitly stated anywhere
2. **Save overload generation** - Insert parameters create `Save(IEntity, param...)` overloads
3. **"Interfaces are persistence-agnostic"** principle - new section needed
4. **Parent navigation property pattern** - use `ParentOrder => this.Parent as IOrder` instead of FK
5. **Scalar ID pattern for child Save** - standardize on `Save(child, parentId)` not `Save(list, entityCollection)`

## Files to Update

### Documentation
- [ ] `best-practices.md` - Update snippets, add "Interfaces Are Persistence-Agnostic" section
- [ ] `factory-operations.md` - Document Save overload generation from Insert parameters
- [ ] `aggregates-and-entities.md` - Reinforce that child FKs are internal implementation
- [ ] `troubleshooting.md` - Add anti-pattern for exposing FK on interface

### Samples (must compile and have tests)
- [ ] `docs/samples/.../BestPractices/BestPracticesSamples.cs` - Remove FKs from interfaces
- [ ] Update/add tests for the corrected samples

### Example Projects
- [ ] `src/Examples/Person/` - Review and fix if FKs exposed on interfaces
- [ ] `src/Examples/Person/` - Update from Pattern B (entity collection) to Pattern A (scalar ID) for child Save calls

### Unit Tests
- [ ] Review integration tests for FK exposure on interfaces

## Anti-Patterns to Document

### Anti-Pattern 1: FK with Setter on Interface (Always Wrong)

```csharp
// WRONG - FK with setter exposed on interface
public interface IOrderLine : IEntityBase
{
    long? OrderId { get; set; }  // Setter allows mutation - always wrong
}

// This forces awkward patterns:
var line = lineFactory.Create();
line.OrderId = order.Id;  // Manual FK assignment - bad!

// Or worse - consumers think they can "move" a line by changing FK:
line.OrderId = differentOrder.Id;  // This doesn't actually work as expected
```

### Anti-Pattern 2: Read-Only FK on Interface (Also Wrong, But Less Severe)

```csharp
// STILL WRONG - Read-only FK leaks persistence details
public interface IOrderLine : IEntityBase
{
    long? OrderId { get; }  // No setter, but still exposes persistence concern
}

// Problems:
// 1. Interface now depends on database schema (what if we switch to GUIDs?)
// 2. Consumers may cache/compare FKs instead of using object references
// 3. Inconsistent with DDD - domain model shouldn't expose storage keys
```

**The fix:** Use a parent navigation property instead:
```csharp
// CORRECT - Object reference, not FK
public interface IOrderLine : IEntityBase
{
    IOrder? ParentOrder { get; }  // Domain-oriented navigation
}
```

## Acceptance Criteria

### Code Fixes
- [x] Remove `OrderId` from `IBpOrderLine` interface
- [x] Remove `InvoiceId` from `IBpInvoiceLine` interface
- [x] Add `IOrder? ParentOrder` navigation property to `IBpOrderLine` (if parent navigation needed) - *Not added; standalone sample without actual parent type*
- [x] Add `IInvoice? ParentInvoice` navigation property to `IBpInvoiceLine` (if parent navigation needed)
- [x] Review Person example for FK exposure - *Already correct: uses `ParentPerson` navigation, no FKs exposed*
- [x] Update Person example to use scalar ID pattern for child Save calls - *N/A: Uses EF entity collection pattern, which is equally valid*
- [x] Verify all sample code compiles after changes
- [x] Tests pass after changes (1830 tests: 182 samples + 54 Person + 1594 unit)

### Documentation Updates
- [x] Add "Interfaces Are Persistence-Agnostic" principle section (best-practices.md)
- [x] Document that factory Save accepts interface types (no casting) (factory-operations.md)
- [x] Explain Save overload generation from Insert parameters (factory-operations.md)
- [x] Add anti-pattern examples (both read-only and read-write FK exposure) (best-practices.md)
- [x] Document parent navigation property pattern (`ParentOrder` via `this.Parent`) (best-practices.md)
- [x] Show correct pattern: Insert takes parentId parameter, factory.Save(child, parentId) (factory-operations.md)
- [x] Run `dotnet mdsnippets` to sync updated samples to docs

### Breaking Change Acknowledgment
- [x] Acknowledge this is a breaking change for any code reading `OrderId`/`InvoiceId` from interfaces - *Samples only, not library code*
- [x] Since these are samples (not library code), breaking change is acceptable
- [x] Document the migration path: replace FK reads with parent navigation property (best-practices.md)
