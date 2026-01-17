namespace Neatoo;

/// <summary>
/// Wrapper for explicit async lazy loading of child entities or related data.
/// Separates UI binding concerns (state properties) from async loading (explicit await).
/// </summary>
/// <typeparam name="T">The type of value to lazy load. Must be a reference type.</typeparam>
/// <remarks>
/// <para>
/// Key principle: Accessing <see cref="Value"/> never triggers a load - it returns current state only.
/// Loading is always explicit via <c>await</c> or <see cref="LoadAsync"/>.
/// </para>
/// <para>
/// Always use <see cref="ILazyLoadFactory"/> to create instances. Do not instantiate directly.
/// </para>
/// </remarks>
public class LazyLoad<T> where T : class
{
    private readonly Func<Task<T?>> _loader;

#pragma warning disable CS0649 // Field is never assigned to - will be assigned by LoadAsync in future implementation
    private T? _value;
    private bool _isLoaded;
#pragma warning restore CS0649

    /// <summary>
    /// Creates a new lazy load wrapper with the specified loader delegate.
    /// </summary>
    /// <param name="loader">Async function that loads the value when invoked.</param>
    public LazyLoad(Func<Task<T?>> loader)
    {
        _loader = loader;
    }

    /// <summary>
    /// Gets the current value. Returns <c>null</c> if not yet loaded.
    /// Never triggers a load - use <c>await</c> or <see cref="LoadAsync"/> to load.
    /// </summary>
    public T? Value => _value;

    /// <summary>
    /// Gets whether the value has been loaded.
    /// </summary>
    public bool IsLoaded => _isLoaded;
}
