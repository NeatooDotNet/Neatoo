# C1: Add Domain Events Support

**Priority:** Critical
**Category:** Missing DDD Feature
**Effort:** High
**Status:** Not Started

---

## Problem Statement

Neatoo currently has no built-in domain event support. This creates several issues:

1. Cross-aggregate communication requires manual coordination
2. Side effects (notifications, auditing) must be handled directly in factory methods
3. Event sourcing integration is not possible
4. No decoupled way to react to domain state changes

---

## Proposed Solution

### 1. Define Domain Event Infrastructure

```csharp
// Core interface
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
    Guid EventId { get; }
}

// Base implementation
public abstract record DomainEventBase : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public Guid EventId { get; init; } = Guid.NewGuid();
}
```

### 2. Add Event Collection to EntityBase

```csharp
public abstract class EntityBase<T> : ValidateBase<T>, IEntityBase
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected void AddDomainEvent(IDomainEvent @event)
        => _domainEvents.Add(@event);

    public IReadOnlyList<IDomainEvent> DomainEvents
        => _domainEvents.AsReadOnly();

    internal void ClearDomainEvents() => _domainEvents.Clear();
}
```

### 3. Create Event Dispatcher

```csharp
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken token = default);
    Task DispatchAsync(IDomainEvent @event, CancellationToken token = default);
}

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken token = default);
}
```

### 4. Usage Pattern

```csharp
// Define an event
public record OrderPlacedEvent : DomainEventBase
{
    public Guid OrderId { get; init; }
    public decimal TotalAmount { get; init; }
}

// Raise in entity
[Insert]
public async Task Insert([Service] IDbContext db, [Service] IDomainEventDispatcher dispatcher)
{
    await RunRules();
    if (!IsSavable) return;

    AddDomainEvent(new OrderPlacedEvent
    {
        OrderId = Id,
        TotalAmount = Total
    });

    // Persistence...
    await db.SaveChangesAsync();

    // Dispatch after successful save
    await dispatcher.DispatchAsync(DomainEvents);
    ClearDomainEvents();
}
```

---

## Implementation Tasks

- [ ] Create `IDomainEvent` interface in `Neatoo/DomainEvents/`
- [ ] Create `DomainEventBase` abstract record
- [ ] Add `_domainEvents` collection to `EntityBase<T>`
- [ ] Add `AddDomainEvent()`, `DomainEvents`, `ClearDomainEvents()` members
- [ ] Create `IDomainEventDispatcher` interface
- [ ] Create `IDomainEventHandler<T>` interface
- [ ] Create default `DomainEventDispatcher` implementation using DI
- [ ] Add DI registration extension methods
- [ ] Handle serialization of pending events (client-server scenarios)
- [ ] Write unit tests for event collection
- [ ] Write integration tests for dispatch
- [ ] Add documentation with examples
- [ ] Consider: Should events auto-dispatch on successful Save()?

---

## Design Decisions Needed

1. **When to dispatch?**
   - Option A: Manually in factory methods (more control)
   - Option B: Automatically after successful Save() (less boilerplate)
   - Option C: Both (default auto, opt-out available)

2. **Client-server handling?**
   - Should events raised on client be serialized and dispatched on server?
   - Or should events only be raised server-side?

3. **Transaction scope?**
   - Should dispatch happen inside or outside the transaction?
   - Outbox pattern support?

---

## Files to Create/Modify

| File | Action |
|------|--------|
| `src/Neatoo/DomainEvents/IDomainEvent.cs` | Create |
| `src/Neatoo/DomainEvents/DomainEventBase.cs` | Create |
| `src/Neatoo/DomainEvents/IDomainEventDispatcher.cs` | Create |
| `src/Neatoo/DomainEvents/IDomainEventHandler.cs` | Create |
| `src/Neatoo/DomainEvents/DomainEventDispatcher.cs` | Create |
| `src/Neatoo/EntityBase.cs` | Modify |
| `src/Neatoo/IEntityBase.cs` | Modify |
| `src/Neatoo/ServiceCollectionExtensions.cs` | Modify |
| `docs/domain-events.md` | Create |

---

## References

- [Microsoft Domain Events Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)
- [Jimmy Bogard on Domain Events](https://lostechies.com/jimmybogard/2014/05/13/a-better-domain-events-pattern/)
