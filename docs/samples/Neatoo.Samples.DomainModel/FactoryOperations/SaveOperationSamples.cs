/// <summary>
/// Code samples for docs/factory-operations.md - Insert/Update/Delete sections
///
/// Snippets in this file:
/// - docs:factory-operations:insert-operation
/// - docs:factory-operations:update-operation
/// - docs:factory-operations:delete-operation
/// - docs:factory-operations:service-injection
///
/// Corresponding tests: SaveOperationSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Samples.DomainModel.FactoryOperations;

/// <summary>
/// Mock entity for database persistence.
/// </summary>
public class InventoryItemEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Mock database context for save examples.
/// </summary>
public interface IInventoryDb
{
    List<InventoryItemEntity> Items { get; }
    void Add(InventoryItemEntity entity);
    InventoryItemEntity? Find(Guid id);
    void Remove(InventoryItemEntity entity);
    Task SaveChangesAsync();
}

public class MockInventoryDb : IInventoryDb
{
    public List<InventoryItemEntity> Items { get; } = [];

    public void Add(InventoryItemEntity entity) => Items.Add(entity);

    public InventoryItemEntity? Find(Guid id) => Items.FirstOrDefault(i => i.Id == id);

    public void Remove(InventoryItemEntity entity) => Items.Remove(entity);

    public Task SaveChangesAsync() => Task.CompletedTask;
}

#region insert-operation
/// <summary>
/// Entity demonstrating Insert operation pattern.
/// </summary>
public partial interface IInventoryItem : IEntityBase
{
    Guid Id { get; }
    string? Name { get; set; }
    int Quantity { get; set; }
    DateTime LastUpdated { get; }
}

[Factory]
internal partial class InventoryItem : EntityBase<InventoryItem>, IInventoryItem
{
    public InventoryItem(IEntityBaseServices<InventoryItem> services) : base(services) { }

    public partial Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public partial string? Name { get; set; }

    public partial int Quantity { get; set; }
    public partial DateTime LastUpdated { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
        LastUpdated = DateTime.UtcNow;
    }

    [Fetch]
    public void Fetch(InventoryItemEntity entity)
    {
        Id = entity.Id;
        Name = entity.Name;
        Quantity = entity.Quantity;
        LastUpdated = entity.LastUpdated;
    }

    [Insert]
    public async Task Insert([Service] IInventoryDb db, CancellationToken cancellationToken)
    {
        await RunRules(token: cancellationToken);
        if (!IsSavable)
            return;

        var entity = new InventoryItemEntity
        {
            Id = Id,
            Name = Name ?? "",
            Quantity = Quantity,
            LastUpdated = DateTime.UtcNow
        };
        db.Add(entity);
        await db.SaveChangesAsync();

        LastUpdated = entity.LastUpdated;
    }
    #endregion

    #region factory-update-operation
    [Update]
    public async Task Update([Service] IInventoryDb db, CancellationToken cancellationToken)
    {
        await RunRules(token: cancellationToken);
        if (!IsSavable)
            return;

        var entity = db.Find(Id);
        if (entity == null)
            throw new KeyNotFoundException("Item not found");

        // Only update modified properties
        if (this[nameof(Name)].IsModified)
            entity.Name = Name ?? "";
        if (this[nameof(Quantity)].IsModified)
            entity.Quantity = Quantity;

        entity.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync();

        LastUpdated = entity.LastUpdated;
    }
    #endregion

    #region delete-operation
    [Delete]
    public async Task Delete([Service] IInventoryDb db, CancellationToken cancellationToken)
    {
        var entity = db.Find(Id);
        if (entity != null)
        {
            db.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
    #endregion
}
