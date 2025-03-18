using System.Reflection;

namespace Neatoo.Core;

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


public class PropertyInfoList<T> : IPropertyInfoList<T>
{

    protected CreatePropertyInfoWrapper CreatePropertyInfo { get; }
    protected static IDictionary<string, IPropertyInfo> PropertyInfos { get; } = new Dictionary<string, IPropertyInfo>();
    private static bool isRegistered = false;

    protected static object lockRegisteredProperties = new object();

    public PropertyInfoList(CreatePropertyInfoWrapper createPropertyInfoWrapper)
    {

        CreatePropertyInfo = createPropertyInfoWrapper;

        RegisterProperties();
    }


    private static Type[] neatooTypes = new Type[] { typeof(Base<>), typeof(ListBase<>), typeof(ValidateBase<>), typeof(ValidateListBase<>), typeof(EditBase<>), typeof(EditListBase<>) };

    protected void RegisterProperties()
    {
        lock (lockRegisteredProperties)
        {
            if (isRegistered)
            {
                return;
            }

            isRegistered = true;


            var type = typeof(T);

            // If a type does a 'new' on the property you will have duplicate PropertyNames
            // So honor to top-level type that has that propertyName

            // Problem -- this will include All of the properties even ones we don't declare
            do
            {
                var properties = type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | BindingFlags.DeclaredOnly).ToList();

                foreach (var p in properties)
                {
                    var prop = CreatePropertyInfo(p);
                    if (!PropertyInfos.ContainsKey(p.Name))
                    {
                        PropertyInfos.Add(p.Name, prop);
                    }
                }

                type = type.BaseType;

            } while (type != null && (!type.IsGenericType || !neatooTypes.Contains(type.GetGenericTypeDefinition())));

            do
            {
                var objProp = type.GetProperty(nameof(IValidateBase.ObjectInvalid), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | BindingFlags.DeclaredOnly);

                if (objProp != null)
                {
                    PropertyInfos.Add(nameof(IValidateBase.ObjectInvalid), CreatePropertyInfo(objProp));
                    break;
                }

                type = type.BaseType;
            }
            while (type != null && (!type.IsGenericType || neatooTypes.Contains(type.GetGenericTypeDefinition())));
        }
    }

    public IPropertyInfo GetPropertyInfo(string propertyName)
    {
        RegisterProperties();

        if (!PropertyInfos.TryGetValue(propertyName, out var prop))
        {
            throw new Exception($"{propertyName} missing on {typeof(T).FullName}");
        }

        return prop;
    }

    public IEnumerable<IPropertyInfo> Properties()
    {
        RegisterProperties();
        return PropertyInfos.Select(p => p.Value);
    }

    public bool HasProperty(string propertyName)
    {
        RegisterProperties();
        return PropertyInfos.ContainsKey(propertyName);
    }
}
