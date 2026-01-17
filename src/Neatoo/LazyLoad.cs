namespace Neatoo;

public class LazyLoad<T> where T : class
{
    private readonly Func<Task<T?>> _loader;

#pragma warning disable CS0649 // Field is never assigned to - will be assigned by LoadAsync in future implementation
    private T? _value;
#pragma warning restore CS0649

    public LazyLoad(Func<Task<T?>> loader)
    {
        _loader = loader;
    }

    public T? Value => _value;
}
