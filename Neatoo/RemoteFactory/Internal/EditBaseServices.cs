using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Neatoo.Internal;

public class EditBaseServices<T> : ValidateBaseServices<T>, IEditBaseServices<T>
    where T : EditBase<T>
{
    public IFactorySave<T>? Factory { get; }

    public IEditPropertyManager EditPropertyManager { get; }

    public new IValidatePropertyManager<IValidateProperty> ValidatePropertyManager => EditPropertyManager;

    public new IPropertyManager<IProperty> PropertyManager => EditPropertyManager;

    public EditBaseServices(IFactorySave<T>? factory) : base() {

        PropertyInfoList = new PropertyInfoList<T>((System.Reflection.PropertyInfo pi) => new PropertyInfoWrapper(pi));

        EditPropertyManager = new EditPropertyManager(PropertyInfoList, new DefaultFactory());
        Factory = factory;
    }
    public EditBaseServices(CreateEditPropertyManager propertyManager, IPropertyInfoList<T> propertyInfoList, RuleManagerFactory<T> ruleManager)
    {
        PropertyInfoList = propertyInfoList;
        this.ruleManagerFactory = ruleManager;
        EditPropertyManager = propertyManager(propertyInfoList);
    }

    public EditBaseServices(CreateEditPropertyManager propertyManager, IPropertyInfoList<T> propertyInfoList, RuleManagerFactory<T> ruleManager, IFactorySave<T> factory)
    {
        PropertyInfoList = propertyInfoList;
        this.ruleManagerFactory = ruleManager;
        EditPropertyManager = propertyManager(propertyInfoList);
        Factory = factory;
    }

}
