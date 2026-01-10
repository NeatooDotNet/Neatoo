using KnockOff;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Rules;

namespace Neatoo.UnitTest.Unit.Rules;

/// <summary>
/// Unit tests for the RuleProxy class.
/// Tests the simple data container behavior used in the rules system.
/// </summary>
[TestClass]
[KnockOff<IValidateBase>]
public partial class RuleProxyTests
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
        var targetStub = new Stubs.IValidateBase();

        // Act
        ruleProxy.Target = targetStub;

        // Assert
        Assert.AreSame(targetStub, ruleProxy.Target);
    }

    [TestMethod]
    public void Target_SetToNull_ReturnsNull()
    {
        // Arrange
        var ruleProxy = new RuleProxy();
        var targetStub = new Stubs.IValidateBase();
        ruleProxy.Target = targetStub;

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
        var targetStub1 = new Stubs.IValidateBase();
        var targetStub2 = new Stubs.IValidateBase();
        var targetStub3 = new Stubs.IValidateBase();

        // Act
        ruleProxy.Target = targetStub1;
        Assert.AreSame(targetStub1, ruleProxy.Target);

        ruleProxy.Target = targetStub2;
        Assert.AreSame(targetStub2, ruleProxy.Target);

        ruleProxy.Target = targetStub3;

        // Assert
        Assert.AreSame(targetStub3, ruleProxy.Target);
    }

    [TestMethod]
    public void Target_SetSameValueTwice_ReturnsSameValue()
    {
        // Arrange
        var ruleProxy = new RuleProxy();
        var targetStub = new Stubs.IValidateBase();

        // Act
        ruleProxy.Target = targetStub;
        ruleProxy.Target = targetStub;

        // Assert
        Assert.AreSame(targetStub, ruleProxy.Target);
    }

    #endregion

    #region Interface Implementation Tests

    [TestMethod]
    public void Target_AcceptsAnyIValidateBaseImplementation()
    {
        // Arrange
        var ruleProxy = new RuleProxy();
        var targetStub = new Stubs.IValidateBase();

        // Act
        ruleProxy.Target = targetStub;

        // Assert
        Assert.AreSame(targetStub, ruleProxy.Target);
    }

    #endregion

    #region Object Initialization Pattern Tests

    [TestMethod]
    public void ObjectInitializer_WithTarget_SetsTarget()
    {
        // Arrange
        var targetStub = new Stubs.IValidateBase();

        // Act
        var ruleProxy = new RuleProxy
        {
            Target = targetStub
        };

        // Assert
        Assert.AreSame(targetStub, ruleProxy.Target);
    }

    #endregion

    #region Reference Semantics Tests

    [TestMethod]
    public void Target_MaintainsReferenceSemantics()
    {
        // Arrange
        var ruleProxy = new RuleProxy();
        var targetStub = new Stubs.IValidateBase();

        // Act
        ruleProxy.Target = targetStub;
        var retrievedTarget = ruleProxy.Target;

        // Assert - Same reference
        Assert.IsTrue(ReferenceEquals(targetStub, retrievedTarget));
    }

    [TestMethod]
    public void MultipleRuleProxies_CanShareSameTarget()
    {
        // Arrange
        var targetStub = new Stubs.IValidateBase();
        var ruleProxy1 = new RuleProxy();
        var ruleProxy2 = new RuleProxy();

        // Act
        ruleProxy1.Target = targetStub;
        ruleProxy2.Target = targetStub;

        // Assert
        Assert.AreSame(ruleProxy1.Target, ruleProxy2.Target);
    }

    [TestMethod]
    public void MultipleRuleProxies_WithDifferentTargets_MaintainIndependence()
    {
        // Arrange
        var targetStub1 = new Stubs.IValidateBase();
        var targetStub2 = new Stubs.IValidateBase();
        var ruleProxy1 = new RuleProxy();
        var ruleProxy2 = new RuleProxy();

        // Act
        ruleProxy1.Target = targetStub1;
        ruleProxy2.Target = targetStub2;

        // Assert
        Assert.AreNotSame(ruleProxy1.Target, ruleProxy2.Target);
        Assert.AreSame(targetStub1, ruleProxy1.Target);
        Assert.AreSame(targetStub2, ruleProxy2.Target);
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
        var targetStub = new Stubs.IValidateBase();
        ruleProxy.Target = targetStub;

        // Assert - verify it's set
        Assert.IsNotNull(ruleProxy.Target);

        // Act
        ruleProxy.Target = null!;

        // Assert
        Assert.IsNull(ruleProxy.Target);
    }

    #endregion
}
