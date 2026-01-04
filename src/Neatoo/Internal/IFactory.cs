namespace Neatoo.Internal;

public interface IFactory
{
    ValidateProperty<P> CreateValidateProperty<P>(IPropertyInfo propertyInfo);
    EntityProperty<P> CreateEntityProperty<P>(IPropertyInfo propertyInfo);
}