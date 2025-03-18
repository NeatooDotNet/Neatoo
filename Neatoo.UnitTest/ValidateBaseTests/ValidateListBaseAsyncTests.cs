using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.UnitTest.ValidateBaseTests;



[TestClass]
public class ValidateListBaseAsyncBaseAsyncTests
{

    IServiceScope scope;
    IValidateAsyncObjectList List;
    IValidateAsyncObject Child;

    [TestInitialize]
    public void TestInitailize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        List = scope.GetRequiredService<IValidateAsyncObjectList>();
        Child = scope.GetRequiredService<IValidateAsyncObject>();
        List.Add(Child);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        Assert.IsFalse(List.IsBusy);
        Assert.IsFalse(List.IsSelfBusy);
    }

    [TestMethod]
    public void ValidateListBaseAsync_Constructor()
    {

    }



    [TestMethod]
    public async Task ValidateListBaseAsync_ChildInvalid()
    {
        Child.FirstName = "Error";
        await List.WaitForTasks();
        Assert.IsFalse(Child.IsValid);
        Assert.IsFalse(Child.IsSelfValid);
        Assert.IsFalse(List.IsBusy);
        Assert.IsFalse(List.IsValid);
        Assert.IsTrue(List.IsSelfValid);
    }

    [TestMethod]
    public async Task ValidateListBaseAsync_Child_IsBusy()
    {
        Child.FirstName = "Error";

        Assert.IsTrue(List.IsBusy);
        Assert.IsFalse(List.IsSelfBusy);
        Assert.IsTrue(Child.IsBusy);
        Assert.IsTrue(Child.IsSelfBusy);

        await List.WaitForTasks();

        Assert.IsFalse(List.IsBusy);
        Assert.IsFalse(List.IsValid);
        Assert.IsTrue(List.IsSelfValid);
        Assert.IsFalse(Child.IsValid);
        Assert.IsFalse(Child.IsSelfValid);
    }

}
