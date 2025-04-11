using System.Reflection;

namespace Neatoo.Internal;

public class BaseServices<T> : IBaseServices<T>
    where T : Base<T> // Important - I need the concrete type at least once. This is where I get it
{
    public BaseServices()
    {
        PropertyInfoList = new PropertyInfoList<T>((PropertyInfo pi) => new PropertyInfoWrapper(pi));
        PropertyManager = new PropertyManager<IProperty>(PropertyInfoList, new DefaultFactory());
    }

    public BaseServices(CreatePropertyManager propertyManager, IPropertyInfoList<T> propertyInfoList)
    {
        PropertyInfoList = propertyInfoList;
        PropertyManager = propertyManager(PropertyInfoList);
    }


    public IPropertyManager<IProperty> PropertyManager { get; protected set; }
    public IPropertyInfoList<T> PropertyInfoList { get; }

}
