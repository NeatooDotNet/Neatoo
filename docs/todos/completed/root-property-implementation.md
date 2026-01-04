# Root Property Implementation

## Status: Complete

Add a `Root` property to entities that returns the aggregate root, enabling cross-aggregate validation.

---

## Purpose

The `Root` property identifies which aggregate an entity belongs to. This enables:

1. **Cross-aggregate constraint**: Throw when adding an entity from a different aggregate
2. **DDD compliance**: Enforce aggregate boundaries at runtime

---

## Design

### Interface

```csharp
public interface IEntityBase
{
    IBase? Parent { get; }    // Immediate containing entity
    IBase? Root { get; }      // Aggregate root (NEW)
}
```

### Computed Implementation

```csharp
// In EntityBase:
public IBase? Root => Parent == null ? null : (Parent.Root ?? Parent);
```

**Logic:**
- If `Parent` is null → entity is not in an aggregate → `Root` is null
- If `Parent.Root` is not null → return it (Parent knows its root)
- If `Parent.Root` is null → Parent IS the root → return Parent

### Example Hierarchy

```
Company (aggregate root)
├── Company.Parent = null
├── Company.Root = null (it IS the root)
│
└── Departments (list)
    └── Dept1
        ├── Dept1.Parent = Company
        ├── Dept1.Root = Company.Root ?? Company = Company ✓
        │
        └── Projects (list)
            └── Project1
                ├── Project1.Parent = Dept1
                ├── Project1.Root = Dept1.Root ?? Dept1 = Company ✓
```

**Verification:** Root correctly returns `Company` at any depth.

---

## Usage in Add Constraint

```csharp
// In EntityListBase.InsertItem():
if (item.Root != null && item.Root != this.Root)
{
    throw new InvalidOperationException(
        $"Cannot add {item.GetType().Name} to list: " +
        $"item belongs to aggregate '{item.Root.GetType().Name}', " +
        $"but this list belongs to aggregate '{this.Root?.GetType().Name ?? "none"}'.");
}
```

---

## Behavior Matrix

| Scenario | item.Root | this.Root | Result |
|----------|-----------|-----------|--------|
| Add brand new item | null | Company | ✅ Allowed |
| Add from same aggregate | Company | Company | ✅ Allowed |
| Add from different aggregate | Company1 | Company2 | ❌ Throw |
| Add to root-level list | Company | null | ✅ Allowed (list is the root) |

---

## DDD Justification

From DDD principles:
- Aggregates are consistency boundaries
- Entities belong to exactly one aggregate
- Cross-aggregate references should be by ID, not direct object reference

See [entitylistbase-add-use-cases.md](./entitylistbase-add-use-cases.md) for full DDD analysis.

---

## Implementation Considerations

### C1: Skip Check When Paused?

During deserialization (`IsPaused=true`), should we skip the Root check?

**Recommendation:** Yes - deserialized data is from trusted source (server/DB).

```csharp
if (!IsPaused && item.Root != null && item.Root != this.Root)
{
    throw new InvalidOperationException(...);
}
```

### C2: Performance

Root is computed by walking up the Parent chain.

| Depth | Cost |
|-------|------|
| 1-4 levels | Negligible |
| 10+ levels | Consider caching |

**Recommendation:** Use computed property. Hierarchy depth is typically shallow.

### C3: Which Interface?

Where should `Root` be defined?

| Option | Implication |
|--------|-------------|
| `IBase` | All Neatoo objects have Root |
| `IEntityBase` | Only entities have Root |

**Recommendation:** `IEntityBase` - Root is for aggregate membership, which is an entity concept.

---

## Dependencies

- None - Root is a standalone property

## Dependents

- [entitylistbase-add-use-cases.md](./entitylistbase-add-use-cases.md) - Add constraint uses Root
- [containinglist-property-implementation.md](./containinglist-property-implementation.md) - Intra-aggregate moves check Root first

---

## Task List

### Implementation
- [x] Add `Root` property to `IEntityBase` interface
- [x] Add `Root` to `IEntityListBase` interface
- [x] Implement computed `Root` in `EntityBase`
- [x] Implement `Root` in `EntityListBase` (lists also need Root)
- [x] Add cross-aggregate check in `EntityListBase.InsertItem()`
- [x] Skip check when `IsPaused`

### Testing
- [x] Unit test: Root computation at various depths
- [x] Unit test: Root is null for standalone entity
- [x] Unit test: Add from same aggregate succeeds
- [x] Unit test: Add from different aggregate throws
- [x] Unit test: Add brand new item (Root=null) succeeds
- [x] Unit test: Check skipped when paused

### Documentation
- [x] Update API documentation for Root property
- [x] Add examples showing cross-aggregate constraint

## Implementation Notes

### Files Modified
- `src/Neatoo/EntityBase.cs` - Added `Root` to interface and implementation
- `src/Neatoo/EntityListBase.cs` - Added `Root` to interface, implementation, and cross-aggregate check

### Test File
- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/RootPropertyTests.cs` - 13 tests covering all scenarios

### Documentation Updated
- `docs/aggregates-and-entities.md` - Added Root property to EntityBase table and new section on cross-aggregate enforcement
- `docs/meta-properties.md` - Added Parent and Root to property hierarchy and detailed sections
