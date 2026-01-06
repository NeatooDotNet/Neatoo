/// <summary>
/// Code samples for docs/factory-operations.md - Factory.Save() usage section
///
/// Compile-time validation only (docs use inline examples):
/// - docs:factory-operations:save-usage-examples
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

#region docs:factory-operations:save-usage-examples
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
