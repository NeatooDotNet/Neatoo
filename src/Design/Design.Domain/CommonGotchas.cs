// -----------------------------------------------------------------------------
// Design.Domain - Common Gotchas
// -----------------------------------------------------------------------------
// This file documents common pitfalls developers encounter with Neatoo.
// Each gotcha includes a WRONG pattern, a RIGHT pattern, and explanation.
// Tests in Design.Tests/GotchaTests verify these behaviors.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain;

// =============================================================================
// GOTCHA 1: Assuming rules fire during [Create]
// =============================================================================
// During factory operations ([Create], [Fetch], [Insert], [Update], [Delete]),
// the object is PAUSED. Rules do NOT fire during these methods.
//
// This is intentional - it prevents cascading rule execution while the
// object is being initialized or persisted.
//
// COMMON MISTAKE: Setting a property in [Create] and expecting a
// dependent property to be calculated by the time [Create] returns.
//
// WRONG:
//   [Create]
//   public void Create() {
//       Quantity = 10;
//       Price = 5.00m;
//       // Total is still 0! The calculation rule hasn't run.
//   }
//
// RIGHT (Option 1): Calculate explicitly in Create:
//   [Create]
//   public void Create() {
//       Quantity = 10;
//       Price = 5.00m;
//       Total = Quantity * Price;  // Set explicitly
//   }
//
// RIGHT (Option 2): Await rules after Create completes:
//   var entity = factory.Create();
//   await entity.WaitForTasks();  // Rules run after FactoryComplete
//   // Now Total is calculated
//
// WHY: Rules are paused during factory operations to prevent partial state
// from triggering validation failures or infinite loops.
// =============================================================================

/// <summary>
/// Demonstrates Gotcha 1: Rules don't fire during [Create].
/// </summary>
[Factory]
public partial class Gotcha1Demo : ValidateBase<Gotcha1Demo>
{
    public partial int Quantity { get; set; }
    public partial decimal Price { get; set; }
    public partial decimal Total { get; set; }

    /// <summary>
    /// Tracks whether the calculation rule has run (for testing).
    /// </summary>
    public bool RuleHasRun { get; private set; }

    public Gotcha1Demo(IValidateBaseServices<Gotcha1Demo> services) : base(services)
    {
        // This rule calculates Total when Quantity or Price changes
        RuleManager.AddAction(
            t =>
            {
                t.Total = t.Quantity * t.Price;
                t.RuleHasRun = true;
            },
            t => t.Quantity,
            t => t.Price);
    }

    /// <summary>
    /// WRONG WAY: Sets properties expecting rule to calculate Total.
    /// After Create() returns, Total is still 0 because rules were paused.
    /// </summary>
    [Create]
    public void Create()
    {
        Quantity = 10;
        Price = 5.00m;
        // Total is NOT calculated here - rule is paused!
    }

    /// <summary>
    /// RIGHT WAY: Calculate explicitly during Create.
    /// </summary>
    [Create]
    public void CreateWithExplicitCalculation()
    {
        Quantity = 10;
        Price = 5.00m;
        Total = Quantity * Price;  // Calculate explicitly
    }
}

// =============================================================================
// GOTCHA 2: DeletedList behavior for IsNew=true items
// =============================================================================
// When you remove an item from an EntityListBase:
// - If IsNew=true: Item is DISCARDED (not tracked)
// - If IsNew=false: Item goes to DeletedList
//
// This is intentional - there's no reason to track deletion of something
// that was never persisted.
//
// COMMON MISTAKE: Removing a new item and expecting it in DeletedList.
//
// WRONG assumption:
//   var item = itemFactory.Create();  // IsNew=true
//   parent.Items.Add(item);
//   parent.Items.Remove(item);
//   // Expecting item in DeletedList - IT'S NOT THERE
//
// CORRECT understanding:
//   var item = itemFactory.Create();  // IsNew=true
//   parent.Items.Add(item);
//   parent.Items.Remove(item);
//   // Item is discarded - no DeletedList entry
//   // This is correct behavior - item was never persisted
//
// For fetched items:
//   var parent = await factory.Fetch(1);  // Items have IsNew=false
//   var item = parent.Items[0];           // IsNew=false
//   parent.Items.Remove(item);
//   // Item IS in DeletedList
//   // Save() will call [Delete] on this item
// =============================================================================

/// <summary>
/// Demonstrates Gotcha 2: DeletedList only tracks non-new items.
/// </summary>
[Factory]
public partial class Gotcha2Parent : EntityBase<Gotcha2Parent>
{
    public partial string? Name { get; set; }
    public partial Gotcha2ItemList? Items { get; set; }

    public Gotcha2Parent(IEntityBaseServices<Gotcha2Parent> services) : base(services) { }

    [Create]
    public void Create([Service] IGotcha2ItemListFactory itemListFactory)
    {
        Items = itemListFactory.Create();
    }

    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IGotcha2ItemListFactory itemListFactory)
    {
        using (PauseAllActions())
        {
            this["Name"].LoadValue($"Parent-{id}");
            Items = itemListFactory.FetchForParent(id);
        }
    }

    [Remote]
    [Insert]
    public void Insert([Service] IGotcha2Repository repository) { }

    [Remote]
    [Update]
    public void Update([Service] IGotcha2Repository repository) { }

    [Remote]
    [Delete]
    public void Delete([Service] IGotcha2Repository repository) { }
}

[Factory]
public partial class Gotcha2Item : EntityBase<Gotcha2Item>
{
    public partial int Id { get; set; }
    public partial string? Name { get; set; }

    public Gotcha2Item(IEntityBaseServices<Gotcha2Item> services) : base(services) { }

    [Create]
    public void Create()
    {
        // New item - IsNew=true after Create
    }

    /// <summary>
    /// Used during Fetch to simulate a persisted item (IsNew=false).
    /// </summary>
    [Fetch]
    public void Fetch(int id)
    {
        this["Id"].LoadValue(id);
        this["Name"].LoadValue($"Item-{id}");
        // After Fetch, IsNew=false - this item exists in database
    }

    [Insert]
    public void Insert() { }

    [Update]
    public void Update() { }

    [Delete]
    public void Delete() { }
}

[Factory]
public partial class Gotcha2ItemList : EntityListBase<Gotcha2Item>
{
    /// <summary>
    /// Exposes DeletedList count for testing.
    /// </summary>
    public int DeletedCount => DeletedList.Count;

    [Create]
    public void Create() { }

    [Fetch]
    public void FetchForParent(int parentId, [Service] IGotcha2ItemFactory itemFactory)
    {
        // Simulate fetching 2 items from database
        var item1 = itemFactory.Fetch(1);
        var item2 = itemFactory.Fetch(2);
        Add(item1);
        Add(item2);
    }
}

public interface IGotcha2Repository
{
    void Insert();
    void Update();
    void Delete();
}

// =============================================================================
// GOTCHA 3: Method-injected [Service] unavailable on client
// =============================================================================
// [Service] parameters on methods are resolved from the SERVER's DI container.
// If you call a method with [Service] on the client, you get a DI exception.
//
// COMMON MISTAKE: Calling a non-[Remote] method with [Service] on client.
//
// WRONG:
//   // In Blazor WASM client:
//   var employee = await employeeFactory.Create();
//   employee.DoServerThing();  // Has [Service] IDbContext - THROWS!
//
// RIGHT:
//   // Methods with server-only services need [Remote]
//   [Remote]
//   public void DoServerThing([Service] IDbContext db) { ... }
//   // Now client calls HTTP proxy, server resolves IDbContext
//
// KEY INSIGHT: [Remote] means "this is an entry point from client to server."
// Once on server, subsequent method calls don't need [Remote] - they're
// already server-side.
// =============================================================================

/// <summary>
/// Demonstrates Gotcha 3: Server-only services need [Remote].
/// </summary>
[Factory]
public partial class Gotcha3Demo : EntityBase<Gotcha3Demo>
{
    public partial string? Name { get; set; }

    public Gotcha3Demo(IEntityBaseServices<Gotcha3Demo> services) : base(services) { }

    [Create]
    public void Create() { }

    // =========================================================================
    // WRONG: This method has [Service] but no [Remote].
    // On client, IServerOnlyService is not registered - DI throws.
    // This is commented out as an example; see the RIGHT way below.
    // =========================================================================
    // public void DoServerThingWrong([Service] IServerOnlyService svc)
    // {
    //     svc.DoWork();
    // }

    // =========================================================================
    // RIGHT: [Remote] tells factory to generate HTTP proxy for client.
    // Server resolves IServerOnlyService from its DI container.
    // The method uses a factory operation like [Fetch] which supports
    // method-level [Service] injection.
    // =========================================================================

    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IServerOnlyService svc)
    {
        this["Name"].LoadValue(svc.GetDataById(id));
    }

    [Remote]
    [Insert]
    public void Insert([Service] IServerOnlyService svc) { }

    [Remote]
    [Update]
    public void Update([Service] IServerOnlyService svc) { }

    [Remote]
    [Delete]
    public void Delete([Service] IServerOnlyService svc) { }
}

public interface IServerOnlyService
{
    string GetServerData();
    string GetDataById(int id);
}

// =============================================================================
// GOTCHA 4: PauseAllActions breaks rule calculations
// =============================================================================
// When IsPaused=true, property setters do NOT trigger rules.
// This is useful for batch updates, but can cause stale calculated values.
//
// COMMON MISTAKE: Setting multiple properties while paused, expecting
// calculated properties to update.
//
// WRONG:
//   using (entity.PauseAllActions()) {
//       entity.Quantity = 10;
//       entity.Price = 5.00m;
//   }
//   // Expecting Total to be 50.00 - BUT rules haven't run yet!
//   // ResumeAllActions() resumes the ability to fire rules,
//   // but doesn't automatically run rules for changes made while paused.
//
// RIGHT (Option 1): Run rules explicitly after resume:
//   using (entity.PauseAllActions()) {
//       entity.Quantity = 10;
//       entity.Price = 5.00m;
//   }
//   await entity.RunRules(RunRulesFlag.All);  // Explicitly run all rules
//   // Now Total is 50.00
//
// RIGHT (Option 2): Set one property at a time without pausing:
//   entity.Quantity = 10;
//   entity.Price = 5.00m;  // Each setter triggers rules
//   await entity.WaitForTasks();  // Wait for async rules
//   // Total is 50.00
//
// DESIGN DECISION: PauseAllActions is for performance during batch updates.
// You must explicitly run rules afterward if you need calculations.
// =============================================================================

/// <summary>
/// Demonstrates Gotcha 4: Rules don't run while paused.
/// </summary>
[Factory]
public partial class Gotcha4Demo : ValidateBase<Gotcha4Demo>
{
    public partial int Quantity { get; set; }
    public partial decimal Price { get; set; }
    public partial decimal Total { get; set; }

    public Gotcha4Demo(IValidateBaseServices<Gotcha4Demo> services) : base(services)
    {
        RuleManager.AddAction(
            t => t.Total = t.Quantity * t.Price,
            t => t.Quantity,
            t => t.Price);
    }

    [Create]
    public void Create() { }
}

// =============================================================================
// GOTCHA 5: IsModified includes child modifications
// =============================================================================
// IsModified returns true if THIS object OR ANY CHILD is modified.
// Use IsSelfModified to check only the current object.
//
// COMMON MISTAKE: Checking IsModified to determine if the current object
// needs an [Update] call, when actually a child was modified.
//
// WRONG assumption:
//   if (parent.IsModified) {
//       // Parent itself might not be modified - could be a child
//       await parent.Update(...);  // Might update unchanged data
//   }
//
// RIGHT (for persistence logic):
//   if (parent.IsSelfModified) {
//       // Only update if THIS object changed
//       await parent.Update(...);
//   }
//   foreach (var child in parent.Items) {
//       if (child.IsSelfModified) {
//           // Handle child updates
//       }
//   }
//
// NOTE: You typically don't write this persistence logic manually.
// The framework's Save() method handles it correctly.
// This gotcha is about understanding what IsModified means.
// =============================================================================

/// <summary>
/// Demonstrates Gotcha 5: IsModified vs IsSelfModified.
/// </summary>
[Factory]
public partial class Gotcha5Parent : EntityBase<Gotcha5Parent>
{
    public partial string? Name { get; set; }
    public partial Gotcha5Child? Child { get; set; }

    public Gotcha5Parent(IEntityBaseServices<Gotcha5Parent> services) : base(services) { }

    [Create]
    public void Create([Service] IGotcha5ChildFactory childFactory)
    {
        Child = childFactory.Create();
    }

    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IGotcha5ChildFactory childFactory)
    {
        using (PauseAllActions())
        {
            this["Name"].LoadValue($"Parent-{id}");
            Child = childFactory.Fetch(id * 10);
        }
    }

    [Remote]
    [Insert]
    public void Insert([Service] IGotcha5Repository repository) { }

    [Remote]
    [Update]
    public void Update([Service] IGotcha5Repository repository) { }

    [Remote]
    [Delete]
    public void Delete([Service] IGotcha5Repository repository) { }
}

[Factory]
public partial class Gotcha5Child : EntityBase<Gotcha5Child>
{
    public partial string? Value { get; set; }

    public Gotcha5Child(IEntityBaseServices<Gotcha5Child> services) : base(services) { }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(int id)
    {
        this["Value"].LoadValue($"Child-{id}");
    }

    [Insert]
    public void Insert() { }

    [Update]
    public void Update() { }

    [Delete]
    public void Delete() { }
}

public interface IGotcha5Repository
{
    void Insert();
    void Update();
    void Delete();
}

// =============================================================================
// GOTCHA SUMMARY TABLE
// =============================================================================
//
// +-----+------------------------------------------+-----------------------------+
// | #   | Gotcha                                   | Solution                    |
// +-----+------------------------------------------+-----------------------------+
// | 1   | Rules don't fire during [Create]        | Calculate explicitly or     |
// |     |                                          | await WaitForTasks()        |
// +-----+------------------------------------------+-----------------------------+
// | 2   | DeletedList ignores IsNew=true items    | Expected behavior - new     |
// |     |                                          | items don't need deletion   |
// +-----+------------------------------------------+-----------------------------+
// | 3   | [Service] on methods needs [Remote]     | Add [Remote] or use         |
// |     |                                          | constructor injection       |
// +-----+------------------------------------------+-----------------------------+
// | 4   | PauseAllActions stops rule calculations | Call RunRules() after       |
// |     |                                          | ResumeAllActions()          |
// +-----+------------------------------------------+-----------------------------+
// | 5   | IsModified includes children            | Use IsSelfModified for      |
// |     |                                          | current object only         |
// +-----+------------------------------------------+-----------------------------+
// =============================================================================
