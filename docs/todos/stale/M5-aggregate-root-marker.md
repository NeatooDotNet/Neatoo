# M5: Add IAggregateRoot Marker Interface

**Priority:** Medium
**Category:** DDD Alignment
**Effort:** Low
**Status:** Not Started

---

## Problem Statement

There's no explicit way to mark a class as an Aggregate Root in Neatoo. While `EntityBase<T>` can serve as an aggregate root, child entities also inherit from `EntityBase<T>`, making it unclear which entities are roots.

---

## Current State

```csharp
// Both aggregate root and child entity look the same
internal partial class Order : EntityBase<Order> { }      // Aggregate Root
internal partial class OrderLine : EntityBase<OrderLine> { }  // Child Entity
```

The only distinction is the `IsChild` flag, which is set at runtime when added to a parent.

---

## Proposed Solution

Add a marker interface to indicate aggregate roots:

```csharp
// src/Neatoo/IAggregateRoot.cs
namespace Neatoo;

/// <summary>
/// Marker interface for aggregate roots in the domain model.
/// </summary>
/// <remarks>
/// <para>
/// Classes implementing <see cref="IAggregateRoot"/> should have <c>[Remote]</c> on their
/// factory operations (<c>[Fetch]</c>, <c>[Insert]</c>, <c>[Update]</c>, <c>[Delete]</c>).
/// </para>
/// <para>
/// Child entities within the aggregate should NOT implement this interface.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Factory]
/// internal partial class Order : EntityBase&lt;Order&gt;, IAggregateRoot, IOrder
/// {
///     public partial IOrderLineList Lines { get; set; }
///
///     [Remote]
///     [Fetch]
///     public async Task Fetch(int id, [Service] IDbContext db) { ... }
/// }
/// </code>
/// </example>
public interface IAggregateRoot : IEntityBase
{
}
```

---

## Benefits

1. **Clear Intent:** Documents which entities are aggregate roots
2. **Enforcement:** Repositories can require `IAggregateRoot`
3. **Tooling:** Analyzers can verify aggregate boundary rules
4. **Discovery:** Easy to find all aggregates in a codebase

---

## Usage Examples

### Marking an Aggregate Root

```csharp
[Factory]
internal partial class Order : EntityBase<Order>, IAggregateRoot, IOrder
{
    public partial IOrderLineList Lines { get; set; }

    [Insert]
    public async Task Insert([Service] IOrderRepository repo)
    {
        await RunRules();
        if (!IsSavable) return;
        await repo.InsertAsync(this);
    }
}
```

### Type-Safe Repository

```csharp
public interface IRepository<T> where T : IAggregateRoot
{
    Task<T?> GetByIdAsync(Guid id);
    Task InsertAsync(T aggregate);
    Task UpdateAsync(T aggregate);
    Task DeleteAsync(T aggregate);
}

// Usage - only accepts aggregate roots
public class OrderRepository : IRepository<IOrder>
{
    // ...
}
```

### Finding All Aggregates

```csharp
// In tests or tooling
var aggregateTypes = typeof(MyAssembly).Assembly
    .GetTypes()
    .Where(t => typeof(IAggregateRoot).IsAssignableFrom(t) && !t.IsInterface);
```

---

## Future Enhancements

### Analyzer Rules

| Rule ID | Description |
|---------|-------------|
| NEATOO100 | Only IAggregateRoot should have [Insert]/[Update]/[Delete] |
| NEATOO101 | Child entities should not implement IAggregateRoot |
| NEATOO102 | Repository methods should accept IAggregateRoot only |

### Optional Enforcement in Save()

```csharp
// Future enhancement - opt-in strict mode
public virtual async Task<IEntityBase> Save()
{
    if (StrictAggregateMode && !(this is IAggregateRoot))
        throw new SaveOperationException(
            SaveFailureReason.NotAggregateRoot,
            "Only aggregate roots can be saved directly");
    // ...
}
```

---

## Implementation Tasks

- [ ] Create `IAggregateRoot` interface
- [ ] Add XML documentation with examples
- [ ] Update documentation to reference the marker
- [ ] Add example usage in sample projects
- [ ] Consider analyzer rules for future
- [ ] Update entity quick-start to mention marker

---

## Migration Notes

This is a purely additive change. Existing code will continue to work. Developers can optionally add the marker to their aggregate roots for better documentation.

---

## Files to Create/Modify

| File | Action |
|------|--------|
| `src/Neatoo/IAggregateRoot.cs` | Create interface |
| `docs/aggregates.md` | Update documentation |
| `samples/` | Add examples using marker |
