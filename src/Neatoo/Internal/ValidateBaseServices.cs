using Neatoo.Rules;
using Neatoo.Rules.Rules;

namespace Neatoo.Internal;

public class ValidateBaseServices<T> : IValidateBaseServices<T>
    where T : ValidateBase<T>
{
    public IPropertyInfoList<T> PropertyInfoList { get; protected set; }
    public IValidatePropertyManager<IValidateProperty> ValidatePropertyManager { get; }
    public IPropertyFactory<T> PropertyFactory { get; protected set; }

    public RuleManagerFactory<T> ruleManagerFactory { get; protected set; }

    public ValidateBaseServices() : base()
    {
        PropertyInfoList = new PropertyInfoList<T>((System.Reflection.PropertyInfo pi) => new PropertyInfoWrapper(pi));
        var factory = new DefaultFactory();
        this.ValidatePropertyManager = new ValidatePropertyManager<IValidateProperty>(PropertyInfoList, factory);
        this.PropertyFactory = new DefaultPropertyFactory<T>(PropertyInfoList, factory);
        this.ruleManagerFactory = new RuleManagerFactory<T>(new AttributeToRule());
    }

    public ValidateBaseServices(IPropertyInfoList<T> propertyInfoList, RuleManagerFactory<T> createRuleManager)
    {
        PropertyInfoList = propertyInfoList;
        var factory = new DefaultFactory();
        this.PropertyFactory = new DefaultPropertyFactory<T>(PropertyInfoList, factory);
        this.ruleManagerFactory = createRuleManager;
    }

    public ValidateBaseServices(CreateValidatePropertyManager validatePropertyManager, IPropertyInfoList<T> propertyInfoList, RuleManagerFactory<T> createRuleManager)
    {
        PropertyInfoList = new PropertyInfoList<T>((System.Reflection.PropertyInfo pi) => new PropertyInfoWrapper(pi));
        var factory = new DefaultFactory();
        ValidatePropertyManager = validatePropertyManager(PropertyInfoList);
        this.PropertyFactory = new DefaultPropertyFactory<T>(PropertyInfoList, factory);
        this.ruleManagerFactory = createRuleManager;
    }

    public ValidateBaseServices(
        CreateValidatePropertyManager validatePropertyManager,
        IPropertyInfoList<T> propertyInfoList,
        IPropertyFactory<T> propertyFactory,
        RuleManagerFactory<T> createRuleManager)
    {
        PropertyInfoList = propertyInfoList;
        ValidatePropertyManager = validatePropertyManager(PropertyInfoList);
        this.PropertyFactory = propertyFactory;
        this.ruleManagerFactory = createRuleManager;
    }

    public IRuleManager<T> CreateRuleManager(T target)
    {
        return ruleManagerFactory.CreateRuleManager(target, this.PropertyInfoList);
    }
}
