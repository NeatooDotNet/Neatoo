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
            this.DisplayName = dnAttribute.DisplayName;
        }
        else
        {
            this.DisplayName = propertyInfo.Name;
        }
    }

    [JsonConstructor]
    public EntityProperty(string name, T value, bool isSelfModified, bool isReadOnly, IRuleMessage[] serializedRuleMessages) : base(name, value, serializedRuleMessages, isReadOnly)
    {
        this.IsSelfModified = isSelfModified;
        // DisplayName is restored from reflection metadata in OnDeserialized via ApplyPropertyInfo
        this.DisplayName = name;
    }

    [JsonIgnore]
    public IEntityMetaProperties? EntityChild => this.Value as IEntityMetaProperties;

    protected override void OnPropertyChanged(string propertyName)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(Value))
        {
            if (!this.IsPaused)
            {
                this.IsSelfModified = true && this.EntityChild == null; // Never consider ourself modified if holding a Neatoo object
            }
        }
    }

    public bool IsModified => this.IsSelfModified || (this.EntityChild?.IsModified ?? false);

    public bool IsSelfModified { get; protected set; } = false;

    public bool IsPaused { get; set; } = false;

    [JsonIgnore]
    public string DisplayName { get; private set; }

    public void ApplyPropertyInfo(IPropertyInfo propertyInfo)
    {
        var dnAttribute = propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
        DisplayName = dnAttribute?.DisplayName ?? propertyInfo.Name;
    }

    public void MarkSelfUnmodified()
    {
        this.IsSelfModified = false;
    }

    public override void LoadValue(object? value)
    {
        base.LoadValue(value);
        this.IsSelfModified = false;
    }
}

public delegate IEntityPropertyManager CreateEntityPropertyManager(IPropertyInfoList propertyInfoList);

public class EntityPropertyManager : ValidatePropertyManager<IEntityProperty>, IEntityPropertyManager
{
    public EntityPropertyManager(IPropertyInfoList propertyInfoList, IFactory factory) : base(propertyInfoList, factory)
    {
    }

    protected new IValidateProperty CreateProperty<PV>(IPropertyInfo propertyInfo)
    {
        var property = this.Factory.CreateEntityProperty<PV>(propertyInfo);
        property.IsPaused = this.IsPaused;
        return property;
    }

    public bool IsModified => this.PropertyBag.Any(p => p.Value.IsModified);
    public bool IsSelfModified => this.PropertyBag.Any(p => p.Value.IsSelfModified);
    public bool IsPaused { get; private set; } = false;

    public IEnumerable<string> ModifiedProperties => this.PropertyBag.Where(f => f.Value.IsModified).Select(f => f.Value.Name);

    public void MarkSelfUnmodified()
    {
        foreach (var fd in this.PropertyBag)
        {
            fd.Value.MarkSelfUnmodified();
        }
    }

    public void PauseAllActions()
    {
        this.IsPaused = true;
        foreach (var fd in this.PropertyBag)
        {
            fd.Value.IsPaused = true;
        }
    }

    public void ResumeAllActions()
    {
        this.IsPaused = false;
        foreach (var fd in this.PropertyBag)
        {
            fd.Value.IsPaused = false;
        }
    }

    public override void OnDeserialized()
    {
        base.OnDeserialized();

        // Restore DisplayName from reflection metadata for each property
        foreach (var kvp in PropertyBag)
        {
            var propertyInfo = PropertyInfoList.GetPropertyInfo(kvp.Key);
            if (propertyInfo != null)
            {
                kvp.Value.ApplyPropertyInfo(propertyInfo);
            }
        }
    }
}

/// <summary>
/// Exception thrown when a child object's data type is incompatible in an entity context.
/// </summary>
[Serializable]
public class PropertyInfoEntityChildDataWrongTypeException : PropertyException
{
    public PropertyInfoEntityChildDataWrongTypeException() { }
    public PropertyInfoEntityChildDataWrongTypeException(string message) : base(message) { }
    public PropertyInfoEntityChildDataWrongTypeException(string message, Exception inner) : base(message, inner) { }
}
