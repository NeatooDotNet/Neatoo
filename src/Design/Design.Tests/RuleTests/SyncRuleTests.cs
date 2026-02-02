// -----------------------------------------------------------------------------
// Design.Tests - Synchronous Rule Tests
// -----------------------------------------------------------------------------
// Tests demonstrating synchronous validation rules using RuleBase.
// -----------------------------------------------------------------------------

using Design.Domain.Rules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.RuleTests;

[TestClass]
public class SyncRuleTests
{
    private IServiceScope _scope = null!;
    private IRuleBasicsDemoFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IRuleBasicsDemoFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void Rule_TriggersOnPropertyChange()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity.Quantity = 5;
        entity.Price = 10.00m;

        // Assert
        Assert.AreEqual(50.00m, entity.Total, "Total should be calculated by rule");
    }

    [TestMethod]
    public async Task ValidationRule_MakesInvalidOnFailure()
    {
        // Arrange
        var entity = _factory.Create();
        entity.Name = "Valid"; // Start with valid name
        await entity.WaitForTasks();
        Assert.IsTrue(entity.IsValid);

        // Act
        entity.Name = null; // Triggers NameRequiredRule
        await entity.WaitForTasks();

        // Assert
        Assert.IsFalse(entity.IsValid, "Entity should be invalid when name is empty");
    }

    [TestMethod]
    public async Task ValidationRule_MakesValidOnPass()
    {
        // Arrange - Set valid first, then make invalid (to trigger the rule)
        var entity = _factory.Create();
        entity.Name = "Initial Valid"; // First set a valid name to trigger rule
        await entity.WaitForTasks();
        Assert.IsTrue(entity.IsValid, "Should be valid with name");

        entity.Name = ""; // Make invalid (empty string triggers rule, unlike null which doesn't change)
        await entity.WaitForTasks();
        Assert.IsFalse(entity.IsValid, "Should be invalid with empty name");

        // Act
        entity.Name = "Valid Name";
        await entity.WaitForTasks();

        // Assert
        Assert.IsTrue(entity.IsValid, "Entity should be valid when name is provided");
    }

    [TestMethod]
    public void ActionRule_UpdatesMultipleProperties()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity.Quantity = 10;
        entity.Price = 5.00m;

        // Assert
        Assert.AreEqual(50.00m, entity.Total);

        // Change one trigger property
        entity.Quantity = 20;
        Assert.AreEqual(100.00m, entity.Total);
    }
}
