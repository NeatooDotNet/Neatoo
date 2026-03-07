// -----------------------------------------------------------------------------
// Design.Domain - Generator Demo Interfaces
// -----------------------------------------------------------------------------
// Interface-first pattern for generator interaction demonstration entities.
// Every entity gets a matched public interface; concretes are internal.
// -----------------------------------------------------------------------------

using Neatoo;

namespace Design.Domain.Generators;

/// <summary>
/// Root interface for generator demo entity.
/// </summary>
public interface IGeneratorDemo : IEntityRoot
{
    string? Name { get; set; }
    int Value { get; set; }
}
