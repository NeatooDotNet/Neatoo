using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neatoo.Rules;

namespace Neatoo.UnitTest.Unit.Rules;

/// <summary>
/// Unit tests for the RuleProxy class.
/// Tests the simple data container behavior used in the rules system.
/// </summary>
[TestClass]
public class RuleProxyTests
{
    #region Constructor Tests

    [TestMethod]
    public void Constructor_Default_CreatesInstance()
    {
        // Arrange & Act
        var ruleProxy = new RuleProxy();

        // Assert
        Assert.IsNotNull(ruleProxy);
    }

    [TestMethod]
    public void Constructor_Default_TargetIsNull()
    {
        // Arrange & Act
        var ruleProxy = new RuleProxy();

        // Assert
        Assert.IsNull(ruleProxy.Target);
    }

    #endregion

    #region Target Property Tests

    [TestMethod]
    public void Target_SetValue_ReturnsSetValue()
    {
        // Arrange
        var ruleProxy = new RuleProxy();
        var mockTarget = new Mock<IValidateBase>();

        // Act
        ruleProxy.Target = mockTarget.Object;

        // Assert
        Assert.AreSame(mockTarget.Object, ruleProxy.Target);
    }

    [TestMethod]
    public void Target_SetToNull_ReturnsNull()
    {
        // Arrange
        var ruleProxy = new RuleProxy();
        var mockTarget = new Mock<IValidateBase>();
        ruleProxy.Target = mockTarget.Object;

        // Act
        ruleProxy.Target = null!;

        // Assert
        Assert.IsNull(ruleProxy.Target);
    }

    [TestMethod]
    public void Target_SetMultipleTimes_ReturnsLastValue()
    {
        // Arrange
        var ruleProxy = new RuleProxy();
        var mockTarget1 = new Mock<IValidateBase>();
        var mockTarget2 = new Mock<IValidateBase>();
        var mockTarget3 = new Mock<IValidateBase>();

        // Act
        ruleProxy.Target = mockTarget1.Object;
        Assert.AreSame(mockTarget1.Object, ruleProxy.Target);

        ruleProxy.Target = mockTarget2.Object;
        Assert.AreSame(mockTarget2.Object, ruleProxy.Target);

        ruleProxy.Target = mockTarget3.Object;

        // Assert
        Assert.AreSame(mockTarget3.Object, ruleProxy.Target);
    }

    [TestMethod]
    public void Target_SetSameValueTwice_ReturnsSameValue()
    {
        // Arrange
        var ruleProxy = new RuleProxy();
        var mockTarget = new Mock<IValidateBase>();

        // Act
        ruleProxy.Target = mockTarget.Object;
        ruleProxy.Target = mockTarget.Object;

        // Assert
        Assert.AreSame(mockTarget.Object, ruleProxy.Target);
    }

    #endregion

    #region Interface Implementation Tests

    [TestMethod]
    public void Target_AcceptsAnyIValidateBaseImplementation()
    {
        // Arrange
        var ruleProxy = new RuleProxy();
        var mockTarget = new Mock<IValidateBase>();

        // Setup minimal required interface members
        mockTarget.SetupGet(x => x.IsPaused).Returns(false);
        mockTarget.SetupGet(x => x.IsValid).Returns(true);

        // Act
        ruleProxy.Target = mockTarget.Object;

        // Assert
        Assert.AreSame(mockTarget.Object, ruleProxy.Target);
    }

    #endregion

    #region Object Initialization Pattern Tests

    [TestMethod]
    public void ObjectInitializer_WithTarget_SetsTarget()
    {
        // Arrange
        var mockTarget = new Mock<IValidateBase>();

        // Act
        var ruleProxy = new RuleProxy
        {
            Target = mockTarget.Object
        };

        // Assert
        Assert.AreSame(mockTarget.Object, ruleProxy.Target);
    }

    #endregion

    #region Reference Semantics Tests

    [TestMethod]
    public void Target_MaintainsReferenceSemantics()
    {
        // Arrange
        var ruleProxy = new RuleProxy();
        var mockTarget = new Mock<IValidateBase>();

        // Act
        ruleProxy.Target = mockTarget.Object;
        var retrievedTarget = ruleProxy.Target;

        // Assert - Same reference
        Assert.IsTrue(ReferenceEquals(mockTarget.Object, retrievedTarget));
    }

    [TestMethod]
    public void MultipleRuleProxies_CanShareSameTarget()
    {
        // Arrange
        var mockTarget = new Mock<IValidateBase>();
        var ruleProxy1 = new RuleProxy();
        var ruleProxy2 = new RuleProxy();

        // Act
        ruleProxy1.Target = mockTarget.Object;
        ruleProxy2.Target = mockTarget.Object;

        // Assert
        Assert.AreSame(ruleProxy1.Target, ruleProxy2.Target);
    }

    [TestMethod]
    public void MultipleRuleProxies_WithDifferentTargets_MaintainIndependence()
    {
        // Arrange
        var mockTarget1 = new Mock<IValidateBase>();
        var mockTarget2 = new Mock<IValidateBase>();
        var ruleProxy1 = new RuleProxy();
        var ruleProxy2 = new RuleProxy();

        // Act
        ruleProxy1.Target = mockTarget1.Object;
        ruleProxy2.Target = mockTarget2.Object;

        // Assert
        Assert.AreNotSame(ruleProxy1.Target, ruleProxy2.Target);
        Assert.AreSame(mockTarget1.Object, ruleProxy1.Target);
        Assert.AreSame(mockTarget2.Object, ruleProxy2.Target);
    }

    #endregion

    #region Edge Case Tests

    [TestMethod]
    public void Target_GetBeforeSet_ReturnsNull()
    {
        // Arrange
        var ruleProxy = new RuleProxy();

        // Act
        var target = ruleProxy.Target;

        // Assert
        Assert.IsNull(target);
    }

    [TestMethod]
    public void Target_SetToNullAfterValue_ReturnsNull()
    {
        // Arrange
        var ruleProxy = new RuleProxy();
        var mockTarget = new Mock<IValidateBase>();
        ruleProxy.Target = mockTarget.Object;

        // Assert - verify it's set
        Assert.IsNotNull(ruleProxy.Target);

        // Act
        ruleProxy.Target = null!;

        // Assert
        Assert.IsNull(ruleProxy.Target);
    }

    #endregion
}
