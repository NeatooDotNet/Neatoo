using Neatoo.Rules;

namespace Neatoo.Internal;

public delegate IValidatePropertyManager<IValidateProperty> CreateValidatePropertyManager(IPropertyInfoList propertyInfoList);

public class ValidatePropertyManager<P> : PropertyManager<P>, IValidatePropertyManager<P>
    where P : IValidateProperty
{

    public ValidatePropertyManager(IPropertyInfoList propertyInfoList, IFactory factory) : base(propertyInfoList, factory)
    {
    }


    protected new IProperty CreateProperty<PV>(IPropertyInfo propertyInfo)
    {
        return Factory.CreateValidateProperty<PV>(propertyInfo);
    }

    public bool IsSelfValid => !PropertyBag.Any(_ => !_.Value.IsSelfValid);
    public bool IsValid => !PropertyBag.Any(_ => !_.Value.IsValid);

    public IReadOnlyCollection<IPropertyMessage> PropertyMessages => PropertyBag.SelectMany(_ => _.Value.PropertyMessages).ToList().AsReadOnly();


    public async Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null)
    {
        foreach (var p in PropertyBag.Values)
        {
            await p.RunRules(runRules, token);
        }
    }

    public void ClearSelfMessages()
    {
        foreach (var p in PropertyBag)
        {
            p.Value.ClearSelfErrors();
        }
    }

    public void ClearAllMessages()
    {
        foreach (var p in PropertyBag)
        {
            p.Value.ClearAllErrors();
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
