using Neatoo;
using Neatoo.Core;
using Neatoo.Rules;

namespace AsyncBlazor;

public class AsyncValidate : ValidateBase<AsyncValidate>
{
    public AsyncValidate() : base(new ValidateBaseServices<AsyncValidate>())
    {
        AddRules(RuleManager);
    }

    private static void AddRules(IRuleManager<AsyncValidate> ruleManager)
    {
        ruleManager.AddActionAsync(async (AsyncValidate t) =>
        {
            await Task.Delay(1000);
            t.AsyncPropertyB = t.AsyncPropertyA * 2;
        }, _ => _.AsyncPropertyA);

        ruleManager.AddActionAsync(async (AsyncValidate t) =>
        {
            await Task.Delay(1000);
            t.AsyncPropertyC = t.AsyncPropertyB * 2;
        }, _ => _.AsyncPropertyB);

        ruleManager.AddActionAsync(async (AsyncValidate t) =>
        {
            await Task.Delay(1000);
            t.AsyncPropertyD = t.AsyncPropertyC / 2;
        }, _ => _.AsyncPropertyC);

        ruleManager.AddValidationAsync(async (AsyncValidate t) =>
        {
            await Task.Delay(1000);
            if (t.AsyncPropertyA == 100)
            {
                return "AsyncPropertyA cannot be 100";
            }
            return string.Empty;
        }, _ => _.AsyncPropertyA);

        ruleManager.AddValidationAsync(async (AsyncValidate t) =>
        {
            await Task.Delay(1000);
            if (t.AsyncPropertyB == 100)
            {
                return "AsyncPropertyB cannot be 100";
            }
            return string.Empty;
        }, _ => _.AsyncPropertyB);

        ruleManager.AddValidationAsync(async (AsyncValidate t) =>
        {
            await Task.Delay(1000);
            if (t.AsyncPropertyC == 100)
            {
                return "AsyncPropertyC cannot be 100";
            }
            return string.Empty;
        }, _ => _.AsyncPropertyC);

        ruleManager.AddValidationAsync(async (AsyncValidate t) =>
        {
            await Task.Delay(1000);
            if (t.AsyncPropertyD == 100)
            {
                return "AsyncPropertyD cannot be 100";
            }
            return string.Empty;
        }, _ => _.AsyncPropertyD);
    }

    new public IValidateProperty this[string propertyName] => base[propertyName];

    public uint AsyncPropertyA { get => Getter<uint>(); set => Setter(value); }

    public uint AsyncPropertyB { get => Getter<uint>(); set => Setter(value); }

    public uint AsyncPropertyC { get => Getter<uint>(); set => Setter(value); }

    public uint AsyncPropertyD { get => Getter<uint>(); set => Setter(value); }

}
