// -----------------------------------------------------------------------------
// Design.Domain - All Four Base Classes Side-by-Side
// -----------------------------------------------------------------------------
// This file is part of the Design Source of Truth. It demonstrates ALL FOUR
// base classes that Neatoo supports, with extensive documentation of when to
// use each class, what they provide, and what gets generated.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.BaseClasses;

// =============================================================================
// BASE CLASS 1: ValidateBase<T> - Value Objects, Read Models, Validation-Only
// =============================================================================
// Use ValidateBase<T> when:
// - You need validation and rules but NOT persistence tracking
// - Building value objects (immutable or semi-immutable domain concepts)
// - Building DTOs/read models that need validation
// - Building forms/wizards that validate but don't directly persist
//
// DESIGN DECISION: ValidateBase<T> is the foundation for ALL Neatoo objects.
// EntityBase<T> extends ValidateBase<T>, so all entity capabilities build on
// this foundation. This follows the principle that validation is always needed,
// but persistence tracking is optional.
//
// STATE PROPERTIES provided by ValidateBase<T>:
// - IsValid: True when all validation rules pass (including children)
// - IsSelfValid: True when THIS object's rules pass (excluding children)
// - IsBusy: True when async operations are running
// - IsPaused: True when events/rules are suppressed (during factory ops)
// - Parent: Reference to parent in object graph
// - PropertyMessages: Collection of validation errors
//
// DID NOT DO THIS: Make ValidateBase track persistence state.
//
// REJECTED PATTERN:
//   public abstract class ValidateBase<T> {
//       public bool IsNew { get; }      // NO - persistence is EntityBase concern
//       public bool IsModified { get; } // NO - modification tracking is EntityBase concern
//   }
//
// WHY NOT: Separation of concerns. Value objects and DTOs don't need IsNew/IsModified.
// Tracking modification state adds memory overhead for objects that will never be persisted.
// =============================================================================

/// <summary>
/// Demonstrates: ValidateBase&lt;T&gt; for value objects and validation-only scenarios.
///
/// Key points:
/// - Provides validation infrastructure without persistence tracking
/// - IsValid/IsSelfValid track validation state
/// - IsBusy tracks async operations
/// - PauseAllActions()/ResumeAllActions() control event firing
/// - RuleManager provides fluent API for adding rules
/// </summary>
[Factory]
public partial class DemoValueObject : ValidateBase<DemoValueObject>
{
    // =========================================================================
    // GENERATOR BEHAVIOR: Partial properties trigger source generation.
    //
    // For this declaration:
    //   public partial string? Name { get; set; }
    //
    // Neatoo.BaseGenerator produces (in DemoValueObject.g.cs):
    //
    //   private IValidateProperty<string?> _nameProperty;
    //   public partial string? Name
    //   {
    //       get => _nameProperty.Value;
    //       set => _nameProperty.SetValue(value);
    //   }
    //
    // The InitializePropertyBackingFields() method is also generated to create
    // the property instances during construction.
    // =========================================================================
    public partial string? Name { get; set; }

    public partial string? Description { get; set; }

    // =========================================================================
    // Constructor Pattern: Services are injected, not created.
    //
    // DESIGN DECISION: All Neatoo objects receive services through constructor.
    // The IValidateBaseServices<T> wraps multiple services into one parameter
    // to enable future service additions without breaking changes.
    //
    // COMMON MISTAKE: Creating services manually.
    //
    // WRONG:
    //   public DemoValueObject() : base(new ValidateBaseServices<DemoValueObject>()) { }
    //
    // RIGHT:
    //   Use the factory to create instances - factory handles DI automatically.
    //   var obj = await DemoValueObjectFactory.Create();
    // =========================================================================
    public DemoValueObject(IValidateBaseServices<DemoValueObject> services) : base(services)
    {
        // Rules are added in constructor - they execute when trigger properties change.
        // See Rules/ folder for extensive RuleManager documentation.
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name is required" : string.Empty,
            t => t.Name);
    }

    // =========================================================================
    // Factory Methods: [Create] initializes new objects.
    //
    // DESIGN DECISION: [Create] methods are NOT marked [Remote] by default.
    // Creating an empty object requires no persistence access, so it can run
    // on client or server. Only methods needing server resources get [Remote].
    // =========================================================================
    [Create]
    public void Create()
    {
        // Called by factory after construction.
        // Initialize default values here if needed.
    }

    [Create]
    public void Create(string name)
    {
        Name = name;
    }
}

// =============================================================================
// BASE CLASS 2: EntityBase<T> - Persistent Domain Entities
// =============================================================================
// Use EntityBase<T> when:
// - You need full CRUD persistence lifecycle
// - Building aggregate roots that own the Save() operation
// - Building child entities within an aggregate
// - Tracking modifications for optimistic concurrency or change detection
//
// DESIGN DECISION: EntityBase<T> extends ValidateBase<T> with persistence tracking.
// This inheritance means entities get ALL validation capabilities plus:
// - IsNew: True when object hasn't been persisted yet
// - IsModified: True when any property changed (including children)
// - IsSelfModified: True when THIS object's properties changed (excluding children)
// - IsDeleted: True when marked for deletion
// - IsSavable: True when entity can be saved (Modified && Valid && !Busy && !Child)
// - IsChild: True when part of a parent aggregate (cannot save independently)
// - Root: Reference to aggregate root
// - ModifiedProperties: List of changed property names
// - Factory: Reference to IFactorySave<T> for persistence operations
//
// STATE MACHINE: Entity persistence states
//
// New Entity: IsNew=true, IsModified=true
//     -> Save() calls [Insert] factory method
//     -> After success: MarkOld(), MarkUnmodified()
//
// Existing Modified: IsNew=false, IsModified=true, IsDeleted=false
//     -> Save() calls [Update] factory method
//     -> After success: MarkUnmodified()
//
// Deleted: IsDeleted=true (regardless of IsNew)
//     -> If IsNew=true: No persistence (never existed)
//     -> If IsNew=false: Save() calls [Delete] factory method
//     -> After success: Object typically discarded
//
// Unmodified: IsModified=false
//     -> IsSavable=false, Save() is no-op
// =============================================================================

/// <summary>
/// Demonstrates: EntityBase&lt;T&gt; for persistent domain entities.
///
/// Key points:
/// - Inherits all ValidateBase capabilities (validation, rules, busy tracking)
/// - Adds IsNew/IsModified/IsDeleted for persistence state
/// - IsSavable = IsModified &amp;&amp; IsValid &amp;&amp; !IsBusy &amp;&amp; !IsChild
/// - Save() routes to Insert/Update/Delete based on state
/// - Child entities (IsChild=true) cannot save independently
/// </summary>
[Factory]
public partial class DemoEntity : EntityBase<DemoEntity>
{
    public partial string? Name { get; set; }

    public partial int Value { get; set; }

    // =========================================================================
    // GENERATOR BEHAVIOR: For EntityBase, properties generate IEntityProperty<T>
    // instead of IValidateProperty<T>. IEntityProperty adds:
    // - IsModified tracking per property
    // - LoadValue() for setting without marking modified
    // - MarkSelfUnmodified() for clearing modification state
    //
    // Generated code (in DemoEntity.g.cs):
    //
    //   private IEntityProperty<string?> _nameProperty;
    //   public partial string? Name
    //   {
    //       get => _nameProperty.Value;
    //       set => _nameProperty.SetValue(value);
    //   }
    // =========================================================================

    public DemoEntity(IEntityBaseServices<DemoEntity> services) : base(services)
    {
        // Note: IEntityBaseServices<T> extends IValidateBaseServices<T>
        // so all validation services are available.

        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name is required" : string.Empty,
            t => t.Name);

        RuleManager.AddValidation(
            t => t.Value < 0 ? "Value must be non-negative" : string.Empty,
            t => t.Value);
    }

    // =========================================================================
    // Factory Methods: CRUD Operations
    //
    // DESIGN DECISION: Each operation has a specific attribute:
    // - [Create]: Initialize new object (typically local, no [Remote])
    // - [Fetch]: Load existing from persistence (needs [Remote])
    // - [Insert]: Persist new object (called by Save())
    // - [Update]: Persist changes (called by Save())
    // - [Delete]: Remove from persistence (called by Save())
    //
    // Save() automatically routes to Insert/Update/Delete based on state.
    // You NEVER call Insert/Update/Delete directly - Save() does that.
    //
    // COMMON MISTAKE: Calling Insert/Update/Delete directly.
    //
    // WRONG:
    //   var entity = await factory.Create();
    //   await factory.Insert(entity);  // NO! Use Save()
    //
    // RIGHT:
    //   var entity = await factory.Create();
    //   entity.Name = "Test";
    //   await entity.Save();  // Routes to Insert because IsNew=true
    // =========================================================================

    [Create]
    public void Create()
    {
        // After this method completes, FactoryComplete(FactoryOperation.Create)
        // is called, which calls MarkNew().
        // Result: IsNew=true, IsModified=true (empty but new = modified)
    }

    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IDemoRepository repository)
    {
        // Method [Service] injection - repository only available on server.
        // After Fetch completes, entity is: IsNew=false, IsModified=false

        // Use LoadValue to set properties without triggering modification tracking.
        // See PropertySystem/PropertyBasics.cs for LoadValue vs SetValue.
        using (PauseAllActions())
        {
            var data = repository.GetById(id);
            this["Name"].LoadValue(data.Name);
            this["Value"].LoadValue(data.Value);
        }
    }

    [Remote]
    [Insert]
    public void Insert([Service] IDemoRepository repository)
    {
        // Called by Save() when IsNew=true
        repository.Insert(Name!, Value);

        // After Insert completes, FactoryComplete(FactoryOperation.Insert) is called:
        // - MarkUnmodified() clears modification state
        // - MarkOld() sets IsNew=false
    }

    [Remote]
    [Update]
    public void Update([Service] IDemoRepository repository)
    {
        // Called by Save() when IsNew=false && IsModified=true && !IsDeleted
        repository.Update(Name!, Value);

        // After Update completes:
        // - MarkUnmodified() clears modification state
    }

    [Remote]
    [Delete]
    public void Delete([Service] IDemoRepository repository)
    {
        // Called by Save() when IsDeleted=true && IsNew=false
        repository.Delete(Name!);
    }
}

// =============================================================================
// BASE CLASS 3: ValidateListBase<I> - Collections of Read Models/Value Objects
// =============================================================================
// Use ValidateListBase<I> when:
// - You need a collection of ValidateBase items
// - Building lists of DTOs or value objects
// - The list items don't need persistence tracking
//
// DESIGN DECISION: ValidateListBase extends ObservableCollection<I>.
// This provides standard collection behaviors plus:
// - IsValid/IsSelfValid aggregated from all children
// - IsBusy aggregated from all children
// - Parent reference for object graph
// - PropertyChanged/NeatooPropertyChanged events bubble up
// - PauseAllActions for batch operations
//
// DID NOT DO THIS: Track list-level modification.
//
// REJECTED PATTERN:
//   public abstract class ValidateListBase<I> {
//       public bool IsModified { get; }  // NO - that's EntityListBase
//   }
//
// WHY NOT: ValidateListBase is for read models and value objects that don't
// need persistence. Adding IsModified would add overhead and confusion.
// =============================================================================

/// <summary>
/// Demonstrates: ValidateListBase&lt;I&gt; for collections of value objects/read models.
///
/// Key points:
/// - Extends ObservableCollection&lt;I&gt; with validation aggregation
/// - IsValid = all children are valid
/// - IsBusy = any child is busy
/// - Parent-child relationships managed automatically
/// </summary>
[Factory]
public partial class DemoValueObjectList : ValidateListBase<DemoValueObject>
{
    // ValidateListBase has no required constructor - uses default.

    // Factory methods can populate the list
    [Create]
    public void Create()
    {
        // Start with empty list
    }

    [Remote]
    [Fetch]
    public void Fetch([Service] IDemoRepository repository, [Service] IDemoValueObjectFactory valueObjectFactory)
    {
        // Fetch returns a list of value objects
        var items = repository.GetAllNames();

        // Note: List bases don't have PauseAllActions - items are added directly
        // Rules on individual items run as they are added
        foreach (var name in items)
        {
            var item = valueObjectFactory.Create(name);
            Add(item);
        }
    }
}

// =============================================================================
// BASE CLASS 4: EntityListBase<I> - Collections of Child Entities
// =============================================================================
// Use EntityListBase<I> when:
// - You need a collection of EntityBase items within an aggregate
// - Child entities should be persisted with the aggregate root
// - You need DeletedList for tracking removed items
//
// DESIGN DECISION: EntityListBase extends ValidateListBase with persistence tracking.
// Critical capabilities:
// - IsModified: True when any child is modified OR DeletedList has items
// - DeletedList: Items removed from list that need deletion on save
// - Root: Reference to aggregate root (for consistency checks)
// - Cascade: Adding/removing items updates child state automatically
//
// DELETEDLIST LIFECYCLE: How EntityListBase manages removed items
//
// 1. ITEM REMOVED FROM LIST (list.Remove(item) or list.RemoveAt(index)):
//    |-- If item.IsNew = true:
//    |   +-- Item discarded (never persisted, no DeletedList entry)
//    |-- If item.IsNew = false:
//        |-- item.MarkDeleted() called -> IsDeleted = true
//        |-- Item added to DeletedList
//        +-- item.ContainingList reference PRESERVED (for save routing)
//
// 2. DURING AGGREGATE SAVE (Root.Save()):
//    |-- For each item in DeletedList:
//    |   +-- [Delete] factory method called to persist deletion
//    |-- After successful persistence:
//        |-- DeletedList.Clear() called
//        +-- ContainingList references cleared on deleted items
//
// 3. INTRA-AGGREGATE MOVE (item moves from ListA to ListB within same aggregate):
//    |-- ListA.Remove(item) -> item added to ListA.DeletedList
//    |-- ListB.Add(item):
//        |-- item.UnDelete() called -> IsDeleted = false
//        |-- item removed from ListA.DeletedList
//        +-- item.ContainingList updated to ListB
//    +-- Result: Item moves without triggering persistence delete
//
// 4. FACTORY COMPLETE CLEANUP:
//    +-- FactoryComplete(FactoryOperation.Update) triggers DeletedList cleanup
//
// DID NOT DO THIS: Keep deleted items in the main list with a flag.
//
// REJECTED PATTERN:
//   foreach (var item in list)
//   {
//       if (!item.IsDeleted) { // Have to check every iteration }
//   }
//
// WHY NOT: Separate DeletedList means iteration only sees active items.
// Persistence code iterates DeletedList for deletes, main list for others.
// =============================================================================

/// <summary>
/// Demonstrates: EntityListBase&lt;I&gt; for collections of child entities.
///
/// Key points:
/// - Extends ValidateListBase with persistence tracking
/// - IsModified = any child modified OR DeletedList has items
/// - DeletedList tracks removed non-new items for persistence deletion
/// - Adding items: MarkAsChild(), set ContainingList
/// - Removing non-new items: MarkDeleted(), add to DeletedList
/// - Root property for aggregate boundary enforcement
/// </summary>
[Factory]
public partial class DemoEntityList : EntityListBase<DemoEntity>
{
    // DESIGN DECISION: EntityListBase doesn't define IsSavable or Save().
    // Lists are ALWAYS saved through their parent aggregate root.
    // The parent's Save() method iterates the list and calls Insert/Update/Delete.

    /// <summary>
    /// Test helper: Exposes the count of items in DeletedList.
    /// The DeletedList is protected, but tests need to verify deletion behavior.
    /// </summary>
    public int DeletedCount => DeletedList.Count;

    [Create]
    public void Create()
    {
        // Empty list
    }
}

// =============================================================================
// SERVICE INTERFACE FOR DEMOS
// =============================================================================
// This is a simple repository interface used by the demo classes.
// In real applications, this would be in the Infrastructure layer.
// =============================================================================

/// <summary>
/// Demo repository interface for persistence operations.
/// </summary>
public interface IDemoRepository
{
    (string Name, int Value) GetById(int id);
    void Insert(string name, int value);
    void Update(string name, int value);
    void Delete(string name);
    IEnumerable<string> GetAllNames();
}

// =============================================================================
// DESIGN DECISION SUMMARY: When to Use Each Base Class
// =============================================================================
//
// +-------------------+------------------+------------------+------------------+
// | Need              | Single Object    | Collection       |
// +-------------------+------------------+------------------+------------------+
// | Validation only   | ValidateBase<T>  | ValidateListBase<I> |
// | (DTOs, VOs)       |                  |                  |
// +-------------------+------------------+------------------+------------------+
// | Full persistence  | EntityBase<T>    | EntityListBase<I>   |
// | (Aggregates)      |                  |                  |
// +-------------------+------------------+------------------+------------------+
//
// DESIGN DECISION: The class hierarchy is intentional:
//
//   ValidateBase<T>       ValidateListBase<I>
//        ^                       ^
//        |                       |
//   EntityBase<T>          EntityListBase<I>
//
// Entities extend validation, lists mirror this structure.
// This ensures consistent validation semantics across all object types.
// =============================================================================

// =============================================================================
// COMMON MISTAKES SUMMARY
// =============================================================================
//
// COMMON MISTAKE: Calling Save() on child entities.
//
// WRONG:
//   var parent = await ParentFactory.Fetch(id);
//   parent.Children[0].Name = "New Name";
//   await parent.Children[0].Save();  // THROWS: IsChild=true, IsSavable=false
//
// RIGHT:
//   await parent.Save();  // Parent save persists all child changes
//
// COMMON MISTAKE: Using SetValue instead of LoadValue during Fetch.
//
// WRONG:
//   [Fetch]
//   public void Fetch(int id, [Service] IRepo repo) {
//       Name = repo.Get(id).Name;  // Sets IsModified=true!
//   }
//
// RIGHT:
//   [Fetch]
//   public void Fetch(int id, [Service] IRepo repo) {
//       this["Name"].LoadValue(repo.Get(id).Name);  // IsModified stays false
//   }
//
// COMMON MISTAKE: Expecting removed items to persist without aggregate Save().
//
// WRONG:
//   parent.Children.Remove(child);  // Child in DeletedList
//   // Assuming child is deleted in database - IT IS NOT YET
//
// RIGHT:
//   parent.Children.Remove(child);  // Child in DeletedList
//   await parent.Save();  // NOW child [Delete] method called
//
// COMMON MISTAKE: Not waiting for async operations.
//
// WRONG:
//   entity.Name = "Test";  // May trigger async validation rule
//   if (entity.IsValid) { }  // May be checking before rule completes!
//
// RIGHT:
//   entity.Name = "Test";
//   await entity.WaitForTasks();  // Wait for async rules
//   if (entity.IsValid) { }  // Now safe to check
// =============================================================================
