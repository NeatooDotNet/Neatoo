using Neatoo.RemoteFactory;

namespace Neatoo;

/// <summary>
/// Provides services required by <see cref="EntityBase{T}"/> for entity management, persistence, and validation.
/// </summary>
/// <remarks>
/// <para>
/// This interface extends <see cref="IValidateBaseServices{T}"/> with entity-specific functionality,
/// including property modification tracking and factory-based persistence. Inheriting classes do not
/// need to list all services individually, and services can be added without breaking changes.
/// </para>
/// <para>
/// This interface is registered in the dependency injection container.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the entity object that will use these services. Must derive from <see cref="EntityBase{T}"/>.</typeparam>
public interface IEntityBaseServices<T> : IValidateBaseServices<T>
    where T : EntityBase<T>
{
    /// <summary>
    /// Gets the property manager that supports modification tracking for all properties on the entity.
    /// </summary>
    IEntityPropertyManager EntityPropertyManager { get; }

    /// <summary>
    /// Gets the factory responsible for saving the entity, or <c>null</c> if no factory is configured.
    /// </summary>
    /// <value>The <see cref="IFactorySave{T}"/> instance for the entity type, or <c>null</c>.</value>
    IFactorySave<T>? Factory { get; }
}
