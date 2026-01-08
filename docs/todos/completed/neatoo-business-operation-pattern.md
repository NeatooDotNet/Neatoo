# Neatoo Documentation Request: Business Operation Pattern

**Date:** 2026-01-08
**From:** zTreatment project
**To:** Neatoo documentation team
**Status:** ✅ ADDRESSED (2026-01-08)

---

## The Problem We Were Trying to Solve

We needed to implement business operations like `Archive()` and `Complete()` that:

1. Modify domain model state (set properties)
2. Persist changes to database
3. Return the updated instance (following Neatoo's pattern)
4. Be callable through the public interface (no `[Service]` parameters)

**Desired API:**
```csharp
visit = await visit.Archive();
consultation = await consultation.Complete();
```

---

## Why This Was Difficult to Discover

### Initial Attempts That Failed

**Attempt 1: [Remote] with [Service] parameter**

```csharp
// On interface
Task<IVisit> Archive();

// Implementation - DOESN'T WORK
[Remote]
public async Task<IVisit> Archive([Service] IVisitFactory visitFactory)
{
    // ... modify properties ...
    return await visitFactory.Save((Visit)this);
}
```

**Problem:** C# requires exact signature match for interface implementation. Interface has `Archive()`, implementation has `Archive([Service] IVisitFactory)` - won't compile.

**Attempt 2: Explicit interface implementation**

```csharp
async Task<IVisit> IVisit.Archive() => await Archive(null!);
```

**Problem:** Passes null for required service.

### What We Searched For

- "Neatoo business operations"
- "Neatoo persist from entity method"
- Factory patterns in documentation
- `[Execute]` command pattern

### What We Found (But Wasn't the Answer)

The `[Execute]` command pattern works but requires:
- Separate static command class
- Injected delegate at call site
- Operation NOT on the entity interface

```csharp
[Factory]
public static partial class ArchiveVisitCommand
{
    [Execute]
    internal static async Task<IVisit?> _Archive(
        int visitId,
        [Service] IVisitFactory visitFactory,
        [Service] AppDbContext dbContext) { ... }
}

// Usage - less discoverable
var visit = await _archiveVisit(visit.Id);
```

---

## The Solution: EntityBase.Save()

After checking the Neatoo GitHub repository directly, we discovered that `EntityBase<T>` already has:

```csharp
public virtual async Task<IEntityBase> Save()
public virtual async Task<IEntityBase> Save(CancellationToken token)
```

This enables a clean pattern:

```csharp
public interface IVisit : IEntityBase
{
    Task<IVisit> Archive();
}

public class Visit : EntityBase<Visit>, IVisit
{
    public async Task<IVisit> Archive()
    {
        // Validate preconditions
        if (Archived)
            throw new InvalidOperationException("Visit is already archived");

        // Modify properties (client-side)
        Status = VisitStatus.OUT;
        Archived = true;
        LastUpdated = DateTime.UtcNow;

        // Persist via existing Save() - triggers remote [Update]
        return (IVisit) await this.Save();
    }

    [Remote]
    [Update]
    public async Task Update([Service] AppDbContext db)
    {
        var entity = await db.Visits.FindAsync(Id);

        // Map modified properties
        if (this[nameof(Status)].IsModified)
            entity.Status = Status;
        if (this[nameof(Archived)].IsModified)
            entity.Archived = Archived;

        // Handle archive-specific DB operations if needed
        if (this[nameof(Archived)].IsModified && Archived)
        {
            // Additional cleanup per business rules
        }

        await db.SaveChangesAsync();
    }
}
```

### How It Works

1. **Client:** `Archive()` validates and sets properties
2. **Client:** `this.Save()` serializes entity and sends to server
3. **Server:** `[Update]` receives entity, persists modified properties
4. **Server:** Returns updated entity
5. **Client:** Receives new instance

### Why This Pattern Is Good

| Benefit | Description |
|---------|-------------|
| Clean API | `visit = await visit.Archive()` |
| Interface-compatible | No `[Service]` params in interface |
| No extra infrastructure | Uses existing `EntityBase.Save()` |
| Atomic | Can't forget to save after operation |
| Discoverable | Operation is on the entity where expected |
| Testable | Can mock at interface level |

---

## What's Missing from Neatoo Documentation

### 1. EntityBase.Save() Not Documented

The `Save()` method on `EntityBase<T>` is not mentioned in the skill documentation. The docs only show:

```csharp
// Factory-based save (documented)
person = await personFactory.Save(person);
```

But not:

```csharp
// Entity-based save (NOT documented)
person = (IPerson) await person.Save();
```

### 2. No "Business Operation" Pattern

The documentation covers:
- `[Create]` - Initialize new entity
- `[Fetch]` - Load from database
- `[Insert]`/`[Update]`/`[Delete]` - Persistence operations
- `[Execute]` - Commands and queries

But doesn't cover: **"How do I create a business operation that modifies state AND persists?"**

### 3. Interface Method Pattern Not Shown

No examples show how to create interface methods that:
- Take no `[Service]` parameters
- Internally call `this.Save()`
- Return the updated entity

---

## Suggested Documentation Additions

### New Section: "Business Operations on Entities"

```markdown
## Business Operations

For domain operations that modify state AND persist (like `Archive()`, `Complete()`, `Approve()`):

### Pattern: Entity Method with this.Save()

\`\`\`csharp
public interface IOrder : IEntityBase
{
    Task<IOrder> Cancel();
}

public class Order : EntityBase<Order>, IOrder
{
    public async Task<IOrder> Cancel()
    {
        if (Status == OrderStatus.Shipped)
            throw new InvalidOperationException("Cannot cancel shipped order");

        Status = OrderStatus.Cancelled;
        CancelledDate = DateTime.UtcNow;

        return (IOrder) await this.Save();
    }
}
\`\`\`

**Key points:**
- Method is on the interface (no [Service] params)
- Modifies properties client-side
- Calls `this.Save()` to persist
- Returns cast to interface type
- [Update] handles any operation-specific DB work
```

### Update: EntityBase Reference

Add to the EntityBase meta-properties table:

| Method | Return Type | Description |
|--------|-------------|-------------|
| `Save()` | `Task<IEntityBase>` | Persist entity - routes to Insert/Update/Delete based on state |
| `Save(CancellationToken)` | `Task<IEntityBase>` | Save with cancellation support |

---

## Our Implementation Plan

We will implement this pattern for:

1. **Visit.Archive()** - Sets Status=OUT, Archived=true, persists
2. **Consultation.Complete()** - Sets Active=false, handles cleanup, persists

These will serve as reference implementations demonstrating the pattern.

---

## Questions for Neatoo Team

1. Is `EntityBase.Save()` the recommended approach for this pattern?
2. Are there any gotchas we should be aware of?
3. Should we prefer this over the `[Execute]` command pattern for operations that logically belong on the entity?

---

## Answers (2026-01-08)

### 1. Is `EntityBase.Save()` the recommended approach for this pattern?

**Yes.** `EntityBase.Save()` is the recommended approach for business operations that modify state and persist. It has been documented in:
- [Factory Operations - Business Operations](../factory-operations.md#business-operations)
- [Meta-Properties - Save()](../meta-properties.md#save)

### 2. Are there any gotchas we should be aware of?

**Gotchas:**
- `Save()` throws `SaveOperationException` if the entity is not savable (child, invalid, not modified, busy, or no factory)
- Always reassign after Save: `entity = (IEntity)await entity.Save()` (returns new instance)
- Business operations on child entities will throw `SaveOperationException(IsChildObject)` - save through the parent

### 3. Should we prefer this over the `[Execute]` command pattern for operations that logically belong on the entity?

**Yes, prefer business operations on the entity when:**
- The operation logically belongs on the entity (Archive, Complete, Approve)
- No additional parameters are needed beyond entity state
- You want a clean API: `visit = await visit.Archive()`

**Use `[Execute]` command pattern when:**
- Operation needs parameters not available on the entity
- Operation spans multiple aggregates
- You want to keep the entity interface minimal

---

## Documentation Added

1. **docs/factory-operations.md**
   - "Entity-Based Save" section documenting `entity.Save()`
   - "Business Operations" section with full pattern and example

2. **docs/meta-properties.md**
   - Added `Save()` and `Save(CancellationToken)` to property hierarchy
   - Added "Save()" section with usage and exception details

3. **docs/samples/Neatoo.Samples.DomainModel/FactoryOperations/BusinessOperationSamples.cs**
   - Complete sample demonstrating the pattern with `IVisit.Archive()`
   - Example showing `entity.Save()` vs `factory.Save()` equivalence

4. **Skill files updated**
   - `~/.claude/skills/neatoo/factories.md` - Entity-Based Save and Business Operations sections
   - `~/.claude/skills/neatoo/entities.md` - EntityBase Methods table with Save()
