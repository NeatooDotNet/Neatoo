using Neatoo.RemoteFactory;

namespace Neatoo;

/// <summary>
/// Wrap the NeatooBase services into an interface so that 
/// the inheriting classes don't need to list all services
/// and services can be added
/// REGISTERED IN DI CONTAINER
/// </summary>
public interface IEditBaseServices<T> : IValidateBaseServices<T>
    where T : EditBase<T>
{

    IEditPropertyManager EditPropertyManager { get; }
    IFactorySave<T>? Factory { get; }
}
