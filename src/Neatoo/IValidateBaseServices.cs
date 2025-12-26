using Neatoo.Rules;

namespace Neatoo;

/// <summary>
/// Provides services required by <see cref="ValidateBase{T}"/> for validation, rules, and property management.
/// </summary>
/// <remarks>
/// This interface extends <see cref="IBaseServices{T}"/> with validation-specific functionality,
/// including rule management and validation property management. Inheriting classes do not need
/// to list all services individually, and services can be added without breaking changes.
/// </remarks>
/// <typeparam name="T">The type of the validate object that will use these services. Must derive from <see cref="ValidateBase{T}"/>.</typeparam>
public interface IValidateBaseServices<T> : IBaseServices<T>
    where T : ValidateBase<T>
{
    /// <summary>
    /// Creates a new rule manager for the specified target object.
    /// </summary>
    /// <param name="target">The target object for which to create the rule manager.</param>
    /// <returns>A new <see cref="IRuleManager{T}"/> instance configured for the target.</returns>
    IRuleManager<T> CreateRuleManager(T target);

    /// <summary>
    /// Gets the property manager that supports validation for all properties on the object.
    /// </summary>
    IValidatePropertyManager<IValidateProperty> ValidatePropertyManager { get; }
}
