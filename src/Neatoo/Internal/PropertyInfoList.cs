using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Neatoo.Internal;



public class PropertyInfoList<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] T> : IPropertyInfoList<T>
{

    protected CreatePropertyInfoWrapper CreatePropertyInfo { get; }
    protected static IDictionary<string, IPropertyInfo> PropertyInfos { get; } = new Dictionary<string, IPropertyInfo>();
    private static bool isRegistered = false;

    protected static object lockRegisteredProperties = new object();

    public PropertyInfoList(CreatePropertyInfoWrapper createPropertyInfoWrapper)
    {

        this.CreatePropertyInfo = createPropertyInfoWrapper;

        this.RegisterProperties();
    }


    private static Type[] neatooTypes = new Type[] { typeof(ValidateBase<>), typeof(ValidateListBase<>), typeof(EntityBase<>), typeof(EntityListBase<>) };

    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "Walks up the inheritance chain from typeof(T) which has [DynamicallyAccessedMembers] " +
        "preserving all properties. BaseType returns types in the same hierarchy whose properties are " +
        "already preserved by the annotation on T. The walk stops at Neatoo base types (ValidateBase<>, EntityBase<>) " +
        "which are framework types that are always preserved.")]
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
                    var prop = this.CreatePropertyInfo(p);
                    if (!PropertyInfos.ContainsKey(p.Name))
                    {
                        PropertyInfos.Add(p.Name, prop);
                    }
                }

                type = type.BaseType;

            } while (type != null && (!type.IsGenericType || !neatooTypes.Contains(type.GetGenericTypeDefinition())));

            do
            {
                var objProp = type.GetProperty(nameof(IValidateBaseInternal.ObjectInvalid), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | BindingFlags.DeclaredOnly);

                if (objProp != null)
                {
                    PropertyInfos.Add(nameof(IValidateBaseInternal.ObjectInvalid), this.CreatePropertyInfo(objProp));
                    break;
                }

                type = type.BaseType;
            }
            while (type != null && (!type.IsGenericType || neatooTypes.Contains(type.GetGenericTypeDefinition())));
        }
    }

    public IPropertyInfo? GetPropertyInfo(string propertyName)
    {
        this.RegisterProperties();

        if (!PropertyInfos.TryGetValue(propertyName, out var prop))
        {
            return null;
        }

        return prop;
    }

    public IEnumerable<IPropertyInfo> Properties()
    {
        this.RegisterProperties();
        return PropertyInfos.Select(p => p.Value);
    }

    public bool HasProperty(string propertyName)
    {
        this.RegisterProperties();
        return PropertyInfos.ContainsKey(propertyName);
    }
}
