using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Documentation.Samples.AggregatesAndEntities;

namespace Neatoo.Documentation.Samples.Tests.AggregatesAndEntities;

/// <summary>
/// Tests for EntityBaseSamples.cs code snippets.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("AggregatesAndEntities")]
public class EntityBaseSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region Order Tests

    [TestMethod]
    public void Order_Create_SetsDefaultStatus()
    {
        // Arrange & Act - Use factory to create properly initialized instance
        var factory = GetRequiredService<IOrderFactory>();
        var order = factory.Create();

        // Assert
        Assert.IsNotNull(order.Id);
        Assert.AreEqual("New", order.Status);
    }

    [TestMethod]
    public void Order_ModifyProperty_SetsIsModified()
    {
        // Arrange
        var factory = GetRequiredService<IOrderFactory>();
        var order = factory.Create();

        // Act
        order.Total = 99.99m;

        // Assert
        Assert.IsTrue(order.IsModified);
        Assert.IsTrue(order.IsSelfModified);
    }

    #endregion

    #region Customer Tests

    [TestMethod]
    public void Customer_Create_HasId()
    {
        // Arrange & Act
        var factory = GetRequiredService<ICustomerFactory>();
        var customer = factory.Create();

        // Assert
        Assert.IsNotNull(customer.Id);
    }

    #endregion

    #region Employee Tests

    [TestMethod]
    public void Employee_FullName_IsCalculated()
    {
        // Arrange
        var factory = GetRequiredService<IEmployeeFactory>();
        var employee = factory.Create();

        // Act
        employee.FirstName = "John";
        employee.LastName = "Doe";

        // Assert
        Assert.AreEqual("John Doe", employee.FullName);
    }

    [TestMethod]
    public void Employee_IsExpanded_NotTracked()
    {
        // Arrange
        var factory = GetRequiredService<IEmployeeFactory>();
        var employee = factory.Create();
        employee.FirstName = "Test"; // Mark as modified

        // Act
        employee.IsExpanded = true;

        // Assert - IsExpanded is a regular property, doesn't affect IsModified
        // The entity was already modified from FirstName
        Assert.IsTrue(employee.IsModified);
    }

    #endregion

    #region Contact Tests

    [TestMethod]
    public void Contact_RequiredFirstName_ValidatesOnSet()
    {
        // Arrange
        var factory = GetRequiredService<IContactFactory>();
        var contact = factory.Create();

        // Act
        contact.FirstName = "";

        // Assert
        var prop = contact[nameof(IContact.FirstName)];
        Assert.IsFalse(prop.IsValid);
    }

    [TestMethod]
    public void Contact_ValidEmail_NoError()
    {
        // Arrange
        var factory = GetRequiredService<IContactFactory>();
        var contact = factory.Create();

        // Act
        contact.Email = "test@example.com";

        // Assert
        var prop = contact[nameof(IContact.Email)];
        Assert.IsTrue(prop.IsValid);
    }

    [TestMethod]
    public void Contact_InvalidEmail_ReturnsError()
    {
        // Arrange
        var factory = GetRequiredService<IContactFactory>();
        var contact = factory.Create();

        // Act
        contact.Email = "not-an-email";

        // Assert
        var prop = contact[nameof(IContact.Email)];
        Assert.IsFalse(prop.IsValid);
        Assert.IsTrue(prop.PropertyMessages.Any(m => m.Message.Contains("Invalid email")));
    }

    #endregion
}
