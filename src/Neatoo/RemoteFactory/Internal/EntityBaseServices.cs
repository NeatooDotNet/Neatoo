using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Neatoo.Internal;

public class EntityBaseServices<T> : ValidateBaseServices<T>, IEntityBaseServices<T>
    where T : EntityBase<T>
{
    public IFactorySave<T>? Factory { get; }

    public IEntityPropertyManager EntityPropertyManager { get; }

    public new IValidatePropertyManager<IValidateProperty> ValidatePropertyManager => EntityPropertyManager;

    public new IPropertyManager<IProperty> PropertyManager => EntityPropertyManager;

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
