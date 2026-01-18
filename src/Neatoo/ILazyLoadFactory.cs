namespace Neatoo;

/// <summary>
/// Factory for creating <see cref="LazyLoad{T}"/> instances.
/// Inject via <c>[Service] ILazyLoadFactory</c> parameter.
/// </summary>
public interface ILazyLoadFactory
{
    /// <summary>
    /// Creates a lazy load wrapper with the specified loader delegate.
    /// </summary>
    /// <typeparam name="TChild">The type of value to lazy load.</typeparam>
    /// <param name="loader">Async function that loads the value when invoked.</param>
    /// <returns>A new <see cref="LazyLoad{T}"/> configured to load via the delegate.</returns>
    LazyLoad<TChild> Create<TChild>(Func<Task<TChild?>> loader) where TChild : class;

    /// <summary>
    /// Creates a lazy load wrapper with a pre-loaded value.
    /// The returned instance has <see cref="LazyLoad{T}.IsLoaded"/> set to <c>true</c>.
    /// </summary>
    /// <typeparam name="TChild">The type of value.</typeparam>
    /// <param name="value">The pre-loaded value.</param>
    /// <returns>A new <see cref="LazyLoad{T}"/> with the value already loaded.</returns>
    LazyLoad<TChild> Create<TChild>(TChild? value) where TChild : class;
}

/// <summary>
/// Default implementation of <see cref="ILazyLoadFactory"/>.
/// </summary>
public class LazyLoadFactory : ILazyLoadFactory
{
    /// <inheritdoc />
    public LazyLoad<TChild> Create<TChild>(Func<Task<TChild?>> loader) where TChild : class
    {
        return new LazyLoad<TChild>(loader);
    }

    /// <inheritdoc />
    public LazyLoad<TChild> Create<TChild>(TChild? value) where TChild : class
    {
        return new LazyLoad<TChild>(value);
    }
}
