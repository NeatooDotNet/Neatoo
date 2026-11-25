namespace Neatoo.Internal;

public interface IFactory
{
    ValidateProperty<P> CreateValidateProperty<P>(IPropertyInfo propertyInfo);
    EntityProperty<P> CreateEntityProperty<P>(IPropertyInfo propertyInfo);

    /// <summary>
    /// Creates the task tracker a property holds its work in flight in. The property manager
    /// gives one to each property that enters its bag.
    /// </summary>
    IAsyncTasks CreateAsyncTasks();
}
