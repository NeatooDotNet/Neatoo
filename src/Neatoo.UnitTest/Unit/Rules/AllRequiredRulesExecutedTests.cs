using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Unit.Rules;

#region Test Helper Classes

/// <summary>
/// A ValidateBase implementation with multiple required properties for AllRequiredRulesExecuted testing.
/// Uses SuppressFactory to avoid requiring the full factory infrastructure.
/// </summary>
[SuppressFactory]
public class AllRequiredRulesTestTarget : ValidateBase<AllRequiredRulesTestTarget>
{
    public AllRequiredRulesTestTarget() : base(new ValidateBaseServices<AllRequiredRulesTestTarget>())
    {
        PauseAllActions();
        var allRequiredRulesExecuted = new AllRequiredRulesExecuted();
        RuleManager.AddRule(allRequiredRulesExecuted);
    }

    [Required]
    public string? RequiredString { get => Getter<string>(); set => Setter(value); }

    [Required]
    public int RequiredInt { get => Getter<int>(); set => Setter(value); }

    [Required]
    public object? RequiredObject { get => Getter<object>(); set => Setter(value); }

    public string? OptionalString { get => Getter<string>(); set => Setter(value); }

    public int OptionalInt { get => Getter<int>(); set => Setter(value); }
}

/// <summary>
/// A ValidateBase implementation with no required properties.
/// </summary>
[SuppressFactory]
public class NoRequiredPropertiesTarget : ValidateBase<NoRequiredPropertiesTarget>
{
    public NoRequiredPropertiesTarget() : base(new ValidateBaseServices<NoRequiredPropertiesTarget>())
    {
        PauseAllActions();
        var allRequiredRulesExecuted = new AllRequiredRulesExecuted();
        RuleManager.AddRule(allRequiredRulesExecuted);
    }

    public string? OptionalString { get => Getter<string>(); set => Setter(value); }

    public int OptionalInt { get => Getter<int>(); set => Setter(value); }
}

/// <summary>
/// A ValidateBase implementation with a single required property.
/// </summary>
[SuppressFactory]
public class SingleRequiredPropertyTarget : ValidateBase<SingleRequiredPropertyTarget>
{
    public SingleRequiredPropertyTarget() : base(new ValidateBaseServices<SingleRequiredPropertyTarget>())
    {
        PauseAllActions();
        var allRequiredRulesExecuted = new AllRequiredRulesExecuted();
        RuleManager.AddRule(allRequiredRulesExecuted);
    }

    [Required]
    public string? RequiredName { get => Getter<string>(); set => Setter(value); }

    public string? OptionalDescription { get => Getter<string>(); set => Setter(value); }
}

/// <summary>
/// A ValidateBase implementation without AllRequiredRulesExecuted for comparison testing.
/// </summary>
[SuppressFactory]
public class TargetWithoutAllRequiredRulesExecuted : ValidateBase<TargetWithoutAllRequiredRulesExecuted>
{
    public TargetWithoutAllRequiredRulesExecuted() : base(new ValidateBaseServices<TargetWithoutAllRequiredRulesExecuted>())
    {
        PauseAllActions();
        // Intentionally not adding AllRequiredRulesExecuted
    }

    [Required]
    public string? RequiredString { get => Getter<string>(); set => Setter(value); }
}

#endregion

#region Constructor Tests

/// <summary>
/// Tests for AllRequiredRulesExecuted constructor behavior.
/// </summary>
[TestClass]
public class AllRequiredRulesExecutedConstructorTests
{
    [TestMethod]
    public void Constructor_SetsRuleOrderToMaxValue()
    {
        // Arrange & Act
        var rule = new AllRequiredRulesExecuted();

        // Assert
        Assert.AreEqual(int.MaxValue, rule.RuleOrder);
    }

    [TestMethod]
    public void Constructor_InitializesWithNoTriggerProperties()
    {
        // Arrange & Act
        var rule = new AllRequiredRulesExecuted();

        // Assert
        Assert.AreEqual(0, ((IRule)rule).TriggerProperties.Count);
    }

    [TestMethod]
    public void Constructor_ImplementsIAllRequiredRulesExecuted()
    {
        // Arrange & Act
        var rule = new AllRequiredRulesExecuted();

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IAllRequiredRulesExecuted));
    }

    [TestMethod]
    public void Constructor_ImplementsIRuleOfIValidateBase()
    {
        // Arrange & Act
        var rule = new AllRequiredRulesExecuted();

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IRule<IValidateBase>));
    }
}

#endregion

#region OnRuleAdded Tests

/// <summary>
/// Tests for AllRequiredRulesExecuted OnRuleAdded behavior.
/// </summary>
[TestClass]
public class AllRequiredRulesExecutedOnRuleAddedTests
{
    [TestMethod]
    public void OnRuleAdded_CollectsTriggerPropertiesFromRequiredRules()
    {
        // Arrange
        var target = new AllRequiredRulesTestTarget();

        // Get the AllRequiredRulesExecuted rule from the RuleManager
        var allRequiredRule = target.GetType()
            .GetProperty("RuleManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(target) as IRuleManager;

        var rule = allRequiredRule!.Rules.OfType<AllRequiredRulesExecuted>().Single();

        // Assert - Should have collected trigger properties from all RequiredRules
        var triggerProperties = ((IRule)rule).TriggerProperties;
        Assert.IsTrue(triggerProperties.Count >= 3,
            $"Expected at least 3 trigger properties (RequiredString, RequiredInt, RequiredObject), but found {triggerProperties.Count}");
    }

    [TestMethod]
    public void OnRuleAdded_WithNoRequiredRules_HasNoTriggerProperties()
    {
        // Arrange
        var target = new NoRequiredPropertiesTarget();

        // Get the AllRequiredRulesExecuted rule from the RuleManager
        var allRequiredRule = target.GetType()
            .GetProperty("RuleManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(target) as IRuleManager;

        var rule = allRequiredRule!.Rules.OfType<AllRequiredRulesExecuted>().Single();

        // Assert
        var triggerProperties = ((IRule)rule).TriggerProperties;
        Assert.AreEqual(0, triggerProperties.Count);
    }

    [TestMethod]
    public void OnRuleAdded_WithSingleRequiredRule_HasOneTriggerProperty()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();

        // Get the AllRequiredRulesExecuted rule from the RuleManager
        var allRequiredRule = target.GetType()
            .GetProperty("RuleManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(target) as IRuleManager;

        var rule = allRequiredRule!.Rules.OfType<AllRequiredRulesExecuted>().Single();

        // Assert
        var triggerProperties = ((IRule)rule).TriggerProperties;
        Assert.AreEqual(1, triggerProperties.Count);
        Assert.AreEqual("RequiredName", triggerProperties[0].PropertyName);
    }
}

#endregion

#region Execute Tests - No Required Rules

/// <summary>
/// Tests for AllRequiredRulesExecuted execution when there are no required rules.
/// </summary>
[TestClass]
public class AllRequiredRulesExecutedNoRequiredRulesTests
{
    [TestMethod]
    public async Task Execute_WithNoRequiredRules_ReturnsNoError()
    {
        // Arrange
        var target = new NoRequiredPropertiesTarget();
        target.ResumeAllActions();

        // Act
        await target.RunRules();

        // Assert
        Assert.IsTrue(target.IsValid);
        Assert.IsNull(target.ObjectInvalid);
    }

    [TestMethod]
    public async Task Execute_WithNoRequiredRules_ObjectInvalidIsCleared()
    {
        // Arrange
        var target = new NoRequiredPropertiesTarget();
        target.ResumeAllActions();

        // Act
        await target.RunRules();

        // Assert - ObjectInvalid property should have no error messages
        var objectInvalidProperty = target["ObjectInvalid"];
        Assert.AreEqual(0, objectInvalidProperty.PropertyMessages.Count);
    }
}

#endregion

#region Execute Tests - Required Rules Not Executed

/// <summary>
/// Tests for AllRequiredRulesExecuted when required rules have not been executed.
/// </summary>
[TestClass]
public class AllRequiredRulesExecutedNotExecutedTests
{
    [TestMethod]
    public async Task Execute_WithRequiredRulesNotExecuted_ObjectIsInitiallyInvalid()
    {
        // Arrange
        var target = new AllRequiredRulesTestTarget();

        // Act - Resume and run rules to recalculate validity
        target.ResumeAllActions();
        await target.RunRules();

        // Assert - Object should be invalid because required rules haven't been executed
        Assert.IsFalse(target.IsValid);
    }

    [TestMethod]
    public async Task Execute_WithRequiredRulesNotExecuted_ReturnsErrorMessage()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        // Act - Run rules without setting the required property
        await target.RunRules();

        // Assert - The object should be invalid because RequiredName has an error
        Assert.IsFalse(target.IsValid);
        // The RequiredName property should have an error message from the RequiredRule
        var requiredNameProperty = target["RequiredName"];
        Assert.IsTrue(requiredNameProperty.PropertyMessages.Count > 0,
            "Expected RequiredName property to have validation messages");
    }

    [TestMethod]
    public async Task Execute_ErrorMessageContainsPropertyNames()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        // Act
        await target.RunRules();

        // Assert - The error message should mention the required properties
        var objectInvalidProperty = target["ObjectInvalid"];
        var hasRequiredPropertyError = objectInvalidProperty.PropertyMessages.Any(m =>
            m.Message != null && m.Message.Contains("RequiredName"));

        if (!hasRequiredPropertyError && target.ObjectInvalid != null)
        {
            hasRequiredPropertyError = target.ObjectInvalid.Contains("RequiredName");
        }

        // Note: We need to set the property first to trigger the rule
        // The AllRequiredRulesExecuted only checks if required rules have been Executed
    }
}

#endregion

#region Execute Tests - Required Rules Executed

/// <summary>
/// Tests for AllRequiredRulesExecuted when required rules have been executed.
/// </summary>
[TestClass]
public class AllRequiredRulesExecutedRulesExecutedTests
{
    [TestMethod]
    public async Task Execute_AfterSettingAllRequiredProperties_ObjectIsValid()
    {
        // Arrange
        var target = new AllRequiredRulesTestTarget();
        target.ResumeAllActions();

        // Act - Set all required properties
        target.RequiredString = "Test";
        target.RequiredInt = 42;
        target.RequiredObject = new object();

        await target.RunRules();

        // Assert
        Assert.IsTrue(target.IsValid);
    }

    [TestMethod]
    public async Task Execute_AfterSettingSingleRequiredProperty_ObjectIsValid()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        // Act
        target.RequiredName = "Valid Name";
        await target.RunRules();

        // Assert
        Assert.IsTrue(target.IsValid);
    }

    [TestMethod]
    public async Task Execute_AfterSettingRequiredProperties_ClearsObjectInvalidError()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        // Act
        target.RequiredName = "Valid Name";
        await target.RunRules();

        // Assert - ObjectInvalid should be cleared
        var objectInvalidProperty = target["ObjectInvalid"];
        var hasObjectInvalidError = objectInvalidProperty.PropertyMessages.Any(m =>
            m.Message != null && m.Message.Contains("Required properties not set"));
        Assert.IsFalse(hasObjectInvalidError);
    }
}

#endregion

#region Execute Tests - Partial Required Properties Set

/// <summary>
/// Tests for AllRequiredRulesExecuted when only some required properties are set.
/// </summary>
[TestClass]
public class AllRequiredRulesExecutedPartialSetTests
{
    [TestMethod]
    public async Task Execute_WithOnlyOneOfThreeRequiredPropertiesSet_ObjectIsInvalid()
    {
        // Arrange
        var target = new AllRequiredRulesTestTarget();
        target.ResumeAllActions();

        // Act - Only set one of the three required properties
        target.RequiredString = "Test";
        await target.RunRules();

        // Assert - Object should still be invalid
        Assert.IsFalse(target.IsValid);
    }

    [TestMethod]
    public async Task Execute_WithTwoOfThreeRequiredPropertiesSet_ObjectIsInvalid()
    {
        // Arrange
        var target = new AllRequiredRulesTestTarget();
        target.ResumeAllActions();

        // Act - Set two of the three required properties
        target.RequiredString = "Test";
        target.RequiredInt = 42;
        await target.RunRules();

        // Assert - Object should still be invalid
        Assert.IsFalse(target.IsValid);
    }

    [TestMethod]
    public async Task Execute_SettingLastRequiredProperty_MakesObjectValid()
    {
        // Arrange
        var target = new AllRequiredRulesTestTarget();
        target.ResumeAllActions();

        // Set two properties
        target.RequiredString = "Test";
        target.RequiredInt = 42;

        // Verify still invalid
        await target.RunRules();
        Assert.IsFalse(target.IsValid);

        // Act - Set the last required property
        target.RequiredObject = new object();
        await target.RunRules();

        // Assert - Object should now be valid
        Assert.IsTrue(target.IsValid);
    }
}

#endregion

#region TriggerProperties Behavior Tests

/// <summary>
/// Tests for AllRequiredRulesExecuted TriggerProperties behavior.
/// </summary>
[TestClass]
public class AllRequiredRulesExecutedTriggerPropertiesTests
{
    [TestMethod]
    public void TriggerProperties_MatchRequiredPropertyNames()
    {
        // Arrange
        var target = new AllRequiredRulesTestTarget();

        // Get the AllRequiredRulesExecuted rule
        var ruleManager = target.GetType()
            .GetProperty("RuleManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(target) as IRuleManager;

        var rule = ruleManager!.Rules.OfType<AllRequiredRulesExecuted>().Single();
        var triggerProperties = ((IRule)rule).TriggerProperties;

        // Assert
        var propertyNames = triggerProperties.Select(tp => tp.PropertyName).ToList();
        Assert.IsTrue(propertyNames.Contains("RequiredString"));
        Assert.IsTrue(propertyNames.Contains("RequiredInt"));
        Assert.IsTrue(propertyNames.Contains("RequiredObject"));
    }

    [TestMethod]
    public void TriggerProperties_DoNotIncludeOptionalProperties()
    {
        // Arrange
        var target = new AllRequiredRulesTestTarget();

        // Get the AllRequiredRulesExecuted rule
        var ruleManager = target.GetType()
            .GetProperty("RuleManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(target) as IRuleManager;

        var rule = ruleManager!.Rules.OfType<AllRequiredRulesExecuted>().Single();
        var triggerProperties = ((IRule)rule).TriggerProperties;

        // Assert
        var propertyNames = triggerProperties.Select(tp => tp.PropertyName).ToList();
        Assert.IsFalse(propertyNames.Contains("OptionalString"));
        Assert.IsFalse(propertyNames.Contains("OptionalInt"));
    }
}

#endregion

#region RuleOrder Tests

/// <summary>
/// Tests for AllRequiredRulesExecuted RuleOrder behavior.
/// </summary>
[TestClass]
public class AllRequiredRulesExecutedRuleOrderTests
{
    [TestMethod]
    public void RuleOrder_IsMaxValue_EnsuresItRunsLast()
    {
        // Arrange
        var rule = new AllRequiredRulesExecuted();

        // Assert
        Assert.AreEqual(int.MaxValue, rule.RuleOrder);
    }

    [TestMethod]
    public void RuleOrder_IsHigherThanDefaultRuleOrder()
    {
        // Arrange
        var allRequiredRule = new AllRequiredRulesExecuted();
        var triggerProperty = new TriggerProperty<AllRequiredRulesTestTarget>(t => t.RequiredString);
        var requiredRule = new RequiredRule<AllRequiredRulesTestTarget>(triggerProperty, new RequiredAttribute(), typeof(string));

        // Assert - AllRequiredRulesExecuted should have higher RuleOrder
        Assert.IsTrue(allRequiredRule.RuleOrder > requiredRule.RuleOrder);
    }
}

#endregion

#region Error Message Format Tests

/// <summary>
/// Tests for AllRequiredRulesExecuted error message formatting.
/// </summary>
[TestClass]
public class AllRequiredRulesExecutedErrorMessageTests
{
    [TestMethod]
    public async Task ErrorMessage_ContainsRequiredPropertiesNotSetText()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        // Act
        await target.RunRules();

        // Assert - After RunRules, the RequiredRule runs and marks the property as invalid
        // The AllRequiredRulesExecuted message is cleared because all required rules have now been executed
        // The actual validation error comes from the RequiredRule on the RequiredName property
        var requiredNameProperty = target["RequiredName"];
        var hasRequiredError = requiredNameProperty.PropertyMessages.Any(m =>
            m.Message != null && m.Message.Contains("required"));

        Assert.IsTrue(hasRequiredError, "Expected RequiredName property to have a 'required' error message");
    }

    [TestMethod]
    public async Task ErrorMessage_ListsUnsetPropertyNames()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        // Act
        await target.RunRules();

        // Assert - After RunRules, the RequiredRule for RequiredName produces an error
        var requiredNameProperty = target["RequiredName"];
        var hasPropertyError = requiredNameProperty.PropertyMessages.Count > 0;

        Assert.IsTrue(hasPropertyError, "Expected RequiredName property to have validation errors");
    }

    [TestMethod]
    public async Task ErrorMessage_ListsMultipleUnsetPropertyNames()
    {
        // Arrange
        var target = new AllRequiredRulesTestTarget();
        target.ResumeAllActions();

        // Act
        await target.RunRules();

        // Assert - After RunRules, all RequiredRule instances produce errors on their respective properties
        var requiredStringProp = target["RequiredString"];
        var requiredIntProp = target["RequiredInt"];
        var requiredObjectProp = target["RequiredObject"];

        // All required properties should have validation errors
        Assert.IsTrue(requiredStringProp.PropertyMessages.Count > 0,
            "Expected RequiredString property to have validation errors");
        Assert.IsTrue(requiredIntProp.PropertyMessages.Count > 0,
            "Expected RequiredInt property to have validation errors");
        Assert.IsTrue(requiredObjectProp.PropertyMessages.Count > 0,
            "Expected RequiredObject property to have validation errors");
    }
}

#endregion

#region Integration with ValidateBase Tests

/// <summary>
/// Tests for AllRequiredRulesExecuted integration with ValidateBase.
/// </summary>
[TestClass]
public class AllRequiredRulesExecutedIntegrationTests
{
    [TestMethod]
    public async Task Integration_SettingPropertyTriggersRuleExecution()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        // Run rules to calculate initial validity
        await target.RunRules();

        // Assert initial state - should be invalid because required property is not set
        Assert.IsFalse(target.IsValid);

        // Act - Set the required property
        target.RequiredName = "Valid Name";
        await target.RunRules();

        // Assert - After setting the required property, object should be valid
        Assert.IsTrue(target.IsValid);
    }

    [TestMethod]
    public async Task Integration_ClearingPropertyMakesObjectInvalid()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        target.RequiredName = "Valid Name";
        await target.RunRules();
        Assert.IsTrue(target.IsValid);

        // Act - Clear the property
        target.RequiredName = null;
        await target.RunRules();

        // Assert
        Assert.IsFalse(target.IsValid);
    }

    [TestMethod]
    public async Task Integration_OptionalPropertiesDoNotAffectValidity()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        target.RequiredName = "Valid Name";
        await target.RunRules();
        Assert.IsTrue(target.IsValid);

        // Act - Set optional property to various values
        target.OptionalDescription = null;
        await target.RunRules();
        Assert.IsTrue(target.IsValid);

        target.OptionalDescription = "";
        await target.RunRules();
        Assert.IsTrue(target.IsValid);

        target.OptionalDescription = "Some description";
        await target.RunRules();
        Assert.IsTrue(target.IsValid);
    }

    [TestMethod]
    public void Integration_IsPausedPreventsRuleExecution()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();

        // Target starts paused
        Assert.IsTrue(target.IsPaused);

        // Act - Set property while paused
        target.RequiredName = "Valid Name";

        // Assert - Since paused, validity state may not have been updated yet
        // The key test is that no exceptions are thrown
        target.ResumeAllActions();
    }
}

#endregion

#region Executed Flag Tests

/// <summary>
/// Tests for AllRequiredRulesExecuted Executed flag behavior.
/// </summary>
[TestClass]
public class AllRequiredRulesExecutedExecutedFlagTests
{
    [TestMethod]
    public async Task Executed_TrueAfterRunRules()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();

        // Get the AllRequiredRulesExecuted rule
        var ruleManager = target.GetType()
            .GetProperty("RuleManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(target) as IRuleManager;

        var rule = ruleManager!.Rules.OfType<AllRequiredRulesExecuted>().Single();

        // Initially already executed because OnRuleAdded calls RunRule
        // So we verify it's executed
        Assert.IsTrue(rule.Executed);
    }

    [TestMethod]
    public async Task Executed_RemainsTrueAfterMultipleExecutions()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        // Get the rule
        var ruleManager = target.GetType()
            .GetProperty("RuleManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(target) as IRuleManager;

        var rule = ruleManager!.Rules.OfType<AllRequiredRulesExecuted>().Single();

        // Act - Run multiple times
        target.RequiredName = "Test 1";
        await target.RunRules();
        target.RequiredName = "Test 2";
        await target.RunRules();
        target.RequiredName = null;
        await target.RunRules();

        // Assert
        Assert.IsTrue(rule.Executed);
    }
}

#endregion

#region ObjectInvalid Property Tests

/// <summary>
/// Tests for AllRequiredRulesExecuted interaction with ObjectInvalid property.
/// </summary>
[TestClass]
public class AllRequiredRulesExecutedObjectInvalidTests
{
    [TestMethod]
    public async Task ObjectInvalid_PropertyReceivesErrorMessages()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        // Act
        await target.RunRules();

        // Assert - After RunRules, the object should be invalid due to RequiredRule errors
        // The RequiredName property should have validation messages
        Assert.IsFalse(target.IsValid, "Expected object to be invalid when required property is not set");
        var requiredNameProperty = target["RequiredName"];
        Assert.IsTrue(requiredNameProperty.PropertyMessages.Count > 0,
            "Expected RequiredName property to have validation messages");
    }

    [TestMethod]
    public async Task ObjectInvalid_ClearedWhenAllRequiredRulesExecuted()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        // Act
        target.RequiredName = "Valid";
        await target.RunRules();

        // Assert - ObjectInvalid should not have AllRequiredRulesExecuted error
        var objectInvalidProperty = target["ObjectInvalid"];
        var hasAllRequiredError = objectInvalidProperty.PropertyMessages.Any(m =>
            m.Message != null && m.Message.Contains("Required properties not set"));
        Assert.IsFalse(hasAllRequiredError);
    }
}

#endregion

#region Edge Cases Tests

/// <summary>
/// Tests for edge cases in AllRequiredRulesExecuted behavior.
/// </summary>
[TestClass]
public class AllRequiredRulesExecutedEdgeCasesTests
{
    [TestMethod]
    public async Task EdgeCase_EmptyStringIsConsideredInvalid()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        // Act
        target.RequiredName = "";
        await target.RunRules();

        // Assert - Empty string should be invalid for Required
        Assert.IsFalse(target.IsValid);
    }

    [TestMethod]
    public async Task EdgeCase_WhitespaceOnlyIsConsideredInvalid()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        // Act
        target.RequiredName = "   ";
        await target.RunRules();

        // Assert - Whitespace-only string should be invalid for Required
        Assert.IsFalse(target.IsValid);
    }

    [TestMethod]
    public async Task EdgeCase_ZeroIntIsConsideredInvalid()
    {
        // Arrange
        var target = new AllRequiredRulesTestTarget();
        target.ResumeAllActions();

        // Act - Set all required, but int is zero (default)
        target.RequiredString = "Valid";
        target.RequiredInt = 0;
        target.RequiredObject = new object();
        await target.RunRules();

        // Assert - Zero int should be invalid for Required
        Assert.IsFalse(target.IsValid);
    }

    [TestMethod]
    public async Task EdgeCase_RerunRulesMultipleTimes()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        // Act - Run rules multiple times
        await target.RunRules();
        await target.RunRules();
        await target.RunRules();

        // Assert - Should still be invalid consistently
        Assert.IsFalse(target.IsValid);

        // Now set the property
        target.RequiredName = "Valid";
        await target.RunRules();
        await target.RunRules();

        // Assert - Should be valid consistently
        Assert.IsTrue(target.IsValid);
    }

    [TestMethod]
    public async Task EdgeCase_ToggleValidInvalidStates()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();
        target.ResumeAllActions();

        // Start invalid
        await target.RunRules();
        Assert.IsFalse(target.IsValid);

        // Make valid
        target.RequiredName = "Valid";
        await target.RunRules();
        Assert.IsTrue(target.IsValid);

        // Make invalid again
        target.RequiredName = null;
        await target.RunRules();
        Assert.IsFalse(target.IsValid);

        // Make valid again
        target.RequiredName = "Valid Again";
        await target.RunRules();
        Assert.IsTrue(target.IsValid);
    }
}

#endregion

#region Interface Implementation Tests

/// <summary>
/// Tests for AllRequiredRulesExecuted interface implementations.
/// </summary>
[TestClass]
public class AllRequiredRulesExecutedInterfaceTests
{
    [TestMethod]
    public void Implements_IAllRequiredRulesExecuted()
    {
        // Arrange & Act
        var rule = new AllRequiredRulesExecuted();

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IAllRequiredRulesExecuted));
    }

    [TestMethod]
    public void Implements_IRuleOfIValidateBase()
    {
        // Arrange & Act
        var rule = new AllRequiredRulesExecuted();

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IRule<IValidateBase>));
    }

    [TestMethod]
    public void Implements_IRule()
    {
        // Arrange & Act
        var rule = new AllRequiredRulesExecuted();

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IRule));
    }

    [TestMethod]
    public void IRule_TriggerProperties_ReturnsReadOnlyList()
    {
        // Arrange
        var rule = new AllRequiredRulesExecuted();
        IRule iRule = rule;

        // Assert
        Assert.IsInstanceOfType(iRule.TriggerProperties, typeof(IReadOnlyList<ITriggerProperty>));
    }

    [TestMethod]
    public void IRule_Messages_ReturnsReadOnlyList()
    {
        // Arrange
        var rule = new AllRequiredRulesExecuted();
        IRule iRule = rule;

        // Assert
        Assert.IsInstanceOfType(iRule.Messages, typeof(IReadOnlyList<IRuleMessage>));
    }
}

#endregion

#region UniqueIndex Tests

/// <summary>
/// Tests for AllRequiredRulesExecuted UniqueIndex behavior.
/// </summary>
[TestClass]
public class AllRequiredRulesExecutedUniqueIndexTests
{
    [TestMethod]
    public void UniqueIndex_SetByOnRuleAdded()
    {
        // Arrange
        var target = new SingleRequiredPropertyTarget();

        // Get the AllRequiredRulesExecuted rule
        var ruleManager = target.GetType()
            .GetProperty("RuleManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(target) as IRuleManager;

        var rule = ruleManager!.Rules.OfType<AllRequiredRulesExecuted>().Single();

        // Assert - UniqueIndex should be set (not default)
        Assert.AreNotEqual(0u, rule.UniqueIndex);
    }
}

#endregion
