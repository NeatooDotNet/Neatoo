using Neatoo.Rules;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Neatoo.Internal;



public class EntityProperty<T> : ValidateProperty<T>, IEntityProperty<T>
{

    public EntityProperty(IPropertyInfo propertyInfo) : base(propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);

        var dnAttribute = propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
        if(dnAttribute != null)
        {
            DisplayName = dnAttribute.DisplayName;
        }
        else
        {
            DisplayName = propertyInfo.Name;
        }
    }

    [JsonConstructor]
    public EntityProperty(string name, T value, bool isSelfModified, bool isReadOnly, string displayName, IRuleMessage[] serializedRuleMessages) : base(name, value, serializedRuleMessages, isReadOnly)
    {
        IsSelfModified = isSelfModified;
        DisplayName = displayName; // TODO - Find a better way than serializing this
    }

    [JsonIgnore]
    public IEntityMetaProperties? EntityChild => Value as IEntityMetaProperties;

    protected override void OnPropertyChanged(string propertyName)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(Value))
        {
            if (!IsPaused)
            {
                IsSelfModified = true && EntityChild == null; // Never consider ourself modified if holding a Neatoo object
            }
        }
    }

    public bool IsModified => IsSelfModified || (EntityChild?.IsModified ?? false);

    public bool IsSelfModified { get; protected set; } = false;

    public bool IsPaused { get; set; } = false;

    public string DisplayName { get; init; }

    public void MarkSelfUnmodified()
    {
        IsSelfModified = false;
    }

    public override void LoadValue(object? value)
    {
        base.LoadValue(value);
        IsSelfModified = false;
    }
}

public delegate IEntityPropertyManager CreateEntityPropertyManager(IPropertyInfoList propertyInfoList);

public class EntityPropertyManager : ValidatePropertyManager<IEntityProperty>, IEntityPropertyManager
{


    public EntityPropertyManager(IPropertyInfoList propertyInfoList, IFactory factory) : base(propertyInfoList, factory)
    {

    }

    protected new IProperty CreateProperty<PV>(IPropertyInfo propertyInfo)
    {
        var property = Factory.CreateEntityProperty<PV>(propertyInfo);
        property.IsPaused = IsPaused;
        return property;
    }

    public bool IsModified => PropertyBag.Any(p => p.Value.IsModified);
    public bool IsSelfModified => PropertyBag.Any(p => p.Value.IsSelfModified);
    public bool IsPaused = false;

    public IEnumerable<string> ModifiedProperties => PropertyBag.Where(f => f.Value.IsModified).Select(f => f.Value.Name);

    public void MarkSelfUnmodified()
    {
        foreach (var fd in PropertyBag)
        {
            fd.Value.MarkSelfUnmodified();
        }
    }

    public void PauseAllActions()
    {
        IsPaused = true;
        foreach (var fd in PropertyBag)
        {
            fd.Value.IsPaused = true;
        }
    }

    public void ResumeAllActions()
    {
        IsPaused = false;
        foreach (var fd in PropertyBag)
        {
            fd.Value.IsPaused = false;
        }
    }   
}


[Serializable]
public class PropertyInfoEntityChildDataWrongTypeException : Exception
{
    public PropertyInfoEntityChildDataWrongTypeException() { }
    public PropertyInfoEntityChildDataWrongTypeException(string message) : base(message) { }
    public PropertyInfoEntityChildDataWrongTypeException(string message, Exception inner) : base(message, inner) { }

}
