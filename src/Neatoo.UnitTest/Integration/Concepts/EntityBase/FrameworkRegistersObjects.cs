using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Concepts.EntityBase.FrameworkRegisters;

// Fixtures for FrameworkRegistersTests: a root with a child in a property and a child in a list.
// Each object has one async rule that awaits a gate the test holds, standing in for a slow
// server reply, and throws when the value is "throw".

public interface IRegisteredChild : IEntityBase
{
    string? Value { get; set; }

    Func<Task>? Gate { get; set; }
}

public interface IRegisteredChildList : IEntityListBase<IRegisteredChild>
{
}

public interface IRegisteredRoot : IEntityRoot
{
    string? Value { get; set; }

    Func<Task>? Gate { get; set; }

    IRegisteredChild? Child { get; }

    IRegisteredChildList? Children { get; }

    int Inserts { get; }

    /// <summary>Assigns a child that was created on its own, as a verb on the root would.</summary>
    void Attach(IRegisteredChild child);
}

[Factory]
internal partial class RegisteredChild : EntityBase<RegisteredChild>, IRegisteredChild
{
    public RegisteredChild(IEntityBaseServices<RegisteredChild> services) : base(services)
    {
        RuleManager.AddValidationAsync(async t =>
        {
            await (t.Gate?.Invoke() ?? Task.CompletedTask);

            if (t.Value == "throw")
            {
                throw new InvalidOperationException("Child rule failed");
            }

            return string.Empty;
        }, t => t.Value);
    }

    public partial string? Value { get; set; }

    public Func<Task>? Gate { get; set; }

    [Create]
    public void Create() { }
}

internal class RegisteredChildList : EntityListBase<IRegisteredChild>, IRegisteredChildList
{
}

[Factory]
internal partial class RegisteredRoot : EntityBase<RegisteredRoot>, IRegisteredRoot
{
    public RegisteredRoot(IEntityBaseServices<RegisteredRoot> services) : base(services)
    {
        RuleManager.AddValidationAsync(async t =>
        {
            await (t.Gate?.Invoke() ?? Task.CompletedTask);

            if (t.Value == "throw")
            {
                throw new InvalidOperationException("Root rule failed");
            }

            return string.Empty;
        }, t => t.Value);
    }

    public partial string? Value { get; set; }
    public partial IRegisteredChild? Child { get; set; }
    public partial IRegisteredChildList? Children { get; set; }

    public Func<Task>? Gate { get; set; }

    public int Inserts { get; private set; }

    public void Attach(IRegisteredChild child)
    {
        this.Child = child;
    }

    [Create]
    public void Create([Service] IRegisteredChildFactory childFactory, [Service] IRegisteredChildList children)
    {
        this.Child = childFactory.Create();
        children.Add(childFactory.Create());
        this.Children = children;
    }

    [Insert]
    public void Insert()
    {
        this.Inserts++;
    }
}
