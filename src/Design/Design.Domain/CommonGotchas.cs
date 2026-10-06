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
// RIGHT: Call RunRules at the end of your factory method.
// RunRules works even while paused — it has no IsPaused guard.
//   [Create]
//   public async Task Create() {
//       Quantity = 10;
//       Price = 5.00m;
//       await RunRules(RunRulesFlag.All);  // Forces all rules to execute
//       // Total is now 50.00
//   }
//
// NOTE: ResumeAllActions (called by FactoryComplete) does NOT run rules.
// It only recalculates cached validity. PropertyChanged does NOT fire
// for changes made while paused. The only thing that runs rules is
// an explicit RunRules() call or a property change after unpausing.
//
// WHY: Rules are paused during factory operations to prevent partial state
// from triggering validation failures or infinite loops.
// =============================================================================

/// <summary>
/// Demonstrates Gotcha 1: Rules don't fire during [Create].
/// </summary>
[Factory]
internal partial class Gotcha1Demo : ValidateBase<Gotcha1Demo>, IGotcha1Demo
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
        #region skill-computed-gap-rule
        // This rule calculates Total when Quantity or Price changes
        RuleManager.AddAction(
            t =>
            {
                t.Total = t.Quantity * t.Price;
                t.RuleHasRun = true;
            },
            t => t.Quantity,
            t => t.Price);
        #endregion
    }

    #region skill-create-without-run-rules
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
    #endregion

    #region skill-create-run-rules
    /// <summary>
    /// RIGHT WAY: Call RunRules at end of factory method.
    /// RunRules works even while paused — no IsPaused guard.
    /// </summary>
    [Create]
    public async Task CreateWithRunRules()
    {
        Quantity = 10;
        Price = 5.00m;
        await RunRules(RunRulesFlag.All);  // Forces all rules to execute
        // Total is now 50.00
    }
    #endregion
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
//   // The root's Save() runs the list's [Update], which removes this
//   // item's row - no child [Delete] runs (children have none)
// =============================================================================

/// <summary>
/// Demonstrates Gotcha 2: DeletedList only tracks non-new items.
/// </summary>
[Factory]
internal partial class Gotcha2Parent : EntityBase<Gotcha2Parent>, IGotcha2Parent
{
    public partial string? Name { get; set; }
    public partial IGotcha2ItemList? Items { get; set; }

    public Gotcha2Parent(IEntityBaseServices<Gotcha2Parent> services) : base(services) { }

    [Create]
    public void Create([Service] IGotcha2ItemListFactory itemListFactory)
    {
        Items = itemListFactory.Create();
    }

    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IGotcha2ItemListFactory itemListFactory)
    {
        Name = $"Parent-{id}";
        Items = itemListFactory.FetchForParent(id);
    }

    [Remote]
    [Insert]
    internal void Insert([Service] IGotcha2Repository repository) { }

    [Remote]
    [Update]
    internal void Update([Service] IGotcha2Repository repository) { }

    [Remote]
    [Delete]
    internal void Delete([Service] IGotcha2Repository repository) { }
}

[Factory]
internal partial class Gotcha2Item : EntityBase<Gotcha2Item>, IGotcha2Item
{
    public partial int Id { get; set; }
    public partial string? Name { get; set; }

    public Gotcha2Item(IEntityBaseServices<Gotcha2Item> services) : base(services) { }

    [Create]
    public void Create()
    {
        // New item - IsNew=true after Create
    }

    // Child entity factory methods (Fetch/Insert/Update/Delete) are internal:
    // server-only, trimmable on client.

    [Fetch]
    internal void Fetch(int id)
    {
        Id = id;
        Name = $"Item-{id}";
    }

    [Insert]
    internal void Insert() { }

    [Update]
    internal void Update() { }

    [Delete]
    internal void Delete() { }
}

[Factory]
internal partial class Gotcha2ItemList : EntityListBase<IGotcha2Item>, IGotcha2ItemList
{
    /// <summary>
    /// Exposes DeletedList count for testing.
    /// </summary>
    public int DeletedCount => DeletedList.Count;

    [Create]
    public void Create() { }

    [Fetch]
    internal void FetchForParent(int parentId, [Service] IGotcha2ItemFactory itemFactory)
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
// GOTCHA 3: A server-only [Service] on an operation that runs on the client
// =============================================================================
// A [Service] parameter resolves in the DI container of the tier the factory
// operation runs on. A root operation without [Remote] runs on the caller's
// tier - on a Blazor WASM client, the client container - so a server-only
// service there (DbContext, repository) is not registered and DI throws.
//
// COMMON MISTAKE: A client-called root operation with a server-only [Service]
// and no [Remote].
//
// WRONG:
//   [Fetch]
//   internal void Fetch(int id, [Service] IServerOnlyService svc) { ... }
//   // Client: await factory.Fetch(1) runs locally - IServerOnlyService
//   // is not registered on the client, DI throws.
//
// RIGHT:
//   [Remote]
//   [Fetch]
//   internal void Fetch(int id, [Service] IServerOnlyService svc) { ... }
//   // The client call crosses to the server; the server container resolves it.
//
// KEY INSIGHT: [Remote] means "this is an entry point from client to server."
// Once on the server, child operations reached from it don't need [Remote] -
// they already run there, so they take server-only services freely.
//
// [Service] belongs on factory operations ([Create], [Fetch], [Insert],
// [Update], [Delete], [Execute]). An ordinary entity method gets no generated
// proxy, so [Remote] on it does nothing; server work goes through a factory
// operation or an [Execute] command.
//
// DID NOT DO THIS: Move the server-only service to the entity's constructor.
//
// WHY NOT: The constructor runs on both tiers, so a server-only service there
// breaks construction on the client instead of one call.
// =============================================================================

/// <summary>
/// Demonstrates Gotcha 3: Server-only services go on [Remote] entry points.
/// </summary>
[Factory]
internal partial class Gotcha3Demo : EntityBase<Gotcha3Demo>, IGotcha3Demo
{
    public partial string? Name { get; set; }

    public Gotcha3Demo(IEntityBaseServices<Gotcha3Demo> services) : base(services) { }

    [Create]
    public void Create() { }

    // =========================================================================
    // RIGHT: [Remote] makes the client call cross to the server, where
    // IServerOnlyService is registered.
    // =========================================================================

    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IServerOnlyService svc)
    {
        Name = svc.GetDataById(id);
    }

    [Remote]
    [Insert]
    internal void Insert([Service] IServerOnlyService svc) { }

    [Remote]
    [Update]
    internal void Update([Service] IServerOnlyService svc) { }

    [Remote]
    [Delete]
    internal void Delete([Service] IServerOnlyService svc) { }
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
//   // ResumeAllActions() does NOT run rules. It does NOT fire PropertyChanged
//   // for changes made while paused. It only recalculates cached validity.
//
// RIGHT: Call RunRules inside or after the paused block.
// RunRules works even while paused — it has no IsPaused guard.
//   using (entity.PauseAllActions()) {
//       entity.Quantity = 10;
//       entity.Price = 5.00m;
//       await entity.RunRules(RunRulesFlag.All);  // Works while paused
//   }
//   // Total is 50.00
//
// DESIGN DECISION: PauseAllActions is for performance during batch updates.
// You must explicitly call RunRules() if you need computed values.
//
// WARNING: On an entity, a property set while paused is not marked modified.
// Edits made inside PauseAllActions() on a fetched entity leave IsModified
// false, so IsSavable stays false and the edits are not saved. Factory
// operations are already paused; never wrap their bodies in PauseAllActions().
// =============================================================================

/// <summary>
/// Demonstrates Gotcha 4: Rules don't run while paused.
/// </summary>
[Factory]
internal partial class Gotcha4Demo : ValidateBase<Gotcha4Demo>, IGotcha4Demo
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
// COMMON MISTAKE: Checking IsModified inside a root's [Update] to decide
// whether to write the root's own row, when actually a child was modified.
//
// WRONG (inside the root's [Update]):
//   var row = repository.Get(Id);
//   if (IsModified) {
//       // The root itself might not be modified - could be a child
//       MapTo(row);  // Rewrites unchanged root columns
//   }
//
// RIGHT (inside the root's [Update]):
//   var row = repository.Get(Id);
//   if (IsSelfModified) {
//       MapTo(row);  // Only when THIS object's own properties changed
//   }
//   itemsFactory.Save(Items, row.Items);  // the list's [Update] decides per
//                                          // child: new, modified, removed
//   repository.SaveChanges();
//
// NOTE: The root never inspects children's state itself. The list's [Update]
// writes only new and modified children (each through the child factory's
// Save), removes the rows of removed children, and skips the rest.
// This gotcha is about understanding what IsModified means.
// =============================================================================

/// <summary>
/// Demonstrates Gotcha 5: IsModified vs IsSelfModified.
/// </summary>
[Factory]
internal partial class Gotcha5Parent : EntityBase<Gotcha5Parent>, IGotcha5Parent
{
    public partial string? Name { get; set; }
    public partial IGotcha5Child? Child { get; set; }

    public Gotcha5Parent(IEntityBaseServices<Gotcha5Parent> services) : base(services) { }

    [Create]
    public void Create([Service] IGotcha5ChildFactory childFactory)
    {
        Child = childFactory.Create();
    }

    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IGotcha5ChildFactory childFactory)
    {
        Name = $"Parent-{id}";
        Child = childFactory.Fetch(id * 10);
    }

    [Remote]
    [Insert]
    internal void Insert([Service] IGotcha5Repository repository) { }

    [Remote]
    [Update]
    internal void Update([Service] IGotcha5Repository repository) { }

    [Remote]
    [Delete]
    internal void Delete([Service] IGotcha5Repository repository) { }
}

[Factory]
internal partial class Gotcha5Child : EntityBase<Gotcha5Child>, IGotcha5Child
{
    public partial string? Value { get; set; }

    public Gotcha5Child(IEntityBaseServices<Gotcha5Child> services) : base(services) { }

    [Create]
    public void Create() { }

    // Child entity factory methods (Fetch/Insert/Update/Delete) are internal:
    // server-only, trimmable on client.

    [Fetch]
    internal void Fetch(int id)
    {
        Value = $"Child-{id}";
    }

    [Insert]
    internal void Insert() { }

    [Update]
    internal void Update() { }

    [Delete]
    internal void Delete() { }
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
// | 1   | Rules don't fire during [Create]        | await RunRules() at end     |
// |     |                                          | of factory method           |
// +-----+------------------------------------------+-----------------------------+
// | 2   | DeletedList ignores IsNew=true items    | Expected behavior - new     |
// |     |                                          | items don't need deletion   |
// +-----+------------------------------------------+-----------------------------+
// | 3   | Server-only [Service] on a root         | Add [Remote] to the client  |
// |     | operation that runs on the client        | entry point                 |
// +-----+------------------------------------------+-----------------------------+
// | 4   | PauseAllActions stops rule calculations | Call RunRules() explicitly  |
// |     |                                          | (works even while paused)   |
// +-----+------------------------------------------+-----------------------------+
// | 5   | IsModified includes children            | Use IsSelfModified for      |
// |     |                                          | current object only         |
// +-----+------------------------------------------+-----------------------------+
// =============================================================================
