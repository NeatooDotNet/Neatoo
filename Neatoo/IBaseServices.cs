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
