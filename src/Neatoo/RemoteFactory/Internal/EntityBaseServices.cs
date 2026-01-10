using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Neatoo.Internal;

public class EntityBaseServices<T> : ValidateBaseServices<T>, IEntityBaseServices<T>
    where T : EntityBase<T>
{
    public IFactorySave<T>? Factory { get; }

    public IEntityPropertyManager EntityPropertyManager { get; }

    public new IValidatePropertyManager<IValidateProperty> ValidatePropertyManager => EntityPropertyManager;

    /// <summary>
    /// Unit testing constructor only. Creates an instance with a null factory.
    /// </summary>
    /// <remarks>
    /// <para><strong>WARNING: For unit testing purposes only.</strong></para>
    /// <para>Save operations will fail because no factory is configured.</para>
    /// <para>Do not use this constructor in production code.</para>
    /// </remarks>
    public EntityBaseServices() : base()
    {
        this.PropertyInfoList = new PropertyInfoList<T>((System.Reflection.PropertyInfo pi) => new PropertyInfoWrapper(pi));
        this.EntityPropertyManager = new EntityPropertyManager(this.PropertyInfoList, new DefaultFactory());
        this.Factory = null;
    }

    public EntityBaseServices(IFactorySave<T>? factory) : base() {

        this.PropertyInfoList = new PropertyInfoList<T>((System.Reflection.PropertyInfo pi) => new PropertyInfoWrapper(pi));

        this.EntityPropertyManager = new EntityPropertyManager(this.PropertyInfoList, new DefaultFactory());
        this.Factory = factory;
    }
    public EntityBaseServices(CreateEntityPropertyManager propertyManager, IPropertyInfoList<T> propertyInfoList, RuleManagerFactory<T> ruleManager)
    {
        this.PropertyInfoList = propertyInfoList;
        this.ruleManagerFactory = ruleManager;
        this.EntityPropertyManager = propertyManager(propertyInfoList);
    }

    public EntityBaseServices(CreateEntityPropertyManager propertyManager, IPropertyInfoList<T> propertyInfoList, RuleManagerFactory<T> ruleManager, IFactorySave<T> factory)
    {
        this.PropertyInfoList = propertyInfoList;
        this.ruleManagerFactory = ruleManager;
        this.EntityPropertyManager = propertyManager(propertyInfoList);
        this.Factory = factory;
    }

}
