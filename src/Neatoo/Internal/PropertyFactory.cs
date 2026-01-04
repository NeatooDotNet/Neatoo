namespace Neatoo.Internal;


/// <summary>
/// You can't register generic delegates in C#
/// so you make a factory class
/// </summary>
public class DefaultFactory : IFactory
{
    public DefaultFactory() { }

    public ValidateProperty<P> CreateValidateProperty<P>(IPropertyInfo propertyInfo)
    {
        return new ValidateProperty<P>(propertyInfo);
    }
    public EntityProperty<P> CreateEntityProperty<P>(IPropertyInfo propertyInfo)
    {
        return new EntityProperty<P>(propertyInfo);
    }
}

/// <summary>
/// Exception thrown when there is an error with the global factory configuration.
/// </summary>
[Serializable]
public class GlobalFactoryException : ConfigurationException
{
    public GlobalFactoryException() { }
    public GlobalFactoryException(string message) : base(message) { }
    public GlobalFactoryException(string message, Exception inner) : base(message, inner) { }
}