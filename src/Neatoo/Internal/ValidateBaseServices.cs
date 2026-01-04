using Neatoo.Rules;
using Neatoo.Rules.Rules;

namespace Neatoo.Internal;

public class ValidateBaseServices<T> : IValidateBaseServices<T>
    where T : ValidateBase<T>
{
    public IPropertyInfoList<T> PropertyInfoList { get; protected set; }
    public IValidatePropertyManager<IValidateProperty> ValidatePropertyManager { get; }

    public RuleManagerFactory<T> ruleManagerFactory { get; protected set; }

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

    public ValidateBaseServices(CreateValidatePropertyManager validatePropertyManager, IPropertyInfoList<T> propertyInfoList, RuleManagerFactory<T> createRuleManager)
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
