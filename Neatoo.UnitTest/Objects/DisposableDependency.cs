namespace Neatoo.UnitTest.Objects;


public class DisposableDependencyList : List<DisposableDependency> { }

public interface IDisposableDependency : IDisposable
{
    Guid UniqueId { get; }
    bool IsDisposed { get; set; }
}

public class DisposableDependency : IDisposableDependency
{
    public DisposableDependency(DisposableDependencyList list)
    {
        list.Add(this);
    }

    public Guid UniqueId { get; } = Guid.NewGuid();
    public bool IsDisposed { get; set; } = false;

    public void Dispose()
    {
        if (IsDisposed) throw new Exception("Already Disposed!");
        IsDisposed = true;
    }
}
