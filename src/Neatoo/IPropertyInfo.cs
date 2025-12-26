using System.Reflection;

namespace Neatoo;

/// <summary>
/// Provides metadata about a property defined on a Neatoo object.
/// </summary>
/// <remarks>
/// <see cref="IPropertyInfo"/> wraps <see cref="System.Reflection.PropertyInfo"/> and provides
/// additional functionality for property management within the Neatoo framework. It is used
/// internally to define and access property metadata during object initialization and property operations.
/// </remarks>
public interface IPropertyInfo
{
    /// <summary>
    /// Gets the underlying <see cref="System.Reflection.PropertyInfo"/> for this property.
    /// </summary>
    PropertyInfo PropertyInfo { get; }

    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the declared type of the property.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets a unique key that identifies this property.
    /// </summary>
    /// <remarks>
    /// The key typically combines the declaring type and property name to ensure uniqueness
    /// across the type hierarchy.
    /// </remarks>
    string Key { get; }

    /// <summary>
    /// Gets a value indicating whether the property has a private setter.
    /// </summary>
    /// <value><c>true</c> if the property setter is private; otherwise, <c>false</c>.</value>
    public bool IsPrivateSetter { get; }

    /// <summary>
    /// Gets a custom attribute of the specified type applied to this property.
    /// </summary>
    /// <typeparam name="T">The type of attribute to retrieve.</typeparam>
    /// <returns>The attribute instance if found; otherwise, <c>null</c>.</returns>
    T? GetCustomAttribute<T>() where T : Attribute;

    /// <summary>
    /// Gets all custom attributes applied to this property.
    /// </summary>
    /// <returns>An enumerable collection of attributes applied to this property.</returns>
    IEnumerable<Attribute> GetCustomAttributes();
}

/// <summary>
/// Provides access to the collection of property metadata for a Neatoo object type.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IPropertyInfoList"/> is used internally by Neatoo to manage property definitions
/// for a specific type. It provides lookup capabilities for property metadata.
/// </para>
/// <para>
/// <strong>Important:</strong> This interface should not be registered directly in the DI container.
/// Use <see cref="IPropertyInfoList{T}"/> for DI registration.
/// </para>
/// </remarks>
public interface IPropertyInfoList
{
    /// <summary>
    /// Gets the property metadata for the specified property name.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <returns>The <see cref="IPropertyInfo"/> for the specified property, or <c>null</c> if not found.</returns>
    IPropertyInfo? GetPropertyInfo(string name);

    /// <summary>
    /// Gets all property metadata defined for this type.
    /// </summary>
    /// <returns>An enumerable collection of all <see cref="IPropertyInfo"/> instances.</returns>
    IEnumerable<IPropertyInfo> Properties();

    /// <summary>
    /// Determines whether a property with the specified name exists.
    /// </summary>
    /// <param name="propertyName">The name of the property to check.</param>
    /// <returns><c>true</c> if the property exists; otherwise, <c>false</c>.</returns>
    bool HasProperty(string propertyName);
}

/// <summary>
/// Provides strongly-typed access to the collection of property metadata for a specific Neatoo object type.
/// </summary>
/// <typeparam name="T">The type of Neatoo object whose properties are described.</typeparam>
/// <remarks>
/// This interface is intended for DI container registration. It ensures that each Neatoo object type
/// receives its own property metadata collection, preventing cross-type access to properties.
/// </remarks>
public interface IPropertyInfoList<T> : IPropertyInfoList { }