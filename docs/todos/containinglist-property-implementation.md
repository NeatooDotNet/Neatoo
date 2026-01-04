# ContainingList Property Implementation

## Status: Planning

Add a `ContainingList` property to entities that tracks which list contains them, enabling consistent Delete/Remove behavior and intra-aggregate moves.

---

## Purpose

The `ContainingList` property identifies which list an entity belongs to. This enables:

1. **Delete consistency**: `entity.Delete()` delegates to `list.Remove(entity)`
2. **Intra-aggregate moves**: Clean up DeletedList when moving between lists in same aggregate
3. **UnDelete support**: Know which list to restore to

---

## Design

### Visibility: Internal Only

`ContainingList` is an implementation detail for framework coordination, not a public API. It fits with existing internal interfaces in `InternalInterfaces.cs`.

### Interface Changes

```csharp
// In InternalInterfaces.cs - add to existing interface:
internal interface IEntityBaseInternal : IValidateBaseInternal
{
    void MarkModified();      // existing
    void MarkAsChild();       // existing

    // NEW:
    IEntityListBase? ContainingList { get; }
    void SetContainingList(IEntityListBase? list);
}

// In InternalInterfaces.cs - add to existing interface:
internal interface IEntityListBaseInternal
{
    IEnumerable DeletedList { get; }  // existing

    // NEW:
    void RemoveFromDeletedList(IEntityBase item);
}
```

This follows the existing pattern where internal operations are exposed through `*Internal` interfaces.

### Key Insight: ContainingList Stays Set When Removed

When an entity is removed from a list, it goes to `DeletedList` but `ContainingList` **stays set**. This represents "which list owns this entity for persistence."

| State | ContainingList | IsDeleted | In DeletedList | In Main List |
|-------|----------------|-----------|----------------|--------------|
| Active in list | Set | false | No | Yes |
| Removed (pending delete) | **Set** | true | Yes | No |
| After save (truly deleted) | null | true | No | No |
| Brand new (never added) | null | false | No | No |

---

## Implementation

### Setting ContainingList

```csharp
// In EntityListBase.InsertItem():
protected override void InsertItem(int index, I item)
{
    // ... validation checks ...

    // Handle intra-aggregate move (before setting new ContainingList)
    if (item.ContainingList != null && item.ContainingList != this)
    {
        // Same Root (already checked), different list - this is a move
        var oldList = (IEntityListBaseInternal)item.ContainingList;
        oldList.RemoveFromDeletedList(item);
        item.UnDelete();
    }

    // Set ContainingList to this list
    ((IEntityBaseInternal)item).SetContainingList(this);

    base.InsertItem(index, item);
}
```

### Keeping ContainingList on Remove

```csharp
// In EntityListBase.RemoveItem():
protected override void RemoveItem(int index)
{
    var item = this[index];

    if (!item.IsNew)
    {
        item.Delete();          // Sets IsDeleted = true
        DeletedList.Add(item);  // Track for DB deletion
    }

    // NOTE: Do NOT clear ContainingList here!
    // Item is still "owned" by this list for persistence.
    // ContainingList stays set so we can find it if moved.

    base.RemoveItem(index);
}
```

### Clearing ContainingList After Save

```csharp
// In EntityListBase.FactoryComplete():
public void FactoryComplete(FactoryOperation operation)
{
    if (operation == FactoryOperation.Update)
    {
        // Deletion is now persisted - clear ContainingList on deleted items
        foreach (var item in DeletedList)
        {
            ((IEntityBaseInternal)item).SetContainingList(null);
        }
        DeletedList.Clear();
    }

    // ... rest of factory complete logic ...
}
```

### Delete Delegates to List

```csharp
// In EntityBase.Delete():
public void Delete()
{
    if (ContainingList != null)
    {
        ContainingList.Remove(this);  // Delegates to list's Remove()
        return;                        // Remove() handles everything
    }

    MarkDeleted();  // Standalone entity (not in any list)
}
```

---

## Behavior: Delete() vs Remove()

**Before (inconsistent):**

| Operation | In List? | IsDeleted | In DeletedList |
|-----------|----------|-----------|----------------|
| `list.Remove(item)` | No | true | Yes |
| `item.Delete()` | **Yes** | true | **No** |

**After (consistent):**

| Operation | In List? | IsDeleted | In DeletedList |
|-----------|----------|-----------|----------------|
| `list.Remove(item)` | No | true | Yes |
| `item.Delete()` | No | true | Yes |

Both operations now behave identically.

---

## Behavior: Intra-Aggregate Moves

```csharp
// Move project from Dept1 to Dept2 (same Company aggregate)
var project = dept1.Projects[0];  // project.ContainingList = dept1.Projects

dept1.Projects.Remove(project);
// project.ContainingList = dept1.Projects (still set!)
// project in dept1.Projects.DeletedList
// project.IsDeleted = true

dept2.Projects.Add(project);
// Check: project.Root == this.Root ✓ (both Company)
// Check: project.ContainingList == dept1.Projects (different list)
// → Call dept1.Projects.RemoveFromDeletedList(project)
// → project.UnDelete()
// → project.ContainingList = dept2.Projects
// → Add to dept2.Projects

// Result: project cleanly moved, not in any DeletedList
```

---

## New Method: RemoveFromDeletedList

Need to expose a way to remove from DeletedList for intra-aggregate moves:

```csharp
public interface IEntityListBaseInternal
{
    IList<IEntityBase> DeletedList { get; }
    void RemoveFromDeletedList(IEntityBase item);
}

// Implementation:
internal void RemoveFromDeletedList(IEntityBase item)
{
    DeletedList.Remove((I)item);
}
```

---

## Edge Cases

### Entity Not In DeletedList But Has ContainingList

Could happen if:
- New item was added then removed (new items don't go to DeletedList)
- ContainingList would be set, but item isn't in DeletedList

**Handling:** Check if in DeletedList before trying to remove.

```csharp
if (item.ContainingList != null && item.ContainingList != this)
{
    var oldList = (IEntityListBaseInternal)item.ContainingList;
    if (oldList.DeletedList.Contains(item))
    {
        oldList.RemoveFromDeletedList(item);
    }
    item.UnDelete();  // Safe even if already not deleted
}
```

### ContainingList Set But List No Longer Exists

Could happen if list was garbage collected. The reference would be invalid.

**Handling:** This shouldn't happen in practice - if the list is gone, the entity should be too (part of same aggregate).

---

## Dependencies

- [root-property-implementation.md](./root-property-implementation.md) - Root check happens before ContainingList logic

## Dependents

- [entitylistbase-add-use-cases.md](./entitylistbase-add-use-cases.md) - Add logic uses ContainingList
- [entity-delete-vs-list-remove.md](./entity-delete-vs-list-remove.md) - Delete consistency uses ContainingList

---

## Task List

### Implementation
- [ ] Add `ContainingList` property to `IEntityBaseInternal` (internal, not public)
- [ ] Add `SetContainingList()` to `IEntityBaseInternal`
- [ ] Implement both in `EntityBase`
- [ ] Add `RemoveFromDeletedList()` to `IEntityListBaseInternal`
- [ ] Implement in `EntityListBase`
- [ ] Update `InsertItem()`: set ContainingList
- [ ] Update `InsertItem()`: handle intra-aggregate moves
- [ ] Update `RemoveItem()`: keep ContainingList set (don't clear)
- [ ] Update `FactoryComplete()`: clear ContainingList when DeletedList purged
- [ ] Update `Delete()`: delegate to ContainingList.Remove()

### Testing
- [ ] Unit test: ContainingList set on Add
- [ ] Unit test: ContainingList stays set on Remove
- [ ] Unit test: ContainingList cleared after save
- [ ] Unit test: Delete() removes from list
- [ ] Unit test: Delete() on standalone entity works
- [ ] Unit test: Intra-aggregate move cleans up DeletedList
- [ ] Unit test: Move new item between lists (no DeletedList involvement)

### Documentation
- [ ] Update API documentation for ContainingList property
- [ ] Add examples showing Delete/Remove consistency
- [ ] Add examples showing intra-aggregate moves
