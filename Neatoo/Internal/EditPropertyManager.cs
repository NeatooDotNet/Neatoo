using Neatoo.Rules;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Neatoo.Internal;



public class EditProperty<T> : ValidateProperty<T>, IEditProperty<T>
{

    public EditProperty(IPropertyInfo propertyInfo) : base(propertyInfo)
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
    public EditProperty(string name, T value, bool isSelfModified, bool isReadOnly, string displayName, IRuleMessage[] serializedRuleMessages) : base(name, value, serializedRuleMessages, isReadOnly)
    {
        IsSelfModified = isSelfModified;
        DisplayName = displayName; // TODO - Find a better way than serializing this
    }

    [JsonIgnore]
    public IEditMetaProperties? EditChild => Value as IEditMetaProperties;

    protected override void OnPropertyChanged(string propertyName)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(Value))
        {
            if (!IsPaused)
            {
                IsSelfModified = true && EditChild == null; // Never consider ourself modified if holding a Neatoo object
            }
        }
    }

    public bool IsModified => IsSelfModified || (EditChild?.IsModified ?? false);

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

public delegate IEditPropertyManager CreateEditPropertyManager(IPropertyInfoList propertyInfoList);

public class EditPropertyManager : ValidatePropertyManager<IEditProperty>, IEditPropertyManager
{


    public EditPropertyManager(IPropertyInfoList propertyInfoList, IFactory factory) : base(propertyInfoList, factory)
    {

    }

    protected new IProperty CreateProperty<PV>(IPropertyInfo propertyInfo)
    {
        var property = Factory.CreateEditProperty<PV>(propertyInfo);
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
public class PropertyInfoEditChildDataWrongTypeException : Exception
{
    public PropertyInfoEditChildDataWrongTypeException() { }
    public PropertyInfoEditChildDataWrongTypeException(string message) : base(message) { }
    public PropertyInfoEditChildDataWrongTypeException(string message, Exception inner) : base(message, inner) { }

}
