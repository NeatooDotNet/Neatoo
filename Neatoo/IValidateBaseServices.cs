using Neatoo.Core;
using Neatoo.Rules;
using Neatoo.Rules.Rules;

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

public class ValidateBaseServices<T> : IValidateBaseServices<T>
    where T : ValidateBase<T>
{
    public IPropertyInfoList<T> PropertyInfoList { get; protected set; }
    public IValidatePropertyManager<IValidateProperty> ValidatePropertyManager { get; }

    public RuleManagerFactory<T> ruleManagerFactory { get; protected set; }

    public IPropertyManager<IProperty> PropertyManager => ValidatePropertyManager;

    public ValidateBaseServices() : base()
    {
        PropertyInfoList = new PropertyInfoList<T>((System.Reflection.PropertyInfo pi) => new PropertyInfoWrapper(pi));
        this.ValidatePropertyManager = new ValidatePropertyManager<IValidateProperty>(PropertyInfoList, new DefaultFactory());
        this.ruleManagerFactory = new RuleManagerFactory<T>(new AttributeToRule());
    }

    public ValidateBaseServices(IPropertyInfoList<T> propertyInfoList, RuleManagerFactory<T> createRuleManager)
    {
        this.ruleManagerFactory = createRuleManager;
    }

    public ValidateBaseServices(CreateValidatePropertyManager  validatePropertyManager, IPropertyInfoList<T> propertyInfoList, RuleManagerFactory<T> createRuleManager)
    {
        PropertyInfoList = new PropertyInfoList<T>((System.Reflection.PropertyInfo pi) => new PropertyInfoWrapper(pi));
        ValidatePropertyManager = validatePropertyManager(PropertyInfoList);
        this.ruleManagerFactory = createRuleManager;
    }



    public IRuleManager<T> CreateRuleManager(T target)
    {
        return ruleManagerFactory.CreateRuleManager(target, this.PropertyInfoList);
    }

}
