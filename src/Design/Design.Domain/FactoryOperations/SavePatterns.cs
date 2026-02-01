// -----------------------------------------------------------------------------
// Design.Domain - [Insert]/[Update]/[Delete] Factory Operation Patterns
// -----------------------------------------------------------------------------
// This file demonstrates the persistence operations. These are called by Save(),
// NOT directly by user code.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.FactoryOperations;

// =============================================================================
// [Insert] / [Update] / [Delete] - Persistence Operations
// =============================================================================
// These attributes mark methods that Save() will route to based on entity state:
// - IsNew=true, IsDeleted=false -> [Insert]
// - IsNew=false, IsDeleted=false, IsModified=true -> [Update]
// - IsDeleted=true, IsNew=false -> [Delete]
//
// DESIGN DECISION: You NEVER call these directly. Save() handles routing.
//
// COMMON MISTAKE: Calling Insert/Update/Delete directly.
//
// WRONG:
//   var entity = await factory.Create();
//   entity.Name = "Test";
//   await factory.Insert(entity);  // NO! Don't do this.
//
// RIGHT:
//   var entity = await factory.Create();
//   entity.Name = "Test";
//   await entity.Save();  // This calls Insert because IsNew=true
//
// After [Insert] or [Update] completes, FactoryComplete is called:
// - MarkUnmodified() - clears modification state
// - MarkOld() - sets IsNew=false (for Insert)
//
// After [Delete] completes:
// - The object is typically discarded
// - Parent list's DeletedList is cleared
//
// GENERATOR BEHAVIOR: Save() is implemented on IFactorySave<T>:
//
//   public async Task<T> Save(T obj) {
//       if (obj.IsNew && !obj.IsDeleted) return await Insert(obj);
//       if (obj.IsDeleted && !obj.IsNew) return await Delete(obj);
//       if (obj.IsModified) return await Update(obj);
//       return obj;  // Nothing to do
//   }
// =============================================================================

/// <summary>
/// Demonstrates: [Insert]/[Update]/[Delete] patterns for persistence.
/// </summary>
[Factory]
public partial class SaveDemo : EntityBase<SaveDemo>
{
    public partial int Id { get; set; }
    public partial string? Name { get; set; }
    public partial decimal Amount { get; set; }

    public SaveDemo(IEntityBaseServices<SaveDemo> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name is required" : string.Empty,
            t => t.Name);
    }

    [Create]
    public void Create()
    {
        Amount = 0;
    }

    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] ISaveDemoRepository repository)
    {
        var data = repository.GetById(id);
        this["Id"].LoadValue(data.Id);
        this["Name"].LoadValue(data.Name);
        this["Amount"].LoadValue(data.Amount);
    }

    // =========================================================================
    // [Insert] - Persist New Entity
    // =========================================================================
    // Called by Save() when: IsNew=true && !IsDeleted
    //
    // DESIGN DECISION: Insert receives the entity state, not individual values.
    // The method has access to all properties and can decide what to persist.
    //
    // Pattern: Often returns generated ID for database-assigned keys.
    // =========================================================================
    [Remote]
    [Insert]
    public void Insert([Service] ISaveDemoRepository repository)
    {
        // Database assigns the Id - we get it back and store it
        var generatedId = repository.Insert(Name!, Amount);

        // Use LoadValue to set Id without marking as modified
        this["Id"].LoadValue(generatedId);

        // After Insert completes, FactoryComplete(Insert) is called:
        // - MarkUnmodified() clears modification state
        // - MarkOld() sets IsNew=false
        // Result: IsNew=false, IsModified=false
    }

    // =========================================================================
    // [Update] - Persist Changes to Existing Entity
    // =========================================================================
    // Called by Save() when: !IsNew && IsModified && !IsDeleted
    //
    // DESIGN DECISION: Update typically persists all modified properties.
    // The ModifiedProperties collection tracks which properties changed.
    // Some implementations do partial updates; others overwrite everything.
    // =========================================================================
    [Remote]
    [Update]
    public void Update([Service] ISaveDemoRepository repository)
    {
        repository.Update(Id, Name!, Amount);

        // After Update completes, FactoryComplete(Update) is called:
        // - MarkUnmodified() clears modification state
        // Result: IsModified=false
    }

    // =========================================================================
    // [Delete] - Remove Entity from Persistence
    // =========================================================================
    // Called by Save() when: IsDeleted=true && IsNew=false
    //
    // Note: If IsNew=true AND IsDeleted=true, nothing is called.
    // A new entity that's deleted was never persisted, so no delete needed.
    // =========================================================================
    [Remote]
    [Delete]
    public void Delete([Service] ISaveDemoRepository repository)
    {
        repository.Delete(Id);

        // After Delete completes, the entity is typically discarded.
        // No state changes needed - object won't be used.
    }
}

// =============================================================================
// Aggregate Save - Parent Coordinates Child Persistence
// =============================================================================
// When saving an aggregate, the root's Save coordinates all persistence:
// 1. Save the root entity (Insert or Update)
// 2. For each child in lists: Insert/Update/Delete as needed
// 3. Clear DeletedList after successful persistence
// =============================================================================

/// <summary>
/// Demonstrates: Aggregate save pattern with child persistence.
/// </summary>
[Factory]
public partial class SaveAggregateDemo : EntityBase<SaveAggregateDemo>
{
    public partial int Id { get; set; }
    public partial string? Title { get; set; }
    public partial SaveDemoItemList? Items { get; set; }

    public SaveAggregateDemo(IEntityBaseServices<SaveAggregateDemo> services) : base(services) { }

    [Create]
    public void Create([Service] ISaveDemoItemListFactory itemsFactory)
    {
        Items = itemsFactory.Create();
    }

    [Remote]
    [Fetch]
    public void Fetch(int id,
        [Service] ISaveAggregateRepository repository,
        [Service] ISaveDemoItemListFactory itemsFactory,
        [Service] ISaveDemoItemFactory itemFactory)
    {
        using (PauseAllActions())
        {
            var data = repository.GetParentById(id);
            this["Id"].LoadValue(data.Id);
            this["Title"].LoadValue(data.Title);

            Items = itemsFactory.Create();
            foreach (var childData in repository.GetChildrenByParentId(id))
            {
                var item = itemFactory.Create();
                item["Id"].LoadValue(childData.Id);
                item["Name"].LoadValue(childData.Name);
                item["Quantity"].LoadValue(childData.Quantity);
                Items.Add(item);
            }
        }
    }

    // =========================================================================
    // Aggregate Insert - Save Parent and All New Children
    // =========================================================================
    // When inserting an aggregate:
    // 1. Insert the parent first (to get the ID for FK relationships)
    // 2. Insert all children that are new
    //
    // DESIGN DECISION: Parent Insert is responsible for child persistence.
    // This keeps the aggregate boundary clear - everything is one transaction.
    // =========================================================================
    [Remote]
    [Insert]
    public void Insert(
        [Service] ISaveAggregateRepository repository,
        [Service] ISaveDemoItemFactory itemFactory)
    {
        // Insert parent first - get generated ID
        var generatedId = repository.InsertParent(Title!);
        this["Id"].LoadValue(generatedId);

        // Insert all children (all are new in a new aggregate)
        foreach (var item in Items!)
        {
            var childId = repository.InsertChild(Id, item.Name!, item.Quantity);
            item["Id"].LoadValue(childId);
        }
    }

    // =========================================================================
    // Aggregate Update - Coordinate Insert/Update/Delete for Children
    // =========================================================================
    // When updating an aggregate:
    // 1. Update the parent if modified
    // 2. For each child:
    //    - If IsNew: Insert
    //    - If IsModified && !IsNew: Update
    // 3. For each item in DeletedList: Delete
    // 4. Clear DeletedList
    //
    // DESIGN DECISION: Update iterates Items and DeletedList separately.
    // Items contains active children; DeletedList contains removed ones.
    // =========================================================================
    [Remote]
    [Update]
    public void Update([Service] ISaveAggregateRepository repository)
    {
        // Update parent if it has changes
        if (IsSelfModified)
        {
            repository.UpdateParent(Id, Title!);
        }

        // Process active items
        foreach (var item in Items!)
        {
            if (item.IsNew)
            {
                var childId = repository.InsertChild(Id, item.Name!, item.Quantity);
                item["Id"].LoadValue(childId);
            }
            else if (item.IsSelfModified)
            {
                repository.UpdateChild(item.Id, item.Name!, item.Quantity);
            }
        }

        // Process deleted items
        // GENERATOR BEHAVIOR: DeletedList is populated when items are removed
        // from the list while IsPaused=false.
        //
        // Accessing DeletedList requires casting to internal interface or
        // using a protected method. The framework handles this in FactoryComplete.
        // For demonstration, we show the pattern:
        var deletedItems = GetDeletedItems();
        foreach (var item in deletedItems)
        {
            repository.DeleteChild(item.Id);
        }

        // Note: DeletedList is automatically cleared in FactoryComplete(Update)
    }

    // Helper to access DeletedList for demonstration
    // In practice, the generated Save code handles this
    private IEnumerable<SaveDemoItem> GetDeletedItems()
    {
        // This would access the protected DeletedList
        // Actual implementation uses internal interfaces
        return Array.Empty<SaveDemoItem>(); // Placeholder
    }

    [Remote]
    [Delete]
    public void Delete([Service] ISaveAggregateRepository repository)
    {
        // Delete children first (FK constraint)
        foreach (var item in Items!)
        {
            repository.DeleteChild(item.Id);
        }

        // Then delete parent
        repository.DeleteParent(Id);
    }
}

[Factory]
public partial class SaveDemoItem : EntityBase<SaveDemoItem>
{
    public partial int Id { get; set; }
    public partial string? Name { get; set; }
    public partial int Quantity { get; set; }

    public SaveDemoItem(IEntityBaseServices<SaveDemoItem> services) : base(services) { }

    [Create]
    public void Create() { }

    // =========================================================================
    // Child entities have Insert/Update/Delete but are NOT called directly.
    // The parent's Insert/Update/Delete calls the repository methods.
    //
    // DESIGN DECISION: Child entities don't typically have their own
    // [Insert]/[Update]/[Delete] methods with [Remote]. The parent handles it.
    //
    // DID NOT DO THIS: Have child entities call their own persistence.
    //
    // REJECTED PATTERN:
    //   // In parent Update:
    //   foreach (var item in Items) {
    //       await item.Save();  // NO! Child can't save (IsChild=true)
    //   }
    //
    // WHY NOT: Children are part of the aggregate. The aggregate root owns
    // the transaction boundary. Having children save themselves would break
    // the aggregate pattern and create multiple transactions.
    // =========================================================================
}

[Factory]
public partial class SaveDemoItemList : EntityListBase<SaveDemoItem>
{
    [Create]
    public void Create() { }
}

// =============================================================================
// Support Interfaces
// =============================================================================

public interface ISaveDemoRepository
{
    (int Id, string Name, decimal Amount) GetById(int id);
    int Insert(string name, decimal amount);
    void Update(int id, string name, decimal amount);
    void Delete(int id);
}

public interface ISaveAggregateRepository
{
    (int Id, string Title) GetParentById(int id);
    IEnumerable<(int Id, string Name, int Quantity)> GetChildrenByParentId(int parentId);
    int InsertParent(string title);
    void UpdateParent(int id, string title);
    void DeleteParent(int id);
    int InsertChild(int parentId, string name, int quantity);
    void UpdateChild(int id, string name, int quantity);
    void DeleteChild(int id);
}
