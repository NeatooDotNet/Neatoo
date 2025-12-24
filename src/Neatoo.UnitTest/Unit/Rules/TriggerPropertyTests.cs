using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Rules;

namespace Neatoo.UnitTest.Unit.Rules;

#region Test Helpers

public class TriggerPropertyTestSubject
{
    public TriggerPropertyTestSubjectChild? Child { get; set; }
}

public class TriggerPropertyTestSubjectChild
{
    public string? ChildProperty { get; set; }
}

#endregion

/// <summary>
/// Unit tests for the TriggerProperty class.
/// Tests the property path matching functionality used by the rules system.
/// </summary>
[TestClass]
public class TriggerPropertyTests
{
    [TestMethod]
    public void IsMatch_WithNestedPropertyPath_ReturnsTrue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<TriggerPropertyTestSubject>(t => t.Child!.ChildProperty);

        // Act
        var result = triggerProperty.IsMatch("Child.ChildProperty");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsMatch_WithNonMatchingPath_ReturnsFalse()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<TriggerPropertyTestSubject>(t => t.Child!.ChildProperty);

        // Act
        var result = triggerProperty.IsMatch("Other.Property");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsMatch_WithPartialPath_ReturnsFalse()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<TriggerPropertyTestSubject>(t => t.Child!.ChildProperty);

        // Act
        var result = triggerProperty.IsMatch("Child");

        // Assert
        Assert.IsFalse(result);
    }
}
