// -----------------------------------------------------------------------------
// Design.Domain - State Properties
// -----------------------------------------------------------------------------
// This file documents all state properties: IsValid, IsModified, IsNew, etc.
// These properties control entity lifecycle and UI binding.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.PropertySystem;

// =============================================================================
// State Properties Overview
// =============================================================================
// Neatoo objects have "meta properties" that track object state.
// These are read-only and computed from object contents.
//
// ValidateBase<T> state properties:
// - IsValid: All validation rules pass (including children)
// - IsSelfValid: This object's rules pass (excluding children)
// - IsBusy: Async operations running (rules, lazy loading)
// - IsPaused: Events and rules suppressed
// - Parent: Reference to parent in object graph
//
// EntityBase<T> adds (inherits all ValidateBase properties):
// - IsNew: Never been persisted (needs Insert)
// - IsModified: Any property changed (including children)
// - IsSelfModified: This object's properties changed (excluding children)
// - IsDeleted: Marked for deletion
// - IsSavable: Can call Save() successfully
// - IsChild: Part of a parent aggregate
// - Root: Reference to aggregate root
// - ModifiedProperties: Names of changed properties
// - IsMarkedModified: Explicitly marked (not from property changes)
//
// ValidateListBase<I> state properties:
// - IsValid: All items are valid
// - IsSelfValid: Always true (lists don't have own rules)
// - IsBusy: Any item is busy
// - IsPaused: Events suppressed
// - Parent: Reference to owning object
//
// EntityListBase<I> adds:
// - IsModified: Any item modified OR DeletedList has items
// - IsSelfModified: Always false (lists track through children)
// - DeletedList: Items removed pending deletion (protected)
// - Root: Reference to aggregate root
// =============================================================================

/// <summary>
/// Demonstrates: Validation state properties (IsValid, IsSelfValid).
/// </summary>
[Factory]
public partial class ValidationStateDemo : ValidateBase<ValidationStateDemo>
{
    public partial string? RequiredField { get; set; }
    public partial ValidationChildDemo? Child { get; set; }

    public ValidationStateDemo(IValidateBaseServices<ValidationStateDemo> services) : base(services)
    {
        // Add validation rule that makes this object invalid when field is empty
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.RequiredField) ? "Required field is required" : string.Empty,
            t => t.RequiredField);
    }

    [Create]
    public void Create([Service] IValidationChildDemoFactory childFactory)
    {
        Child = childFactory.Create();
    }

    // =========================================================================
    // IsValid vs IsSelfValid
    // =========================================================================
    // IsValid: Aggregates validation from entire object graph.
    //   - False if ANY validation rule fails (this or children)
    //   - Computed from PropertyManager.IsValid (aggregates children)
    //
    // IsSelfValid: Only this object's direct properties.
    //   - False only if THIS object's rules fail
    //   - True even if children are invalid
    //
    // Example:
    //   Parent.RequiredField = "Valid"  -> Parent.IsSelfValid = true
    //   Child.RequiredField = null      -> Child.IsSelfValid = false
    //   Result:
    //     Parent.IsSelfValid = true (parent's own rules pass)
    //     Parent.IsValid = false (child is invalid)
    //     Child.IsSelfValid = false (child's rule fails)
    //     Child.IsValid = false (same, no grandchildren)
    // =========================================================================
}

[Factory]
public partial class ValidationChildDemo : ValidateBase<ValidationChildDemo>
{
    public partial string? RequiredField { get; set; }

    public ValidationChildDemo(IValidateBaseServices<ValidationChildDemo> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.RequiredField) ? "Child field is required" : string.Empty,
            t => t.RequiredField);
    }

    [Create]
    public void Create() { }
}

/// <summary>
/// Demonstrates: Modification state properties (IsModified, IsSelfModified, IsNew).
/// </summary>
[Factory]
public partial class ModificationStateDemo : EntityBase<ModificationStateDemo>
{
    public partial string? Name { get; set; }
    public partial ModificationChildDemo? Child { get; set; }

    public ModificationStateDemo(IEntityBaseServices<ModificationStateDemo> services) : base(services) { }

    [Create]
    public void Create([Service] IModificationChildDemoFactory childFactory)
    {
        Child = childFactory.Create();
    }

    [Remote]
    [Fetch]
    public void Fetch(int id,
        [Service] IStatePropertiesRepository repository,
        [Service] IModificationChildDemoFactory childFactory)
    {
        using (PauseAllActions())
        {
            var data = repository.GetById(id);
            this["Name"].LoadValue(data.Name);

            Child = childFactory.Create();
            Child["Value"].LoadValue(data.ChildValue);
        }
        // After Fetch: IsNew=false, IsModified=false, IsSelfModified=false
    }

    // =========================================================================
    // IsModified vs IsSelfModified
    // =========================================================================
    // IsModified: Aggregates modification from entire object graph.
    //   - True if ANY property changed (this or children)
    //   - Also true if IsDeleted or IsNew
    //   - Used for: IsSavable check, UI "unsaved changes" indicator
    //
    // IsSelfModified: Only this object's direct properties.
    //   - True only if THIS object's properties changed
    //   - Or if IsDeleted or IsMarkedModified
    //   - Used for: Deciding whether to call Update vs child-only save
    //
    // Example:
    //   Parent.Name unchanged          -> Parent.IsSelfModified = false
    //   Child.Value = "Changed"        -> Child.IsSelfModified = true
    //   Result:
    //     Parent.IsSelfModified = false (parent unchanged)
    //     Parent.IsModified = true (child changed)
    //     Child.IsSelfModified = true (child changed)
    //     Child.IsModified = true (same, no grandchildren)
    // =========================================================================

    // =========================================================================
    // IsNew - Persistence State
    // =========================================================================
    // IsNew = true: Object was created but never persisted.
    //   - Set by FactoryComplete(Create) calling MarkNew()
    //   - Save() will call [Insert]
    //
    // IsNew = false: Object exists in database.
    //   - Set by FactoryComplete(Insert/Fetch) calling MarkOld()
    //   - Save() will call [Update] or [Delete]
    //
    // STATE TRANSITIONS:
    //   Create() -> IsNew=true
    //   Fetch() -> IsNew=false
    //   Insert() -> IsNew becomes false after completion
    // =========================================================================

    [Remote]
    [Insert]
    public void Insert([Service] IStatePropertiesRepository repository) { }

    [Remote]
    [Update]
    public void Update([Service] IStatePropertiesRepository repository) { }

    [Remote]
    [Delete]
    public void Delete([Service] IStatePropertiesRepository repository) { }
}

[Factory]
public partial class ModificationChildDemo : EntityBase<ModificationChildDemo>
{
    public partial string? Value { get; set; }

    public ModificationChildDemo(IEntityBaseServices<ModificationChildDemo> services) : base(services) { }

    [Create]
    public void Create() { }
}

/// <summary>
/// Demonstrates: IsSavable, IsDeleted, IsChild, Root.
/// </summary>
[Factory]
public partial class SaveStateDemo : EntityBase<SaveStateDemo>
{
    public partial string? Name { get; set; }

    public SaveStateDemo(IEntityBaseServices<SaveStateDemo> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name required" : string.Empty,
            t => t.Name);
    }

    [Create]
    public void Create() { }

    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IStatePropertiesRepository repository)
    {
        this["Name"].LoadValue(repository.GetById(id).Name);
    }

    // =========================================================================
    // IsSavable - Can Save() Be Called?
    // =========================================================================
    // IsSavable = IsModified && IsValid && !IsBusy && !IsChild
    //
    // All conditions must be true:
    // - IsModified: Must have changes to persist
    // - IsValid: All validation must pass
    // - !IsBusy: No async operations in progress
    // - !IsChild: Not a child entity (children save through parent)
    //
    // COMMON MISTAKE: Checking IsSavable without awaiting tasks.
    //
    // WRONG:
    //   entity.Name = "Test";  // Triggers async validation
    //   if (entity.IsSavable) { }  // IsBusy might be true!
    //
    // RIGHT:
    //   entity.Name = "Test";
    //   await entity.WaitForTasks();  // Wait for async rules
    //   if (entity.IsSavable) { }  // Now accurate
    // =========================================================================

    // =========================================================================
    // IsDeleted - Deletion Marking
    // =========================================================================
    // IsDeleted = true: Entity is marked for deletion.
    //   - Set by calling Delete() or MarkDeleted()
    //   - Removed from parent list: automatically marked deleted
    //   - Save() will call [Delete] (if not IsNew)
    //
    // UnDelete() reverses the deletion marking.
    //   - Called automatically when re-adding to a list
    //   - Can be called manually to cancel deletion
    // =========================================================================

    // =========================================================================
    // IsChild - Aggregate Membership
    // =========================================================================
    // IsChild = true: Entity is part of a parent aggregate.
    //   - Set when added to an EntityListBase
    //   - Cannot call Save() directly (throws SaveOperationException)
    //   - Persisted through parent's Save()
    //
    // COMMON MISTAKE: Trying to save child entities.
    //
    // WRONG:
    //   parent.Items[0].Name = "Changed";
    //   await parent.Items[0].Save();  // THROWS! IsChild=true, IsSavable=false
    //
    // RIGHT:
    //   parent.Items[0].Name = "Changed";
    //   await parent.Save();  // Parent coordinates all child saves
    // =========================================================================

    // =========================================================================
    // Root - Aggregate Root Reference
    // =========================================================================
    // Root: Reference to the aggregate root entity.
    //   - null for aggregate root itself (or standalone entity)
    //   - Points to top-level parent for nested children
    //   - Used for: Intra-aggregate move detection, consistency checks
    //
    // Computed by walking Parent chain:
    //   - If Parent is null, Root is null (this is root or standalone)
    //   - If Parent has a Root, return that Root
    //   - If Parent has no Root, Parent IS the root
    // =========================================================================

    [Remote]
    [Insert]
    public void Insert([Service] IStatePropertiesRepository repository) { }

    [Remote]
    [Update]
    public void Update([Service] IStatePropertiesRepository repository) { }

    [Remote]
    [Delete]
    public void Delete([Service] IStatePropertiesRepository repository) { }
}

/// <summary>
/// Demonstrates: IsBusy, IsPaused, async operation tracking.
/// </summary>
[Factory]
public partial class BusyStateDemo : ValidateBase<BusyStateDemo>
{
    public partial string? Name { get; set; }
    public partial string? ComputedValue { get; set; }

    public BusyStateDemo(IValidateBaseServices<BusyStateDemo> services) : base(services)
    {
        // Add an async rule - IsBusy becomes true while it runs
        RuleManager.AddActionAsync(
            async t =>
            {
                // Simulate async work
                await Task.Delay(100);
                t.ComputedValue = $"Processed: {t.Name}";
            },
            t => t.Name);
    }

    [Create]
    public void Create() { }

    // =========================================================================
    // IsBusy - Async Operations Tracking
    // =========================================================================
    // IsBusy = true: Async operations are running.
    //   - Async validation rules
    //   - Async action rules
    //   - Property-level async operations
    //
    // IsBusy affects:
    //   - IsSavable: Cannot save while busy
    //   - UI: Can disable save button, show spinner
    //
    // WaitForTasks() awaits all pending async operations:
    //   entity.Name = "Test";  // Triggers async rule
    //   // entity.IsBusy might be true here
    //   await entity.WaitForTasks();
    //   // entity.IsBusy is now false
    //   // entity.ComputedValue is now populated
    // =========================================================================

    // =========================================================================
    // IsPaused - Event Suppression
    // =========================================================================
    // IsPaused = true: Events, rules, and notifications suppressed.
    //   - Property setters don't trigger rules
    //   - PropertyChanged events don't fire
    //   - Used during: Factory operations, deserialization, batch updates
    //
    // PauseAllActions() returns IDisposable:
    //   using (entity.PauseAllActions())
    //   {
    //       entity.Name = "A";  // No rules
    //       entity.Value = 1;   // No rules
    //   }
    //   // ResumeAllActions() called automatically
    //
    // DESIGN DECISION: LoadValue() works even when paused.
    // This is critical for Fetch operations which pause, then load values.
    // SetValue() via property setter is affected by pause state.
    // =========================================================================
}

// =============================================================================
// Support Types
// =============================================================================

public interface IStatePropertiesRepository
{
    (string Name, string ChildValue) GetById(int id);
}
