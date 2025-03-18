namespace Neatoo.Core;


/// <summary>
/// You can't register generic delegates in C#
/// so you make a factory class
/// </summary>
public class DefaultFactory : IFactory
{
    public DefaultFactory() { }

    public Property<P> CreateProperty<P>(IPropertyInfo propertyInfo)
    {
        return new Property<P>(propertyInfo);
    }
    public ValidateProperty<P> CreateValidateProperty<P>(IPropertyInfo propertyInfo)
    {
        return new ValidateProperty<P>(propertyInfo);
    }
    public EditProperty<P> CreateEditProperty<P>(IPropertyInfo propertyInfo)
    {
        return new EditProperty<P>(propertyInfo);
    }
}

[Serializable]
public class GlobalFactoryException : Exception
{
    public GlobalFactoryException() { }
    public GlobalFactoryException(string message) : base(message) { }
    public GlobalFactoryException(string message, Exception inner) : base(message, inner) { }
}