using Neatoo.Rules;

namespace Neatoo;

/// <summary>
/// Wrap the NeatooBase services into an interface so that 
/// the inheriting classes don't need to list all services
/// and services can be added
/// </summary>
public interface IValidateBaseServices<T> : IBaseServices<T>
    where T : ValidateBase<T>
{
    IRuleManager<T> CreateRuleManager(T target);

    IValidatePropertyManager<IValidateProperty> ValidatePropertyManager { get; }
}
