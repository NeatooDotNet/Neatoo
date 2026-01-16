using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Neatoo.UnitTest.AsyncFlowTests;

internal class AsyncDelayRule : AsyncRuleBase<AsyncValidateObject>
{
    public AsyncDelayRule()
    {
        AddTriggerProperties(_ => _.AsyncDelayRuleValue);
    }
    public int RunCount { get; private set; } = 0;
    protected override async Task<IRuleMessages> Execute(AsyncValidateObject target, CancellationToken? token)
    {
        RunCount++;
        await Task.Delay(target.AsyncDelayRuleValue!.Value);
        return RuleMessages.None;
    }
}

internal class AsyncDelayUpdateChildRule : AsyncRuleBase<AsyncValidateObject>
{
    public AsyncDelayUpdateChildRule()
    {
        AddTriggerProperties(a => a.AsyncDelayUpdateChildRuleValue, a => a.Child.AsyncDelayUpdateChildRuleValue);
    }
    public int RunCount { get; private set; } = 0;

    protected override async Task<IRuleMessages> Execute(AsyncValidateObject target, CancellationToken? token)
    {
        RunCount++;
        await Task.Delay(50);
        if (RunCount < 3)
        {
            // Async Loop between parent and child
            if (target.Child != null)
            {
                target.Child.AsyncDelayUpdateChildRuleValue = Guid.NewGuid().ToString();
            }
        }
        return None;
    }
}

internal class SyncRuleA : RuleBase<AsyncValidateObject>
{
    public SyncRuleA()
    {
        AddTriggerProperties(_ => _.SyncA);
    }

    public int RunCount { get; private set; } = 0;

    protected override IRuleMessages Execute(AsyncValidateObject target)
    {
        target.NestedSyncB = Guid.NewGuid().ToString();
        RunCount++;
        return RuleMessages.None;
    }
}
internal class NestedSyncRuleB : RuleBase<AsyncValidateObject>
{
    public NestedSyncRuleB()
    {
        AddTriggerProperties(_ => _.NestedSyncB);
    }
    public int RunCount { get; private set; } = 0;

    protected override IRuleMessages Execute(AsyncValidateObject target)
    {
        RunCount++;
        return RuleMessages.None;
    }
}

internal class AsyncRuleCanWait : AsyncRuleBase<AsyncValidateObject>
{
    public AsyncRuleCanWait()
    {
        AddTriggerProperties(_ => _.AsyncRulesCanWait);
    }
    public int RunCount { get; private set; } = 0;
    protected override async Task<IRuleMessages> Execute(AsyncValidateObject target, CancellationToken? token)
    {
        RunCount++;
        await Task.Delay(50);

        // This is how to await for a property to be set
        await target.AsyncRuleCanWaitProperty.SetValue("Value");

        return RuleMessages.None;
    }
}

internal class AsyncRuleCanWaitNested : AsyncRuleBase<AsyncValidateObject>
{
    public AsyncRuleCanWaitNested()
    {
        AddTriggerProperties(_ => _.AsyncRulesCanWait);
    }
    public int RunCount { get; private set; } = 0;
    protected override async Task<IRuleMessages> Execute(AsyncValidateObject target, CancellationToken? token)
    {
        RunCount++;
        await Task.Delay(50);
        target.AsyncRulesCanWaitNested = "Ran";
        target.AsyncDelayUpdateChildRuleValue = Guid.NewGuid().ToString();
        return RuleMessages.None;
    }

}

[SuppressFactory]
internal partial class AsyncValidateObject : ValidateBase<AsyncValidateObject>
{
    public AsyncValidateObject(IValidateBaseServices<AsyncValidateObject> services) : base(services)
    {
        RuleManager.AddRule(AsyncDelayUpdateChildRule = new AsyncDelayUpdateChildRule());
        RuleManager.AddRule(SyncRuleA = new SyncRuleA());
        RuleManager.AddRule(NestedSyncRuleB = new NestedSyncRuleB());
        RuleManager.AddRule(new AsyncRuleCanWait());
        RuleManager.AddRule(new AsyncRuleCanWaitNested());
        RuleManager.AddRule(new AsyncDelayRule());
    }

    public AsyncDelayUpdateChildRule AsyncDelayUpdateChildRule { get; private set; }
    public SyncRuleA SyncRuleA { get; private set; }
    public NestedSyncRuleB NestedSyncRuleB { get; private set; }

    public partial string? HasNoRules { get; set; }

    public partial string? SyncA { get; set; }

    public partial string? NestedSyncB { get; set; }

    public partial string? AsyncDelayUpdateChildRuleValue { get; set; }

    public IValidateProperty AsyncRuleCanWaitProperty => this[nameof(AsyncRulesCanWait)];

    public partial string? AsyncRulesCanWait { get; set; }
    public partial string? AsyncRulesCanWaitNested { get; set; }

    public partial int? AsyncDelayRuleValue { get; set; }

    public partial AsyncValidateObject Child { get; set; }

    protected override async Task ChildNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        if(eventArgs.FullPropertyName == nameof(AsyncRulesCanWait))
        {
            await Task.Delay(20);
        }
        await base.ChildNeatooPropertyChanged(eventArgs);
    }
}
