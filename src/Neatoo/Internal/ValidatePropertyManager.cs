using Neatoo.Rules;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Neatoo.Internal;

public delegate IValidatePropertyManager<IValidateProperty> CreateValidatePropertyManager(IPropertyInfoList propertyInfoList);

public class ValidatePropertyManager<P> : PropertyManager<P>, IValidatePropertyManager<P>
    where P : IValidateProperty
{
    public ValidatePropertyManager(IPropertyInfoList propertyInfoList, IFactory factory) : base(propertyInfoList, factory) { }


    protected new IProperty CreateProperty<PV>(IPropertyInfo propertyInfo)
    {
        return this.Factory.CreateValidateProperty<PV>(propertyInfo);
    }


    [JsonIgnore]
    public bool IsSelfValid { get; protected set; } = true;
    [JsonIgnore]
    public bool IsValid { get; protected set; } = true;
    [JsonIgnore]
    public bool IsPaused { get; protected set; }

    public IReadOnlyCollection<IPropertyMessage> PropertyMessages => this.PropertyBag.SelectMany(_ => _.Value.PropertyMessages).ToList().AsReadOnly();

    public async Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null)
    {
        foreach (var p in this.PropertyBag)
        {
            await p.Value.RunRules(runRules, token);
        }
    }

    protected override void Property_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (this.IsPaused)
        {
            return;
        }

        if (sender is IValidateProperty property)
        {
            var raiseIsValid = this.IsValid;

            if (e.PropertyName == nameof(IValidateProperty.IsValid)
                    || e.PropertyName == nameof(IValidateProperty.Value))
            {
                this.IsValid = !this.PropertyBag.Any(p => !p.Value.IsValid);
            }

            if (raiseIsValid != this.IsValid)
            {
                base.Property_PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsValid)));
            }

            var raiseIsSelfValid = this.IsSelfValid;

            if (e.PropertyName == nameof(IValidateProperty.IsSelfValid)
                    || e.PropertyName == nameof(IValidateProperty.Value))
            {
                this.IsSelfValid = !this.PropertyBag.Any(p => !p.Value.IsSelfValid);
            }

            if (raiseIsSelfValid != this.IsSelfValid)
            {
                base.Property_PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsSelfValid)));
            }
        }

        base.Property_PropertyChanged(sender, e);
    }

    public override void OnDeserialized()
    {
        base.OnDeserialized();
        this.IsValid = !this.PropertyBag.Any(p => !p.Value.IsValid);
        this.IsSelfValid = !this.PropertyBag.Any(p => !p.Value.IsSelfValid);
    }

    public void ClearSelfMessages()
    {
        foreach (var p in this.PropertyBag)
        {
            p.Value.ClearSelfMessages();
        }
    }

    public void ClearAllMessages()
    {
        foreach (var p in this.PropertyBag)
        {
            p.Value.ClearAllMessages();
        }
    }

    public virtual void PauseAllActions()
    {
        if (!this.IsPaused)
        {
            this.IsPaused = true;
        }
    }

    public virtual void ResumeAllActions()
    {
        if (this.IsPaused)
        {
            this.IsPaused = false;
        }
    }
}


/// <summary>
/// Exception thrown when a child object's data type is incompatible in a validation context.
/// </summary>
[Serializable]
public class PropertyValidateChildDataWrongTypeException : PropertyException
{
    public PropertyValidateChildDataWrongTypeException() { }
    public PropertyValidateChildDataWrongTypeException(string message) : base(message) { }
    public PropertyValidateChildDataWrongTypeException(string message, Exception inner) : base(message, inner) { }
}
