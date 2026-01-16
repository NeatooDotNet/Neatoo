namespace Neatoo.Internal;

/// <summary>
/// Implementation of <see cref="IPropertyFactory{TOwner}"/> that creates
/// <see cref="EntityProperty{T}"/> instances for EntityBase-derived classes.
/// </summary>
/// <typeparam name="TOwner">The type of the Neatoo entity that owns the properties.</typeparam>
/// <remarks>
/// EntityBase classes require EntityProperty instances (which implement IEntityProperty)
/// for modification tracking. This factory ensures the correct property type is created.
/// </remarks>
public class EntityPropertyFactory<TOwner> : IPropertyFactory<TOwner>
    where TOwner : IValidateBase
{
    private readonly IPropertyInfoList<TOwner> _propertyInfoList;
    private readonly IFactory _factory;

    public EntityPropertyFactory(IPropertyInfoList<TOwner> propertyInfoList, IFactory factory)
    {
        _propertyInfoList = propertyInfoList;
        _factory = factory;
    }

    /// <inheritdoc />
    public IValidateProperty<TProperty> Create<TProperty>(TOwner owner, string propertyName)
    {
        var propertyInfo = _propertyInfoList.GetPropertyInfo(propertyName)
            ?? throw new PropertyNotFoundException($"Property '{propertyName}' not found in '{typeof(TOwner).Name}'");

        return _factory.CreateEntityProperty<TProperty>(propertyInfo);
    }
}
