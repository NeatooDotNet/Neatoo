using System.Reflection;

namespace Neatoo.Core;

public interface IPropertyInfo
{
    PropertyInfo PropertyInfo { get; }
    string Name { get; }
    Type Type { get; }
    string Key { get; }
    public bool IsPrivateSetter { get; }
}

public delegate IPropertyInfo CreatePropertyInfoWrapper(PropertyInfo property);

public class PropertyInfoWrapper : IPropertyInfo
{
    public PropertyInfoWrapper(PropertyInfo propertyInfo)
    {
        this.PropertyInfo = propertyInfo;
        IsPrivateSetter = !propertyInfo.CanWrite || propertyInfo.SetMethod?.IsPrivate == true;
    }

    public PropertyInfo PropertyInfo { get; }
    public string Name => PropertyInfo.Name;
    public Type Type => PropertyInfo.PropertyType;
    public string Key => Name;
    public bool IsPrivateSetter { get; }
}
