namespace Neatoo;

/// <summary>
/// Marker interface that identifies a type as a Neatoo domain object.
/// </summary>
/// <remarks>
/// <see cref="INeatooObject"/> is the root marker interface for all Neatoo domain objects.
/// It provides a common type that can be used for generic constraints and type checking
/// to identify objects participating in the Neatoo framework. All Neatoo base classes
/// implement this interface.
/// </remarks>
public interface INeatooObject
{
}
