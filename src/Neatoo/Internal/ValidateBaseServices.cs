using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.Diagnostics.CodeAnalysis;

namespace Neatoo.Internal;

public class ValidateBaseServices<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] T> : IValidateBaseServices<T>
    where T : ValidateBase<T>
{
    public IPropertyInfoList<T> PropertyInfoList { get; protected set; }
    public IValidatePropertyManager<IValidateProperty> ValidatePropertyManager { get; }
    public IPropertyFactory<T> PropertyFactory { get; protected set; }
    public ILogger<T> Logger { get; protected set; }

    public RuleManagerFactory<T> ruleManagerFactory { get; protected set; }

    public ValidateBaseServices() : base()
    {
        PropertyInfoList = new PropertyInfoList<T>((System.Reflection.PropertyInfo pi) => new PropertyInfoWrapper(pi));
        var factory = new DefaultFactory();
        this.ValidatePropertyManager = new ValidatePropertyManager<IValidateProperty>(PropertyInfoList, factory);
        this.PropertyFactory = new DefaultPropertyFactory<T>(PropertyInfoList, factory);
        this.ruleManagerFactory = new RuleManagerFactory<T>(new AttributeToRule());
        this.Logger = new NullLogger<T>();
    }

    public ValidateBaseServices(IPropertyInfoList<T> propertyInfoList, RuleManagerFactory<T> createRuleManager)
    {
        PropertyInfoList = propertyInfoList;
        var factory = new DefaultFactory();
        this.PropertyFactory = new DefaultPropertyFactory<T>(PropertyInfoList, factory);
        this.ruleManagerFactory = createRuleManager;
        this.Logger = new NullLogger<T>();
    }

    public ValidateBaseServices(CreateValidatePropertyManager validatePropertyManager, IPropertyInfoList<T> propertyInfoList, RuleManagerFactory<T> createRuleManager)
    {
        PropertyInfoList = new PropertyInfoList<T>((System.Reflection.PropertyInfo pi) => new PropertyInfoWrapper(pi));
        var factory = new DefaultFactory();
        ValidatePropertyManager = validatePropertyManager(PropertyInfoList);
        this.PropertyFactory = new DefaultPropertyFactory<T>(PropertyInfoList, factory);
        this.ruleManagerFactory = createRuleManager;
        this.Logger = new NullLogger<T>();
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
        this.Logger = new NullLogger<T>();
    }

    public ValidateBaseServices(
        CreateValidatePropertyManager validatePropertyManager,
        IPropertyInfoList<T> propertyInfoList,
        IPropertyFactory<T> propertyFactory,
        RuleManagerFactory<T> createRuleManager,
        ILoggerFactory? loggerFactory)
    {
        PropertyInfoList = propertyInfoList;
        ValidatePropertyManager = validatePropertyManager(PropertyInfoList);
        this.PropertyFactory = propertyFactory;
        this.ruleManagerFactory = createRuleManager;
        this.Logger = loggerFactory?.CreateLogger<T>()
            ?? new NullLogger<T>();
    }

    public IRuleManager<T> CreateRuleManager(T target)
    {
        return ruleManagerFactory.CreateRuleManager(target, this.PropertyInfoList);
    }
}
