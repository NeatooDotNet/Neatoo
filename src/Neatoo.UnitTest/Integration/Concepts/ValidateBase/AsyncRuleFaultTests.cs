using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.ValidateBase;

public interface IAsyncRuleFaultObject : IValidateBase
{
    /// <summary>The rule on this property throws when the value is "throw".</summary>
    string? Value { get; set; }

    /// <summary>What the rule awaits before it answers. Stands in for a slow server reply.</summary>
    Func<Task>? Gate { get; set; }
}

[Factory]
internal partial class AsyncRuleFaultObject : ValidateBase<AsyncRuleFaultObject>, IAsyncRuleFaultObject
{
    public AsyncRuleFaultObject(IValidateBaseServices<AsyncRuleFaultObject> services) : base(services)
    {
        RuleManager.AddValidationAsync(async t =>
        {
            await (t.Gate?.Invoke() ?? Task.CompletedTask);

            if (t.Value == "throw")
            {
                throw new InvalidOperationException("Rule failed");
            }

            return string.Empty;
        }, t => t.Value);
    }

    public partial string? Value { get; set; }

    public Func<Task>? Gate { get; set; }

    /// <summary>The tracker the services supplied, for the DI test.</summary>
    internal IAsyncTasks Tracker => this.RunningTasks;

    [Create]
    public void Create() { }
}

/// <summary>
/// An async rule that throws surfaces from WaitForTasks() once, and the object then tracks and
/// waits for later work. The trigger property keeps the message until the rule runs clean.
/// </summary>
[TestClass]
public class AsyncRuleFaultTests : IntegrationTestBase
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private IAsyncRuleFaultObject target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
        target = GetRequiredService<IAsyncRuleFaultObjectFactory>().Create();
    }

    /// <summary>Writes "throw", waits, and returns what the wait threw.</summary>
    private async Task<AggregateException> FaultOnce()
    {
        var gate = new TaskCompletionSource();
        target.Gate = () => gate.Task;

        target.Value = "throw";
        var wait = target.WaitForTasks();
        gate.SetResult();

        return await Assert.ThrowsExactlyAsync<AggregateException>(() => wait.WaitAsync(Limit));
    }

    [TestMethod]
    public async Task AsyncRuleThatThrows_SurfacesFromWaitForTasks()
    {
        var thrown = await FaultOnce();

        var inner = thrown.InnerExceptions.Single();
        Assert.IsInstanceOfType<InvalidOperationException>(inner);
        Assert.AreEqual("Rule failed", inner.Message);
    }

    [TestMethod]
    public async Task AsyncRuleThatThrows_LeavesTheTriggerPropertyInvalidWithTheMessage()
    {
        await FaultOnce();

        Assert.IsFalse(target.IsValid);
        Assert.IsFalse(target[nameof(IAsyncRuleFaultObject.Value)].IsValid);
        Assert.AreEqual("Rule failed", target.PropertyMessages.Single().Message);
    }

    [TestMethod]
    public async Task AsyncRuleThatThrows_IsNotThrownByASecondWait()
    {
        await FaultOnce();

        await target.WaitForTasks().WaitAsync(Limit);
    }

    [TestMethod]
    public async Task AfterAFault_ALaterWriteIsTrackedAndWaitedFor()
    {
        await FaultOnce();

        var gate = new TaskCompletionSource();
        target.Gate = () => gate.Task;

        target.Value = "fine";
        var wait = target.WaitForTasks();

        Assert.IsFalse(wait.IsCompleted, "the wait is for the rule that is still running");

        gate.SetResult();
        await wait.WaitAsync(Limit);
    }

    [TestMethod]
    public async Task AfterAFault_TheObjectIsValidOnceTheRuleRunsClean()
    {
        await FaultOnce();

        target.Gate = null;
        await target[nameof(IAsyncRuleFaultObject.Value)].SetValue("fine");

        Assert.IsTrue(target.IsValid, string.Join("; ", target.PropertyMessages.Select(m => m.Message)));
    }

    [TestMethod]
    public void ValidateBase_TakesItsTrackerFromTheRegisteredFactory()
    {
        var supplied = new List<IAsyncTasks>();
        var services = new ServiceCollection();
        services.AddNeatooServices(NeatooFactory.Logical, typeof(AsyncRuleFaultObject).Assembly);
        services.AddTransient<CreateAsyncTasks>(_ => () =>
        {
            var tracker = new AsyncTasks();
            supplied.Add(tracker);
            return tracker;
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var created = (AsyncRuleFaultObject)scope.ServiceProvider.GetRequiredService<IAsyncRuleFaultObjectFactory>().Create();

        // The same factory supplies each property's tracker, so it is called more than once.
        CollectionAssert.Contains(supplied, created.Tracker);
        Assert.IsTrue(supplied.Count >= 2, "the entity's tracker and one for each of its properties");
    }
}
