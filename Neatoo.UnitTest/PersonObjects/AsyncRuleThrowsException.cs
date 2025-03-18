using Neatoo.Rules;
using Neatoo.UnitTest.ValidateBaseTests;

namespace Neatoo.UnitTest.PersonObjects;


public interface IAsyncRuleThrowsException : IRule<IValidateAsyncObject>
{
}

public class AsyncRuleThrowsException : AsyncRuleBase<IValidateAsyncObject>, IAsyncRuleThrowsException
{
    public AsyncRuleThrowsException() : base()
    {
        AddTriggerProperties(_ => _.ThrowException);
    }

    public override async Task<PropertyErrors> Execute(IValidateAsyncObject target, CancellationToken? token)
    {
        await Task.Delay(5);
        if (target.ThrowException == "Throw")
        {
            throw new Exception("Rule Failed");
        }
        return PropertyErrors.None;
    }
}
