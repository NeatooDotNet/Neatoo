// -----------------------------------------------------------------------------
// Design.Domain - [Fetch] Factory Operation Patterns
// -----------------------------------------------------------------------------
// This file demonstrates the [Fetch] attribute for loading existing objects
// from persistence. A root [Fetch] the client calls carries [Remote]; child
// and list [Fetch] never do.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.FactoryOperations;

// =============================================================================
// [Fetch] - Load Existing Objects from Persistence
// =============================================================================
// Use [Fetch] for:
// - Loading a single entity by ID
// - Loading with complex criteria
// - Loading aggregates with children
// - Loading read models / projections
//
// DESIGN DECISION: A root [Fetch] the client calls carries [Remote]; child
// and list [Fetch] are internal and never [Remote]. The reason is the entry
// point, not database access: a child [Fetch] that reads from the repository
// runs on the server too, because only server-side code calls it.
//
// After [Fetch] completes:
// - EntityBase: IsNew=false, IsModified=false (just loaded, no changes)
// - ValidateBase: No persistence state changes
//
// GENERATOR BEHAVIOR: For [Remote][Fetch], the generated factory sends the
// call from the client to the single POST /api/neatoo endpoint; on the server
// it resolves the object and the [Service] parameters, calls FactoryStart,
// your method, then FactoryComplete, and returns the object. The real output
// is under Generated/Neatoo.Generator/Neatoo.Factory/.
// =============================================================================

/// <summary>
/// Demonstrates: [Fetch] patterns for loading existing entities.
/// </summary>
[Factory]
internal partial class FetchDemo : EntityBase<FetchDemo>, IFetchDemo
{
    public partial int Id { get; set; }
    public partial string? Name { get; set; }
    public partial string? Description { get; set; }

    public FetchDemo(IEntityBaseServices<FetchDemo> services) : base(services) { }

    [Create]
    public void Create() { }

    // =========================================================================
    // Pattern 1: Fetch by Primary Key
    // =========================================================================
    // The most common pattern - load a single entity by ID.
    //
    // GENERATOR BEHAVIOR: instance factory methods run inside
    // FactoryStart/FactoryComplete - the object is PAUSED for the duration of
    // the method body.
    //
    // DESIGN DECISION: Inside a factory operation, assign properties directly.
    // The object is paused, so assignment marks nothing modified and runs no
    // rules. LoadValue, PauseAllActions and MarkUnmodified are not used inside
    // a factory operation.
    // =========================================================================
    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IFetchDemoRepository repository)
    {
        var data = repository.GetById(id);

        // Paused - plain assignment is a clean baseline load
        Id = data.Id;
        Name = data.Name;
        Description = data.Description;

        // After this method completes:
        // - IsNew = false (it exists in DB)
        // - IsModified = false (just loaded, no changes)
    }

    // =========================================================================
    // Pattern 2: Fetch with Criteria Object
    // =========================================================================
    // Complex queries can use a criteria DTO instead of multiple parameters.
    // =========================================================================
    [Remote]
    [Fetch]
    internal void Fetch(FetchDemoCriteria criteria, [Service] IFetchDemoRepository repository)
    {
        var data = repository.GetByCriteria(criteria.Name, criteria.MinValue);

        Id = data.Id;
        Name = data.Name;
        Description = data.Description;
    }

    // =========================================================================
    // Pattern 3 (RETIRED): Explicit PauseAllActions inside a factory method
    // =========================================================================
    // COMMON MISTAKE: Wrapping a factory method body in
    // `using (PauseAllActions())`. The factory operation already pauses the
    // object (FactoryStart) and resumes it when the operation completes
    // (FactoryComplete) - and because PauseAllActions was a no-op on the
    // already-paused object, disposing the `using` RESUMES the object EARLY,
    // before the factory operation is finished. Rules and modification
    // tracking then apply to any code after the using block.
    //
    // Reserve PauseAllActions() for non-factory code that needs to load or
    // rearrange state without firing rules.
    // =========================================================================

    // Standard persistence methods
    [Remote]
    [Insert]
    internal void Insert([Service] IFetchDemoRepository repository)
    {
        repository.Insert(Name!, Description);
    }

    [Remote]
    [Update]
    internal void Update([Service] IFetchDemoRepository repository)
    {
        repository.Update(Id, Name!, Description);
    }

    [Remote]
    [Delete]
    internal void Delete([Service] IFetchDemoRepository repository)
    {
        repository.Delete(Id);
    }
}

// =============================================================================
// Fetch Aggregate with Children
// =============================================================================
// When fetching an aggregate, you typically fetch children in the same call.
// =============================================================================

/// <summary>
/// Demonstrates: Fetching aggregate with child collections.
/// </summary>
[Factory]
internal partial class FetchWithChildrenDemo : EntityBase<FetchWithChildrenDemo>, IFetchWithChildrenDemo
{
    public partial int Id { get; set; }
    public partial string? Title { get; set; }
    public partial IFetchDemoItemList? Items { get; set; }

    public FetchWithChildrenDemo(IEntityBaseServices<FetchWithChildrenDemo> services) : base(services) { }

    [Create]
    public void Create([Service] IFetchDemoItemListFactory itemsFactory)
    {
        Items = itemsFactory.Create();
    }

    // =========================================================================
    // DESIGN DECISION: Every object in the graph loads through its OWN factory
    // [Fetch]: the parent delegates to the list factory, the list delegates to
    // the item factory. Each object gets its own factory lifecycle and lands
    // with correct persistence state (IsNew=false, IsModified=false). See the
    // OrderAggregate for the fully documented canonical form.
    //
    // COMMON MISTAKE: Loading children with itemFactory.Create() + LoadValue
    // inside the parent's Fetch. Create marks the item NEW
    // (FactoryComplete(Create) -> MarkNew()), and nothing ever marks it old -
    // so the next Save re-INSERTS every fetched child. Children load through
    // [Fetch], never [Create].
    //
    // DID NOT DO THIS: Lazy load children separately.
    //
    // REJECTED PATTERN:
    //   public FetchDemoItemList Items {
    //       get {
    //           if (_items == null) { _items = await ItemsFactory.Fetch(this.Id); }
    //           return _items;
    //       }
    //   }
    //
    // WHY NOT: Hand-rolled lazy loading adds complexity and can cause N+1
    // query problems. Explicit fetch keeps the data access visible and
    // predictable. When lazy loading is genuinely needed, use EntityLazyLoad
    // (see PropertySystem/LazyLoadProperty.cs).
    // =========================================================================
    [Remote]
    [Fetch]
    internal void Fetch(int id,
        [Service] IFetchParentRepository parentRepository,
        [Service] IFetchDemoItemListFactory itemsFactory)
    {
        // Object is paused by its own factory operation - loads are clean
        var parentData = parentRepository.GetById(id);
        Id = parentData.Id;
        Title = parentData.Title;

        // Children load through the list factory's [Fetch]
        Items = itemsFactory.Fetch(id);

        // After fetch:
        // - Parent: IsNew=false, IsModified=false
        // - Each child: IsNew=false, IsModified=false
    }

    [Remote]
    [Insert]
    internal void Insert([Service] IFetchParentRepository parentRepository) { }

    [Remote]
    [Update]
    internal void Update([Service] IFetchParentRepository parentRepository) { }

    [Remote]
    [Delete]
    internal void Delete([Service] IFetchParentRepository parentRepository) { }
}

[Factory]
internal partial class FetchDemoItem : EntityBase<FetchDemoItem>, IFetchDemoItem
{
    public partial int Id { get; set; }
    public partial string? Name { get; set; }

    public FetchDemoItem(IEntityBaseServices<FetchDemoItem> services) : base(services) { }

    [Create]
    public void Create() { }

    // Child entities DO have their own [Fetch] - the list's [Fetch] calls it
    // per item so every child completes its own factory lifecycle
    // (IsNew=false, IsModified=false after load).
    //
    // Child persistence methods are internal and deliberately NOT [Remote]:
    // they only ever execute inside the aggregate's server-side save flow, so
    // they need no client-callable endpoint. The generated factory's local
    // methods get IsServerRuntime guards from the internal visibility alone.
    // [Create] stays public so the factory interface remains public for client-side creation.

    [Fetch]
    internal void Fetch(int id, string name)
    {
        Id = id;      // paused by the factory operation - assignment is clean
        Name = name;
    }

    [Insert]
    internal void Insert([Service] IFetchChildRepository repository) { }

    [Update]
    internal void Update([Service] IFetchChildRepository repository) { }

    [Delete]
    internal void Delete([Service] IFetchChildRepository repository) { }
}

[Factory]
internal partial class FetchDemoItemList : EntityListBase<IFetchDemoItem>, IFetchDemoItemList
{
    [Create]
    public void Create() { }

    // The list's own [Fetch] populates it while the list is paused by its own
    // factory operation - the adds are baseline loads, nothing is marked
    // modified. Lists also carry [Update] in the canonical persistence flow
    // (see OrderAggregate/OrderItemList.cs); omitted here to keep this demo
    // focused on fetch.
    [Fetch]
    internal void Fetch(int parentId,
        [Service] IFetchChildRepository childRepository,
        [Service] IFetchDemoItemFactory itemFactory)
    {
        foreach (var childData in childRepository.GetByParentId(parentId))
        {
            Add(itemFactory.Fetch(childData.Id, childData.Name));
        }
    }
}

// =============================================================================
// Support Types
// =============================================================================

public class FetchDemoCriteria
{
    public string? Name { get; set; }
    public int MinValue { get; set; }
}

public interface IFetchDemoRepository
{
    (int Id, string Name, string Description) GetById(int id);
    (int Id, string Name, string Description) GetByCriteria(string? name, int minValue);
    void Insert(string name, string? description);
    void Update(int id, string name, string? description);
    void Delete(int id);
}

public interface IFetchParentRepository
{
    (int Id, string Title) GetById(int id);
}

public interface IFetchChildRepository
{
    IEnumerable<(int Id, string Name)> GetByParentId(int parentId);
}
