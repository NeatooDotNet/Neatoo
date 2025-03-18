using Neatoo.Core;
using System.Reflection;

namespace Neatoo;


/// <summary>
/// Wrap the NeatooBase services into an interface so that 
/// the inheriting classes don't need to list all services
/// and services can be added
/// </summary>
public interface IBaseServices<T>
{
    IPropertyManager<IProperty> PropertyManager { get; }
    IPropertyInfoList<T> PropertyInfoList { get; }
}


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
