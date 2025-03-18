using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Rules;
using Neatoo.UnitTest.PersonObjects;

namespace Neatoo.UnitTest.ValidateBaseTests;


public interface ISharedShortNameRuleTarget : IValidateBase
{
    string ShortName { get; set; }
    string FirstName { get; set; }
    string LastName { get; set; }
}

public interface ISharedShortNameRule<T> : IRule<T> where T : class, ISharedShortNameRuleTarget
{
}

public class SharedShortNameRule<T> : Rules.AsyncRuleBase<T>, ISharedShortNameRule<T> where T : class, ISharedShortNameRuleTarget
{

    public SharedShortNameRule() : base()
    {
        base.AddTriggerProperties(_=> _.ShortName, _=>_.FirstName, _=>_.LastName);
    }

    public override async Task<PropertyErrors> Execute(T target, CancellationToken? token)
    {
        await Task.Delay(10);

        var sn = $"{target.FirstName} {target.LastName}";

        // Prevent cascading rules or any PropertyChanged events
        LoadProperty(target, _ => _.ShortName, sn);

        return PropertyErrors.None;

    }
}

public interface ISharedAsyncRuleObject : IPersonBase { }

public class SharedAsyncRuleObject : PersonValidateBase<SharedAsyncRuleObject>, ISharedAsyncRuleObject, ISharedShortNameRuleTarget
{
    public SharedAsyncRuleObject(IValidateBaseServices<SharedAsyncRuleObject> services,
                    ISharedShortNameRule<SharedAsyncRuleObject> sharedRule) : base(services)
    {
        RuleManager.AddRule(sharedRule);
    }
}

[TestClass]
public class SharedAsyncRuleTests
{

    private IServiceScope scope;
    private ISharedAsyncRuleObject target;

    [TestInitialize]
    public void TestInitailize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        target = scope.GetRequiredService<ISharedAsyncRuleObject>();

    }


    [TestCleanup]
    public void TestInitalize()
    {
        scope.Dispose();
    }

    [TestMethod]
    public async Task SharedAsyncRuleTests_ShortName()
    {
        target.FirstName = "John";
        target.LastName = "Smith";

        await target.WaitForTasks();

        Assert.AreEqual("John Smith", target.ShortName);

    }
}
