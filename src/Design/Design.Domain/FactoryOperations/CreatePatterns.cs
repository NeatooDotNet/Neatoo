// -----------------------------------------------------------------------------
// Design.Domain - [Create] Factory Operation Patterns
// -----------------------------------------------------------------------------
// This file demonstrates the [Create] attribute for initializing new objects.
// [Create] is unique among factory operations: it typically runs locally and
// does NOT need [Remote].
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.FactoryOperations;

// =============================================================================
// [Create] - Initialize New Objects
// =============================================================================
// Use [Create] for:
// - Constructing new domain objects
// - Setting default values
// - Initializing child collections
// - Pre-populating from static/cached data
//
// DESIGN DECISION: [Create] methods are NOT [Remote] by default.
// Creating an object doesn't require persistence access. The object exists
// only in memory until Save() is called. This allows:
// - Client-side object creation (faster UX in Blazor)
// - Offline capability for object construction
// - Server-side creation when called from other server methods
//
// After [Create] completes, FactoryComplete(FactoryOperation.Create) is called:
// - For EntityBase: MarkNew() sets IsNew=true
// - For ValidateBase: No special state changes
//
// GENERATOR BEHAVIOR: For each [Create] method, RemoteFactory generates:
//
// Interface:
//   public interface ICreateDemoFactory {
//       CreateDemo Create();
//       CreateDemo Create(string name);
//   }
//
// Implementation (no [Remote], so direct call):
//   public CreateDemo Create() {
//       var obj = serviceProvider.GetRequiredService<CreateDemo>();
//       obj.FactoryStart(FactoryOperation.Create);
//       obj.Create();  // Your method
//       obj.FactoryComplete(FactoryOperation.Create);
//       return obj;
//   }
// =============================================================================

/// <summary>
/// Demonstrates: [Create] patterns for object initialization.
/// </summary>
[Factory]
public partial class CreateDemo : EntityBase<CreateDemo>
{
    public partial string? Name { get; set; }
    public partial int Priority { get; set; }

    public CreateDemo(IEntityBaseServices<CreateDemo> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name is required" : string.Empty,
            t => t.Name);
    }

    // =========================================================================
    // Pattern 1: Parameterless Create
    // =========================================================================
    // The simplest create - just construct with defaults.
    // After this runs, IsNew=true, IsModified=true (new objects are modified).
    // =========================================================================
    [Create]
    public void Create()
    {
        // No initialization needed - properties start at default values
        // Name = null, Priority = 0
    }

    // =========================================================================
    // Pattern 2: Create with Parameters
    // =========================================================================
    // Initialize with provided values. Still no [Remote] needed.
    // Multiple [Create] overloads are supported.
    // =========================================================================
    [Create]
    public void Create(string name)
    {
        Name = name;
        Priority = 1;  // Default priority for named items
    }

    [Create]
    public void Create(string name, int priority)
    {
        Name = name;
        Priority = priority;
    }

    // =========================================================================
    // Pattern 3: Create with Service (rare, but supported)
    // =========================================================================
    // Some creates need injected services, but NOT [Remote].
    // Example: Loading default values from configuration.
    //
    // DID NOT DO THIS: Force [Remote] whenever [Service] is used.
    //
    // REJECTED PATTERN:
    //   [Remote]  // Auto-added because of [Service]?
    //   [Create]
    //   public void Create([Service] IConfig config) { }
    //
    // WHY NOT: [Service] might inject client-available services (IConfiguration,
    // IOptions<T>). Only use [Remote] when the service is server-only.
    // =========================================================================
    [Create]
    public void CreateWithDefaults([Service] ICreateDefaults defaults)
    {
        // ICreateDefaults could be available on both client and server
        Name = defaults.DefaultName;
        Priority = defaults.DefaultPriority;
    }

    // =========================================================================
    // Standard persistence operations (for completeness)
    // =========================================================================
    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] ICreateDemoRepository repository)
    {
        using (PauseAllActions())
        {
            var data = repository.GetById(id);
            this["Name"].LoadValue(data.Name);
            this["Priority"].LoadValue(data.Priority);
        }
    }

    [Remote]
    [Insert]
    public void Insert([Service] ICreateDemoRepository repository)
    {
        repository.Insert(Name!, Priority);
    }

    [Remote]
    [Update]
    public void Update([Service] ICreateDemoRepository repository)
    {
        repository.Update(Name!, Priority);
    }

    [Remote]
    [Delete]
    public void Delete([Service] ICreateDemoRepository repository)
    {
        repository.Delete(Name!);
    }
}

// =============================================================================
// Create with Child Collections
// =============================================================================
// When creating an aggregate, child collections are typically created empty.
// =========================================================================

/// <summary>
/// Demonstrates: Creating aggregate with child collections.
/// </summary>
[Factory]
public partial class CreateWithChildrenDemo : EntityBase<CreateWithChildrenDemo>
{
    public partial string? Title { get; set; }
    public partial CreateDemoItemList? Items { get; set; }

    public CreateWithChildrenDemo(IEntityBaseServices<CreateWithChildrenDemo> services) : base(services)
    {
    }

    // =========================================================================
    // DESIGN DECISION: Child collections are created via their own factory.
    // The parent's Create method uses the child factory to create the collection.
    //
    // COMMON MISTAKE: Creating child collection without factory.
    //
    // WRONG:
    //   [Create]
    //   public void Create() {
    //       Items = new CreateDemoItemList();  // No DI, no tracking!
    //   }
    //
    // RIGHT:
    //   [Create]
    //   public void Create([Service] ICreateDemoItemListFactory itemsFactory) {
    //       Items = itemsFactory.Create();  // Proper DI and initialization
    //   }
    // =========================================================================
    [Create]
    public void Create([Service] ICreateDemoItemListFactory itemsFactory)
    {
        Items = itemsFactory.Create();
        // Items is now an empty EntityListBase, ready for Add() calls
    }
}

[Factory]
public partial class CreateDemoItem : EntityBase<CreateDemoItem>
{
    public partial string? Name { get; set; }

    public CreateDemoItem(IEntityBaseServices<CreateDemoItem> services) : base(services) { }

    [Create]
    public void Create() { }

    [Create]
    public void Create(string name)
    {
        Name = name;
    }
}

[Factory]
public partial class CreateDemoItemList : EntityListBase<CreateDemoItem>
{
    [Create]
    public void Create() { }
}

// =============================================================================
// Support Interfaces
// =============================================================================

public interface ICreateDefaults
{
    string DefaultName { get; }
    int DefaultPriority { get; }
}

public interface ICreateDemoRepository
{
    (string Name, int Priority) GetById(int id);
    void Insert(string name, int priority);
    void Update(string name, int priority);
    void Delete(string name);
}
