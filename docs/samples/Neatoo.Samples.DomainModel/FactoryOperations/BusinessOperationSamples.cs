/// <summary>
/// Code samples for docs/factory-operations.md - Business Operations section
///
/// Snippets in this file:
/// - docs:factory-operations:business-operation-pattern
/// - docs:factory-operations:entity-save-method
/// - docs:meta-properties:business-operation-example
///
/// Corresponding tests: BusinessOperationSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Samples.DomainModel.FactoryOperations;

/// <summary>
/// Enum for visit status.
/// </summary>
public enum VisitStatus
{
    Active,
    Completed,
    Archived
}

/// <summary>
/// Mock entity for database persistence.
/// </summary>
public class VisitEntity
{
    public Guid Id { get; set; }
    public string? PatientName { get; set; }
    public VisitStatus Status { get; set; }
    public bool Archived { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Mock database context for visit examples.
/// </summary>
public interface IVisitDb
{
    List<VisitEntity> Visits { get; }
    VisitEntity? Find(Guid id);
    Task SaveChangesAsync();
}

public class MockVisitDb : IVisitDb
{
    public List<VisitEntity> Visits { get; } = [];
    public VisitEntity? Find(Guid id) => Visits.FirstOrDefault(v => v.Id == id);
    public Task SaveChangesAsync() => Task.CompletedTask;
}

#region docs:factory-operations:business-operation-pattern
/// <summary>
/// Entity with business operations that modify state and persist.
/// The Archive() method demonstrates the pattern: validate, modify, persist via Save().
/// </summary>
public partial interface IVisit : IEntityBase
{
    Guid Id { get; }
    string? PatientName { get; set; }
    VisitStatus Status { get; set; }
    bool Archived { get; }
    DateTime LastUpdated { get; }

    /// <summary>
    /// Archives the visit. No [Service] parameters - callable from interface.
    /// </summary>
    Task<IVisit> Archive();
}

[Factory]
internal partial class Visit : EntityBase<Visit>, IVisit
{
    public Visit(IEntityBaseServices<Visit> services) : base(services) { }

    public partial Guid Id { get; set; }

    [Required(ErrorMessage = "Patient name is required")]
    public partial string? PatientName { get; set; }

    public partial VisitStatus Status { get; set; }
    public partial bool Archived { get; set; }
    public partial DateTime LastUpdated { get; set; }

    /// <summary>
    /// Business operation: Archives the visit.
    /// </summary>
    /// <returns>The updated entity after persistence.</returns>
    public async Task<IVisit> Archive()
    {
        // Validate preconditions
        if (Archived)
            throw new InvalidOperationException("Visit is already archived");

        // Modify properties (client-side)
        Status = VisitStatus.Archived;
        Archived = true;
        LastUpdated = DateTime.UtcNow;

        // Persist via existing Save() - triggers [Update]
        return (IVisit)await this.Save();
    }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
        Status = VisitStatus.Active;
        Archived = false;
        LastUpdated = DateTime.UtcNow;
    }

    [Remote]
    [Fetch]
    public void Fetch(VisitEntity entity)
    {
        Id = entity.Id;
        PatientName = entity.PatientName;
        Status = entity.Status;
        Archived = entity.Archived;
        LastUpdated = entity.LastUpdated;
    }

    [Remote]
    [Insert]
    public async Task Insert([Service] IVisitDb db)
    {
        await RunRules();
        if (!IsSavable)
            return;

        var entity = new VisitEntity
        {
            Id = Id,
            PatientName = PatientName,
            Status = Status,
            Archived = Archived,
            LastUpdated = DateTime.UtcNow
        };
        db.Visits.Add(entity);
        await db.SaveChangesAsync();
        LastUpdated = entity.LastUpdated;
    }

    [Remote]
    [Update]
    public async Task Update([Service] IVisitDb db)
    {
        await RunRules();
        if (!IsSavable)
            return;

        var entity = db.Find(Id);
        if (entity == null)
            throw new KeyNotFoundException("Visit not found");

        // Only update modified properties
        if (this[nameof(PatientName)].IsModified)
            entity.PatientName = PatientName;
        if (this[nameof(Status)].IsModified)
            entity.Status = Status;
        if (this[nameof(Archived)].IsModified)
            entity.Archived = Archived;

        entity.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync();
        LastUpdated = entity.LastUpdated;
    }
}
#endregion

#region docs:factory-operations:entity-save-method
/// <summary>
/// Examples demonstrating entity.Save() vs factory.Save() patterns.
/// </summary>
public static class EntitySaveExamples
{
    /// <summary>
    /// Factory-based save - the documented pattern.
    /// </summary>
    public static async Task<IVisit> FactorySavePattern(
        IVisit visit,
        IVisitFactory visitFactory)
    {
        visit.PatientName = "Updated Name";

        // Factory-based save
        return await visitFactory.Save(visit);
    }

    /// <summary>
    /// Entity-based save - equivalent, but called on the entity.
    /// The entity internally calls its factory.Save(this).
    /// </summary>
    public static async Task<IVisit> EntitySavePattern(IVisit visit)
    {
        visit.PatientName = "Updated Name";

        // Entity-based save - same result
        return (IVisit)await visit.Save();
    }

    /// <summary>
    /// Business operation pattern - combines state modification with save.
    /// This is the preferred pattern for domain operations.
    /// </summary>
    public static async Task<IVisit> BusinessOperationPattern(IVisit visit)
    {
        // Single call: validates, modifies state, persists
        return await visit.Archive();
    }
}
#endregion

#region docs:meta-properties:business-operation-example
/// <summary>
/// Minimal example showing the business operation pattern.
/// Used in meta-properties.md to demonstrate Save() usage.
/// </summary>
public partial interface IOrder : IEntityBase
{
    OrderStatus Status { get; set; }
    DateTime? CompletedDate { get; }

    Task<IOrder> Complete();
}

public enum OrderStatus { Pending, Completed }

[Factory]
[SuppressFactory]  // Suppress factory generation - example only
internal partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services) { }

    public partial OrderStatus Status { get; set; }
    public partial DateTime? CompletedDate { get; set; }

    public async Task<IOrder> Complete()
    {
        if (Status == OrderStatus.Completed)
            throw new InvalidOperationException("Already completed");

        Status = OrderStatus.Completed;
        CompletedDate = DateTime.UtcNow;

        return (IOrder)await this.Save();
    }

    [Create]
    public void Create() => Status = OrderStatus.Pending;
}
#endregion
