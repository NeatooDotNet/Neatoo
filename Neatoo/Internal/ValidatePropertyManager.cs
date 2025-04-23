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
        return Factory.CreateValidateProperty<PV>(propertyInfo);
    }


    [JsonIgnore]
    public bool IsSelfValid { get; protected set; } = true;
    [JsonIgnore]
    public bool IsValid { get; protected set; } = true;
    [JsonIgnore]
    public bool IsPaused { get; protected set; }

    public IReadOnlyCollection<IPropertyMessage> PropertyMessages => PropertyBag.SelectMany(_ => _.Value.PropertyMessages).ToList().AsReadOnly();

    public async Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null)
    {
        foreach (var p in PropertyBag)
        {
            await p.Value.RunRules(runRules, token);
        }
    }

    protected override void Property_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (IsPaused)
        {
            return;
        }

        if (sender is IValidateProperty property)
        {
            bool raiseIsValid = IsValid;

            if (e.PropertyName == nameof(IValidateProperty.IsValid)
                    || e.PropertyName == nameof(IValidateProperty.Value))
            {
                IsValid = !PropertyBag.Any(p => !p.Value.IsValid);
            }

            if (raiseIsValid != IsValid)
            {
                base.Property_PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsValid)));
            }

            bool raiseIsSelfValid = IsSelfValid;

            if (e.PropertyName == nameof(IValidateProperty.IsSelfValid)
                    || e.PropertyName == nameof(IValidateProperty.Value))
            {
                IsSelfValid = !PropertyBag.Any(p => !p.Value.IsSelfValid);
            }

            if (raiseIsSelfValid != IsSelfValid)
            {
                base.Property_PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsSelfValid)));
            }
        }

        base.Property_PropertyChanged(sender, e);
    }

    public override void OnDeserialized()
    {
        base.OnDeserialized();
        IsValid = !PropertyBag.Any(p => !p.Value.IsValid);
        IsSelfValid = !PropertyBag.Any(p => !p.Value.IsSelfValid);
    }

    public void ClearSelfMessages()
    {
        foreach (var p in PropertyBag)
        {
            p.Value.ClearSelfMessages();
        }
    }

    public void ClearAllMessages()
    {
        foreach (var p in PropertyBag)
        {
            p.Value.ClearAllMessages();
        }
    }

    public virtual void PauseAllActions()
    {
        if (!IsPaused)
        {
            IsPaused = true;
        }
    }

    public virtual void ResumeAllActions()
    {
        if (IsPaused)
        {
            IsPaused = false;
        }
    }
}


[Serializable]
public class PropertyValidateChildDataWrongTypeException : Exception
{
    public PropertyValidateChildDataWrongTypeException() { }
    public PropertyValidateChildDataWrongTypeException(string message) : base(message) { }
    public PropertyValidateChildDataWrongTypeException(string message, Exception inner) : base(message, inner) { }

}
