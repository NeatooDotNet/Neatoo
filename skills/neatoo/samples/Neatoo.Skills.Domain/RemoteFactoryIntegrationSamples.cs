using Neatoo;
using Neatoo.RemoteFactory;

namespace Neatoo.Skills.Domain;

// =============================================================================
// REMOTE FACTORY INTEGRATION SAMPLES - Entity state during factory operations
// =============================================================================

// -----------------------------------------------------------------------------
// Entity with Full CRUD Lifecycle
// -----------------------------------------------------------------------------

/// <summary>
/// Repository interface for demonstrating persistence operations.
/// </summary>
public interface ISkillRemoteFactoryRepository
{
    Task<(int Id, string Name, string Department)> FetchAsync(int id);
    Task<int> InsertAsync(string name, string department);
    Task UpdateAsync(int id, string name, string department);
    Task DeleteAsync(int id);
}

/// <summary>
/// Entity demonstrating state changes during factory operations.
/// </summary>
[Factory]
public partial class SkillRfIntegrationRoot : EntityBase<SkillRfIntegrationRoot>
{
    public SkillRfIntegrationRoot(IEntityBaseServices<SkillRfIntegrationRoot> services) : base(services)
    {
        ChildrenProperty.LoadValue(new SkillRfIntegrationChildList());
    }

    public partial int Id { get; set; }
    public partial string Name { get; set; }
    public partial string Department { get; set; }
    public partial ISkillRfIntegrationChildList Children { get; set; }

    #region remote-factory-create
    [Create]
    public void Create()
    {
        // After Create completes:
        // - IsNew = true (entity not yet persisted)
        // - IsModified = false (initial state is clean)
        // - IsPaused = false (validation rules active)
        Id = 0;
        Name = "";
        Department = "";
    }
    #endregion

    #region remote-factory-fetch
    [Remote, Fetch]
    public async Task Fetch(int id, [Service] ISkillRemoteFactoryRepository repo)
    {
        // During Fetch:
        // - IsPaused = true (validation and modification tracking suspended)
        // - Property assignments use LoadValue semantics (no IsModified change)

        var data = await repo.FetchAsync(id);
        Id = data.Id;
        Name = data.Name;
        Department = data.Department;

        // After Fetch completes:
        // - IsNew = false (entity was loaded from persistence)
        // - IsModified = false (loaded state is considered clean)
        // - IsPaused = false (validation resumes)
    }
    #endregion

    #region remote-factory-insert
    [Remote, Insert]
    public async Task Insert([Service] ISkillRemoteFactoryRepository repo)
    {
        // Called when: IsNew == true during Save()
        Id = await repo.InsertAsync(Name, Department);

        // After Insert completes:
        // - IsNew = false (entity now exists in persistence)
        // - IsModified = false (changes have been persisted)
    }
    #endregion

    #region remote-factory-update
    [Remote, Update]
    public async Task Update([Service] ISkillRemoteFactoryRepository repo)
    {
        // Called when: IsNew == false && IsModified == true during Save()
        await repo.UpdateAsync(Id, Name, Department);

        // After Update completes:
        // - IsModified = false (changes have been persisted)
    }
    #endregion

    #region remote-factory-delete
    [Remote, Delete]
    public async Task Delete([Service] ISkillRemoteFactoryRepository repo)
    {
        // Called when: IsDeleted == true during Save()
        await repo.DeleteAsync(Id);

        // After Delete completes:
        // - Entity cannot be modified further
    }
    #endregion
}

// -----------------------------------------------------------------------------
// Child Entity (No [Remote] - persisted through aggregate root)
// -----------------------------------------------------------------------------

public interface ISkillRfIntegrationChild : IEntityBase
{
    int Id { get; set; }
    string Value { get; set; }
}

[Factory]
public partial class SkillRfIntegrationChild : EntityBase<SkillRfIntegrationChild>, ISkillRfIntegrationChild
{
    public SkillRfIntegrationChild(IEntityBaseServices<SkillRfIntegrationChild> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Value { get; set; }

    #region remote-factory-child-no-remote
    // Child entities do NOT use [Remote] - they persist through the aggregate root
    [Create]
    public void Create()
    {
        // IsChild = true (set when added to parent collection)
        // IsSavable = false (must save through aggregate root)
    }

    [Fetch]
    public void Fetch(int id, string value)
    {
        Id = id;
        Value = value;
    }

    // Insert/Update/Delete called by parent's Save() - no [Remote] needed
    [Insert]
    public void Insert() { /* Persist through aggregate root */ }

    [Update]
    public void Update() { /* Persist through aggregate root */ }

    [Delete]
    public void Delete() { /* Persist through aggregate root */ }
    #endregion
}

public interface ISkillRfIntegrationChildList : IEntityListBase<ISkillRfIntegrationChild>
{
    int DeletedCount { get; }
}

public class SkillRfIntegrationChildList : EntityListBase<ISkillRfIntegrationChild>, ISkillRfIntegrationChildList
{
    public int DeletedCount => DeletedList.Count;
}

// -----------------------------------------------------------------------------
// Usage Samples - State transitions during factory operations
// -----------------------------------------------------------------------------

/// <summary>
/// Static methods demonstrating entity state during factory operations.
/// These are extracted by MarkdownSnippets for documentation.
/// </summary>
public static class SkillRemoteFactoryStateSamples
{
    #region remote-factory-issavable-check
    /// <summary>
    /// IsSavable combines multiple state checks before persistence.
    /// </summary>
    public static async Task<bool> CheckSavableBeforeSave(SkillRfIntegrationRoot entity)
    {
        // IsSavable = IsModified && IsValid && !IsBusy && !IsChild
        if (!entity.IsSavable)
        {
            // Don't persist - one or more conditions failed:
            // - !IsModified: No changes to save
            // - !IsValid: Validation failed
            // - IsBusy: Async rules still running
            // - IsChild: Must save through parent aggregate
            return false;
        }

        // Safe to persist
        return true;
    }
    #endregion

    #region remote-factory-child-state-cascade
    /// <summary>
    /// Child state cascades to parent aggregate.
    /// </summary>
    public static void ChildStateCascadesToParent(
        SkillRfIntegrationRoot parent,
        ISkillRfIntegrationChildFactory childFactory)
    {
        // Add child
        var child = childFactory.Create();
        parent.Children.Add(child);

        // Child state affects parent:
        // - child.IsModified = true → parent.IsModified = true
        // - child.IsValid = false → parent.IsValid = false
        // - child.IsBusy = true → parent.IsBusy = true

        // Child cannot save independently:
        // - child.IsChild = true (after adding to collection)
        // - child.IsSavable = false (IsChild prevents saving)
    }
    #endregion

    #region remote-factory-deletedlist-lifecycle
    /// <summary>
    /// DeletedList lifecycle for removed items.
    /// </summary>
    public static void DeletedListLifecycle(
        SkillRfIntegrationRoot parent,
        ISkillRfIntegrationChildFactory childFactory)
    {
        // Step 1: New items are discarded when removed (never persisted)
        var newChild = childFactory.Create();
        parent.Children.Add(newChild);
        parent.Children.Remove(newChild);  // Discarded - never goes to DeletedList

        // Step 2: Existing items go to DeletedList when removed
        var existingChild = childFactory.Fetch(1, "existing");
        parent.Children.Add(existingChild);
        parent.Children.Remove(existingChild);
        // Now: existingChild.IsDeleted = true
        // Now: parent.Children.DeletedCount = 1

        // Step 3: During Save(), [Delete] called for each DeletedList item
        // Step 4: After Save(), DeletedList is cleared
    }
    #endregion
}
