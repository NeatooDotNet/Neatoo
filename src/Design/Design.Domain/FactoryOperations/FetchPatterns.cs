// -----------------------------------------------------------------------------
// Design.Domain - [Fetch] Factory Operation Patterns
// -----------------------------------------------------------------------------
// This file demonstrates the [Fetch] attribute for loading existing objects
// from persistence. [Fetch] typically needs [Remote] to access database.
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
// DESIGN DECISION: [Fetch] methods almost always need [Remote].
// Fetching requires database access, which is only on the server.
//
// After [Fetch] completes:
// - EntityBase: IsNew=false, IsModified=false (just loaded, no changes)
// - ValidateBase: No persistence state changes
//
// GENERATOR BEHAVIOR: For [Remote][Fetch], RemoteFactory generates:
//
// Client-side factory (HTTP call):
//   public async Task<FetchDemo> Fetch(int id) {
//       var request = new FetchRequest { Id = id };
//       var response = await httpClient.PostAsJsonAsync("FetchDemo/Fetch", request);
//       return await response.Content.ReadFromJsonAsync<FetchDemo>();
//   }
//
// Server-side factory (actual execution):
//   public FetchDemo Fetch(int id) {
//       var obj = serviceProvider.GetRequiredService<FetchDemo>();
//       var repository = serviceProvider.GetRequiredService<IRepository>();
//       obj.FactoryStart(FactoryOperation.Fetch);
//       obj.Fetch(id, repository);  // Your method with [Service] resolved
//       obj.FactoryComplete(FactoryOperation.Fetch);
//       return obj;
//   }
// =============================================================================

/// <summary>
/// Demonstrates: [Fetch] patterns for loading existing entities.
/// </summary>
[Factory]
public partial class FetchDemo : EntityBase<FetchDemo>
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
    // DESIGN DECISION: Use LoadValue() inside Fetch, not property setters.
    // LoadValue() sets the value WITHOUT marking the property as modified.
    // After Fetch, the entity should be IsModified=false (unchanged from DB).
    //
    // COMMON MISTAKE: Using property setters in Fetch.
    //
    // WRONG:
    //   [Fetch]
    //   public void Fetch(int id, [Service] IRepo repo) {
    //       Name = repo.Get(id).Name;  // IsModified becomes true!
    //   }
    //
    // RIGHT:
    //   [Fetch]
    //   public void Fetch(int id, [Service] IRepo repo) {
    //       this["Name"].LoadValue(repo.Get(id).Name);  // IsModified stays false
    //   }
    // =========================================================================
    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IFetchDemoRepository repository)
    {
        var data = repository.GetById(id);

        // LoadValue sets value without triggering modification tracking
        this["Id"].LoadValue(data.Id);
        this["Name"].LoadValue(data.Name);
        this["Description"].LoadValue(data.Description);

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
    public void Fetch(FetchDemoCriteria criteria, [Service] IFetchDemoRepository repository)
    {
        var data = repository.GetByCriteria(criteria.Name, criteria.MinValue);

        this["Id"].LoadValue(data.Id);
        this["Name"].LoadValue(data.Name);
        this["Description"].LoadValue(data.Description);
    }

    // =========================================================================
    // Pattern 3: Fetch with PauseAllActions
    // =========================================================================
    // Using PauseAllActions() prevents rules from firing during load.
    // This is optional but can improve performance for complex objects.
    // =========================================================================
    [Remote]
    [Fetch]
    public void FetchOptimized(int id, [Service] IFetchDemoRepository repository)
    {
        using (PauseAllActions())
        {
            var data = repository.GetById(id);

            this["Id"].LoadValue(data.Id);
            this["Name"].LoadValue(data.Name);
            this["Description"].LoadValue(data.Description);

            // Rules are paused - no validation during load
        }
        // ResumeAllActions() called automatically when using block exits
        // Rules can now run if needed
    }

    // Standard persistence methods
    [Remote]
    [Insert]
    public void Insert([Service] IFetchDemoRepository repository)
    {
        repository.Insert(Name!, Description);
    }

    [Remote]
    [Update]
    public void Update([Service] IFetchDemoRepository repository)
    {
        repository.Update(Id, Name!, Description);
    }

    [Remote]
    [Delete]
    public void Delete([Service] IFetchDemoRepository repository)
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
public partial class FetchWithChildrenDemo : EntityBase<FetchWithChildrenDemo>
{
    public partial int Id { get; set; }
    public partial string? Title { get; set; }
    public partial FetchDemoItemList? Items { get; set; }

    public FetchWithChildrenDemo(IEntityBaseServices<FetchWithChildrenDemo> services) : base(services) { }

    [Create]
    public void Create([Service] IFetchDemoItemListFactory itemsFactory)
    {
        Items = itemsFactory.Create();
    }

    // =========================================================================
    // DESIGN DECISION: Fetch children via their factory within parent Fetch.
    // The parent's Fetch method creates the child list, then populates it.
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
    // WHY NOT: Lazy loading adds complexity and can cause N+1 query problems.
    // Explicit fetch keeps the data access visible and predictable.
    // If you need lazy loading, implement it explicitly with clear naming.
    // =========================================================================
    [Remote]
    [Fetch]
    public void Fetch(int id,
        [Service] IFetchParentRepository parentRepository,
        [Service] IFetchChildRepository childRepository,
        [Service] IFetchDemoItemListFactory itemsFactory,
        [Service] IFetchDemoItemFactory itemFactory)
    {
        using (PauseAllActions())
        {
            // Load parent data
            var parentData = parentRepository.GetById(id);
            this["Id"].LoadValue(parentData.Id);
            this["Title"].LoadValue(parentData.Title);

            // Create the child list
            Items = itemsFactory.Create();

            // Load child data and create child entities
            var childDataList = childRepository.GetByParentId(id);
            foreach (var childData in childDataList)
            {
                var item = itemFactory.Create();
                item["Id"].LoadValue(childData.Id);
                item["Name"].LoadValue(childData.Name);

                // Add to list - this sets IsChild=true on the item
                Items.Add(item);
            }
        }

        // After fetch:
        // - Parent: IsNew=false, IsModified=false
        // - Each child: IsNew=false, IsModified=false, IsChild=true
    }

    [Remote]
    [Insert]
    public void Insert([Service] IFetchParentRepository parentRepository) { }

    [Remote]
    [Update]
    public void Update([Service] IFetchParentRepository parentRepository) { }

    [Remote]
    [Delete]
    public void Delete([Service] IFetchParentRepository parentRepository) { }
}

[Factory]
public partial class FetchDemoItem : EntityBase<FetchDemoItem>
{
    public partial int Id { get; set; }
    public partial string? Name { get; set; }

    public FetchDemoItem(IEntityBaseServices<FetchDemoItem> services) : base(services) { }

    [Create]
    public void Create() { }

    // Child entities don't need [Fetch] - they're populated by parent's Fetch
    // They DO need Insert/Update/Delete for the parent's Save to work

    [Remote]
    [Insert]
    public void Insert([Service] IFetchChildRepository repository) { }

    [Remote]
    [Update]
    public void Update([Service] IFetchChildRepository repository) { }

    [Remote]
    [Delete]
    public void Delete([Service] IFetchChildRepository repository) { }
}

[Factory]
public partial class FetchDemoItemList : EntityListBase<FetchDemoItem>
{
    [Create]
    public void Create() { }

    // Lists don't have Fetch/Insert/Update/Delete
    // They coordinate children during parent's Save
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
