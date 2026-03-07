// -----------------------------------------------------------------------------
// Design.Domain - Factory Operation Demo Interfaces
// -----------------------------------------------------------------------------
// Interface-first pattern for factory operation demonstration entities.
// Each entity and list has a matched public interface.
// -----------------------------------------------------------------------------

using Neatoo;

namespace Design.Domain.FactoryOperations;

// =============================================================================
// Save Pattern Interfaces
// =============================================================================

/// <summary>
/// Root interface for standalone save demo entity.
/// </summary>
public interface ISaveDemo : IEntityRoot
{
    int Id { get; }
    string? Name { get; set; }
    decimal Amount { get; set; }
}

/// <summary>
/// Root interface for aggregate save demo.
/// </summary>
public interface ISaveAggregateDemo : IEntityRoot
{
    int Id { get; }
    string? Title { get; set; }
    ISaveDemoItemList? Items { get; }
}

/// <summary>
/// Child interface for save demo item.
/// </summary>
public interface ISaveDemoItem : IEntityBase
{
    int Id { get; }
    string? Name { get; set; }
    int Quantity { get; set; }
}

/// <summary>
/// List interface for save demo item list.
/// </summary>
public interface ISaveDemoItemList : IEntityListBase<ISaveDemoItem> { }

// =============================================================================
// Create Pattern Interfaces
// =============================================================================

/// <summary>
/// Root interface for standalone create demo entity.
/// </summary>
public interface ICreateDemo : IEntityRoot
{
    string? Name { get; set; }
    int Priority { get; set; }
}

/// <summary>
/// Root interface for create-with-children demo.
/// </summary>
public interface ICreateWithChildrenDemo : IEntityRoot
{
    string? Title { get; set; }
    ICreateDemoItemList? Items { get; }
}

/// <summary>
/// Child interface for create demo item.
/// </summary>
public interface ICreateDemoItem : IEntityBase
{
    string? Name { get; set; }
}

/// <summary>
/// List interface for create demo item list.
/// </summary>
public interface ICreateDemoItemList : IEntityListBase<ICreateDemoItem> { }

// =============================================================================
// Fetch Pattern Interfaces
// =============================================================================

/// <summary>
/// Root interface for standalone fetch demo entity.
/// </summary>
public interface IFetchDemo : IEntityRoot
{
    int Id { get; }
    string? Name { get; set; }
    string? Description { get; set; }
}

/// <summary>
/// Root interface for fetch-with-children demo.
/// </summary>
public interface IFetchWithChildrenDemo : IEntityRoot
{
    int Id { get; }
    string? Title { get; set; }
    IFetchDemoItemList? Items { get; }
}

/// <summary>
/// Child interface for fetch demo item.
/// </summary>
public interface IFetchDemoItem : IEntityBase
{
    int Id { get; }
    string? Name { get; set; }
}

/// <summary>
/// List interface for fetch demo item list.
/// </summary>
public interface IFetchDemoItemList : IEntityListBase<IFetchDemoItem> { }

// =============================================================================
// Remote Boundary Interfaces
// =============================================================================

/// <summary>
/// Root interface for remote boundary demo.
/// </summary>
public interface IRemoteBoundaryDemo : IEntityRoot
{
    int Id { get; }
    string? Name { get; set; }
}

// =============================================================================
// Service Injection Interfaces
// =============================================================================

/// <summary>
/// Root interface for service injection demo.
/// </summary>
public interface IServiceInjectionDemo : IEntityRoot
{
    string? Name { get; set; }
}

// =============================================================================
// Dual Use Entity Interfaces
// =============================================================================

/// <summary>
/// Root interface for dual-use entity demo.
/// Can serve as aggregate root or as child within another aggregate.
/// </summary>
public interface IDualUseEntity : IEntityRoot
{
    int Id { get; }
    string? Street { get; set; }
    string? City { get; set; }
}
