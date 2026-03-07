// -----------------------------------------------------------------------------
// Design.Domain - Common Gotcha Demo Interfaces
// -----------------------------------------------------------------------------
// Interface-first pattern for gotcha demonstration entities.
// -----------------------------------------------------------------------------

using Neatoo;

namespace Design.Domain;

// =============================================================================
// Gotcha 1: Rules don't fire during [Create]
// =============================================================================

/// <summary>
/// Interface for Gotcha 1 demo (ValidateBase — no IEntityRoot needed).
/// Exposes RunRules and WaitForTasks because this demo's purpose is
/// demonstrating rule timing during and after factory operations.
/// </summary>
public interface IGotcha1Demo : IValidateBase
{
    int Quantity { get; set; }
    decimal Price { get; set; }
    decimal Total { get; set; }
    bool RuleHasRun { get; }
    Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null);
    Task WaitForTasks();
}

// =============================================================================
// Gotcha 2: DeletedList behavior for IsNew=true items
// =============================================================================

/// <summary>
/// Root interface for Gotcha 2 parent entity.
/// </summary>
public interface IGotcha2Parent : IEntityRoot
{
    string? Name { get; set; }
    IGotcha2ItemList? Items { get; }
}

/// <summary>
/// Child interface for Gotcha 2 item entity.
/// </summary>
public interface IGotcha2Item : IEntityBase
{
    int Id { get; }
    string? Name { get; set; }
}

/// <summary>
/// List interface for Gotcha 2 item list.
/// </summary>
public interface IGotcha2ItemList : IEntityListBase<IGotcha2Item>
{
    int DeletedCount { get; }
}

// =============================================================================
// Gotcha 3: Method-injected [Service] unavailable on client
// =============================================================================

/// <summary>
/// Root interface for Gotcha 3 demo.
/// </summary>
public interface IGotcha3Demo : IEntityRoot
{
    string? Name { get; set; }
}

// =============================================================================
// Gotcha 4: PauseAllActions breaks rule calculations
// =============================================================================

/// <summary>
/// Interface for Gotcha 4 demo (ValidateBase — no IEntityRoot needed).
/// Exposes PauseAllActions and RunRules because this demo's entire purpose
/// is demonstrating pause/resume behavior.
/// </summary>
public interface IGotcha4Demo : IValidateBase
{
    int Quantity { get; set; }
    decimal Price { get; set; }
    decimal Total { get; set; }
    IDisposable PauseAllActions();
    Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null);
    Task WaitForTasks();
}

// =============================================================================
// Gotcha 5: IsModified includes child modifications
// =============================================================================

/// <summary>
/// Root interface for Gotcha 5 parent entity.
/// Exposes WaitForTasks because this demo tests async rule timing.
/// </summary>
public interface IGotcha5Parent : IEntityRoot
{
    string? Name { get; set; }
    IGotcha5Child? Child { get; }
    Task WaitForTasks();
}

/// <summary>
/// Child interface for Gotcha 5 child entity.
/// </summary>
public interface IGotcha5Child : IEntityBase
{
    string? Value { get; set; }
}
