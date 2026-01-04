# Entity.Delete() vs List.Remove() Inconsistency

## Status: Complete

**Solution implemented:** Option B (Entity Tracks Its List via `ContainingList` property). See [containinglist-property-implementation.md](./containinglist-property-implementation.md) for implementation details.

Investigate and resolve the inconsistency between calling `Delete()` on an entity vs removing it from its containing list.

### Related Implementation Plans

| Document | Purpose |
|----------|---------|
| [containinglist-property-implementation.md](./containinglist-property-implementation.md) | `ContainingList` property (the solution) |
| [root-property-implementation.md](./root-property-implementation.md) | `Root` property for aggregate checks |
| [entitylistbase-add-use-cases.md](./entitylistbase-add-use-cases.md) | Add scenarios and requirements |

---

## The Problem

There's an inconsistency between two ways to "delete" an entity that's in a list:

| Operation | In List? | IsDeleted | In DeletedList? | Visible? |
|-----------|----------|-----------|-----------------|----------|
| `list.Remove(item)` (existing) | No | true | Yes | No |
| `item.Delete()` (while in list) | **Yes** | true | No | **Yes** |

### Example

```csharp
var phone = person.Phones[0];  // Existing item from DB

// Option A: Remove from list
person.Phones.Remove(phone);
// Result:
// - phone.IsDeleted = true
// - phone NOT in person.Phones (not visible)
// - phone IN person.Phones.DeletedList
// - On save: deleted from DB ✓

// Option B: Call Delete directly
phone.Delete();
// Result:
// - phone.IsDeleted = true
// - phone STILL in person.Phones (visible!) ← INCONSISTENT
// - phone NOT in DeletedList
// - On save: what happens?? ← UNCLEAR
```

### Related: UnDelete() After Remove

```csharp
person.Phones.Remove(phone);      // phone in DeletedList, IsDeleted=true
phone.UnDelete();                 // phone.IsDeleted = false
// Result:
// - phone.IsDeleted = false
// - phone still in DeletedList ← WRONG?
// - phone NOT back in main list ← EXPECTED?
```

---

## Root Cause

**Entities know their Parent (aggregate root), but NOT their containing list.**

| What Entity Knows | What Entity Doesn't Know |
|-------------------|-------------------------|
| `Parent` (aggregate root) | Which list contains it |
| `Root` (proposed) | Whether it's in a DeletedList |
| `IsDeleted` (own state) | How to remove itself from list |
| `IsChild` | Which specific list it belongs to |

This creates a disconnect - the entity can change its own state (`IsDeleted`), but can't update the list's state (membership, DeletedList).

---

## Questions to Answer

1. **What happens on Save when `item.IsDeleted=true` but item is still in list?**
   - Current behavior: ??? (needs verification)
   - Does save logic check `IsDeleted` on each item?
   - Or does it only process `DeletedList`?

2. **Is there a legitimate use case for `item.Delete()` on a listed item?**
   - Maybe for "soft delete" display purposes?
   - Or is it always a mistake?

3. **Should `UnDelete()` restore list membership?**
   - If item was removed (in DeletedList), should UnDelete put it back?
   - Or is UnDelete only for items that were `Delete()`d directly?

4. **Should entity track its containing list?**
   - Would enable auto-removal on Delete()
   - Would enable proper UnDelete() behavior
   - Adds complexity and memory overhead

---

## Options

### Option A: Document the Quirks

**Description:** Don't change behavior. Document that users should use `Remove()`, not `Delete()`, for items in lists.

| Pros | Cons |
|------|------|
| No code changes | Confusing API |
| Non-breaking | Easy to make mistakes |
| Simple | Inconsistent mental model |

**Implementation:** Documentation only.

---

### Option B: Entity Tracks Its List (RECOMMENDED)

**Description:** Entity knows which list contains it. `Delete()` auto-removes from list.

```csharp
public interface IEntityBase
{
    IBase? Parent { get; }
    IBase? Root { get; }
    IEntityListBase? ContainingList { get; }  // NEW
}
```

**Key insight:** `ContainingList` stays set even when item is in DeletedList. It represents "which list owns this entity for persistence purposes."

| State | ContainingList | IsDeleted | In DeletedList | In Main List |
|-------|----------------|-----------|----------------|--------------|
| Active in list | Set | false | No | Yes |
| Removed (pending delete) | **Set** | true | Yes | No |
| After save (truly deleted) | null | true | No | No |
| Brand new (never added) | null | false | No | No |

**Delete() implementation:**
```csharp
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

**Result: Both operations now behave identically:**

| Operation | Removes from list? | In DeletedList? | IsDeleted? | ContainingList? |
|-----------|-------------------|-----------------|------------|-----------------|
| `list.Remove(item)` | Yes | Yes (if existing) | true | Set |
| `item.Delete()` | Yes (delegates) | Yes (if existing) | true | Set |

**Intra-aggregate move now works:**
```csharp
dept1.Projects.Remove(project);
// project.ContainingList = dept1.Projects (still set!)
// project in dept1.Projects.DeletedList
// project.IsDeleted = true

dept2.Projects.Add(project);
// Detect: project.ContainingList == dept1.Projects (different list, same Root)
// → Remove from dept1.Projects.DeletedList
// → project.IsDeleted = false (UnDelete)
// → Add to dept2.Projects
// → project.ContainingList = dept2.Projects
```

| Pros | Cons |
|------|------|
| Consistent `Delete()` behavior | Memory overhead (one reference per entity) |
| Enables intra-aggregate moves | Must maintain in sync |
| Solves DeletedList issue | |
| Solves UnDelete issue | |

**Implementation:**
- Add `ContainingList` property to `IEntityBase`
- Set in `InsertItem()`
- Keep set in `RemoveItem()` (item goes to DeletedList)
- Clear only in `FactoryComplete()` when DeletedList is purged
- Modify `Delete()` to delegate to list
- Modify `InsertItem()` to handle moves (remove from old DeletedList)

---

### Option C: List Filters Deleted Items

**Description:** List enumeration skips items where `IsDeleted=true`.

```csharp
// In EntityListBase:
public new IEnumerator<I> GetEnumerator()
{
    return base.Items.Where(x => !x.IsDeleted).GetEnumerator();
}

public new int Count => base.Items.Count(x => !x.IsDeleted);
```

| Pros | Cons |
|------|------|
| `Delete()` "works" (item disappears) | Hidden state |
| Non-breaking for existing code | Confusing - item in list but not enumerated |
| Simple implementation | `list[index]` still returns deleted items? |
| | Count vs actual backing count mismatch |

**Implementation:** Override enumeration to filter.

---

### Option D: Prevent Delete() on Listed Items

**Description:** Throw exception if `Delete()` is called on an entity that's in a list.

```csharp
// In EntityBase.Delete():
public void Delete()
{
    if (ContainingList != null)
    {
        throw new InvalidOperationException(
            "Cannot call Delete() on an entity in a list. " +
            "Use list.Remove(entity) instead.");
    }
    IsDeleted = true;
}
```

| Pros | Cons |
|------|------|
| Forces correct usage | Restrictive |
| Clear error message | Breaking change |
| Simple implementation | Still needs `ContainingList` tracking |

**Implementation:** Add `ContainingList` tracking, throw in `Delete()`.

---

### Option E: Sync State on Save

**Description:** Save logic reconciles mismatches between list membership and `IsDeleted` state.

```csharp
// In save/factory logic:
foreach (var item in list)
{
    if (item.IsDeleted && !deletedList.Contains(item))
    {
        // Item was Delete()'d directly - treat as deleted
        deletedList.Add(item);
        list.Remove(item);  // Or handle in save
    }
}
```

| Pros | Cons |
|------|------|
| Handles edge cases | Complex save logic |
| Non-breaking | Delayed consistency |
| No entity changes | State is weird until save |

**Implementation:** Add reconciliation in factory/save methods.

---

## Recommendation

**Option B: Entity Tracks Its List**

This approach solves multiple problems with one mechanism:

| Problem | How ContainingList Solves It |
|---------|------------------------------|
| `Delete()` vs `Remove()` inconsistency | `Delete()` delegates to `ContainingList.Remove()` |
| Intra-aggregate moves with DeletedList | On Add, remove from old list's DeletedList |
| `UnDelete()` not restoring list membership | Can restore to ContainingList |
| Entity doesn't know its list | Now it does |

**Key design decision:** `ContainingList` stays set when item is removed (goes to DeletedList). Only cleared after save completes.

**Implementation order:**
1. Add `ContainingList` property to `IEntityBase`
2. Update `InsertItem()` to set it
3. Update `RemoveItem()` to keep it set (don't clear)
4. Update `FactoryComplete()` to clear when DeletedList is purged
5. Update `Delete()` to delegate to list
6. Update `InsertItem()` to handle intra-aggregate moves (remove from old DeletedList)

---

## Related Issues

- [entitylistbase-add-use-cases.md](./entitylistbase-add-use-cases.md) - Add scenarios and Root property
- Intra-aggregate moves with DeletedList (C2 in add use cases)

---

## Task List

### Phase 1: Investigate Current Behavior
- [ ] Test what happens on save when `item.IsDeleted=true` but item still in list
- [ ] Test `UnDelete()` behavior on items in DeletedList
- [ ] Review factory/save code for how IsDeleted is handled
- [ ] Document current behavior

### Phase 2: Design Decision
- [ ] Review options with stakeholder
- [ ] Decide on approach
- [ ] Document decision rationale

### Phase 3: Implementation (TBD)
- [ ] _[Depends on chosen approach]_

### Phase 4: Testing
- [ ] Unit tests for chosen behavior
- [ ] Integration tests for save scenarios

### Phase 5: Documentation
- [ ] Update API documentation
- [ ] Add examples showing correct usage
