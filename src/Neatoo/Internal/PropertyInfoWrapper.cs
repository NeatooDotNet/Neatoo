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

    // Serializes access to customAttribute and customAttributes. Wrappers are shared across
    // all threads and DI scopes (PropertyInfoList<T>.PropertyInfos is static), so concurrent
    // cold-cache population was corrupting the non-thread-safe Dictionary. Instance lock —
    // different wrappers do not contend. Critical section is a dictionary lookup or reference
    // read once populated, so contention cost is negligible post-warm-up.
    private readonly object cacheLock = new object();

    private Dictionary<Type, Attribute?> customAttribute = new();

    // Virtual seams for test harnesses to count reflection invocations. Production path
    // delegates to System.Reflection; tests can override to track invocation counts.
    protected virtual Attribute? ReflectCustomAttribute(Type attrType)
        => this.PropertyInfo.GetCustomAttribute(attrType);

    protected virtual List<Attribute> ReflectAllCustomAttributes()
        => this.PropertyInfo.GetCustomAttributes().ToList();

    public T? GetCustomAttribute<T>() where T : Attribute
    {
        lock (this.cacheLock)
        {
            if(!this.customAttribute.ContainsKey(typeof(T)))
            {
                this.customAttribute[typeof(T)] = ReflectCustomAttribute(typeof(T));
            }

            return (T?) this.customAttribute[typeof(T)];
        }
    }

    private List<Attribute>? customAttributes;

    public IEnumerable<Attribute> GetCustomAttributes(){
        lock (this.cacheLock)
        {
            if(this.customAttributes == null)
            {
                this.customAttributes = ReflectAllCustomAttributes();
            }
            // Safe to return the reference from inside the lock: the list is assigned exactly
            // once on first population and never mutated afterward. Callers enumerate outside
            // the lock, but since the reference is stable and contents are immutable
            // post-assignment, no torn reads are possible. Any future change that reassigns
            // this field outside the lock (e.g., a "clear cache" API) reintroduces the race.
            return this.customAttributes;
        }
    }
}
