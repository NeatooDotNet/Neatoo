namespace Neatoo;

/// <summary>
/// Factory for creating strongly-typed property backing fields for a specific owner type.
/// </summary>
/// <typeparam name="TOwner">The type of the Neatoo object that owns the properties.</typeparam>
/// <remarks>
/// <para>
/// This interface enables generated property backing fields to be created via DI,
/// allowing per-type customization of property creation behavior. The default implementation
/// creates standard <see cref="IValidateProperty{T}"/> instances.
/// </para>
/// <para>
/// Register a custom implementation to customize property creation for specific types:
/// </para>
/// <code>
/// services.AddSingleton&lt;IPropertyFactory&lt;Person&gt;, CustomPersonPropertyFactory&gt;();
/// </code>
/// </remarks>
public interface IPropertyFactory<TOwner> where TOwner : IValidateBase
{
    /// <summary>
    /// Creates a strongly-typed property backing field.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property value.</typeparam>
    /// <param name="owner">The Neatoo object that owns this property.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>A new <see cref="IValidateProperty{TProperty}"/> instance.</returns>
    IValidateProperty<TProperty> Create<TProperty>(TOwner owner, string propertyName);
}
