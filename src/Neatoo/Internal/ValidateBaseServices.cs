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

    /// <summary>
    /// Creates the task trackers. DI supplies the registered factory; the constructors that
    /// build their own defaults use the framework's tracker.
    /// </summary>
    protected CreateAsyncTasks asyncTasksFactory { get; set; } = static () => new AsyncTasks();

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
        this.ValidatePropertyManager = new ValidatePropertyManager<IValidateProperty>(PropertyInfoList, factory);
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

    /// <summary>
    /// The constructor DI uses: every other constructor's parameters are a subset of this one's,
    /// and the logger factory is optional, so it is chosen whether or not logging is registered.
    /// </summary>
    public ValidateBaseServices(
        CreateValidatePropertyManager validatePropertyManager,
        IPropertyInfoList<T> propertyInfoList,
        IPropertyFactory<T> propertyFactory,
        RuleManagerFactory<T> createRuleManager,
        CreateAsyncTasks createAsyncTasks,
        ILoggerFactory? loggerFactory = null)
        : this(validatePropertyManager, propertyInfoList, propertyFactory, createRuleManager, loggerFactory)
    {
        this.asyncTasksFactory = createAsyncTasks;
    }

    public IRuleManager<T> CreateRuleManager(T target)
    {
        return ruleManagerFactory.CreateRuleManager(target, this.PropertyInfoList);
    }

    public IAsyncTasks CreateAsyncTasks()
    {
        return this.asyncTasksFactory();
    }
}
