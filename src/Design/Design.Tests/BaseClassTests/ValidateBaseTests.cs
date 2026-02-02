// -----------------------------------------------------------------------------
// Design.Tests - ValidateBase Tests
// -----------------------------------------------------------------------------
// Tests demonstrating ValidateBase<T> behavior including validation state,
// rules, and parent-child relationships.
// -----------------------------------------------------------------------------

using Design.Domain.BaseClasses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.BaseClassTests;

[TestClass]
public class ValidateBaseTests
{
    private IServiceScope _scope = null!;
    private IDemoValueObjectFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IDemoValueObjectFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void Create_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var obj = _factory.Create();

        // Assert
        Assert.IsNull(obj.Name);
        Assert.IsNull(obj.Description);
    }

    [TestMethod]
    public void Create_WithParameter_SetsProperty()
    {
        // Arrange & Act
        var obj = _factory.Create("Test Name");

        // Assert
        Assert.AreEqual("Test Name", obj.Name);
    }

    [TestMethod]
    public async Task SetEmptyName_MakesInvalid()
    {
        // Arrange
        var obj = _factory.Create("Valid Name");
        await obj.WaitForTasks();
        Assert.IsTrue(obj.IsValid, "Object with valid name should be valid");

        // Act - Set to empty to trigger validation failure
        obj.Name = "";
        await obj.WaitForTasks();

        // Assert
        Assert.IsFalse(obj.IsValid, "Empty name should fail validation");
        Assert.IsFalse(obj.IsSelfValid, "Empty name should fail self validation");
    }

    [TestMethod]
    public void Create_WithValidName_IsValid()
    {
        // Arrange & Act
        var obj = _factory.Create("Valid Name");

        // Assert
        Assert.IsTrue(obj.IsValid, "Valid name should pass validation");
        Assert.IsTrue(obj.IsSelfValid, "Valid name should pass self validation");
    }

    [TestMethod]
    public void PropertyChanged_FiresOnPropertySet()
    {
        // Arrange
        var obj = _factory.Create();
        var changedProperties = new List<string>();
        obj.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

        // Act
        obj.Name = "Test";

        // Assert
        Assert.IsTrue(changedProperties.Contains("Name"));
    }

    [TestMethod]
    public async Task WaitForTasks_CompletesWhenNotBusy()
    {
        // Arrange
        var obj = _factory.Create();

        // Act
        await obj.WaitForTasks();

        // Assert
        Assert.IsFalse(obj.IsBusy, "Should not be busy after WaitForTasks");
    }

    [TestMethod]
    public void IsBusy_FalseWithSyncRulesOnly()
    {
        // Arrange
        var obj = _factory.Create();

        // Act
        obj.Name = "Test"; // Triggers sync validation rule

        // Assert
        Assert.IsFalse(obj.IsBusy, "Sync rules should not set IsBusy");
    }
}
