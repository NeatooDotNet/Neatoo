// -----------------------------------------------------------------------------
// Design.Tests - Asynchronous Rule Tests
// -----------------------------------------------------------------------------
// Tests demonstrating async validation rules.
// -----------------------------------------------------------------------------

using Design.Domain.Rules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.RuleTests;

[TestClass]
public class AsyncRuleTests
{
    private IServiceScope _scope = null!;
    private IFluentRulesDemoFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IFluentRulesDemoFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public async Task AsyncRule_SetsIsBusy()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity.Email = "test@example.com";

        // Wait for async rule
        await entity.WaitForTasks();

        // Assert
        Assert.IsFalse(entity.IsBusy, "Should not be busy after WaitForTasks");
    }

    [TestMethod]
    public async Task AsyncValidationRule_InvalidatesOnError()
    {
        // Arrange
        var entity = _factory.Create();
        entity.Name = "Valid Name"; // Make name valid

        // Act
        entity.Email = "invalid-email"; // No @ sign

        // Wait for async validation
        await entity.WaitForTasks();

        // Assert
        Assert.IsFalse(entity.IsValid, "Invalid email should fail validation");
    }

    [TestMethod]
    public async Task AsyncValidationRule_ValidOnSuccess()
    {
        // Arrange
        var entity = _factory.Create();
        entity.Name = "Valid Name";

        // Act
        entity.Email = "valid@example.com";

        // Wait for async validation
        await entity.WaitForTasks();

        // Assert
        Assert.IsTrue(entity.IsValid, "Valid email should pass validation");
    }

    [TestMethod]
    public async Task AsyncActionRule_UpdatesProperty()
    {
        // Arrange
        var entity = _factory.Create();
        entity.Name = "Valid Name";

        // Act
        entity.Email = "test@example.com";
        await entity.WaitForTasks();

        // Assert
        // The async action rule sets Status to "Validated: {email}"
        Assert.IsNotNull(entity.Status);
        Assert.IsTrue(entity.Status!.Contains("Validated"));
    }
}
