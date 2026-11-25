using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        var busyAtEachNotification = new List<bool>();
        ValueProperty.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IValidateProperty.IsBusy))
            {
                busyAtEachNotification.Add(ValueProperty.IsBusy);
            }
        };

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
}
