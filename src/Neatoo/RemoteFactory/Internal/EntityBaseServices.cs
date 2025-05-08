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

        PropertyInfoList = new PropertyInfoList<T>((System.Reflection.PropertyInfo pi) => new PropertyInfoWrapper(pi));

        EntityPropertyManager = new EntityPropertyManager(PropertyInfoList, new DefaultFactory());
        Factory = factory;
    }
    public EntityBaseServices(CreateEntityPropertyManager propertyManager, IPropertyInfoList<T> propertyInfoList, RuleManagerFactory<T> ruleManager)
    {
        PropertyInfoList = propertyInfoList;
        this.ruleManagerFactory = ruleManager;
        EntityPropertyManager = propertyManager(propertyInfoList);
    }

    public EntityBaseServices(CreateEntityPropertyManager propertyManager, IPropertyInfoList<T> propertyInfoList, RuleManagerFactory<T> ruleManager, IFactorySave<T> factory)
    {
        PropertyInfoList = propertyInfoList;
        this.ruleManagerFactory = ruleManager;
        EntityPropertyManager = propertyManager(propertyInfoList);
        Factory = factory;
    }

}
