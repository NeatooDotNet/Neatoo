using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.PersonObjects;
using System.ComponentModel;

namespace Neatoo.UnitTest.ValidateBaseTests;





[TestClass]
public class ValidateBaseAsyncTests
{


    private IServiceScope scope;
    private IValidateAsyncObject validate;
    private IValidateAsyncObject child;

    [TestInitialize]
    public async Task TestInitailize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        var validateDto = scope.GetRequiredService<IReadOnlyList<PersonDto>>().Where(p => !p.FatherId.HasValue && !p.MotherId.HasValue).First();
        validate = scope.GetRequiredService<IValidateAsyncObject>();
        child = scope.GetRequiredService<IValidateAsyncObject>();
        validate.Child = child;
        
        child.aLabel = "Child";
        validate.aLabel = "Parent";

        await validate.WaitForTasks();

        validate.PropertyChanged += Validate_PropertyChanged;   
    }

    [TestCleanup]
    public void TestCleanup()
    {
        Assert.IsFalse(validate.IsBusy);
        Assert.IsFalse(validate.IsSelfBusy);
        validate.PropertyChanged -= Validate_PropertyChanged;
    }

    private List<string> propertyChanged = new List<string>();
    private void Validate_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        propertyChanged.Add(e.PropertyName);
    }

    [TestMethod]
    public async Task ValidateBaseAsync_Const()
    {
        Assert.AreSame(child.Parent, validate);
        await validate.WaitForTasks();
        Assert.IsFalse(validate.IsBusy);
    }


    [TestMethod]
    public async Task ValidateBaseAsync_Set()
    {
        validate.FirstName = "Keith";
        await validate.WaitForTasks();
    }

    [TestMethod]
    public async Task ValidateBaseAsync_Set_IsBusy_True()
    {
        validate.FirstName = "Keith";
        Assert.IsTrue(validate.IsBusy);
        Assert.IsTrue(validate.IsSelfBusy);
        await validate.WaitForTasks();
        Assert.IsFalse(validate.IsBusy);
        Assert.IsFalse(validate.IsSelfBusy);
    }

    [TestMethod]
    public async Task ValidateBaseAsync_SetGet()
    {
        var name = Guid.NewGuid().ToString();
        validate.ShortName = name;
        Assert.AreEqual(name, validate.ShortName);
        await validate.WaitForTasks();
    }

    //[TestMethod]
    //public void ValidateBaseAsync_RulesCreated()
    //{
    //    Assert.IsTrue(Core.Factory.StaticFactory.RuleManager.RegisteredRules.ContainsKey(typeof(ValidateBaseAsync)));
    //    Assert.AreEqual(3, Core.Factory.StaticFactory.RuleManager.RegisteredRules[typeof(ValidateBaseAsync)].Count);
    //    Assert.IsInstanceOfType(((IRegisteredRuleList<ValidateBaseAsync>) Core.Factory.StaticFactory.RuleManager.RegisteredRules[typeof(ValidateBaseAsync)]).First(), typeof(ShortNameAsyncRule));
    //    Assert.IsInstanceOfType(((IRegisteredRuleList<ValidateBaseAsync>)Core.Factory.StaticFactory.RuleManager.RegisteredRules[typeof(ValidateBaseAsync)]).Take(2).Last(), typeof(FullNameAsyncRule));
    //    Assert.IsInstanceOfType(((IRegisteredRuleList<ValidateBaseAsync>)Core.Factory.StaticFactory.RuleManager.RegisteredRules[typeof(ValidateBaseAsync)]).Take(3).Last(), typeof(FirstNameTargetAsyncRule));
    //}


    [TestMethod]
    public async Task ValidateBaseAsync_Rule()
    {

        validate.FirstName = "John";
        validate.LastName = "Smith";

        await validate.WaitForTasks();

        Assert.AreEqual("John Smith", validate.ShortName);

    }

    [TestMethod]
    public async Task ValidateBaseAsync_Rule_Recursive()
    {

        validate.Title = "Mr.";
        validate.FirstName = "John";
        validate.LastName = "Smith";

        await validate.WaitForTasks();

        Assert.AreEqual("John Smith", validate.ShortName);
        Assert.AreEqual("Mr. John Smith", validate.FullName);

    }

    [TestMethod]
    public async Task ValidateBaseAsync_Rule_IsValid_True()
    {
        validate.Title = "Mr.";
        validate.FirstName = "John";
        validate.LastName = "Smith";

        await validate.WaitForTasks();

        Assert.IsTrue(validate.IsValid);
    }

    [TestMethod]
    public async Task ValidateBaseAsync_Rule_IsValid_False()
    {
        validate.Title = "Mr.";
        validate.FirstName = "Error";
        validate.LastName = "Smith";

        await validate.WaitForTasks();

        Assert.IsFalse(validate.IsValid);
        Assert.IsFalse(validate[nameof(validate.FirstName)].IsValid);
    }

    [TestMethod]
    public async Task ValidateBaseAsync_Rule_IsValid_False_Fixed()
    {
        validate.Title = "Mr.";
        validate.FirstName = "Error";
        validate.LastName = "Smith";

        await validate.WaitForTasks();

        Assert.IsFalse(validate.IsValid);

        validate.FirstName = "John";

        await validate.WaitForTasks();

        Assert.IsFalse(validate.IsBusy);
        var invalidProperties = validate.Properties.Where(p => !p.IsValid).ToList();

        foreach (var item in invalidProperties)
        {
            Console.WriteLine($"{item.Name} Value: [{item.Value}] IsValid:{item.IsValid} IsBusy: {item.IsBusy}");
        }

        Assert.IsTrue(validate.IsValid);
        Assert.AreEqual(0, validate.BrokenRuleMessages.Count);
        Assert.IsTrue(propertyChanged.Contains(nameof(validate.IsValid)));
    }


    [TestMethod]
    public async Task ValidateBaseAsync_Invalid()
    {
        validate.FirstName = "Error";

        await validate.WaitForTasks();

        Assert.IsFalse(validate.IsBusy);
        Assert.IsFalse(validate.IsValid);
        Assert.IsFalse(validate.IsSelfValid);
        Assert.IsTrue(child.IsValid);
        Assert.IsTrue(child.IsSelfValid);
    }

    [TestMethod]
    public async Task ValidateBaseAsync_Child_Invalid()
    {
        child.FirstName = "Error";
        await validate.WaitForTasks();

        Assert.IsFalse(validate.IsBusy);
        Assert.IsFalse(validate.IsValid);
        Assert.IsTrue(validate.IsSelfValid);
        Assert.IsFalse(child.IsValid);
        Assert.IsFalse(child.IsSelfValid);
    }

    [TestMethod]
    public async Task ValidateBaseAsync_Child_IsBusy()
    {
        child.FirstName = "Error";

        Assert.IsTrue(validate.IsBusy);
        //Assert.IsFalse(validate.IsSelfBusy);
        Assert.IsTrue(child.IsBusy);
        Assert.IsTrue(child.IsSelfBusy);

        await validate.WaitForTasks();

        Assert.IsFalse(validate.IsBusy);
        Assert.IsFalse(validate.IsValid);
        Assert.IsTrue(validate.IsSelfValid);
        Assert.IsFalse(child.IsValid);
        Assert.IsFalse(child.IsSelfValid);
    }

    [TestMethod]
    public async Task ValidateBaseAsync_AsyncRuleThrowsException()
    {
        validate.ThrowException = "Throw";
        await Assert.ThrowsExceptionAsync<AggregateException>(() => validate.WaitForTasks());
        Assert.IsFalse(validate.IsValid);
        Assert.IsFalse(validate[nameof(validate.ThrowException)].IsValid);
    }

    [TestMethod]
    public async Task ValidateBaseAsync_RecursiveRuleAsync()
    {
        validate.ShortName = "Recursive";
        await validate.WaitForTasks();
        Assert.AreEqual("Recursive change", validate.ShortName);
    }

    [TestMethod]
    public async Task ValidateBaseAsync_RecursiveRule_Invalid()
    {
        // This will cause the ShortNameRule to fail
        validate.ShortName = "Recursive Error";
        await validate.WaitForTasks();
        Assert.IsFalse(validate.IsValid);
    }

    [TestMethod]
    public async Task ValidateBaseAsync_RunAllRules()
    {
        await validate.RunAllRules();

        Assert.AreEqual(3, validate.RuleRunCount);
        Assert.IsFalse(validate.IsBusy);
    }
}
