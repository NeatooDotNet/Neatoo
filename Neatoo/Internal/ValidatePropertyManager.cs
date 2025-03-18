namespace Neatoo.Core;

public interface IValidatePropertyManager<out P> : IPropertyManager<P>
    where P : IProperty
{
    // Valid without looking at Children that are IValidateBase
    bool IsSelfValid { get; }
    bool IsValid { get; }
    Task RunAllRules(CancellationToken? token = null);

    IReadOnlyList<string> ErrorMessages { get; }
    void ClearAllErrors();
    void ClearSelfErrors();
}

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

    public IReadOnlyList<string> ErrorMessages => PropertyBag.SelectMany(_ => _.Value.ErrorMessages).ToList().AsReadOnly();


    public async Task RunAllRules(CancellationToken? token = null)
    {
        foreach (var p in PropertyBag.Values)
        {
            await p.RunAllRules(token);
        }
    }

    public void ClearSelfErrors()
    {
        foreach (var p in PropertyBag)
        {
            p.Value.ClearSelfErrors();
        }
    }

    public void ClearAllErrors()
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
