using Neatoo.Rules;
using Neatoo.UnitTest.Integration.Concepts.ValidateBase;

namespace Neatoo.UnitTest.Integration.Aggregates.Person;


public interface IAsyncRuleThrowsException : IRule<IValidateAsyncObject>
{
}

public class AsyncRuleThrowsException : AsyncRuleBase<IValidateAsyncObject>, IAsyncRuleThrowsException
{
    public AsyncRuleThrowsException() : base()
    {
        AddTriggerProperties(_ => _.ThrowException);
    }

    protected override async Task<IRuleMessages> Execute(IValidateAsyncObject target, CancellationToken? token)
    {
        await Task.Delay(5);
        if (target.ThrowException == "Throw")
        {
            throw new Exception("Rule Failed");
        }
        return RuleMessages.None;
    }
}
