// -----------------------------------------------------------------------------
// Design.Tests - Field-Level Authorization Tests
// -----------------------------------------------------------------------------
// Behavioral contracts for MarkReadOnly() field-level authorization.
// These verify the runtime authorization pattern documented in
// Design.Domain/PropertySystem/FieldLevelAuthorization.cs.
// -----------------------------------------------------------------------------

using Design.Domain.PropertySystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.PropertyTests;

[TestClass]
public class FieldLevelAuthorizationTests
{
    private IServiceScope _scope = null!;
    private IFieldLevelAuthDemoFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IFieldLevelAuthDemoFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public async Task MarkReadOnly_DuringFetch_SetsSalaryReadOnly()
    {
        // Scenario 7/9: MarkReadOnly called during Fetch, entity arrives with IsReadOnly=true
        // WHEN Fetch is called with canEditSalary=false,
        // THEN the Salary property has IsReadOnly=true

        // Arrange & Act
        var entity = await _factory.Fetch(1, canEditSalary: false);

        // Assert
        Assert.IsTrue(entity["Salary"].IsReadOnly,
            "Salary should be read-only when user lacks edit permission");
    }

    [TestMethod]
    public async Task MarkReadOnly_DuringFetch_NameRemainsWritable()
    {
        // Negative case: MarkReadOnly only affects the targeted property
        // WHEN Fetch is called with canEditSalary=false,
        // THEN the Name property remains writable

        // Arrange & Act
        var entity = await _factory.Fetch(1, canEditSalary: false);

        // Assert
        Assert.IsFalse(entity["Name"].IsReadOnly,
            "Name should remain writable regardless of salary authorization");
    }

    [TestMethod]
    public async Task MarkReadOnly_WhenCanEdit_SalaryRemainsWritable()
    {
        // Positive case: When user has permission, property stays writable
        // WHEN Fetch is called with canEditSalary=true,
        // THEN the Salary property remains writable

        // Arrange & Act
        var entity = await _factory.Fetch(1, canEditSalary: true);

        // Assert
        Assert.IsFalse(entity["Salary"].IsReadOnly,
            "Salary should remain writable when user has edit permission");
    }

    [TestMethod]
    public async Task MarkReadOnly_SetValueThrowsOnReadOnlyField()
    {
        // Scenario 2: SetValue on MarkReadOnly'd property throws
        // WHEN Salary is marked read-only and SetValue is called,
        // THEN PropertyException is thrown

        // Arrange
        var entity = await _factory.Fetch(1, canEditSalary: false);

        // Act & Assert
        try
        {
            await entity["Salary"].SetValue(100000m);
            Assert.Fail("Expected PropertyException to be thrown for read-only property");
        }
        catch (Exception ex) when (ex is Neatoo.PropertyException)
        {
            // Expected: PropertyReadOnlyException (derives from PropertyException)
            StringAssert.Contains(ex.Message, "read-only");
        }
    }

    [TestMethod]
    public async Task MarkReadOnly_LoadValueSucceedsOnReadOnlyField()
    {
        // Scenario 4: LoadValue bypasses IsReadOnly (persistence still works)
        // WHEN Salary is marked read-only and LoadValue is called,
        // THEN value is set without exception

        // Arrange
        var entity = await _factory.Fetch(1, canEditSalary: false);

        // Act
        entity["Salary"].LoadValue(90000m);

        // Assert
        Assert.AreEqual(90000m, entity["Salary"].Value);
    }
}
