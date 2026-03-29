namespace Neatoo;

/// <summary>
/// Factory for creating <see cref="EntityLazyLoad{T}"/> instances.
/// Inject via <c>[Service] IEntityLazyLoadFactory</c> parameter.
/// </summary>
public interface IEntityLazyLoadFactory
{
    /// <summary>
    /// Creates a lazy load wrapper with the specified loader delegate.
    /// </summary>
    /// <typeparam name="TChild">The type of value to lazy load.</typeparam>
    /// <param name="loader">Async function that loads the value when invoked.</param>
    /// <returns>A new <see cref="EntityLazyLoad{T}"/> configured to load via the delegate.</returns>
    EntityLazyLoad<TChild> Create<TChild>(Func<Task<TChild?>> loader) where TChild : class?;

    /// <summary>
    /// Creates a lazy load wrapper with a pre-loaded value.
    /// The returned instance has <see cref="EntityLazyLoad{T}.IsLoaded"/> set to <c>true</c>.
    /// </summary>
    /// <typeparam name="TChild">The type of value.</typeparam>
    /// <param name="value">The pre-loaded value.</param>
    /// <returns>A new <see cref="EntityLazyLoad{T}"/> with the value already loaded.</returns>
    EntityLazyLoad<TChild> Create<TChild>(TChild? value) where TChild : class?;
}

/// <summary>
/// Default implementation of <see cref="IEntityLazyLoadFactory"/>.
/// </summary>
public class EntityLazyLoadFactory : IEntityLazyLoadFactory
{
    /// <inheritdoc />
    public EntityLazyLoad<TChild> Create<TChild>(Func<Task<TChild?>> loader) where TChild : class?
    {
        return new EntityLazyLoad<TChild>(loader);
    }

    /// <inheritdoc />
    public EntityLazyLoad<TChild> Create<TChild>(TChild? value) where TChild : class?
    {
        return new EntityLazyLoad<TChild>(value);
    }
}
