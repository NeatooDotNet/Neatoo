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
// These attributes mark methods that Save() will route to based on entity state.
//
// GENERATOR BEHAVIOR: the generated LocalSave routes on IsDeleted, then IsNew —
// nothing else (verbatim from the emitted factory code):
//
//   if (target.IsDeleted)      { return LocalDelete(target, ...); }  // throws NotImplementedException if no [Delete]
//   else if (target.IsNew)     { return LocalInsert(target, ...); }
//   else                       { return LocalUpdate(target, ...); }
//
// Consequences worth knowing:
// - IsModified is NEVER consulted by routing. An unmodified existing entity
//   still routes to [Update] if Save is invoked (EntityBase.Save() won't
//   invoke it — IsSavable gates that — but a direct factory.Save(target) will).
// - IsDeleted wins over IsNew: a created-then-deleted entity routes to
//   [Delete], not to a silent no-op.
// - There is no "nothing to do" short-circuit in the factory.
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
// - No state changes; the object is typically discarded.
// - Nothing happens to any parent list — lifecycle hooks fire only on the
//   single factory target. (In the canonical aggregate pattern, deleted
//   CHILDREN never get a [Delete] factory call at all: the list's [Update]
//   removes them from persistence directly, and the LIST's own
//   FactoryComplete(Update) clears its DeletedList.)
// =============================================================================

/// <summary>
/// Demonstrates: [Insert]/[Update]/[Delete] patterns for persistence.
/// </summary>
[Factory]
internal partial class SaveDemo : EntityBase<SaveDemo>, ISaveDemo
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
    internal void Fetch(int id, [Service] ISaveDemoRepository repository)
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
    internal void Insert([Service] ISaveDemoRepository repository)
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
    // Called by Save() when: !IsDeleted && !IsNew (routing never consults
    // IsModified — EntityBase.Save()'s IsSavable gate is what stops
    // unmodified saves before routing happens)
    //
    // DESIGN DECISION: Update typically persists all modified properties.
    // The ModifiedProperties collection tracks which properties changed.
    // Some implementations do partial updates; others overwrite everything.
    // =========================================================================
    [Remote]
    [Update]
    internal void Update([Service] ISaveDemoRepository repository)
    {
        repository.Update(Id, Name!, Amount);

        // After Update completes, FactoryComplete(Update) is called:
        // - MarkUnmodified() clears modification state
        // Result: IsModified=false
    }

    // =========================================================================
    // [Delete] - Remove Entity from Persistence
    // =========================================================================
    // Called by Save() when: IsDeleted=true — checked FIRST, before IsNew
    // (see the routing block at the top of this file).
    //
    // COMMON GOTCHA: a created-then-deleted ROOT still routes here (IsDeleted
    // wins over IsNew), so a [Delete] body should tolerate an entity that was
    // never persisted. Deleted CHILDREN in the canonical aggregate pattern
    // never reach a [Delete] at all — new removed items are discarded by the
    // list, and persisted removed items are deleted by the list's [Update].
    // =========================================================================
    [Remote]
    [Delete]
    internal void Delete([Service] ISaveDemoRepository repository)
    {
        repository.Delete(Id);

        // After Delete completes, the entity is typically discarded.
        // No state changes needed - object won't be used.
    }
}

// =============================================================================
// Aggregate Save - Root Delegates, Every Object Gets Its Own Factory Lifecycle
// =============================================================================
// When saving an aggregate:
// 1. The root's Insert/Update persists the root, then delegates child
//    persistence to the LIST factory's Save.
// 2. The list's [Update] routes each child through the ITEM factory's Save
//    (the child's own IsNew decides insert vs update) and deletes removed
//    children from persistence.
// 3. FactoryComplete fires per factory target as each save completes: items
//    are marked unmodified+old, the list clears its DeletedList, the root is
//    marked unmodified+old. There is NO graph-wide cascade.
// See the OrderAggregate for the fully documented canonical form.
// =============================================================================

/// <summary>
/// Demonstrates: Aggregate save pattern with child persistence.
/// </summary>
[Factory]
internal partial class SaveAggregateDemo : EntityBase<SaveAggregateDemo>, ISaveAggregateDemo
{
    public partial int Id { get; set; }
    public partial string? Title { get; set; }
    public partial ISaveDemoItemList? Items { get; set; }

    public SaveAggregateDemo(IEntityBaseServices<SaveAggregateDemo> services) : base(services) { }

    [Create]
    public void Create([Service] ISaveDemoItemListFactory itemsFactory)
    {
        Items = itemsFactory.Create();
    }

    [Remote]
    [Fetch]
    internal void Fetch(int id,
        [Service] ISaveAggregateRepository repository,
        [Service] ISaveDemoItemListFactory itemsFactory)
    {
        // Paused by its own factory operation - no explicit PauseAllActions
        var data = repository.GetParentById(id);
        this["Id"].LoadValue(data.Id);
        this["Title"].LoadValue(data.Title);

        // Children load through the list factory's [Fetch] - each child
        // completes its own factory lifecycle (IsNew=false, IsModified=false)
        Items = itemsFactory.Fetch(id);
    }

    // =========================================================================
    // Aggregate Insert - Save Root, Delegate Children to the List Factory
    // =========================================================================
    // 1. Insert the parent first (to get the ID for FK relationships)
    // 2. Delegate to the list factory - every item is new, so each routes to
    //    the item factory's Insert
    //
    // DESIGN DECISION: The root's Insert and Update delegate to the SAME list
    // factory Save. This keeps the aggregate boundary clear and gives every
    // child its own factory lifecycle (marked unmodified+old as it saves).
    // =========================================================================
    [Remote]
    [Insert]
    internal void Insert(
        [Service] ISaveAggregateRepository repository,
        [Service] ISaveDemoItemListFactory itemsFactory)
    {
        // Insert parent first - get generated ID (paused - assignment is clean)
        Id = repository.InsertParent(Title!);

        itemsFactory.Save(Items!, Id);
    }

    // =========================================================================
    // Aggregate Update - Delegate Child Persistence to the List Factory
    // =========================================================================
    // COMMON MISTAKE: Iterating Items here and writing child rows to the
    // repository directly. The rows get written, but no child factory
    // operation runs, so nothing marks the children unmodified or old:
    // the aggregate still reports IsModified=true after Save, new children
    // re-insert on the next Save, and the DeletedList never clears.
    // FactoryComplete fires per factory target - never as a cascade from the
    // parent - so each child must be saved through its own factory.
    // =========================================================================
    [Remote]
    [Update]
    internal void Update(
        [Service] ISaveAggregateRepository repository,
        [Service] ISaveDemoItemListFactory itemsFactory)
    {
        // Update parent if it has changes
        if (IsSelfModified)
        {
            repository.UpdateParent(Id, Title!);
        }

        // Deleted items removed from persistence, new/modified items routed
        // through the item factory, DeletedList cleared by the list's
        // FactoryComplete(Update)
        itemsFactory.Save(Items!, Id);
    }

    [Remote]
    [Delete]
    internal void Delete([Service] ISaveAggregateRepository repository)
    {
        // Delete children first (FK constraint). Direct repository deletes are
        // intentional - deleted children get no factory operation, and the
        // whole aggregate is going away.
        foreach (var item in Items!)
        {
            repository.DeleteChild(item.Id);
        }

        // Then delete parent
        repository.DeleteParent(Id);
    }
}

[Factory]
internal partial class SaveDemoItem : EntityBase<SaveDemoItem>, ISaveDemoItem
{
    public partial int Id { get; set; }
    public partial string? Name { get; set; }
    public partial int Quantity { get; set; }

    public SaveDemoItem(IEntityBaseServices<SaveDemoItem> services) : base(services) { }

    [Create]
    public void Create() { }

    // =========================================================================
    // Child entities carry local (non-[Remote]) [Fetch]/[Insert]/[Update]
    // operations, reachable only through the aggregate's factory flow: the
    // signatures require the parent's identity, and the interfaces expose no
    // Save(). Insert and Update share a parameter list, so the generated
    // factory produces a single Save(item, parentId) routed on the ITEM's own
    // IsNew - and each routed call marks the item unmodified+old as it
    // completes.
    //
    // DID NOT DO THIS: Have child entities save themselves.
    //
    // REJECTED PATTERN:
    //   // In parent Update:
    //   foreach (var item in Items) {
    //       await item.Save();  // Does not compile: ISaveDemoItem has no Save()
    //   }
    //
    // WHY NOT: Children are part of the aggregate. The aggregate root owns
    // the transaction boundary. Having children save themselves would break
    // the aggregate pattern and create multiple transactions.
    // =========================================================================

    [Fetch]
    internal void Fetch(int id, string name, int quantity)
    {
        Id = id;      // paused by the factory operation - assignment is clean
        Name = name;
        Quantity = quantity;
    }

    [Insert]
    internal void Insert(int parentId, [Service] ISaveAggregateRepository repository)
    {
        Id = repository.InsertChild(parentId, Name!, Quantity);
    }

    [Update]
    internal void Update(int parentId, [Service] ISaveAggregateRepository repository)
    {
        // parentId unused - exists so Insert/Update share a signature and the
        // generator produces a single Save(item, parentId)
        repository.UpdateChild(Id, Name!, Quantity);
    }
}

[Factory]
internal partial class SaveDemoItemList : EntityListBase<ISaveDemoItem>, ISaveDemoItemList
{
    [Create]
    public void Create() { }

    [Fetch]
    internal void Fetch(int parentId,
        [Service] ISaveAggregateRepository repository,
        [Service] ISaveDemoItemFactory itemFactory)
    {
        foreach (var d in repository.GetChildrenByParentId(parentId))
        {
            Add(itemFactory.Fetch(d.Id, d.Name, d.Quantity));
        }
    }

    [Update]
    internal void Update(int parentId,
        [Service] ISaveAggregateRepository repository,
        [Service] ISaveDemoItemFactory itemFactory)
    {
        foreach (var item in this.Union(DeletedList).ToList())
        {
            if (item.IsDeleted)
            {
                // Defensive: new items removed from the list are discarded,
                // never queued for deletion
                if (!item.IsNew)
                {
                    repository.DeleteChild(item.Id);
                }
            }
            else if (item.IsNew || item.IsModified)
            {
                itemFactory.Save(item, parentId);
            }
        }
    }
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
