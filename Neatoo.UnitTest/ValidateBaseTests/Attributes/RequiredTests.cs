using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory;
using Neatoo.Rules.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.ValidateBaseTests.Attributes;

[SuppressFactory]
public class RequiredObject : ValidateBase<RequiredObject>
{
    public RequiredObject() : base(new ValidateBaseServices<RequiredObject>()) 
    {
        var allRequiredRulesExecuted = new AllRequiredRulesExecuted(RuleManager.Rules.OfType<IRequiredRule>());
        RuleManager.AddRule(allRequiredRulesExecuted);
        allRequiredRulesExecuted.RunRule(this, CancellationToken.None).Wait();
    }

    [Required]
    public string StringValue { get => Getter<string>(); set => Setter(value); }

    [Required]
    public int IntValue { get => Getter<int>(); set => Setter(value); }

    [Required]
    public int? NullableValue { get => Getter<int?>(); set => Setter(value); }

    [Required]
    public List<int> ObjectValue { get => Getter<List<int>>(); set => Setter(value); }

}

[TestClass]
public class RequiredAttributeTests
{
    private RequiredObject requiredObject;

    [TestInitialize]
    public async Task TestInitialize()
    {
        requiredObject = new RequiredObject();
    }

    [TestMethod]
    public async Task RequiredAttribute_InitiallyInValid()
    {
        Assert.IsFalse(requiredObject.IsValid);
    }

    [TestMethod]
    public async Task RequiredAttribute_InValid()
    {

        await requiredObject.RunAllRules();
        Assert.IsFalse(requiredObject.IsValid);
    }

    [TestMethod]
    public void RequiredAttribute_Valid()
    {

        requiredObject.StringValue = "test";
        requiredObject.IntValue = 1;
        requiredObject.NullableValue = 1;
        requiredObject.ObjectValue = new List<int> { 1, 2, 3 };

        Assert.IsFalse(requiredObject.IsBusy);
        Assert.IsTrue(requiredObject.IsValid);
    }
}
