# C1: Add Domain Events Support

**Priority:** Critical
**Category:** Missing DDD Feature
**Effort:** Low (revised from High - leverages RemoteFactory 10.6.0)
**Status:** Not Started

---

## Problem Statement

Neatoo currently has no built-in domain event support. This creates several issues:

1. Cross-aggregate communication requires manual coordination
2. Side effects (notifications, auditing) must be handled directly in factory methods
3. No decoupled way to react to domain state changes

---

## Solution: Leverage RemoteFactory 10.6.0

RemoteFactory 10.6.0 introduced the `[Event]` attribute which provides:

- Fire-and-forget execution with isolated DI scopes
- Graceful shutdown tracking via `IEventTracker`
- Remote execution across client-server boundary
- Exception handling (logged, not thrown to caller)

Neatoo only needs to add **event collection on entities**. The dispatch infrastructure is handled by RemoteFactory.

---

## Implementation

### 1. Add Event Collection to EntityBase

```csharp
public abstract class EntityBase<T> : ValidateBase<T>, IEntityBase
{
    private List<object>? _domainEvents;

    protected void RaiseDomainEvent(object domainEvent)
    {
        _domainEvents ??= new();
        _domainEvents.Add(domainEvent);
    }

    public IReadOnlyList<object> DomainEvents
        => (IReadOnlyList<object>?)_domainEvents ?? [];

    public void ClearDomainEvents() => _domainEvents?.Clear();
}
```

### 2. Define Event Handlers with RemoteFactory [Event]

```csharp
// Event record (simple data carrier)
public record OrderPlacedEvent(Guid OrderId, decimal Total);

// Handler class with [Factory] and [Event]
[Factory]
public static partial class OrderEvents
{
    [Event]
    public static Task OnOrderPlaced(
        Guid orderId,
        decimal total,
        [Service] IEmailService email,
        [Service] IAnalyticsService analytics,
        CancellationToken ct)
    {
        // Fire-and-forget: runs in isolated scope
        return Task.WhenAll(
            email.SendOrderConfirmationAsync(orderId, ct),
            analytics.TrackOrderAsync(orderId, total, ct));
    }
}
```

### 3. Raise and Dispatch in Factory Methods

```csharp
public partial class Order : EntityBase<Order>
{
    public Guid Id { get; private set; }
    public decimal Total { get => Getter<decimal>(); set => Setter(value); }

    [Remote]
    [Insert]
    public async Task Insert(
        [Service] IDbContext db,
        [Service] OrderEvents.OnOrderPlacedEvent onOrderPlaced)
    {
        Id = Guid.NewGuid();

        // Raise event (collected on entity)
        RaiseDomainEvent(new OrderPlacedEvent(Id, Total));

        // Persist
        db.Orders.Add(this);
        await db.SaveChangesAsync();

        // Dispatch after successful save
        foreach (var evt in DomainEvents.OfType<OrderPlacedEvent>())
        {
            _ = onOrderPlaced(evt.OrderId, evt.Total);  // Fire-and-forget
        }
        ClearDomainEvents();
    }
}
```

---

## Why This Approach

| Concern | Solution |
|---------|----------|
| Event collection | `RaiseDomainEvent()` on EntityBase |
| Handler execution | RemoteFactory `[Event]` attribute |
| DI scope isolation | RemoteFactory (automatic) |
| Graceful shutdown | RemoteFactory `IEventTracker` |
| Remote execution | RemoteFactory (works across client-server) |
| Exception handling | RemoteFactory (logged, not thrown) |

---

## Implementation Tasks

- [ ] Add `_domainEvents` collection to `EntityBase<T>`
- [ ] Add `RaiseDomainEvent(object)` protected method
- [ ] Add `DomainEvents` public readonly property
- [ ] Add `ClearDomainEvents()` public method
- [ ] Update `IEntityBase` interface if needed
- [ ] Write unit tests for event collection
- [ ] Create integration test with RemoteFactory `[Event]`
- [ ] Add documentation with examples

---

## Optional: Typed Event Interface

For consistency, add optional marker interface:

```csharp
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}

public abstract record DomainEventBase : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}

// Usage
public record OrderPlacedEvent(Guid OrderId, decimal Total) : DomainEventBase;
```

This is optional - events can be any object type.

---

## Files to Create/Modify

| File | Action |
|------|--------|
| `src/Neatoo/EntityBase.cs` | Add event collection members |
| `src/Neatoo/IEntityBase.cs` | Add `DomainEvents`, `ClearDomainEvents()` |
| `src/Neatoo/DomainEvents/IDomainEvent.cs` | Create (optional) |
| `src/Neatoo/DomainEvents/DomainEventBase.cs` | Create (optional) |
| `docs/concepts/domain-events.md` | Create |

---

## Design Notes

**Why `object` instead of `IDomainEvent`?**
- Simpler - no base type requirement
- Works with records, classes, any type
- RemoteFactory `[Event]` handlers take primitive parameters anyway

**Why manual dispatch in factory methods?**
- Explicit control over timing (after SaveChanges)
- No magic - developer sees exactly what happens
- Matches RemoteFactory's delegate injection pattern

**Why fire-and-forget (`_ = onOrderPlaced(...)`)?**
- Event handlers run in isolated scopes
- Failures logged but don't fail the operation
- Use `await` if you need to ensure completion

---

## References

- [RemoteFactory Events Documentation](https://github.com/NeatooDotNet/RemoteFactory/docs/concepts/events.md)
- RemoteFactory 10.6.0 release notes
