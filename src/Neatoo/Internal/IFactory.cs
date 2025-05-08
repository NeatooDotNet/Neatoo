namespace Neatoo.Internal;

public interface IFactory
{
    Property<P> CreateProperty<P>(IPropertyInfo propertyInfo);
    ValidateProperty<P> CreateValidateProperty<P>(IPropertyInfo propertyInfo);
    EntityProperty<P> CreateEntityProperty<P>(IPropertyInfo propertyInfo);
}