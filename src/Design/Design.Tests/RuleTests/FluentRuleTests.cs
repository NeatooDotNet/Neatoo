// -----------------------------------------------------------------------------
// Design.Tests - Fluent Rule API Tests
// -----------------------------------------------------------------------------
// Tests demonstrating RuleManager fluent API (AddValidation, AddAction).
// -----------------------------------------------------------------------------

using Design.Domain.Rules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.RuleTests;

[TestClass]
public class FluentRuleTests
{
    private IServiceScope _scope = null!;
    private IFluentRulesDemoFactory _factory = null!;
    private ITriggerPatternsDemoFactory _triggerFactory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IFluentRulesDemoFactory>();
        _triggerFactory = _scope.GetRequiredService<ITriggerPatternsDemoFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void AddValidation_ValidatesOnTrigger()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity.Name = ""; // Empty name triggers validation

        // Assert
        Assert.IsFalse(entity.IsValid);
    }

    [TestMethod]
    public void AddAction_ExecutesOnTrigger()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity.Quantity = 5;
        entity.UnitPrice = 20.00m;

        // Assert
        Assert.AreEqual(100.00m, entity.Total, "Action rule should calculate Total");
    }

    [TestMethod]
    public void MultipleValidationRules_AllRun()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity.Name = "test"; // Contains "test" which is not allowed

        // Assert
        Assert.IsFalse(entity.IsValid, "Name containing 'test' should fail validation");
    }

    [TestMethod]
    public void TriggerPatterns_MultipleTriggers()
    {
        // Arrange
        var entity = _triggerFactory.Create();

        // Act
        entity.A = 10;
        entity.B = 20;
        entity.C = 30;

        // Assert
        Assert.AreEqual(60, entity.Sum, "Sum should be calculated from A + B + C");
    }
}
