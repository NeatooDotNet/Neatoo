using System.Diagnostics.CodeAnalysis;

namespace Neatoo;

/// <summary>
/// Factory for creating strongly-typed property backing fields for a specific owner type.
/// </summary>
/// <typeparam name="TOwner">The type of the Neatoo object that owns the properties.</typeparam>
/// <remarks>
/// <para>
/// The generated <c>InitializePropertyBackingFields</c> creates every backing field through
/// <see cref="IValidateBaseServices{T}.PropertyFactory"/>, so substituting the factory
/// substitutes the property type.
/// </para>
/// <para>
/// <b>ValidateBase:</b> <c>ValidateBaseServices&lt;T&gt;</c> resolves <c>IPropertyFactory&lt;T&gt;</c>
/// from DI, so a closed registration applies:
/// </para>
/// <code>
/// services.AddTransient&lt;IPropertyFactory&lt;Person&gt;, CustomPersonPropertyFactory&gt;();
/// </code>
/// <para>
/// <b>EntityBase:</b> <c>EntityBaseServices&lt;T&gt;</c> always constructs its own
/// <c>EntityPropertyFactory&lt;T&gt;</c> and never resolves <c>IPropertyFactory&lt;T&gt;</c> from DI.
/// Wrap the injected <see cref="IEntityBaseServices{T}"/> in an implementation that delegates every
/// member except <see cref="IValidateBaseServices{T}.PropertyFactory"/>, and pass the wrapper to the
/// base constructor. A custom entity property type must derive from <c>EntityProperty&lt;T&gt;</c>
/// and meet the serialization contract described in the Neatoo properties reference
/// (Custom property types).
/// </para>
/// </remarks>
public interface IPropertyFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] TOwner> where TOwner : IValidateBase
{
    /// <summary>
    /// Creates a strongly-typed property backing field.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property value.</typeparam>
    /// <param name="owner">The Neatoo object that owns this property.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>A new <see cref="IValidateProperty{TProperty}"/> instance.</returns>
    IValidateProperty<TProperty> Create<TProperty>(TOwner owner, string propertyName);

    /// <summary>
    /// Creates an EntityLazyLoad property backing field that wraps an inner type.
    /// The returned property subclass provides look-through behavior for
    /// IsValid, IsBusy, IsModified, WaitForTasks, etc.
    /// </summary>
    /// <typeparam name="TInner">The inner type of the EntityLazyLoad wrapper (the T in EntityLazyLoad&lt;T&gt;).</typeparam>
    /// <param name="owner">The Neatoo object that owns this property.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>A new <see cref="IValidateProperty"/> instance for EntityLazyLoad look-through.</returns>
    IValidateProperty CreateEntityLazyLoad<TInner>(TOwner owner, string propertyName) where TInner : class?;
}
