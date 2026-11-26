using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MudBlazor;
using MudBlazor.Services;
using Neatoo.Blazor.MudNeatoo.Components;

namespace Neatoo.Blazor.MudNeatoo.Tests;

/// <summary>
/// The case a page reaches: a user changes a MudNeatoo input, an async rule on that property is
/// still running, and a handler awaits <c>WaitForTasks()</c> on the entity. The wait completes
/// when the rule does. Each test renders the input and raises the wrapped MudBlazor component's
/// own change callback, as a user's change does, without awaiting it.
/// </summary>
[TestClass]
public sealed class InputWriteIsWaitedForTests : IAsyncDisposable
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private BunitContext _context = null!;
    private AsyncRuleTarget _target = null!;
    private readonly List<TaskCompletionSource> _gates = new();

    [TestInitialize]
    public void TestInitialize()
    {
        _context = new BunitContext();
        _context.Services.AddMudServices();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.Render<MudPopoverProvider>();
        _target = new AsyncRuleTarget();
    }

    [TestCleanup]
    public void ReleaseEveryGate()
    {
        foreach (var gate in _gates)
        {
            gate.TrySetResult();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private TaskCompletionSource HoldTheRuleOf(AsyncRuleTarget target)
    {
        var gate = new TaskCompletionSource();
        _gates.Add(gate);
        target.Gate = () => gate.Task;
        return gate;
    }

    /// <summary>Renders a text field over the target's Name.</summary>
    private IRenderedComponent<MudNeatooTextField<string>> TextFieldOver(AsyncRuleTarget target, Func<string?, Task>? setter = null) =>
        _context.Render<MudNeatooTextField<string>>(p =>
        {
            p.Add(c => c.EntityProperty, target[nameof(AsyncRuleTarget.Name)]);

            if (setter != null)
            {
                p.Add(c => c.Setter, setter);
            }
        });

    /// <summary>Raises the wrapped text field's change, as a blur after typing does. Not awaited.</summary>
    private static Task UserTypes(IRenderedComponent<MudNeatooTextField<string>> field, string text)
    {
        var inner = field.FindComponent<MudTextField<string>>().Instance;
        return field.InvokeAsync(() => inner.ValueChanged.InvokeAsync(text));
    }

    private static bool IsDisabled(IRenderedComponent<MudNeatooTextField<string>> field) =>
        field.FindComponent<MudTextField<string>>().Instance.Disabled;

    [TestMethod]
    public async Task AWriteThroughAnInput_ThenWaitForTasksOnTheEntity_CompletesWhenTheRuleDoes()
    {
        var rule = HoldTheRuleOf(_target);
        var field = TextFieldOver(_target);

        var change = UserTypes(field, "Ada");

        Assert.IsTrue(_target.IsBusy, "the entity is busy while the rule runs");
        var wait = _target.WaitForTasks();
        Assert.IsFalse(wait.IsCompleted, "the entity's wait waits for the rule");

        rule.SetResult();
        await wait.WaitAsync(Limit);
        await change.WaitAsync(Limit);
        Assert.IsFalse(_target.IsBusy);
        Assert.AreEqual("Ada", _target.Name);
    }

    [TestMethod]
    public async Task AWriteThroughAnInputOnAChild_ThenWaitForTasksOnTheRoot_CompletesWhenTheChildsRuleDoes()
    {
        var child = new AsyncRuleTarget();
        _target.Child = child;
        var rule = HoldTheRuleOf(child);
        var field = TextFieldOver(child);

        var change = UserTypes(field, "Ada");

        Assert.IsTrue(_target.IsBusy, "the root is busy while its child's rule runs");
        var wait = _target.WaitForTasks();
        Assert.IsFalse(wait.IsCompleted, "the root's wait waits for the child's rule");

        rule.SetResult();
        await wait.WaitAsync(Limit);
        await change.WaitAsync(Limit);
        Assert.IsFalse(_target.IsBusy);
        Assert.IsFalse(child.IsBusy);
    }

    [TestMethod]
    public async Task AWriteThroughAnInputsSetter_ThenWaitForTasksOnTheEntity_CompletesWhenTheRuleDoes()
    {
        // The verb-bound write path: the input calls the Setter, which writes with Set{Name}.
        var rule = HoldTheRuleOf(_target);
        var field = TextFieldOver(_target, value => _target.SetName(value));

        var change = UserTypes(field, "Ada");

        Assert.IsTrue(_target.IsBusy);
        var wait = _target.WaitForTasks();
        Assert.IsFalse(wait.IsCompleted, "the entity's wait waits for the rule");

        rule.SetResult();
        await wait.WaitAsync(Limit);
        await change.WaitAsync(Limit);
        Assert.IsFalse(_target.IsBusy);
        Assert.AreEqual("Ada", _target.Name);
    }

    [TestMethod]
    public async Task TheInput_IsDisabledWhileItsRuleRuns_AndEnabledWhenTheEntitysWaitCompletes()
    {
        var rule = HoldTheRuleOf(_target);
        var field = TextFieldOver(_target);
        Assert.IsFalse(IsDisabled(field));

        var change = UserTypes(field, "Ada");
        field.WaitForAssertion(() => Assert.IsTrue(IsDisabled(field)), Limit);

        rule.SetResult();
        await _target.WaitForTasks().WaitAsync(Limit);
        await change.WaitAsync(Limit);
        field.WaitForAssertion(() => Assert.IsFalse(IsDisabled(field)), Limit);
    }

    [TestMethod]
    public async Task AWriteThroughAnInput_WhoseRuleThrows_IsReportedByTheEntitysWaitOnce_AndTheEntityRecovers()
    {
        var rule = HoldTheRuleOf(_target);
        var field = TextFieldOver(_target);

        var change = UserTypes(field, "throw");
        var wait = _target.WaitForTasks();
        rule.SetResult();

        var reported = await Assert.ThrowsExactlyAsync<AggregateException>(() => wait.WaitAsync(Limit));
        Assert.AreEqual("Rule failed", reported.InnerExceptions.Single().Message);
        var thrown = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => change.WaitAsync(Limit));
        Assert.AreSame(reported.InnerExceptions.Single(), thrown, "the change and the wait report the same exception");

        // Once: the next wait is clean, the entity is idle, and the next change is waited for.
        await _target.WaitForTasks().WaitAsync(Limit);
        Assert.IsFalse(_target.IsBusy);

        var second = HoldTheRuleOf(_target);
        field.WaitForAssertion(() => Assert.IsFalse(IsDisabled(field)), Limit);
        var next = UserTypes(field, "Ada");
        var nextWait = _target.WaitForTasks();
        Assert.IsFalse(nextWait.IsCompleted);
        second.SetResult();
        await nextWait.WaitAsync(Limit);
        await next.WaitAsync(Limit);
        Assert.AreEqual("Ada", _target.Name);
    }
}
