using System.Reflection;

namespace Neatoo.Internal;

public delegate IPropertyInfo CreatePropertyInfoWrapper(PropertyInfo property);

public class PropertyInfoWrapper : IPropertyInfo
{
    public PropertyInfoWrapper(PropertyInfo propertyInfo)
    {
        this.PropertyInfo = propertyInfo;
        this.IsPrivateSetter = !propertyInfo.CanWrite || propertyInfo.SetMethod?.IsPrivate == true;
    }

    public PropertyInfo PropertyInfo { get; }
    public string Name => this.PropertyInfo.Name;
    public Type Type => this.PropertyInfo.PropertyType;
    public string Key => this.Name;
    public bool IsPrivateSetter { get; }

    private Dictionary<Type, Attribute?> customAttribute = new();

    public T? GetCustomAttribute<T>() where T : Attribute
    {
        if(!this.customAttribute.ContainsKey(typeof(T)))
        {
            this.customAttribute[typeof(T)] = this.PropertyInfo.GetCustomAttribute<T>();
        }

        return (T?) this.customAttribute[typeof(T)];
    }

    private List<Attribute>? customAttributes;

    public IEnumerable<Attribute> GetCustomAttributes(){
        if(this.customAttributes == null)
        {
            this.customAttributes = this.PropertyInfo.GetCustomAttributes().ToList();
        }
        return this.customAttributes;
    }
}
