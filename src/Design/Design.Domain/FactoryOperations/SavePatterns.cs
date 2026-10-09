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
// nothing else (see Generated/Neatoo.Generator/Neatoo.Factory/*SaveDemoFactory.g.cs):
//
//   if (target.IsDeleted)      { if (target.IsNew) return null;      // never existed: no-op
//                                return LocalDelete(target, ...); }  // throws NotImplementedException if no [Delete]
//   else if (target.IsNew)     { return LocalInsert(target, ...); }
//   else                       { return LocalUpdate(target, ...); }
//
// Consequences worth knowing:
// - IsModified is NEVER consulted by routing. An unmodified existing entity
//   still routes to [Update] if Save is invoked (EntityBase.Save() won't
//   invoke it — IsSavable gates that — but a direct factory.Save(target) will).
// - A created-then-deleted entity is a no-op: it never existed, so nothing
//   is deleted and the factory returns null.
// - Apart from that case there is no "nothing to do" short-circuit.
//
// DESIGN DECISION: You NEVER call these directly. Save() handles routing.
//
// COMMON MISTAKE: Calling Insert/Update/Delete directly.
//
// WRONG:
//   var entity = factory.Create();
//   entity.Name = "Test";
//   await factory.Insert(entity);  // NO! Don't do this.
//
// RIGHT:
//   var entity = factory.Create();
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
//   CHILDREN never get a [Delete] factory call at all - children have no
//   [Delete]: the list's [Update] removes their rows from the parent row's
//   child collection, and the LIST's own FactoryComplete(Update) clears its
//   DeletedList.)
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
        Id = data.Id;
        Name = data.Name;
        Amount = data.Amount;
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

        // Paused by the Insert operation - assignment does not mark Id modified
        Id = generatedId;

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
    // A created-then-deleted ROOT never reaches this method: the factory
    // treats it as a no-op (see the routing block at the top). Deleted CHILDREN in the canonical aggregate pattern
    // never reach a [Delete] at all — new removed items are discarded by the
    // list, and persisted removed items have their rows removed by the list's
    // [Update].
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
// 1. The root's Insert makes its row (Update gets it from the repository),
//    maps itself into it, then hands the row's CHILD COLLECTION to the LIST
//    factory's Save.
// 2. The list's [Update] brings that collection in line with the list:
//    removed (persisted) children have their rows removed - children have no
//    [Delete]; new children get a new row and go through the ITEM factory's
//    Save (-> [Insert]); modified children go through it with their existing
//    row (-> [Update]); unmodified children are skipped. Each child maps
//    itself into its row.
// 3. The root flushes ONCE (SaveChanges).
// 4. FactoryComplete fires per factory target as each save completes: items
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
    public partial Guid Id { get; set; }
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
    internal bool Fetch(Guid id,
        [Service] ISaveAggregateRepository repository,
        [Service] ISaveDemoItemListFactory itemsFactory)
    {
        // Paused by its own factory operation - no explicit PauseAllActions
        var row = repository.Get(id);
        if (row == null)
        {
            return false;  // the generated factory returns null
        }

        Id = row.Id;
        Title = row.Title;

        // Children load through the list factory's [Fetch] from the child
        // rows - each child completes its own factory lifecycle
        // (IsNew=false, IsModified=false)
        Items = itemsFactory.Fetch(row.Items);
        return true;
    }

    // =========================================================================
    // Aggregate Insert - Make the Row, Hand Its Child Collection to the List
    // =========================================================================
    // 1. Set its own key, make its row, map itself into it, add it
    // 2. Hand row.Items to the list factory - every item is new, so each gets
    //    a new row and routes to the item factory's Insert
    // 3. Flush once
    //
    // DESIGN DECISION: The root's Insert and Update delegate to the SAME list
    // factory Save. This keeps the aggregate boundary clear and gives every
    // child its own factory lifecycle (marked unmodified+old as it saves).
    // =========================================================================
    [Remote]
    [Insert]
    internal async Task Insert(
        [Service] ISaveAggregateRepository repository,
        [Service] ISaveDemoItemListFactory itemsFactory)
    {
        // Re-run every rule on the server and refuse an invalid aggregate.
        // Recommended - the framework does not do this for you. Throw, never
        // return: after [Insert]/[Update] returns, the framework marks the
        // entity saved whether or not anything was written.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        // The entity sets its own key (paused - assignment is clean)
        Id = Guid.NewGuid();

        var row = new SaveAggregateDemoRow();
        MapTo(row);
        repository.Add(row);

        itemsFactory.Save(Items!, row.Items);

        repository.SaveChanges();
    }

    // =========================================================================
    // Aggregate Update - Get the Row, Hand Its Child Collection to the List
    // =========================================================================
    // COMMON MISTAKE: Iterating Items here and writing the child rows
    // directly. The rows get written, but no child factory operation runs, so
    // nothing marks the children unmodified or old: the aggregate still
    // reports IsModified=true after Save, new children get another row on the
    // next Save, and the DeletedList never clears. FactoryComplete fires per
    // factory target - never as a cascade from the parent - so each child must
    // be saved through its own factory.
    // =========================================================================
    [Remote]
    [Update]
    internal async Task Update(
        [Service] ISaveAggregateRepository repository,
        [Service] ISaveDemoItemListFactory itemsFactory)
    {
        // Re-run every rule on the server and refuse an invalid aggregate.
        // Recommended - the framework does not do this for you. Throw, never
        // return: after [Insert]/[Update] returns, the framework marks the
        // entity saved whether or not anything was written.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"SaveAggregateDemo {Id} not found");

        // Write the root's own columns only if they changed
        if (IsSelfModified)
        {
            MapTo(row);
        }

        // Removed items' rows removed, new/modified items routed through the
        // item factory with their row, DeletedList cleared by the list's
        // FactoryComplete(Update)
        itemsFactory.Save(Items!, row.Items);

        repository.SaveChanges();
    }

    [Remote]
    [Delete]
    internal void Delete([Service] ISaveAggregateRepository repository)
    {
        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"SaveAggregateDemo {Id} not found");

        // Removes the root row together with its child rows, as a database
        // cascade delete would. No loop over the children - they have no
        // [Delete], and the whole aggregate is going away.
        repository.Remove(row);

        repository.SaveChanges();
    }

    private void MapTo(SaveAggregateDemoRow row)
    {
        row.Id = Id;
        row.Title = Title!;
    }
}

[Factory]
internal partial class SaveDemoItem : EntityBase<SaveDemoItem>, ISaveDemoItem
{
    public partial Guid Id { get; set; }
    public partial string? Name { get; set; }
    public partial int Quantity { get; set; }

    public SaveDemoItem(IEntityBaseServices<SaveDemoItem> services) : base(services) { }

    [Create]
    public void Create() { }

    // =========================================================================
    // Child entities carry local (non-[Remote]) [Fetch]/[Insert]/[Update]
    // operations that take the child's own row - reachable only through the
    // aggregate's factory flow, because only the list's operations hold the
    // rows, and the interfaces expose no Save(). The child maps itself; no
    // repository is involved. Insert and Update share a parameter list, so
    // the generated factory produces a single Save(item, row) routed on the
    // ITEM's own IsNew - and each routed call marks the item unmodified+old as
    // it completes. No [Delete]: the list's [Update] removes a removed
    // child's row.
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
    internal void Fetch(SaveDemoItemRow row)
    {
        Id = row.Id;      // paused by the factory operation - assignment is clean
        Name = row.Name;
        Quantity = row.Quantity;
    }

    [Insert]
    internal void Insert(SaveDemoItemRow row)
    {
        Id = Guid.NewGuid();  // the entity sets its own key
        MapTo(row);
    }

    [Update]
    internal void Update(SaveDemoItemRow row)
    {
        MapTo(row);
    }

    private void MapTo(SaveDemoItemRow row)
    {
        row.Id = Id;
        row.Name = Name!;
        row.Quantity = Quantity;
    }
}

[Factory]
internal partial class SaveDemoItemList : EntityListBase<ISaveDemoItem>, ISaveDemoItemList
{
    [Create]
    public void Create() { }

    [Fetch]
    internal void Fetch(IEnumerable<SaveDemoItemRow> rows,
        [Service] ISaveDemoItemFactory itemFactory)
    {
        foreach (var row in rows)
        {
            Add(itemFactory.Fetch(row));
        }
    }

    [Update]
    internal void Update(ICollection<SaveDemoItemRow> rows,
        [Service] ISaveDemoItemFactory itemFactory)
    {
        foreach (var item in this.Union(DeletedList))
        {
            if (item.IsDeleted)
            {
                // Defensive: new items removed from the list are discarded,
                // never queued for deletion
                if (!item.IsNew)
                {
                    rows.Remove(rows.Single(r => r.Id == item.Id));
                }
            }
            else if (item.IsNew)
            {
                var row = new SaveDemoItemRow();
                rows.Add(row);
                itemFactory.Save(item, row);
            }
            else if (item.IsModified)
            {
                itemFactory.Save(item, rows.Single(r => r.Id == item.Id));
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

// Aggregate persistence, modeled on an EF Core unit of work: the row classes
// stand in for EF entities, Get returns the root row with its child rows, and
// SaveChanges flushes everything added, changed or removed.

public class SaveAggregateDemoRow
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public List<SaveDemoItemRow> Items { get; } = new();
}

public class SaveDemoItemRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
}

public interface ISaveAggregateRepository
{
    /// <summary>The root row with its child rows, or null if there is none.</summary>
    SaveAggregateDemoRow? Get(Guid id);

    void Add(SaveAggregateDemoRow row);

    /// <summary>Removes the root row and its child rows.</summary>
    void Remove(SaveAggregateDemoRow row);

    void SaveChanges();
}
