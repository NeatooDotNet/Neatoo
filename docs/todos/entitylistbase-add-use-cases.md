# EntityListBase Add Item Use Cases

## Status: Planning

Track all use cases for adding an `EntityBase` to an `EntityListBase`, ensuring they are handled correctly, unit tested, and documented.

### Related Implementation Plans

| Document | Purpose |
|----------|---------|
| [root-property-implementation.md](./root-property-implementation.md) | `Root` property for cross-aggregate check |
| [containinglist-property-implementation.md](./containinglist-property-implementation.md) | `ContainingList` property for intra-aggregate moves |
| [entity-delete-vs-list-remove.md](./entity-delete-vs-list-remove.md) | Delete/Remove consistency |

---

## Questions Requiring Answers

### Q1: Null Item Handling
**Question:** Should adding `null` to an EntityListBase throw an exception or be silently ignored?

**Answer:** Throw an exception. Lists are expected to be serialized and null items don't make sense in that context.

**Decision:** ✅ Throw `ArgumentNullException`

---

### Q2: Duplicate Item Handling
**Question:** Should adding the same item instance twice throw an exception, be silently ignored, or allowed?

**Answer:** Throw an exception. Could create circular references and other issues.

**Decision:** ✅ Throw `InvalidOperationException` if item already in list

---

### Q3: Item Already in Another List/Aggregate
**Question:** If an item already belongs to another list (has a different Parent), should we:
- A) Throw an exception (item can only belong to one aggregate)
- B) Silently overwrite the parent (current behavior)
- C) Detach from old parent first, then add

**Answer:** This is a gap in the implementation requiring deeper analysis before deciding.

**Decision:** ✅ Compare Root (aggregate root), not Parent

**Status:**
- EF Core behavior analyzed
- Parent vs Root distinction identified
- **Decision made:** Add `Root` property, throw if `item.Root != this.Root`
- Allows intra-aggregate moves (e.g., Project between Departments)
- Prevents cross-aggregate moves

#### Q3 Analysis: Moving Items Between Lists/Aggregates

##### Scenarios

| # | Scenario | Description | Current Behavior | Problems |
|---|----------|-------------|------------------|----------|
| 3.A | Move new item | Item `IsNew=true`, remove from ListA, add to ListB | Parent overwritten | Relatively safe - no DB state |
| 3.B | Move existing item | Item `IsNew=false`, remove from ListA, add to ListB | ListA adds to DeletedList, ListB overwrites Parent | **Item scheduled for deletion in ListA!** When ListA saves, item deleted from DB |
| 3.C | Add deleted item from another aggregate | Item in ListA.DeletedList, user adds to ListB | Item added to ListB, Parent overwritten | Item in TWO places: ListA.DeletedList AND ListB |
| 3.D | Add without removing first | Item still in ListA, also add to ListB | Parent overwritten, item in both lists | Same instance in two collections |
| 3.E | Move item with unsaved changes | Item has modified properties | Parent overwritten | Context of "original values" may be lost |
| 3.F | Move across aggregates | Item validated in context of AggregateA | Parent changes to AggregateB | Validation rules may reference wrong siblings |

##### What Can Go Wrong

| # | Risk | Description |
|---|------|-------------|
| W1 | **Data Loss** | ListA saves → deletes item from DB. ListB saves → tries to update non-existent record |
| W2 | **Orphaned Event Handlers** | ListA still subscribed to item.PropertyChanged after "move" |
| W3 | **Double-Counted State** | Item's `IsValid=false` counted by BOTH lists' IsValid aggregation |
| W4 | **DeletedList Corruption** | Item simultaneously in ListA.DeletedList AND ListB's main collection |
| W5 | **Serialization Cycles** | Both lists attempt to serialize the same item instance |
| W6 | **Memory Leaks** | Old list holds reference, prevents garbage collection |
| W7 | **Validation Context** | Item's rules may depend on siblings that no longer exist in new context |

##### Possible Approaches (No Decision Yet)

| Approach | Description | Pros | Cons |
|----------|-------------|------|------|
| **A) Throw if has different Parent** | Exception when adding item that has a Parent different from target list's Parent | Safest, explicit | User must manually remove first |
| **B) Require explicit detach** | Provide `Detach()` method that cleanly removes from current list without marking deleted | Clear intent | More API surface |
| **C) Auto-detach** | Automatically remove from old list when adding to new | Convenient | Magic behavior, hard to debug |
| **D) Allow but warn** | Log warning, proceed with current behavior | Non-breaking | Silently corrupts state |
| **E) Track list membership** | Item tracks which list(s) contain it | Comprehensive | Significant implementation change |

##### Open Questions for Q3

1. Is there a legitimate use case for moving an item between aggregates?
2. Should "move" be an explicit operation (Remove + Add) or a single atomic operation?
3. How should DeletedList items be handled if re-added to a different list?
4. Should items track their containing list (not just Parent aggregate)?

---

#### Q3 Reference: How EF Core Handles This

EF Core has sophisticated handling for moving entities between collections. This may inform Neatoo's design.

##### EF Core Key Behaviors

| Behavior | Description |
|----------|-------------|
| **Auto-remove from old collection** | When you add an entity to a new collection, EF Core automatically removes it from the old collection |
| **Relationship fixup** | EF Core keeps FK values and navigation properties in sync automatically |
| **Single instance per key** | Only one instance of an entity with a given PK can be tracked per DbContext |
| **Re-parenting support** | With `DeleteOrphansTiming.OnSaveChanges`, entities can be temporarily orphaned then re-parented |

##### EF Core Move Example

```csharp
// EF Core automatically handles this:
var post = blogA.Posts.Single(e => e.Title == "My Post");
blogB.Posts.Add(post);
// Result: post automatically removed from blogA.Posts, FK updated to blogB.Id
```

##### EF Core Re-parenting (Delayed Orphan Deletion)

```csharp
context.ChangeTracker.DeleteOrphansTiming = CascadeTiming.OnSaveChanges;

var post = blogA.Posts.Single(e => e.Title == "My Post");
blogA.Posts.Remove(post);  // Not deleted yet - orphaned
context.ChangeTracker.DetectChanges();

blogB.Posts.Add(post);     // Re-parented before SaveChanges
context.ChangeTracker.DetectChanges();

await context.SaveChangesAsync(); // UPDATE, not DELETE+INSERT
```

##### EF Core Delete Behavior (Required vs Optional Relationships)

| Relationship Type | On Remove from Collection | Neatoo Equivalent |
|-------------------|---------------------------|-------------------|
| Optional (FK nullable) | FK set to null, entity remains | N/A - Neatoo uses aggregate pattern |
| Required (FK not null) | Entity marked as Deleted (orphan deletion) | Similar to Neatoo's DeletedList |

##### Key Differences: EF Core vs Neatoo

| Aspect | EF Core | Neatoo |
|--------|---------|--------|
| **Scope** | Single DbContext | Aggregate root (could span multiple lists) |
| **Identity** | Tracked by PK within DbContext | Parent reference on entity |
| **Auto-remove** | Yes - removes from old collection | No - currently allows item in multiple collections |
| **Orphan handling** | Configurable timing | Immediate (DeletedList on remove) |
| **Constraint** | Throws if same PK tracked twice | No constraint currently |

##### Critical Difference: No Key in Neatoo

EF Core's duplicate detection is based on **primary key** - it prevents two different object instances with the same PK from being tracked. Neatoo has no inherent key concept.

| Scenario | EF Core | Neatoo |
|----------|---------|--------|
| Same **instance** added twice | Detected (same PK) | Can detect via `Contains()` or `Parent` check |
| Same **instance** in two lists | Detected (same PK) | Can detect via `Parent` property |
| Two **different instances** representing same DB entity | Detected (same PK) | **Cannot detect** - no key to compare |

**Implications:**
- Neatoo can only prevent **same instance** problems, not **same logical entity** problems
- `item.Parent != null` check catches same instance in another aggregate
- `list.Contains(item)` check catches same instance already in this list
- But if user creates two `Person` objects both with `Id = 5`, Neatoo can't know they're "the same"

**This means Neatoo's constraint would be:**
- Throw if `item.Parent != null && item.Parent != this.Parent` (same instance, different aggregate)
- Throw if `this.Contains(item)` (same instance, already in list)
- Cannot prevent duplicate logical entities (that's a repository/factory concern)

---

#### Q3 Recommendation: Throw if Different Parent

**Recommended Approach:** Option A - Throw exception if `item.Parent != null && item.Parent != this.Parent`

##### Why This Is Logical

1. **Prevents all identified risks with one simple check**

   | Risk | How It's Prevented |
   |------|-------------------|
   | W1: Data Loss | Can't add item that's pending deletion in another aggregate |
   | W2: Orphaned Handlers | Item can't be in two lists simultaneously |
   | W3: Double-Counted State | Item's IsValid/IsBusy only counted once |
   | W4: DeletedList Corruption | Deleted items retain Parent, so they throw |
   | W5: Serialization Cycles | Item only serialized in one aggregate |
   | W6: Memory Leaks | Clear ownership, no dangling references |
   | W7: Validation Context | Item only validated in one aggregate context |

2. **Matches DDD principles**
   - An entity belongs to exactly one aggregate at a time
   - The aggregate root is the consistency boundary
   - Cross-aggregate operations should be explicit

3. **Simple implementation**
   ```csharp
   // In EntityListBase.InsertItem():
   if (item.Parent != null && item.Parent != this.Parent)
   {
       throw new InvalidOperationException(
           $"Cannot add item to list: item already belongs to a different aggregate. " +
           "Remove the item from its current list before adding to a new one.");
   }
   ```

4. **Allows explicit Remove → Add workflow**
   - User CAN move items between aggregates
   - But must do so explicitly and intentionally
   - Prevents accidental corruption

##### Example: Moving a Phone Between People

```csharp
// Setup: Two people, PersonA has a phone
var personA = await PersonFactory.Fetch(1);
var personB = await PersonFactory.Fetch(2);
var phone = personA.Phones[0]; // phone.Parent = personA

// ❌ WRONG - Throws exception (prevents accidental corruption)
personB.Phones.Add(phone);
// Result: InvalidOperationException
// "Cannot add item to list: item already belongs to a different aggregate.
//  Remove the item from its current list before adding to a new one."

// ✅ RIGHT for NEW items - Explicit move
var newPhone = PhoneFactory.Create();
personA.Phones.Add(newPhone);     // newPhone.Parent = personA
personA.Phones.Remove(newPhone);  // newPhone.Parent = null (new item, not in DeletedList)
personB.Phones.Add(newPhone);     // Allowed - Parent is null

// ⚠️ COMPLEX for EXISTING items - Requires understanding
var existingPhone = personA.Phones[0];  // Loaded from DB
personA.Phones.Remove(existingPhone);   // Goes to personA's DeletedList!
personB.Phones.Add(existingPhone);      // Parent was cleared, so allowed...
// BUT: When personA saves → phone DELETED from DB
//      When personB saves → phone treated as new INSERT (loses original ID?)
// This is a CROSS-AGGREGATE MOVE - inherently complex!
```

##### The Complexity of Moving Existing Items

Moving existing (non-new) items between aggregates is inherently complex:

| Step | What Happens | Problem |
|------|--------------|---------|
| `listA.Remove(existingItem)` | Item added to `listA.DeletedList` | Pending deletion |
| `listB.Add(existingItem)` | Item added to `listB`, marked as child | Now in two places |
| Save `listA` | Item deleted from DB | Data loss! |
| Save `listB` | Tries to update deleted record | Error or orphan |

**This is not a Neatoo problem - it's an inherent DDD complexity.** Moving an entity between aggregates is a domain operation that requires:
1. Understanding the business meaning of the move
2. Coordinating saves (or using a Unit of Work)
3. Possibly using a domain event or saga

**Neatoo's job:** Prevent accidental corruption. Make the developer explicitly handle the complexity.

##### What Needs to Happen for Remove → Add to Work

For the explicit workflow to work safely, `Remove()` must clear the Parent:

```csharp
// Current behavior (needs verification):
protected override void RemoveItem(int index)
{
    var item = this[index];

    if (!item.IsNew)
    {
        item.Delete();
        DeletedList.Add(item);
    }

    // NEEDED: Clear Parent so item can be re-added elsewhere
    ((ISetParent)item).SetParent(null);

    base.RemoveItem(index);
}
```

##### Summary

| Item Type | Remove → Add to Different Aggregate | DeletedList? | Result |
|-----------|-------------------------------------|--------------|--------|
| New item (`IsNew=true`) | Works cleanly | No | Item moves, no DB impact |
| Existing item (`IsNew=false`) | **Broken** | Yes - still in old list's DeletedList | Item in TWO places, data loss on save |

##### The Existing Item Problem

Even with the throw-on-different-Parent constraint, existing items cannot be safely moved:

```csharp
var existingPhone = personA.Phones[0];  // IsNew=false, loaded from DB

personA.Phones.Remove(existingPhone);
// Result:
// - existingPhone added to personA.DeletedList ← STILL HERE!
// - existingPhone.Parent cleared (if we implement that)

personB.Phones.Add(existingPhone);
// Result:
// - existingPhone added to personB.Phones
// - existingPhone.Parent = personB
// - existingPhone STILL in personA.DeletedList!

await personA.Save();  // Deletes phone from DB!
await personB.Save();  // Tries to update deleted record - ERROR
```

**Conclusion:** The Remove → Add pattern only works for NEW items. Moving existing items between aggregates is not supported without additional infrastructure (e.g., a `Transfer()` method that removes from DeletedList).

##### Options for Existing Item Moves

| Option | Description | Complexity |
|--------|-------------|------------|
| **A) Don't support it** | Document that existing items cannot move between aggregates | Simple |
| **B) Add `Detach()` method** | Removes from list AND DeletedList, clears Parent | Medium |
| **C) Add `Transfer(item, toList)` method** | Atomic move that handles all state | Medium |
| **D) Track source list on item** | Item knows which list it's in, can remove itself | Complex |

**Recommendation:** Start with Option A (don't support). Document the limitation. If a real use case emerges, consider Option B or C.

**Final Recommendation:** Implement the throw-on-different-Parent constraint. Document that:
1. New items can be moved via Remove → Add
2. Existing items cannot be safely moved between aggregates (by design)

---

#### Q3 Addendum: Parent vs Root

##### The Problem

`item.Parent` is the **immediate containing entity**, not necessarily the aggregate root:

```
Company (aggregate root)
└── Departments (list)
    ├── Dept1
    │   └── Projects (list)
    │       └── Project1  ← Parent = Dept1, NOT Company
    └── Dept2
        └── Projects (list)
```

##### Comparing Parent vs Root

| Scenario | Compare Parent | Compare Root |
|----------|----------------|--------------|
| Move Project from Dept1 to Dept2 (SAME aggregate) | `Dept1 != Dept2` → **Throws** | `Company == Company` → Allowed |
| Move Project to different Company (DIFFERENT aggregate) | `Dept1 != Dept3` → Throws | `Company1 != Company2` → Throws |

##### What Behavior Do We Want?

| Option | Constraint | Behavior |
|--------|------------|----------|
| **Compare Parent** | `item.Parent != this.Parent` | Entity belongs to specific owner. Moving between Depts throws. |
| **Compare Root** | `item.Root != this.Root` | Entity belongs to aggregate. Moving within aggregate allowed. |

##### Do We Need a Root Property?

**Current state:** Neatoo has `item.Parent` but NOT `item.Root`.

**To support intra-aggregate moves, we'd need:**
```csharp
public interface IBase
{
    IBase? Parent { get; }  // Immediate container
    IBase? Root { get; }    // Aggregate root (follows Parent chain to top)
}

// Implementation could be computed:
public IBase? Root => Parent?.Root ?? Parent ?? this;
```

##### Questions to Answer

1. **Is intra-aggregate moving a real use case?**
   - Moving Project between Departments in same Company
   - Moving Item from PendingItems to ShippedItems in same Order

2. **Which constraint is safer?**
   - Compare Parent: More restrictive, prevents more mistakes
   - Compare Root: More permissive, requires Root property

3. **Should Root be computed or tracked?**
   - Computed: `Root => Parent?.Root ?? this` (walks up chain)
   - Tracked: Set explicitly when added to aggregate

##### DDD Principles on Aggregate Boundaries

Research into DDD principles strongly supports preventing cross-aggregate entity sharing:

| Principle | Description | Source |
|-----------|-------------|--------|
| **Aggregate as consistency boundary** | All entities within an aggregate are loaded and stored together | [Martin Fowler](https://martinfowler.com/bliki/DDD_Aggregate.html) |
| **Cross-aggregate reference by ID only** | Aggregates should reference other aggregates by ID, not direct object reference | [Enterprise Craftsmanship](https://enterprisecraftsmanship.com/posts/modeling-relationships-in-ddd-way/) |
| **External access through root only** | Objects outside the aggregate may not reference any entities inside | [Alibaba Cloud](https://www.alibabacloud.com/blog/an-in-depth-understanding-of-aggregation-in-domain-driven-design_598034) |
| **Single transaction per aggregate** | A transaction should modify only one aggregate | [SSENSE Tech](https://medium.com/ssense-tech/ddd-beyond-the-basics-mastering-aggregate-design-26591e218c8c) |
| **Local vs global identity** | Entities inside an aggregate have local identity; only roots have global identity | [ABP.IO](https://docs.abp.io/en/abp/1.1/Entities) |

**Key insight:** In pure DDD, you don't "move" an entity instance between aggregates. Instead:
1. Delete from source aggregate
2. Create new instance in target aggregate (with same business data)
3. Or use domain events for eventual consistency

**This validates Neatoo's constraint:** Throwing when adding an entity from a different aggregate is correct DDD behavior. The same object instance should not exist in two aggregates.

##### Decision: Compare Root, Not Parent

**Comparing Parent is too restrictive.** Moving a Project between Departments in the same Company is a valid use case that should be allowed (same aggregate, different parent).

**Decision:** Add a `Root` property and compare aggregate roots.

```csharp
// Constraint in EntityListBase.InsertItem():
if (item.Root != null && item.Root != this.Root)
{
    throw new InvalidOperationException(
        "Cannot add item to list: item belongs to a different aggregate.");
}
```

##### Root Property Implementation

**Option A: Computed (walks up chain)**
```csharp
public IBase? Root => Parent == null ? null : (Parent.Root ?? Parent);
```
- Pro: No additional tracking needed
- Con: O(n) where n = depth of hierarchy

**Option B: Tracked (set when added)**
```csharp
// In SetParent:
public void SetParent(IBase? parent)
{
    Parent = parent;
    Root = parent?.Root ?? parent;  // Set once
}
```
- Pro: O(1) access
- Con: Must update if hierarchy changes (rare)

**Recommendation:** Option A (computed) for simplicity. Hierarchy depth is typically shallow (2-4 levels), so O(n) is negligible.

##### Updated Behavior Matrix

| Scenario | Root Check | Result |
|----------|------------|--------|
| Move Project from Dept1 to Dept2 (same Company) | `Company == Company` | ✅ Allowed |
| Move Project to different Company | `Company1 != Company2` | ❌ Throws |
| Add new item (no Root yet) | `null` | ✅ Allowed |
| Add item already in this aggregate | `Root == Root` | ✅ Allowed |

##### Implications for Neatoo

1. **EF Core's auto-remove is elegant** but may be "too magic" for explicit DDD modeling
2. **Re-parenting is a legitimate use case** that EF Core supports explicitly
3. **Single-instance constraint** prevents many of the risks identified (W2-W6)
4. **Neatoo could adopt similar constraint**: throw if item.Parent != null && item.Parent != this.Parent

##### Sources

- [Changing Foreign Keys and Navigations - EF Core](https://learn.microsoft.com/en-us/ef/core/change-tracking/relationship-changes)
- [Change Tracking - EF Core](https://learn.microsoft.com/en-us/ef/core/change-tracking/)
- [EF Core Issue #12459 - Entity already tracked](https://github.com/dotnet/efcore/issues/12459)

---

### Q4: Priority Focus
**Question:** Which categories are most important to address first:
- A) Ensuring all state transitions work correctly (implementation)
- B) Comprehensive unit test coverage
- C) Documentation of expected behaviors

**Answer:** All of the above, prioritized in order: A → B → C

**Decision:** ✅ Implementation first, then tests, then documentation

---

## Use Case Categories

### Category 1: IsValid State Transitions

| # | Scenario | List Before | Item | List After | Event? | Status |
|---|----------|-------------|------|------------|--------|--------|
| 1.1 | Add valid to all-valid | `IsValid=true` | `IsValid=true` | `IsValid=true` | No | ⬜ |
| 1.2 | Add invalid to all-valid | `IsValid=true` | `IsValid=false` | `IsValid=false` | **Yes** | ⬜ |
| 1.3 | Add valid to invalid | `IsValid=false` | `IsValid=true` | `IsValid=false` | No | ⬜ |
| 1.4 | Add invalid to invalid | `IsValid=false` | `IsValid=false` | `IsValid=false` | No | ⬜ |

**Notes:** _[Add implementation notes here]_

---

### Category 2: IsBusy State Transitions

| # | Scenario | List Before | Item | List After | Event? | Status |
|---|----------|-------------|------|------------|--------|--------|
| 2.1 | Add non-busy to non-busy | `IsBusy=false` | `IsBusy=false` | `IsBusy=false` | No | ⬜ |
| 2.2 | Add busy to non-busy | `IsBusy=false` | `IsBusy=true` | `IsBusy=true` | **Yes** | ⬜ |
| 2.3 | Add non-busy to busy | `IsBusy=true` | `IsBusy=false` | `IsBusy=true` | No | ⬜ |
| 2.4 | Add busy to busy | `IsBusy=true` | `IsBusy=true` | `IsBusy=true` | No | ⬜ |

**Notes:** _[Add implementation notes here]_

---

### Category 3: IsModified State Transitions

| # | Scenario | List Before | Item | List After | Event? | Status |
|---|----------|-------------|------|------------|--------|--------|
| 3.1 | Add new item to clean | `IsModified=false` | `IsNew=true` | `IsModified=false` | No | ⬜ |
| 3.2 | Add new item to modified | `IsModified=true` | `IsNew=true` | `IsModified=true` | No | ⬜ |
| 3.3 | Add existing item to clean | `IsModified=false` | `IsNew=false` | `IsModified=true` | **Yes** | ⬜ |
| 3.4 | Add existing item to modified | `IsModified=true` | `IsNew=false` | `IsModified=true` | No | ⬜ |

**Definition:** "Existing" means `IsNew=false` (loaded from database). Adding existing items calls `MarkModified()`.

**Notes:** _[Add implementation notes here]_

---

### Category 4: IsDeleted Item Handling

| # | Scenario | Paused? | Expected Result | Status |
|---|----------|---------|-----------------|--------|
| 4.1 | Add non-deleted item | No | Item added normally | ⬜ |
| 4.2 | Add deleted item | No | `UnDelete()` called, then added | ⬜ |
| 4.3 | Add non-deleted item | Yes | Item added (no state management) | ⬜ |
| 4.4 | Add deleted item | Yes | Item goes to `DeletedList`, NOT main list | ⬜ |

**Notes:** _[Add implementation notes here]_

---

### Category 5: Parent/Child State

| # | Scenario | Expected Behavior | Status |
|---|----------|-------------------|--------|
| 5.1 | Add item (normal) | `item.Parent` = list's parent (aggregate root) | ⬜ |
| 5.2 | Add item (normal) | `item.IsChild` = `true` | ⬜ |
| 5.3 | Add item while paused | Parent NOT set | ⬜ |
| 5.4 | Add item while paused | IsChild NOT set | ⬜ |
| 5.5 | Add item with existing parent | _[Depends on Q3 answer]_ | ⬜ |

**Notes:** _[Add implementation notes here]_

---

### Category 6: Event Notifications

| # | Event | When Raised | Status |
|---|-------|-------------|--------|
| 6.1 | `CollectionChanged` | Always (from ObservableCollection) | ⬜ |
| 6.2 | `PropertyChanged("Count")` | Always | ⬜ |
| 6.3 | `PropertyChanged("IsValid")` | When validity changes (1.2) | ⬜ |
| 6.4 | `PropertyChanged("IsBusy")` | When busy state changes (2.2) | ⬜ |
| 6.5 | `PropertyChanged("IsModified")` | When modified state changes (3.3) | ⬜ |
| 6.6 | `NeatooPropertyChanged` | For all above with breadcrumb trail | ⬜ |

**Notes:** _[Add implementation notes here]_

---

### Category 7: Edge Cases

| # | Scenario | Expected Behavior | Status |
|---|----------|-------------------|--------|
| 7.1 | Add null item | Throw `ArgumentNullException` | ⬜ |
| 7.2 | Add same item twice | Throw `InvalidOperationException` | ⬜ |
| 7.3 | Insert at specific index | Same as Add but at index | ⬜ |
| 7.4 | Add to empty list | First item sets initial aggregate state | ⬜ |

**Notes:** _[Add implementation notes here]_

---

### Category 8: Cascading Effects

| # | Scenario | Cascading Effect | Status |
|---|----------|------------------|--------|
| 8.1 | Item validation runs after add | May change item.IsValid → affects list.IsValid | ⬜ |
| 8.2 | Item has async rules | Item becomes busy → list becomes busy | ⬜ |
| 8.3 | Rule modifies item property | Item becomes modified → list reflects this | ⬜ |

**Notes:** _[Add implementation notes here]_

---

### Category 9: Cross-List/Aggregate Scenarios (Q3 - Decided: Compare Root)

| # | Scenario | Expected Behavior | Status |
|---|----------|-------------------|--------|
| 9.1 | Add item from SAME aggregate (different parent) | ✅ Allowed - same Root | ⬜ |
| 9.2 | Add item from DIFFERENT aggregate (new item) | ❌ Throw - different Root | ⬜ |
| 9.3 | Add item from DIFFERENT aggregate (existing item) | ❌ Throw - different Root | ⬜ |
| 9.4 | Add deleted item from another aggregate's DeletedList | ❌ Throw - different Root | ⬜ |
| 9.5 | Add item still in list (same aggregate, not removed) | ❌ Throw - duplicate (Q2) | ⬜ |
| 9.6 | Add item with Root = null (brand new, never added) | ✅ Allowed | ⬜ |

**Decision:** Throw if `item.Root != null && item.Root != this.Root`

**Notes:** _[Add implementation notes here]_

---

## Additional Considerations

### C1: Paused State (Deserialization/Factory)

During `IsPaused=true` (deserialization, factory operations), should we skip the Root check?

| Option | Behavior | Rationale |
|--------|----------|-----------|
| **Skip check when paused** | Allow any item during deserialization | Data from trusted source (server/DB) |
| **Always check** | Throw even during deserialization | Catch corruption early |

**Recommendation:** Skip check when paused. Deserialized data is trusted.

---

### C2: Intra-Aggregate Move with Existing Items (DeletedList Issue)

Even within the same aggregate, moving an existing item has the DeletedList problem:

```csharp
// Same aggregate (Company), different parents (Dept1, Dept2)
var project = dept1.Projects[0];  // IsNew=false
dept1.Projects.Remove(project);   // project in dept1.Projects.DeletedList
dept2.Projects.Add(project);      // Allowed (same Root)
// BUT: project still in dept1.Projects.DeletedList!
```

**Solution: ContainingList property**

See [entity-delete-vs-list-remove.md](./entity-delete-vs-list-remove.md) for full design.

Entity tracks its `ContainingList`. Key insight: `ContainingList` stays set even when in DeletedList.

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

**Decision:** Implement `ContainingList` tracking. This solves C2 AND the Delete/Remove inconsistency (C7).

---

### C3: Which Interface Gets Root?

Where should the `Root` property be defined?

| Interface | Implication |
|-----------|-------------|
| `IBase` | All Neatoo objects have Root (including non-entity types) |
| `IValidateBase` | Only validateable objects have Root |
| `IEntityBase` | Only entities have Root |

**Recommendation:** `IBase` - Root is a fundamental concept for aggregate membership.

---

### C4: Performance of Contains() Check (Q2)

For duplicate detection, `list.Contains(item)` is O(n).

| List Size | Concern |
|-----------|---------|
| < 100 items | Negligible |
| 100-1000 items | Noticeable on frequent adds |
| > 1000 items | Consider HashSet tracking |

**Recommendation:** Use Contains() for now. Optimize later if profiling shows issues.

---

### C5: Deeply Nested Structures

Does computed Root work correctly?

```
Company (Root = null, is the root)
└── Departments
    └── Dept1 (Root = Company)
        └── Projects
            └── Project1 (Root = ??)
```

**Computed:** `Root => Parent == null ? null : (Parent.Root ?? Parent)`
- Project1.Parent = Dept1
- Dept1.Root = Dept1.Parent.Root ?? Dept1.Parent = Company.Root ?? Company = null ?? Company = Company
- Project1.Root = Project1.Parent.Root ?? Project1.Parent = Dept1.Root ?? Dept1 = Company ?? Dept1 = Company ✓

**Verified:** Computed Root correctly returns aggregate root at any depth.

---

### C7: Entity.Delete() vs List.Remove() Inconsistency

**Moved to separate document:** [entity-delete-vs-list-remove.md](./entity-delete-vs-list-remove.md)

This is a distinct design issue related to entities not knowing their containing list.

---

### C6: Error Message Quality

Should error messages include specifics?

```csharp
// Basic
throw new InvalidOperationException("Cannot add item: belongs to different aggregate.");

// Detailed
throw new InvalidOperationException(
    $"Cannot add {item.GetType().Name} to list: " +
    $"item belongs to aggregate rooted at {item.Root?.GetType().Name ?? "null"}, " +
    $"but this list belongs to {this.Root?.GetType().Name ?? "null"}.");
```

**Recommendation:** Detailed messages help debugging. Include types.

---

## Open Questions

- [ ] C1: Skip Root check when paused? (Recommend: Yes)
- [x] C2: Intra-aggregate DeletedList issue → **Solved by ContainingList tracking**
- [ ] C3: Which interface for Root and ContainingList? (Recommend: IEntityBase)
- [ ] C4: Performance concern for Contains()? (Recommend: Defer optimization)
- [ ] C6: Detailed error messages? (Recommend: Yes)
- [x] C7: Entity.Delete() vs List.Remove() → **Solved by ContainingList tracking** - See [separate doc](./entity-delete-vs-list-remove.md)

---

## Task List

### Phase 1: Verify Current Implementation
- [ ] Review `EntityListBase.InsertItem()` against use cases
- [ ] Review `ListBase.InsertItem()` against use cases
- [ ] Review `ValidateListBase` state aggregation
- [ ] Identify gaps between expected and actual behavior

### Phase 2: Unit Tests
- [ ] Category 1: IsValid state transition tests
- [ ] Category 2: IsBusy state transition tests
- [ ] Category 3: IsModified state transition tests
- [ ] Category 4: IsDeleted handling tests
- [ ] Category 5: Parent/Child state tests
- [ ] Category 6: Event notification tests
- [ ] Category 7: Edge case tests (null, duplicate)
- [ ] Category 8: Cascading effect tests
- [ ] Category 9: Cross-list/aggregate tests (blocked by Q3 decision)

### Phase 3: Implement Root and ContainingList Properties
- [x] Review Q3 analysis and open questions
- [x] Decide on approach: Compare Root, not Parent
- [x] Decide on ContainingList tracking (solves C2 and C7)
- [ ] Add `Root` property to `IEntityBase` interface
- [ ] Add `ContainingList` property to `IEntityBase` interface
- [ ] Implement computed `Root` in base classes
- [ ] Update `InsertItem()`: set ContainingList, handle intra-aggregate moves
- [ ] Update `RemoveItem()`: keep ContainingList set (don't clear)
- [ ] Update `FactoryComplete()`: clear ContainingList when DeletedList purged
- [ ] Update `Delete()`: delegate to ContainingList.Remove()
- [ ] Add constraint: throw if `item.Root != this.Root`
- [ ] Add constraint: throw if null item
- [ ] Add constraint: throw if duplicate item
- [ ] Update Category 9 expected behaviors based on decision

### Phase 4: Fix Implementation Issues
- [ ] _[To be determined after Phase 1, 2 & 3]_

### Phase 5: Documentation
- [ ] Document expected behaviors in `docs/aggregates-and-entities.md`
- [ ] Add code samples showing common patterns
- [ ] Document edge case handling

---

## Current Implementation Reference

### Class Hierarchy
```
ObservableCollection<I>
  └─ ListBase<I>
       └─ ValidateListBase<I>
            └─ EntityListBase<I>
```

### Key Files
- `src/Neatoo/EntityListBase.cs` - Entity-specific add logic
- `src/Neatoo/ListBase.cs` - Base add logic, Parent assignment
- `src/Neatoo/ValidateListBase.cs` - Validation state aggregation

### InsertItem Flow (Not Paused)
1. `EntityListBase.InsertItem()` - UnDelete if needed, MarkModified for existing, MarkAsChild
2. `ListBase.InsertItem()` - SetParent, subscribe to events, raise notifications
3. `ObservableCollection.InsertItem()` - Actual collection modification

### State Property Definitions
- `IsValid`: `!this.Any(c => !c.IsValid)` (ValidateListBase:49)
- `IsBusy`: `this.Any(c => c.IsBusy)` (ListBase:65)
- `IsModified`: `this.Any(c => c.IsModified) || this.DeletedList.Any()` (EntityListBase:52)
