using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.UnitTest.ValidateBaseTests;



[TestClass]
public class ValidateBaseTests
{

    private IServiceScope scope;
    private IValidateObject validate;
    private IValidateObject child;

    [TestInitialize]
    public void TestInitailize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        validate = scope.GetRequiredService<IValidateObject>();
        child = scope.GetRequiredService<IValidateObject>();
        validate.Child = child;
        validate.PropertyChanged += Validate_PropertyChanged; 
        validate.Child.PropertyChanged += ChildValidate_PropertyChanged;

        Assert.IsFalse(validate.IsBusy);
    }


    [TestCleanup]
    public async Task TestCleanup()
    {
        await validate.WaitForTasks();
        Assert.IsFalse(validate.IsBusy);
        Assert.IsFalse(validate.IsSelfBusy);
        validate.PropertyChanged -= Validate_PropertyChanged;
        validate.Child.PropertyChanged -= ChildValidate_PropertyChanged;
        scope.Dispose();
    }

    private List<string> propertyChangedCalls = new List<string>();
    private void Validate_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        propertyChangedCalls.Add(e.PropertyName);
    }

    private List<string> childPropertyChangedCalls = new List<string>();
    private void ChildValidate_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        childPropertyChangedCalls.Add(e.PropertyName);
    }

    [TestMethod]
    public void ValidateBase_Constructor()
    {

    }


    [TestMethod]
    public void ValidateBase_Set()
    {
        validate.FirstName = "Keith";
    }

    [TestMethod]
    public void ValidateBase_SetGet()
    {
        var name = Guid.NewGuid().ToString();
        validate.ShortName = name;
        Assert.AreEqual(name, validate.ShortName);
    }

    //[TestMethod]
    //public void ValidateBase_RulesCreated()
    //{
    //    Assert.IsTrue(Core.Factory.StaticFactory.RuleManager.RegisteredRules.ContainsKey(typeof(Validate)));
    //    Assert.AreEqual(3, Core.Factory.StaticFactory.RuleManager.RegisteredRules[typeof(Validate)].Count);
    //    Assert.IsInstanceOfType(((IRegisteredRuleList<Validate>) Core.Factory.StaticFactory.RuleManager.RegisteredRules[typeof(Validate)]).First(), typeof(ShortNameRule));
    //    Assert.IsInstanceOfType(((IRegisteredRuleList<Validate>)Core.Factory.StaticFactory.RuleManager.RegisteredRules[typeof(Validate)]).Take(2).Last(), typeof(FullNameRule));
    //}

    [TestMethod]
    public void ValidateBase_Rule()
    {

        validate.FirstName = "John";
        validate.LastName = "Smith";

        Assert.AreEqual("John Smith", validate.ShortName);

    }

    [TestMethod]
    public void ValidateBase_Rule_Recursive()
    {

        validate.Title = "Mr.";
        validate.FirstName = "John";
        validate.LastName = "Smith";

        Assert.AreEqual("John Smith", validate.ShortName);
        Assert.AreEqual("Mr. John Smith", validate.FullName);

    }

    [TestMethod]
    public void ValidateBase_Rule_SameValue()
    {

        validate.Title = "Mr.";
        validate.FirstName = "John";
        validate.LastName = "Smith";
        var ruleCount = validate.RuleRunCount;
        validate.Title = "Mr.";
        validate.FirstName = "John";
        validate.LastName = "Smith";

        Assert.AreEqual(ruleCount, validate.RuleRunCount);
    }

    [TestMethod]
    public void ValidateBase_Rule_IsValid_True()
    {
        validate.Title = "Mr.";
        validate.FirstName = "John";
        validate.LastName = "Smith";

        Assert.IsTrue(validate.IsValid);
    }

    [TestMethod]
    public void ValidateBase_Rule_IsValid_False()
    {
        validate.Title = "Mr.";
        validate.FirstName = "Error";
        validate.LastName = "Smith";

        Assert.IsFalse(validate.IsValid);
        Assert.IsFalse(validate[nameof(validate.FirstName)].IsValid);
    }

    [TestMethod]
    public void ValidateBase_Rule_IsValid_False_Fixed()
    {
        validate.Title = "Mr.";
        validate.FirstName = "Error";
        validate.LastName = "Smith";

        Assert.IsFalse(validate.IsValid);

        validate.FirstName = "John";

        Assert.IsFalse(validate.IsBusy);
        Assert.IsTrue(validate.IsValid);
        Assert.IsTrue(propertyChangedCalls.Contains(nameof(validate.IsValid)));
        Assert.IsTrue(propertyChangedCalls.Contains(nameof(validate.IsSelfValid)));
    }

    [TestMethod]
    public async Task ValidateBase_RunSelfRules()
    {
        var ruleCount = validate.RuleRunCount;
        await validate.RunSelfRules();
        Assert.AreEqual(ruleCount + 3, validate.RuleRunCount);
    }

    [TestMethod]
    public async Task ValidateBase_RunAllRules()
    {
        var ruleCount = validate.RuleRunCount;
        validate.Age = 10;
        await validate.RunAllRules();
        Assert.AreEqual(ruleCount + 3, validate.RuleRunCount);
    }


    [TestMethod]
    public void ValidateBase_validateInvalid()
    {
        validate.FirstName = "Error";
        Assert.IsFalse(validate.IsBusy);
        Assert.IsFalse(validate.IsValid);
        Assert.IsFalse(validate.IsSelfValid);
        Assert.IsTrue(child.IsValid);
        Assert.IsTrue(child.IsSelfValid);
        Assert.IsTrue(propertyChangedCalls.Contains(nameof(validate.IsValid)));
        Assert.IsTrue(propertyChangedCalls.Contains(nameof(validate.IsSelfValid)));

    }

    [TestMethod]
    public void ValidateBase_ChildInvalid()
    {
        child.FirstName = "Error";
        Assert.IsFalse(validate.IsBusy);
        Assert.IsFalse(validate.IsValid);
        Assert.IsTrue(validate.IsSelfValid);
        Assert.IsFalse(child.IsValid);
        Assert.IsFalse(child.IsSelfValid);

        //Assert.IsTrue(propertyChangedCalls.Contains(nameof(validate.IsValid)));
        //Assert.IsFalse(propertyChangedCalls.Contains(nameof(validate.IsSelfValid)));

        //CollectionAssert.Contains(childPropertyChangedCalls, nameof(child.FirstName));

        //Assert.IsTrue(childPropertyChangedCalls.Contains(nameof(validate.IsValid)));
        //Assert.IsTrue(childPropertyChangedCalls.Contains(nameof(validate.IsSelfValid)));
        //// No async rules - so never busy
        //Assert.IsFalse(childPropertyChangedCalls.Contains(nameof(validate.IsBusy)));
        //Assert.IsFalse(childPropertyChangedCalls.Contains(nameof(validate.IsSelfBusy)));
    }

    [TestMethod]
    public void ValidateBase_Parent()
    {
        Assert.AreSame(validate, child.Parent);
    }

    [TestMethod]
    public void ValidateBase_MarkInvalid()
    {
        string message;
        validate.TestMarkInvalid(message = Guid.NewGuid().ToString());
        Assert.IsFalse(validate.IsValid);
        Assert.IsFalse(validate.IsSelfValid);
        Assert.AreEqual(1, validate.BrokenRuleMessages.Count);
        Assert.AreEqual(message, validate.BrokenRuleMessages.Single());
    }

    [TestMethod]
    public void ValidateBase_RecursiveRule()
    {
        validate.ShortName = "Recursive";
        Assert.AreEqual("Recursive change", validate.ShortName);
    }

    [TestMethod]
    public void ValidateBase_RecursiveRule_Invalid()
    {
        // This will cause the ShortNameRule to fail
        validate.ShortName = "Recursive Error";
        Assert.IsFalse(validate.IsValid);
    }

    [TestMethod]
    public void ValidateBase_ThrowsException()
    {
        // No async fork so an exception should be thrown
        Assert.ThrowsException<AggregateException>(() => validate.FirstName = "Throw");
    }
}
