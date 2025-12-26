namespace Neatoo;

/// <summary>
/// Provides services required by <see cref="Base{T}"/> for property management and metadata.
/// </summary>
/// <remarks>
/// This interface wraps NeatooBase services into a single dependency, so that inheriting classes
/// do not need to list all services individually and services can be added without breaking changes.
/// </remarks>
/// <typeparam name="T">The type of the Neatoo object that will use these services.</typeparam>
public interface IBaseServices<T>
{
    /// <summary>
    /// Gets the property manager responsible for managing property values and change notifications.
    /// </summary>
    IPropertyManager<IProperty> PropertyManager { get; }

    /// <summary>
    /// Gets the property info list containing metadata about all properties for type <typeparamref name="T"/>.
    /// </summary>
    IPropertyInfoList<T> PropertyInfoList { get; }
}
