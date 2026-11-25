using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.ValidateBase;

public interface IPropertyTasksChild : IValidateBase
{
    string? X { get; set; }
}

public interface IPropertyTasksObject : IValidateBase
{
    /// <summary>The rule on this property awaits <see cref="Gate"/> and throws when the value is "throw".</summary>
    string? Value { get; set; }

    IPropertyTasksChild? Child { get; }

    /// <summary>What the rule on <see cref="Value"/> awaits. Stands in for a slow server reply.</summary>
    Func<Task>? Gate { get; set; }

    /// <summary>What the rule triggered on the child path <c>Child.X</c> awaits.</summary>
    Func<Task>? ChildPathGate { get; set; }
}

[Factory]
internal partial class PropertyTasksChild : ValidateBase<PropertyTasksChild>, IPropertyTasksChild
{
    public PropertyTasksChild(IValidateBaseServices<PropertyTasksChild> services) : base(services) { }

    public partial string? X { get; set; }

    [Create]
    public void Create() { }
}

[Factory]
internal partial class PropertyTasksObject : ValidateBase<PropertyTasksObject>, IPropertyTasksObject
{
    public PropertyTasksObject(IValidateBaseServices<PropertyTasksObject> services) : base(services)
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

        RuleManager.AddValidationAsync(async t =>
        {
            await (t.ChildPathGate?.Invoke() ?? Task.CompletedTask);
            return string.Empty;
        }, t => t.Child!.X);
    }

    public partial string? Value { get; set; }
    public partial IPropertyTasksChild? Child { get; set; }

    public Func<Task>? Gate { get; set; }
    public Func<Task>? ChildPathGate { get; set; }

    [Create]
    public void Create([Service] IPropertyTasksChildFactory childFactory)
    {
        this.Child = childFactory.Create();
    }
}

/// <summary>
/// A property the generated code did not register: the property manager creates it on first use.
/// </summary>
[Factory]
public class LazilyCreatedPropertyObject : ValidateBase<LazilyCreatedPropertyObject>
{
    public LazilyCreatedPropertyObject() : base(new ValidateBaseServices<LazilyCreatedPropertyObject>())
    {
        RuleManager.AddValidationAsync(async t =>
        {
            await (t.Gate?.Invoke() ?? Task.CompletedTask);
            return string.Empty;
        }, t => t.Value);
    }

    public string? Value { get => Getter<string>(); set => Setter(value); }

    public Func<Task>? Gate { get; set; }
}

public interface IFaultingChildParent : IValidateBase
{
    /// <summary>A child whose async rule throws when its value is "throw".</summary>
    IAsyncRuleFaultObject? Child { get; }
}

[Factory]
internal partial class FaultingChildParent : ValidateBase<FaultingChildParent>, IFaultingChildParent
{
    public FaultingChildParent(IValidateBaseServices<FaultingChildParent> services) : base(services) { }

    public partial IAsyncRuleFaultObject? Child { get; set; }

    [Create]
    public void Create([Service] IAsyncRuleFaultObjectFactory childFactory)
    {
        this.Child = childFactory.Create();
    }
}

/// <summary>
/// A property is busy exactly while a write to it or an async rule that names it is in flight,
/// and waiting on a busy property suspends. So WaitForTasks() on the entity returns control while
/// a rule is running, however the rule was started (#141).
/// </summary>
[TestClass]
public class PropertyTasksTests : IntegrationTestBase
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private IPropertyTasksObject target = null!;
    private readonly List<TaskCompletionSource> gates = new();

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
        target = GetRequiredService<IPropertyTasksObjectFactory>().Create();
    }

    [TestCleanup]
    public void ReleaseEveryGate()
    {
        // A test that failed while a rule was held must not leave it held.
        foreach (var gate in gates)
        {
            gate.TrySetResult();
        }
    }

    private TaskCompletionSource NewGate()
    {
        var gate = new TaskCompletionSource();
        gates.Add(gate);
        return gate;
    }

    private IValidateProperty ValueProperty => target[nameof(IPropertyTasksObject.Value)];

    /// <summary>
    /// Calls WaitForTasks() and requires the call itself to return while the rule is still held.
    /// A wait that loops instead of suspending never returns from the call.
    /// </summary>
    private static async Task<Task> WaitThatReturnsControl(IValidateBase waited)
    {
        var call = Task.Factory.StartNew(
            () => waited.WaitForTasks(), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);

        var finishedFirst = await Task.WhenAny(call, Task.Delay(Limit));
        Assert.AreSame(call, finishedFirst, "WaitForTasks() did not return control while the rule was held: it is looping, not awaiting.");

        return await call;
    }

    [TestMethod]
    public async Task Property_IsBusyWhileItsAsyncRuleRuns_AndNotAfter()
    {
        var gate = NewGate();
        target.Gate = () => gate.Task;

        target.Value = "a";

        Assert.IsTrue(ValueProperty.IsBusy);

        gate.SetResult();
        await target.WaitForTasks().WaitAsync(Limit);

        Assert.IsFalse(ValueProperty.IsBusy);
        Assert.IsFalse(target.IsBusy);
    }

    [TestMethod]
    public async Task Property_WithTwoOverlappingWrites_StaysBusyUntilBothHaveFinished()
    {
        // The async work here is the parent's rule on the child path, so nothing but the write
        // itself keeps the child's property busy.
        var first = NewGate();
        var second = NewGate();
        var gateQueue = new Queue<TaskCompletionSource>(new[] { first, second });
        target.ChildPathGate = () => gateQueue.Dequeue().Task;

        var x = target.Child![nameof(IPropertyTasksChild.X)];
        var firstWrite = x.SetValue("a");
        var secondWrite = x.SetValue("b");

        first.SetResult();
        await firstWrite.WaitAsync(Limit);

        Assert.IsTrue(x.IsBusy, "the second write is still in flight");

        second.SetResult();
        await secondWrite.WaitAsync(Limit);
        await x.WaitForTasks().WaitAsync(Limit);

        Assert.IsFalse(x.IsBusy);
    }

    [TestMethod]
    public async Task AfterAnAsyncRuleThrows_NeitherThePropertyNorTheEntityIsBusy()
    {
        var gate = NewGate();
        target.Gate = () => gate.Task;

        target.Value = "throw";
        var wait = target.WaitForTasks();
        gate.SetResult();
        await Assert.ThrowsExactlyAsync<AggregateException>(() => wait.WaitAsync(Limit));
        await ValueProperty.WaitForTasks().WaitAsync(Limit);

        Assert.IsFalse(ValueProperty.IsBusy);
        Assert.IsFalse(target.IsBusy);
    }

    [TestMethod]
    public async Task Property_WaitForTasks_IsIncompleteWhileItsRuleRuns_AndCompletesWhenItDoes()
    {
        var gate = NewGate();
        target.Gate = () => gate.Task;

        target.Value = "a";
        var wait = ValueProperty.WaitForTasks();

        Assert.IsFalse(wait.IsCompleted);

        gate.SetResult();
        await wait.WaitAsync(Limit);
    }

    [TestMethod]
    public async Task Property_RaisesIsBusyWhenItGoesBusyAndWhenItGoesIdle()
    {
        var gate = NewGate();
        target.Gate = () => gate.Task;
        var busyAtEachNotification = RecordBusyAtEachNotification();

        target.Value = "a";
        gate.SetResult();
        await target.WaitForTasks().WaitAsync(Limit);

        Assert.IsTrue(busyAtEachNotification.First(), "announced busy");
        Assert.IsFalse(busyAtEachNotification.Last(), "announced idle before the wait completed");
    }

    private List<bool> RecordBusyAtEachNotification()
    {
        var busyAtEachNotification = new List<bool>();
        ValueProperty.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IValidateProperty.IsBusy))
            {
                busyAtEachNotification.Add(ValueProperty.IsBusy);
            }
        };
        return busyAtEachNotification;
    }

    [TestMethod]
    public async Task WhenAnAwaitedRuleRunCompletes_ThePropertyIsAlreadyIdle_AndHasSaidSo()
    {
        var gate = NewGate();
        target.Gate = () => gate.Task;
        var busyAtEachNotification = RecordBusyAtEachNotification();

        var running = target.RunRules(nameof(IPropertyTasksObject.Value));
        gate.SetResult();
        await running.WaitAsync(Limit);

        // No wait on the property: the awaited run is the whole of the work.
        Assert.IsFalse(ValueProperty.IsBusy);
        CollectionAssert.AreEqual(new[] { true, false }, busyAtEachNotification);
    }

    [TestMethod]
    public async Task WhenAnAwaitedWriteCompletes_ThePropertyIsAlreadyIdle_AndHasSaidSo()
    {
        var gate = NewGate();
        target.Gate = () => gate.Task;
        var busyAtEachNotification = RecordBusyAtEachNotification();

        var writing = ValueProperty.SetValue("a");
        gate.SetResult();
        await writing.WaitAsync(Limit);

        Assert.IsFalse(ValueProperty.IsBusy);
        CollectionAssert.AreEqual(new[] { true, false }, busyAtEachNotification);
    }

    [TestMethod]
    public async Task WaitForTasks_ReturnsControlWhileARuleStartedByRunRulesIsInFlight_AndCompletesWhenItDoes()
    {
        var gate = NewGate();
        target.Gate = () => gate.Task;

        var running = target.RunRules(nameof(IPropertyTasksObject.Value));

        var wait = await WaitThatReturnsControl(target);
        Assert.IsFalse(wait.IsCompleted, "the rule is still held");

        gate.SetResult();
        await wait.WaitAsync(Limit);
        await running.WaitAsync(Limit);

        Assert.IsFalse(target.IsBusy);
    }

    [TestMethod]
    public async Task WaitForTasks_ReturnsControlWhileAWriteThroughThePropertyIsInFlight_AndCompletesWhenItDoes()
    {
        // The write a MudNeatoo input makes: through the property, not the generated setter.
        var gate = NewGate();
        target.Gate = () => gate.Task;

        var writing = ValueProperty.SetValue("a");

        var wait = await WaitThatReturnsControl(target);
        Assert.IsFalse(wait.IsCompleted, "the rule is still held");

        gate.SetResult();
        await wait.WaitAsync(Limit);
        await writing.WaitAsync(Limit);

        Assert.IsFalse(target.IsBusy);
    }

    [TestMethod]
    public async Task Property_OnADeserializedObject_IsBusyWhileItsAsyncRuleRuns()
    {
        var deserialized = Deserialize<IPropertyTasksObject>(Serialize(target));
        var gate = NewGate();
        deserialized.Gate = () => gate.Task;
        var value = deserialized[nameof(IPropertyTasksObject.Value)];

        deserialized.Value = "a";

        Assert.IsTrue(value.IsBusy);
        var wait = value.WaitForTasks();
        Assert.IsFalse(wait.IsCompleted);

        gate.SetResult();
        await wait.WaitAsync(Limit);
        await deserialized.WaitForTasks().WaitAsync(Limit);

        Assert.IsFalse(value.IsBusy);
        Assert.IsFalse(deserialized.IsBusy);
    }

    [TestMethod]
    public async Task Property_ThePropertyManagerCreatesOnFirstUse_IsBusyWhileItsAsyncRuleRuns()
    {
        var lazilyCreated = new LazilyCreatedPropertyObject();
        var gate = NewGate();
        lazilyCreated.Gate = () => gate.Task;

        lazilyCreated.Value = "a";
        var value = lazilyCreated[nameof(LazilyCreatedPropertyObject.Value)];

        Assert.IsTrue(value.IsBusy);
        var wait = value.WaitForTasks();
        Assert.IsFalse(wait.IsCompleted);

        gate.SetResult();
        await wait.WaitAsync(Limit);
        await lazilyCreated.WaitForTasks().WaitAsync(Limit);

        Assert.IsFalse(value.IsBusy);
        Assert.IsFalse(lazilyCreated.IsBusy);
    }

    [TestMethod]
    public Task BusyNotifications_AreRaisedOnTheThreadThatOwnsTheObject_WhenTheReplyArrivesOnAnother() => OneThreadAtATime.Run(async owningThread =>
    {
        var gate = NewGate();
        target.Gate = () => gate.Task;
        var propertyThreads = new List<int>();
        var objectThreads = new List<int>();
        ValueProperty.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IValidateProperty.IsBusy))
            {
                propertyThreads.Add(Environment.CurrentManagedThreadId);
            }
        };
        target.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IValidateBase.IsBusy))
            {
                objectThreads.Add(Environment.CurrentManagedThreadId);
            }
        };

        var writing = ValueProperty.SetValue("a");
        await Task.Run(() => gate.SetResult());
        await writing.WaitAsync(Limit);
        await target.WaitForTasks().WaitAsync(Limit);

        CollectionAssert.AreEqual(new[] { owningThread, owningThread }, propertyThreads, "the property: busy, then idle");
        CollectionAssert.AreEqual(new[] { owningThread, owningThread }, objectThreads, "the object: busy, then idle");
    });

    private void ThrowWhenThePropertyGoesIdle()
    {
        ValueProperty.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IValidateProperty.IsBusy) && !ValueProperty.IsBusy)
            {
                throw new InvalidOperationException("Listener failed");
            }
        };
    }

    [TestMethod]
    public async Task AListenerThatThrowsAsThePropertyGoesIdle_FaultsTheAwaitedRuleRun_NotThePropertysWait()
    {
        var gate = NewGate();
        target.Gate = () => gate.Task;
        ThrowWhenThePropertyGoesIdle();

        var running = target.RunRules(nameof(IPropertyTasksObject.Value));
        gate.SetResult();
        var thrown = await Assert.ThrowsAsync<Exception>(() => running.WaitAsync(Limit));

        StringAssert.Contains(thrown.ToString(), "Listener failed");
        await ValueProperty.WaitForTasks().WaitAsync(Limit);
        Assert.IsFalse(ValueProperty.IsBusy);
    }

    [TestMethod]
    public async Task AListenerThatThrowsAsThePropertyGoesIdle_FaultsTheAwaitedWrite_NotThePropertysWait()
    {
        var gate = NewGate();
        target.Gate = () => gate.Task;
        ThrowWhenThePropertyGoesIdle();

        var writing = ValueProperty.SetValue("a");
        gate.SetResult();
        var thrown = await Assert.ThrowsAsync<Exception>(() => writing.WaitAsync(Limit));

        StringAssert.Contains(thrown.ToString(), "Listener failed");
        await ValueProperty.WaitForTasks().WaitAsync(Limit);
        Assert.IsFalse(ValueProperty.IsBusy);
    }

    [TestMethod]
    public Task AParentsWait_ReportsItsChildsFaultOnce_AndLeavesTheIdleChildsOwnWaitToReportIt() => OneThreadAtATime.Run(async () =>
    {
        var parent = GetRequiredService<IFaultingChildParentFactory>().Create();
        var child = parent.Child!;
        var gate = NewGate();
        child.Gate = () => gate.Task;

        // Nobody is waiting while the rule runs and throws: the child's tracker and the parent's
        // each hold the fault for their first wait.
        child.Value = "throw";
        gate.SetResult();
        var deadline = DateTime.UtcNow + Limit;
        while ((parent.IsBusy || child.IsBusy) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(5);
        }

        await Assert.ThrowsExactlyAsync<AggregateException>(() => parent.WaitForTasks().WaitAsync(Limit));
        await parent.WaitForTasks().WaitAsync(Limit);

        // The parent's second wait found the child idle and left it alone.
        await Assert.ThrowsExactlyAsync<AggregateException>(() => child.WaitForTasks().WaitAsync(Limit));
    });
}
