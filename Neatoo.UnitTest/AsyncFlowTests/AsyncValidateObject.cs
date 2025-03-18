using Neatoo.Core;
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
    public override async Task<PropertyErrors> Execute(AsyncValidateObject target, CancellationToken? token)
    {
        RunCount++;
        await Task.Delay(target.AsyncDelayRuleValue!.Value);
        return PropertyErrors.None;
    }
}

internal class AsyncDelayUpdateChildRule : AsyncRuleBase<AsyncValidateObject>
{
    public AsyncDelayUpdateChildRule()
    {
        AddTriggerProperties(a => a.AsyncDelayUpdateChildRuleValue, a => a.Child.AsyncDelayUpdateChildRuleValue);
    }
    public int RunCount { get; private set; } = 0;

    public override async Task<PropertyErrors> Execute(AsyncValidateObject target, CancellationToken? token)
    {
        RunCount++;
        await Task.Delay(2);
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

    public override PropertyErrors Execute(AsyncValidateObject target)
    {
        target.NestedSyncB = Guid.NewGuid().ToString();
        RunCount++;
        return PropertyErrors.None;
    }
}
internal class NestedSyncRuleB : RuleBase<AsyncValidateObject>
{
    public NestedSyncRuleB()
    {
        AddTriggerProperties(_ => _.NestedSyncB);
    }
    public int RunCount { get; private set; } = 0;

    public override PropertyErrors Execute(AsyncValidateObject target)
    {
        RunCount++;
        return PropertyErrors.None;
    }
}

internal class AsyncRuleCanWait : AsyncRuleBase<AsyncValidateObject>
{
    public AsyncRuleCanWait()
    {
        AddTriggerProperties(_ => _.AsyncRulesCanWait);
    }
    public int RunCount { get; private set; } = 0;
    public override async Task<PropertyErrors> Execute(AsyncValidateObject target, CancellationToken? token)
    {
        RunCount++;
        await Task.Delay(2);

        // This is how to await for a property to be set
        await target.AsyncRuleCanWaitProperty.SetValue("Value");

        return PropertyErrors.None;
    }
}

internal class AsyncRuleCanWaitNested : AsyncRuleBase<AsyncValidateObject>
{
    public AsyncRuleCanWaitNested()
    {
        AddTriggerProperties(_ => _.AsyncRulesCanWait);
    }
    public int RunCount { get; private set; } = 0;
    public override async Task<PropertyErrors> Execute(AsyncValidateObject target, CancellationToken? token)
    {
        RunCount++;
        await Task.Delay(2);
        target.AsyncRulesCanWaitNested = "Ran";
        target.AsyncDelayUpdateChildRuleValue = Guid.NewGuid().ToString();
        return PropertyErrors.None;
    }

}

[SuppressFactory]
internal class AsyncValidateObject : ValidateBase<AsyncValidateObject>
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

    public string? HasNoRules { get => Getter<string>(); set => Setter(value); }

    public string? SyncA { get => Getter<string>(); set => Setter(value); }

    public string? NestedSyncB { get => Getter<string>(); set => Setter(value); }

    public string? AsyncDelayUpdateChildRuleValue { get => Getter<string>(); set => Setter(value); }

    public IProperty AsyncRuleCanWaitProperty => this[nameof(AsyncRulesCanWait)];

    public string? AsyncRulesCanWait { get => Getter<string>(); set => Setter(value); }
    public string? AsyncRulesCanWaitNested { get => Getter<string>(); set => Setter(value); }

    public int? AsyncDelayRuleValue { get => Getter<int>(); set => Setter(value); }

    public AsyncValidateObject Child { get => Getter<AsyncValidateObject>()!; set => Setter(value); }

    protected override async Task ChildNeatooPropertyChanged(PropertyChangedBreadCrumbs breadCrumbs)
    {
        if(breadCrumbs.FullPropertyName == nameof(AsyncRulesCanWait))
        {
            await Task.Delay(2);
        }
        await base.ChildNeatooPropertyChanged(breadCrumbs);
    }
}
