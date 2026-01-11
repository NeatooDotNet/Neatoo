/// <summary>
/// Code samples for docs/factory-operations.md - Factory.Save() usage section
///
/// Compile-time validation only (docs use inline examples):
/// - docs:factory-operations:save-usage-examples
/// - docs:migration:save-reassignment
/// - docs:migration:cancellation-token
///
/// Corresponding tests: SaveUsageSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;

namespace Neatoo.Samples.DomainModel.FactoryOperations;

/// <summary>
/// Simple entity for demonstrating Save usage patterns.
/// </summary>
public partial interface ISaveableItem : IEntityBase
{
    Guid Id { get; }
    string? Name { get; set; }
}

[Factory]
internal partial class SaveableItem : EntityBase<SaveableItem>, ISaveableItem
{
    public SaveableItem(IEntityBaseServices<SaveableItem> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? Name { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    [Insert]
    public Task Insert()
    {
        // Simulated database insert - in real code would persist to DB
        return Task.CompletedTask;
    }

    [Update]
    public Task Update()
    {
        // Simulated database update
        return Task.CompletedTask;
    }

    [Delete]
    public Task Delete()
    {
        // Simulated database delete
        return Task.CompletedTask;
    }
}

#region save-usage-examples
/// <summary>
/// Examples demonstrating correct Save() usage patterns.
/// </summary>
public static class SaveUsageExamples
{
    /// <summary>
    /// CORRECT: Always reassign after Save().
    /// The Save method returns a new instance after client-server roundtrip.
    /// </summary>
    public static async Task<ISaveableItem> CorrectSavePattern(
        ISaveableItemFactory factory)
    {
        var item = factory.Create();
        item.Name = "New Item";

        // CORRECT - capture the returned instance
        item = await factory.Save(item);

        // Now 'item' has database-generated values
        return item;
    }

    /// <summary>
    /// Delete pattern: mark for deletion, then save.
    /// </summary>
    public static async Task DeletePattern(
        ISaveableItem item,
        ISaveableItemFactory factory)
    {
        // Mark for deletion
        item.Delete();

        // Save triggers the Delete operation
        await factory.Save(item);
    }

    /// <summary>
    /// UnDelete pattern: undo deletion before save.
    /// </summary>
    public static void UnDeletePattern(ISaveableItem item)
    {
        item.Delete();  // Mark for deletion

        // Changed mind - undo the deletion
        item.UnDelete();

        // Now IsDeleted = false, item will not be deleted on save
    }
}
#endregion

#region save-reassignment
/// <summary>
/// Migration pattern: Save() reassignment (v10.5+)
///
/// IMPORTANT: Always reassign the result of Save() to your variable.
/// Save() returns a new instance after the client-server roundtrip.
/// </summary>
public static class SaveReassignmentMigration
{
    public static async Task CorrectPattern(
        ISaveableItem entity,
        ISaveableItemFactory factory)
    {
        // v10.5+ CORRECT: Always reassign
        entity = await factory.Save(entity);

        // The returned entity has:
        // - Server-generated values (timestamps, computed fields)
        // - IsNew = false after Insert
        // - IsModified = false after successful save
    }

    // WRONG: Ignoring the return value (pre-v10.5 habit)
    // await factory.Save(entity);  // DON'T DO THIS
    //
    // The original 'entity' reference becomes stale after Save().
    // Server-side changes are only in the returned instance.
}
#endregion

#region cancellation-token
/// <summary>
/// Migration pattern: CancellationToken support (v10.5+)
///
/// Async operations now support CancellationToken for graceful cancellation.
/// The token flows from client to server via HTTP connection state.
/// </summary>
public static class CancellationTokenMigration
{
    public static async Task RunRulesWithCancellation(
        ISaveableItem entity,
        CancellationToken cancellationToken)
    {
        // v10.5+: RunRules supports CancellationToken
        await entity.RunRules(token: cancellationToken);

        // CancellationToken flows to:
        // - AsyncRuleBase.Execute() methods
        // - Server-side operations via HTTP connection
        // - Database queries (if passed through)
    }

    // For Save operations, cancellation is handled via HTTP:
    // - Client disconnection cancels server operation
    // - No explicit CancellationToken parameter needed on Save
}
#endregion
