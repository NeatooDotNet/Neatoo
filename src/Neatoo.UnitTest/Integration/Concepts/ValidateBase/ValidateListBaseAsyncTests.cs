using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.UnitTest.Integration.Concepts.ValidateBase;



[TestClass]
public class ValidateListBaseAsyncTests
{
    private IServiceScope _scope;
    private IValidateAsyncObjectList _list;
    private IValidateAsyncObject _child;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = UnitTestServices.GetLifetimeScope();
        _list = _scope.GetRequiredService<IValidateAsyncObjectList>();
        _child = _scope.GetRequiredService<IValidateAsyncObject>();
        _list.Add(_child);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        Assert.IsFalse(_list.IsBusy);
        _scope?.Dispose();
    }

    [TestMethod]
    public async Task ChildInvalid_WhenChildHasError_ListIsInvalidButSelfValid()
    {
        // Arrange & Act
        _child.FirstName = "Error";
        await _list.WaitForTasks();

        // Assert
        Assert.IsFalse(_child.IsValid);
        Assert.IsFalse(_child.IsSelfValid);
        Assert.IsFalse(_list.IsBusy);
        Assert.IsFalse(_list.IsValid);
        Assert.IsTrue(_list.IsSelfValid);
    }

    [TestMethod]
    public async Task ChildModified_WithAsyncRules_ListIsBusyUntilComplete()
    {
        // Arrange & Act
        _child.FirstName = "Error";

        // Assert - Initially busy
        Assert.IsTrue(_list.IsBusy);
        Assert.IsTrue(_child.IsBusy);

        // Act - Wait for completion
        await _list.WaitForTasks();

        // Assert - After completion
        Assert.IsFalse(_list.IsBusy);
        Assert.IsFalse(_list.IsValid);
        Assert.IsTrue(_list.IsSelfValid);
        Assert.IsFalse(_child.IsValid);
        Assert.IsFalse(_child.IsSelfValid);
    }
}
