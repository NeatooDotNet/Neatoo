using System.Reflection;

namespace Neatoo;

public interface IPropertyInfo
{
    PropertyInfo PropertyInfo { get; }
    string Name { get; }
    Type Type { get; }
    string Key { get; }
    public bool IsPrivateSetter { get; }
}

/// <summary>
/// DO NOT REGISTER IN DI CONTAINER
/// </summary>
/// <typeparam name="T">Generic to ensure that types can only access their properties</typeparam>
public interface IPropertyInfoList
{
    IPropertyInfo GetPropertyInfo(string name);
    IEnumerable<IPropertyInfo> Properties();
    bool HasProperty(string propertyName);
}

/// <summary>
/// REGISTERED IN THE DI CONTAINER
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IPropertyInfoList<T> : IPropertyInfoList { }