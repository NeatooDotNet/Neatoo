using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Documentation.Samples.AggregatesAndEntities;

namespace Neatoo.Documentation.Samples.Tests.AggregatesAndEntities;

/// <summary>
/// Tests for ValidateBaseSamples.cs code snippets.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("AggregatesAndEntities")]
public class ValidateBaseSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region PersonSearchCriteria Tests

    [TestMethod]
    public async Task PersonSearchCriteria_RequiredSearchTerm_ValidatesOnRunRules()
    {
        // Arrange
        var factory = GetRequiredService<IPersonSearchCriteriaFactory>();
        var criteria = factory.Create();

        // Act
        await criteria.RunRules();

        // Assert
        var prop = criteria[nameof(IPersonSearchCriteria.SearchTerm)];
        Assert.IsFalse(prop.IsValid);
        Assert.IsTrue(prop.PropertyMessages.Any(m => m.Message.Contains("search term required")));
    }

    [TestMethod]
    public void PersonSearchCriteria_ValidSearchTerm_NoError()
    {
        // Arrange
        var factory = GetRequiredService<IPersonSearchCriteriaFactory>();
        var criteria = factory.Create();

        // Act
        criteria.SearchTerm = "John";

        // Assert
        var prop = criteria[nameof(IPersonSearchCriteria.SearchTerm)];
        Assert.IsTrue(prop.IsValid);
    }

    [TestMethod]
    public void PersonSearchCriteria_InvalidDateRange_ReturnsError()
    {
        // Arrange
        var factory = GetRequiredService<IPersonSearchCriteriaFactory>();
        var criteria = factory.Create();

        // Act - Set ToDate first, then FromDate to trigger FromDate's validation rule
        criteria.ToDate = DateTime.Today;
        criteria.FromDate = DateTime.Today.AddDays(5);

        // Assert
        var prop = criteria[nameof(IPersonSearchCriteria.FromDate)];
        Assert.IsFalse(prop.IsValid);
        Assert.IsTrue(prop.PropertyMessages.Any(m => m.Message.Contains("before")));
    }

    [TestMethod]
    public void PersonSearchCriteria_ValidDateRange_NoError()
    {
        // Arrange
        var factory = GetRequiredService<IPersonSearchCriteriaFactory>();
        var criteria = factory.Create();

        // Act
        criteria.FromDate = DateTime.Today;
        criteria.ToDate = DateTime.Today.AddDays(5);

        // Assert
        var prop = criteria[nameof(IPersonSearchCriteria.FromDate)];
        Assert.IsTrue(prop.IsValid);
    }

    #endregion

    #region OrderSearchCriteria Tests

    [TestMethod]
    public void OrderSearchCriteria_CanSetProperties()
    {
        // Arrange
        var factory = GetRequiredService<IOrderSearchCriteriaFactory>();
        var criteria = factory.Create();

        // Act
        criteria.CustomerName = "Acme Corp";
        criteria.OrderDate = DateTime.Today;

        // Assert
        Assert.AreEqual("Acme Corp", criteria.CustomerName);
        Assert.AreEqual(DateTime.Today, criteria.OrderDate);
    }

    #endregion
}
