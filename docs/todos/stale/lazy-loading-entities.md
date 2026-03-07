# Lazy Loading for Neatoo Entities

**Priority:** Medium
**Category:** Feature Enhancement
**Effort:** High
**Status:** Not Started

---

## Summary

Add lazy loading support for child entities in Neatoo aggregates. This allows child collections and navigation properties to be loaded on-demand rather than eagerly, improving performance for large aggregates.

---

## Problem

Currently, when fetching an aggregate root, all child entities must be loaded immediately. For aggregates with large child collections or deeply nested structures, this can cause:

- Unnecessary database queries for unused data
- Memory pressure from loading unneeded entities
- Slower initial load times
- Over-fetching in read scenarios where only root properties are needed

---

## Proposed Solution

Implement lazy loading for child entity properties that:

1. Defers loading until the property is first accessed
2. Integrates with the existing RemoteFactory infrastructure
3. Maintains parent-child relationship management
4. Works seamlessly with Blazor data binding

---

## Design Considerations

### API Options

**Option A: Attribute-based**
```csharp
public partial class Order : EntityBase<Order>
{
    [Lazy]
    public IEntityListBase<IOrderItem> Items { get => Getter<IEntityListBase<IOrderItem>>(); }
}
```

**Option B: Explicit Async Property**
```csharp
public partial class Order : EntityBase<Order>
{
    public async Task<IEntityListBase<IOrderItem>> GetItemsAsync() { ... }
}
```

**Option C: Lazy<T> Wrapper**
```csharp
public partial class Order : EntityBase<Order>
{
    public Lazy<IEntityListBase<IOrderItem>> Items { get; }
}
```

### Technical Challenges

| Challenge | Notes |
|-----------|-------|
| State tracking | How does IsDirty/IsNew propagate for unloaded children? |
| Client-server | Lazy load must work across RemoteFactory boundary |
| Serialization | How to serialize "not yet loaded" state |
| Change detection | PropertyChanged when lazy property is loaded |
| Cascade operations | Save/Delete must handle mixed loaded/unloaded state |

---

## Implementation Tasks

- [ ] Research existing lazy loading patterns (EF Core, NHibernate)
- [ ] Design API and attribute/marker approach
- [ ] Determine interaction with PropertyManager
- [ ] Define serialization format for lazy references
- [ ] Implement lazy property wrapper
- [ ] Generate lazy load factory methods
- [ ] Handle parent-child relationship on lazy load
- [ ] Add Blazor support (async loading in UI)
- [ ] Write documentation
- [ ] Create sample implementation

---

## Research Questions

1. Should lazy loading be opt-in per property or configurable at aggregate level?
2. How to handle validation rules that depend on lazy-loaded children?
3. What happens if lazy load fails (network error)? Error state on parent?
4. Should there be a "load all" method to eagerly load everything?
5. How does this interact with existing `[Remote]` attribute?

---

## Related

- EF Core lazy loading: proxies vs explicit loading
- CSLA.NET lazy loading pattern (potential prior art)

---

## Progress Log

### 2026-01-16
- Created initial todo document
- Outlined design options and challenges

---
