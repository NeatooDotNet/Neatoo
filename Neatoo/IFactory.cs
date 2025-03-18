using Neatoo.Core;

namespace Neatoo;

public interface IFactory
{
    Property<P> CreateProperty<P>(IPropertyInfo propertyInfo);
    ValidateProperty<P> CreateValidateProperty<P>(IPropertyInfo propertyInfo);
    EditProperty<P> CreateEditProperty<P>(IPropertyInfo propertyInfo);
}