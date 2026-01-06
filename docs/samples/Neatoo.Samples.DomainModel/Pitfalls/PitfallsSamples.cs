/// <summary>
/// Code samples for pitfalls.md skill file
///
/// Snippets in this file:
/// - docs:pitfalls:pause-all-actions-interface
/// - docs:pitfalls:parent-aggregate-root
/// - docs:pitfalls:required-whitespace
/// - docs:pitfalls:map-modified-to-declaration
///
/// These demonstrate common mistakes and correct patterns.
/// </summary>

using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Samples.DomainModel.Pitfalls;

#region docs:pitfalls:pause-all-actions-interface
/// <summary>
/// PauseAllActions() is on the concrete class, not the interface.
/// </summary>
public partial interface IPauseActionsPitfall : IValidateBase
{
    string? FirstName { get; set; }
    string? LastName { get; set; }
}

[Factory]
internal partial class PauseActionsPitfall : ValidateBase<PauseActionsPitfall>, IPauseActionsPitfall
{
    public PauseActionsPitfall(IValidateBaseServices<PauseActionsPitfall> services) : base(services) { }

    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }

    [Create]
    public void Create() { }

    /// <summary>
    /// CORRECT: Use PauseAllActions inside entity methods where you have access to the concrete type.
    /// </summary>
    public void BulkUpdate(string first, string last)
    {
        // CORRECT - inside concrete class, PauseAllActions is accessible
        using (PauseAllActions())
        {
            FirstName = first;
            LastName = last;
        }
        // Rules run when disposed
    }
}

/// <summary>
/// Shows the WRONG way - trying to call PauseAllActions on interface.
/// </summary>
public static class PauseActionsPitfallExample
{
    public static void WrongWay(IPauseActionsPitfall person)
    {
        // WRONG - interfaces don't expose PauseAllActions
        // using (person.PauseAllActions()) { }  // Won't compile!

        // Must use methods on the entity that internally use PauseAllActions
        // or work with the concrete type
    }
}
#endregion

#region docs:pitfalls:parent-aggregate-root
/// <summary>
/// Parent property returns the aggregate root, NOT the containing list.
/// </summary>
public partial interface IParentPitfallOrder : IEntityBase
{
    IParentPitfallLineList Lines { get; }
}

public partial interface IParentPitfallLine : IEntityBase
{
    string? Description { get; set; }

    /// <summary>
    /// Parent returns the Order (aggregate root), NOT the LineList.
    /// </summary>
    IParentPitfallOrder? ParentOrder { get; }
}

public interface IParentPitfallLineList : IEntityListBase<IParentPitfallLine> { }

[Factory]
internal partial class ParentPitfallOrder : EntityBase<ParentPitfallOrder>, IParentPitfallOrder
{
    public ParentPitfallOrder(IEntityBaseServices<ParentPitfallOrder> services) : base(services) { }

    public partial IParentPitfallLineList Lines { get; set; }

    [Create]
    public void Create([Service] IParentPitfallLineListFactory listFactory)
    {
        Lines = listFactory.Create();
    }
}

[Factory]
internal partial class ParentPitfallLine : EntityBase<ParentPitfallLine>, IParentPitfallLine
{
    public ParentPitfallLine(IEntityBaseServices<ParentPitfallLine> services) : base(services) { }

    public partial string? Description { get; set; }

    /// <summary>
    /// Parent returns the aggregate root (Order), not the containing list.
    /// Cast to the expected parent type.
    /// </summary>
    public IParentPitfallOrder? ParentOrder => Parent as IParentPitfallOrder;

    /// <summary>
    /// To count siblings, go through the parent aggregate root.
    /// </summary>
    public int SiblingCount => ParentOrder?.Lines.Count ?? 0;

    [Create]
    public void Create() { }
}

[Factory]
internal class ParentPitfallLineList : EntityListBase<IParentPitfallLine>, IParentPitfallLineList
{
    [Create]
    public void Create() { }
}
#endregion

#region docs:pitfalls:required-whitespace
/// <summary>
/// [Required] on strings uses IsNullOrWhiteSpace, catching whitespace-only values.
/// This is STRICTER than standard .NET [Required] behavior.
/// </summary>
public partial interface IRequiredWhitespacePitfall : IValidateBase
{
    /// <summary>
    /// All of these fail validation:
    /// - null
    /// - "" (empty string)
    /// - "   " (whitespace only) - STRICTER than standard .NET!
    /// </summary>
    [Required]
    string? Name { get; set; }
}

[Factory]
internal partial class RequiredWhitespacePitfall : ValidateBase<RequiredWhitespacePitfall>, IRequiredWhitespacePitfall
{
    public RequiredWhitespacePitfall(IValidateBaseServices<RequiredWhitespacePitfall> services) : base(services) { }

    [Required]
    public partial string? Name { get; set; }

    [Create]
    public void Create() { }
}

/// <summary>
/// Demonstrates the stricter behavior.
/// </summary>
public static class RequiredWhitespacePitfallExample
{
    public static void ShowBehavior(IRequiredWhitespacePitfall entity)
    {
        entity.Name = null;      // Invalid - as expected
        entity.Name = "";        // Invalid - as expected
        entity.Name = "   ";     // Invalid - STRICTER than standard .NET!
        entity.Name = "John";    // Valid
    }
}
#endregion

#region docs:pitfalls:map-modified-to-declaration
/// <summary>
/// MapModifiedTo is generated - declare as partial method.
/// </summary>
public partial interface IMapModifiedToPitfall : IEntityBase
{
    Guid Id { get; }
    string? Name { get; set; }
    string? Description { get; set; }
}

/// <summary>
/// EF Core entity for persistence.
/// </summary>
public class MapModifiedToPitfallEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

[Factory]
internal partial class MapModifiedToPitfall : EntityBase<MapModifiedToPitfall>, IMapModifiedToPitfall
{
    public MapModifiedToPitfall(IEntityBaseServices<MapModifiedToPitfall> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? Name { get; set; }
    public partial string? Description { get; set; }

    /// <summary>
    /// Declare as partial - Neatoo.BaseGenerator provides the implementation.
    /// The generated code only copies properties where IsModified == true.
    /// </summary>
    public partial void MapModifiedTo(MapModifiedToPitfallEntity entity);

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    [Update]
    public async Task Update()
    {
        await RunRules();
        if (!IsSavable) return;

        // In real code: var entity = await db.FindAsync(Id);
        var entity = new MapModifiedToPitfallEntity { Id = Id };

        // Only modified properties are copied
        MapModifiedTo(entity);

        // In real code: await db.SaveChangesAsync();
    }
}
#endregion
