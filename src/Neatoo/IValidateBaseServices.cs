using Neatoo.Rules;

namespace Neatoo;

/// <summary>
/// Provides services required by <see cref="ValidateBase{T}"/> for validation, rules, and property management.
/// </summary>
/// <remarks>
/// This interface wraps Neatoo services into a single dependency, so that inheriting classes
/// do not need to list all services individually and services can be added without breaking changes.
/// Provides property management, validation, and rule execution services.
/// </remarks>
/// <typeparam name="T">The type of the validate object that will use these services. Must derive from <see cref="ValidateBase{T}"/>.</typeparam>
public interface IValidateBaseServices<T>
    where T : ValidateBase<T>
{
    /// <summary>
    /// Gets the property info list containing metadata about all properties for type <typeparamref name="T"/>.
    /// </summary>
    IPropertyInfoList<T> PropertyInfoList { get; }

    /// <summary>
    /// Gets the property manager that supports validation for all properties on the object.
    /// </summary>
    IValidatePropertyManager<IValidateProperty> ValidatePropertyManager { get; }

    /// <summary>
    /// Gets the property factory for creating strongly-typed property backing fields.
    /// </summary>
    /// <remarks>
    /// The property factory is used by generated code to create property backing fields
    /// during object initialization.
    /// </remarks>
    IPropertyFactory<T> PropertyFactory { get; }

    /// <summary>
    /// Creates a new rule manager for the specified target object.
    /// </summary>
    /// <param name="target">The target object for which to create the rule manager.</param>
    /// <returns>A new <see cref="IRuleManager{T}"/> instance configured for the target.</returns>
    IRuleManager<T> CreateRuleManager(T target);
}
