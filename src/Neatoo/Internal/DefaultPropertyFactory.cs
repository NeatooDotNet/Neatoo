namespace Neatoo.Internal;

/// <summary>
/// Default implementation of <see cref="IPropertyFactory{TOwner}"/> that creates
/// standard <see cref="ValidateProperty{T}"/> instances.
/// </summary>
/// <typeparam name="TOwner">The type of the Neatoo object that owns the properties.</typeparam>
public class DefaultPropertyFactory<TOwner> : IPropertyFactory<TOwner>
    where TOwner : IValidateBase
{
    private readonly IPropertyInfoList<TOwner> _propertyInfoList;
    private readonly IFactory _factory;

    public DefaultPropertyFactory(IPropertyInfoList<TOwner> propertyInfoList, IFactory factory)
    {
        _propertyInfoList = propertyInfoList;
        _factory = factory;
    }

    /// <inheritdoc />
    public IValidateProperty<TProperty> Create<TProperty>(TOwner owner, string propertyName)
    {
        var propertyInfo = _propertyInfoList.GetPropertyInfo(propertyName)
            ?? throw new PropertyNotFoundException($"Property '{propertyName}' not found in '{typeof(TOwner).Name}'");

        return _factory.CreateValidateProperty<TProperty>(propertyInfo);
    }
}
