using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Unit.Rules;

#region Test Helper Classes

/// <summary>
/// A real ValidateBase implementation for RequiredRule testing.
/// Uses SuppressFactory to avoid requiring the full factory infrastructure.
/// Properties use the Getter/Setter pattern to integrate with the Neatoo property system.
/// </summary>
[SuppressFactory]
public class RequiredRuleTestTarget : ValidateBase<RequiredRuleTestTarget>
{
    public RequiredRuleTestTarget() : base(new ValidateBaseServices<RequiredRuleTestTarget>())
    {
    }

    public string? StringProperty { get => Getter<string>(); set => Setter(value); }
    public int IntProperty { get => Getter<int>(); set => Setter(value); }
    public int? NullableIntProperty { get => Getter<int?>(); set => Setter(value); }
    public double DoubleProperty { get => Getter<double>(); set => Setter(value); }
    public bool BoolProperty { get => Getter<bool>(); set => Setter(value); }
    public DateTime DateTimeProperty { get => Getter<DateTime>(); set => Setter(value); }
    public Guid GuidProperty { get => Getter<Guid>(); set => Setter(value); }
    public object? ObjectProperty { get => Getter<object?>(); set => Setter(value); }
    public List<string>? ListProperty { get => Getter<List<string>>(); set => Setter(value); }
}

/// <summary>
/// Helper class for creating RequiredRule instances with proper property type information.
/// </summary>
internal static class RequiredRuleTestHelper
{
    /// <summary>
    /// Creates a RequiredRule with the property type inferred from the trigger property expression.
    /// </summary>
    public static RequiredRule<T> CreateRequiredRule<T>(
        TriggerProperty<T> triggerProperty,
        RequiredAttribute requiredAttribute) where T : class, IValidateBase
    {
        // Get the property type from the expression
        var propertyType = typeof(T).GetProperty(triggerProperty.PropertyName)?.PropertyType
            ?? typeof(object);
        return new RequiredRule<T>(triggerProperty, requiredAttribute, propertyType);
    }
}

#endregion

#region Constructor Tests

/// <summary>
/// Tests for RequiredRule constructor behavior.
/// </summary>
[TestClass]
public class RequiredRuleConstructorTests
{
    [TestMethod]
    public void Constructor_WithTriggerPropertyAndRequiredAttribute_AddsTriggerProperty()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, requiredAttribute);

        // Assert
        Assert.AreEqual(1, ((IRule)rule).TriggerProperties.Count);
        Assert.AreEqual("StringProperty", ((IRule)rule).TriggerProperties[0].PropertyName);
    }

    [TestMethod]
    public void Constructor_WithRequiredAttributeNoErrorMessage_UsesDefaultErrorMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, requiredAttribute);

        // Assert
        Assert.AreEqual("StringProperty is required.", rule.ErrorMessage);
    }

    [TestMethod]
    public void Constructor_WithRequiredAttributeCustomErrorMessage_UsesCustomErrorMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var requiredAttribute = new RequiredAttribute { ErrorMessage = "Please provide a string value." };

        // Act
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, requiredAttribute);

        // Assert
        Assert.AreEqual("Please provide a string value.", rule.ErrorMessage);
    }

    [TestMethod]
    public void Constructor_WithDifferentPropertyNames_GeneratesCorrectDefaultMessage()
    {
        // Arrange
        var triggerPropertyInt = new TriggerProperty<RequiredRuleTestTarget>(t => t.IntProperty);
        var triggerPropertyNullable = new TriggerProperty<RequiredRuleTestTarget>(t => t.NullableIntProperty);
        var triggerPropertyObject = new TriggerProperty<RequiredRuleTestTarget>(t => t.ObjectProperty);

        // Act
        var ruleInt = RequiredRuleTestHelper.CreateRequiredRule(triggerPropertyInt, new RequiredAttribute());
        var ruleNullable = RequiredRuleTestHelper.CreateRequiredRule(triggerPropertyNullable, new RequiredAttribute());
        var ruleObject = RequiredRuleTestHelper.CreateRequiredRule(triggerPropertyObject, new RequiredAttribute());

        // Assert
        Assert.AreEqual("IntProperty is required.", ruleInt.ErrorMessage);
        Assert.AreEqual("NullableIntProperty is required.", ruleNullable.ErrorMessage);
        Assert.AreEqual("ObjectProperty is required.", ruleObject.ErrorMessage);
    }
}

#endregion

#region String Value Validation Tests

/// <summary>
/// Tests for RequiredRule validation of string values.
/// </summary>
[TestClass]
public class RequiredRuleStringValidationTests
{
    private RequiredRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RequiredRuleTestTarget();
        _target.PauseAllActions(); // Prevent auto-rule execution during test setup
    }

    [TestMethod]
    public async Task RunRule_NullString_ReturnsErrorMessage()
    {
        // Arrange
        _target.StringProperty = null;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("StringProperty", result[0].PropertyName);
        Assert.AreEqual("StringProperty is required.", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_EmptyString_ReturnsErrorMessage()
    {
        // Arrange
        _target.StringProperty = string.Empty;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("StringProperty is required.", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_WhitespaceOnlyString_ReturnsErrorMessage()
    {
        // Arrange
        _target.StringProperty = "   ";

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("StringProperty is required.", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_TabAndNewlineString_ReturnsErrorMessage()
    {
        // Arrange
        _target.StringProperty = "\t\n\r  ";

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ValidString_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = "Valid Value";

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_SingleCharacterString_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = "A";

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_StringWithLeadingWhitespace_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = "  Valid";

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }
}

#endregion

#region Value Type Validation Tests

/// <summary>
/// Tests for RequiredRule validation of value types (int, double, bool, DateTime, Guid).
/// The RequiredRule compares value types against their default values using Equals.
/// </summary>
[TestClass]
public class RequiredRuleValueTypeValidationTests
{
    private RequiredRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RequiredRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_IntDefaultValue_ReturnsErrorMessage()
    {
        // Arrange
        _target.IntProperty = 0; // default value for int

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.IntProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        // When using real ValidateBase, the property system properly handles value types
        // and the RequiredRule can detect default values correctly.
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("IntProperty is required.", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_IntNonDefaultValue_ReturnsNoMessages()
    {
        // Arrange
        _target.IntProperty = 42;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.IntProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_IntNegativeValue_ReturnsNoMessages()
    {
        // Arrange
        _target.IntProperty = -1;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.IntProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_DoubleDefaultValue_ReturnsErrorMessage()
    {
        // Arrange
        _target.DoubleProperty = 0.0; // default value for double

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.DoubleProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("DoubleProperty is required.", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_DoubleNonDefaultValue_ReturnsNoMessages()
    {
        // Arrange
        _target.DoubleProperty = 3.14;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.DoubleProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_BoolDefaultValue_ReturnsErrorMessage()
    {
        // Arrange
        _target.BoolProperty = false; // default value for bool

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.BoolProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("BoolProperty is required.", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_BoolTrue_ReturnsNoMessages()
    {
        // Arrange
        _target.BoolProperty = true;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.BoolProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_DateTimeDefaultValue_ReturnsErrorMessage()
    {
        // Arrange
        _target.DateTimeProperty = default; // DateTime.MinValue

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.DateTimeProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("DateTimeProperty is required.", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_DateTimeWithValue_ReturnsNoMessages()
    {
        // Arrange
        _target.DateTimeProperty = DateTime.Now;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.DateTimeProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_GuidDefaultValue_ReturnsErrorMessage()
    {
        // Arrange
        _target.GuidProperty = Guid.Empty; // default value for Guid

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.GuidProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("GuidProperty is required.", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_GuidWithValue_ReturnsNoMessages()
    {
        // Arrange
        _target.GuidProperty = Guid.NewGuid();

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.GuidProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }
}

#endregion

#region Nullable Value Type Validation Tests

/// <summary>
/// Tests for RequiredRule validation of nullable value types.
/// </summary>
[TestClass]
public class RequiredRuleNullableValueTypeValidationTests
{
    private RequiredRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RequiredRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_NullableIntNull_ReturnsErrorMessage()
    {
        // Arrange
        _target.NullableIntProperty = null;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.NullableIntProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("NullableIntProperty is required.", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_NullableIntDefaultValue_ReturnsNoMessages()
    {
        // Arrange
        _target.NullableIntProperty = 0;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.NullableIntProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        // For nullable types, only null is considered "not set"
        // A value of 0 means the user explicitly chose zero, which is a valid value
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_NullableIntWithValue_ReturnsNoMessages()
    {
        // Arrange
        _target.NullableIntProperty = 42;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.NullableIntProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }
}

#endregion

#region Object/Reference Type Validation Tests

/// <summary>
/// Tests for RequiredRule validation of object/reference types.
/// </summary>
[TestClass]
public class RequiredRuleObjectValidationTests
{
    private RequiredRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RequiredRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_NullObject_ReturnsErrorMessage()
    {
        // Arrange
        _target.ObjectProperty = null;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.ObjectProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("ObjectProperty is required.", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_NonNullObject_ReturnsNoMessages()
    {
        // Arrange
        _target.ObjectProperty = new object();

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.ObjectProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_NullList_ReturnsErrorMessage()
    {
        // Arrange
        _target.ListProperty = null;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.ListProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmptyList_ReturnsNoMessages()
    {
        // Arrange
        _target.ListProperty = new List<string>(); // Empty but not null

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.ListProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert - RequiredRule only checks for null, not empty collection
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ListWithItems_ReturnsNoMessages()
    {
        // Arrange
        _target.ListProperty = new List<string> { "Item1", "Item2" };

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.ListProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }
}

#endregion

#region Custom Error Message Tests

/// <summary>
/// Tests for RequiredRule with custom error messages.
/// </summary>
[TestClass]
public class RequiredRuleCustomErrorMessageTests
{
    private RequiredRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RequiredRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_WithCustomErrorMessage_ReturnsCustomMessage()
    {
        // Arrange
        _target.StringProperty = null;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var requiredAttribute = new RequiredAttribute
        {
            ErrorMessage = "The string value cannot be empty!"
        };
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, requiredAttribute);

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("The string value cannot be empty!", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_WithCustomErrorMessageOnNullValue_ReturnsCustomMessage()
    {
        // Arrange
        _target.ObjectProperty = null;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.ObjectProperty);
        var requiredAttribute = new RequiredAttribute
        {
            ErrorMessage = "Please enter a valid ObjectProperty value."
        };
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, requiredAttribute);

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Please enter a valid ObjectProperty value.", result[0].Message);
    }

    [TestMethod]
    public void ErrorMessage_Property_ReturnsCustomMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var customMessage = "Custom validation message";
        var requiredAttribute = new RequiredAttribute { ErrorMessage = customMessage };

        // Act
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, requiredAttribute);

        // Assert
        Assert.AreEqual(customMessage, rule.ErrorMessage);
    }

    [TestMethod]
    public void ErrorMessage_Property_ReturnsDefaultMessageWhenNotCustomized()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, requiredAttribute);

        // Assert
        Assert.AreEqual("StringProperty is required.", rule.ErrorMessage);
    }
}

#endregion

#region IRequiredRule Interface Tests

/// <summary>
/// Tests for IRequiredRule interface implementation.
/// </summary>
[TestClass]
public class RequiredRuleInterfaceTests
{
    [TestMethod]
    public void RequiredRule_ImplementsIRequiredRule()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IRequiredRule));
    }

    [TestMethod]
    public void RequiredRule_ImplementsIRule()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IRule));
    }

    [TestMethod]
    public void IRequiredRule_ErrorMessage_ReturnsConfiguredMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var requiredAttribute = new RequiredAttribute { ErrorMessage = "Test Error" };
        IRequiredRule rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, requiredAttribute);

        // Act
        var errorMessage = rule.ErrorMessage;

        // Assert
        Assert.AreEqual("Test Error", errorMessage);
    }
}

#endregion

#region TriggerProperties Tests

/// <summary>
/// Tests for RequiredRule TriggerProperties behavior.
/// </summary>
[TestClass]
public class RequiredRuleTriggerPropertiesTests
{
    [TestMethod]
    public void TriggerProperties_ContainsCorrectProperty()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var properties = ((IRule)rule).TriggerProperties;

        // Assert
        Assert.AreEqual(1, properties.Count);
        Assert.AreEqual("StringProperty", properties[0].PropertyName);
    }

    [TestMethod]
    public void TriggerProperties_IsMatch_ReturnsTrue_ForMatchingPropertyName()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var isMatch = ((IRule)rule).TriggerProperties[0].IsMatch("StringProperty");

        // Assert
        Assert.IsTrue(isMatch);
    }

    [TestMethod]
    public void TriggerProperties_IsMatch_ReturnsFalse_ForNonMatchingPropertyName()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var isMatch = ((IRule)rule).TriggerProperties[0].IsMatch("IntProperty");

        // Assert
        Assert.IsFalse(isMatch);
    }

    [TestMethod]
    public void TriggerProperties_IsReadOnly()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var properties = ((IRule)rule).TriggerProperties;

        // Assert
        Assert.IsInstanceOfType(properties, typeof(IReadOnlyList<ITriggerProperty>));
    }
}

#endregion

#region Executed Flag Tests

/// <summary>
/// Tests for RequiredRule Executed flag behavior.
/// </summary>
[TestClass]
public class RequiredRuleExecutedFlagTests
{
    private RequiredRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RequiredRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public void Executed_InitiallyFalse()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Assert
        Assert.IsFalse(rule.Executed);
    }

    [TestMethod]
    public async Task Executed_TrueAfterRunRule()
    {
        // Arrange
        _target.StringProperty = "Valid";

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        await rule.RunRule(_target);

        // Assert
        Assert.IsTrue(rule.Executed);
    }

    [TestMethod]
    public async Task Executed_TrueAfterRunRule_EvenWhenValidationFails()
    {
        // Arrange
        _target.StringProperty = null;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        await rule.RunRule(_target);

        // Assert
        Assert.IsTrue(rule.Executed);
    }

    [TestMethod]
    public async Task Executed_RemainsTrueAfterMultipleExecutions()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        _target.StringProperty = null;
        await rule.RunRule(_target);
        _target.StringProperty = "Valid";
        await rule.RunRule(_target);
        _target.StringProperty = "";
        await rule.RunRule(_target);

        // Assert
        Assert.IsTrue(rule.Executed);
    }
}

#endregion

#region RuleMessages.None Tests

/// <summary>
/// Tests verifying RuleMessages.None is returned for valid values.
/// </summary>
[TestClass]
public class RequiredRuleRuleMessagesNoneTests
{
    private RequiredRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RequiredRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_ValidString_ReturnsEmptyRuleMessages()
    {
        // Arrange
        _target.StringProperty = "Valid";

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ValidInt_ReturnsEmptyRuleMessages()
    {
        // Arrange
        _target.IntProperty = 1;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.IntProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ValidObject_ReturnsEmptyRuleMessages()
    {
        // Arrange
        _target.ObjectProperty = new { Value = 1 };

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.ObjectProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }
}

#endregion

#region Edge Cases and Special Scenarios

/// <summary>
/// Tests for edge cases and special scenarios in RequiredRule.
/// </summary>
[TestClass]
public class RequiredRuleEdgeCasesTests
{
    private RequiredRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RequiredRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_MultipleExecutions_ReturnCorrectResultsEachTime()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act & Assert - First execution with null
        _target.StringProperty = null;
        var result1 = await rule.RunRule(_target);
        Assert.AreEqual(1, result1.Count);

        // Act & Assert - Second execution with valid value
        _target.StringProperty = "Valid";
        var result2 = await rule.RunRule(_target);
        Assert.AreEqual(0, result2.Count);

        // Act & Assert - Third execution with empty string
        _target.StringProperty = "";
        var result3 = await rule.RunRule(_target);
        Assert.AreEqual(1, result3.Count);
    }

    [TestMethod]
    public async Task RunRule_ThroughIRuleInterface_WorksCorrectly()
    {
        // Arrange
        _target.StringProperty = null;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        IRule rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void UniqueIndex_DefaultsToZero()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Assert
        Assert.AreEqual(0u, rule.UniqueIndex);
    }

    [TestMethod]
    public void RuleOrder_DefaultsToOne()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Assert
        Assert.AreEqual(1, rule.RuleOrder);
    }

    [TestMethod]
    public void Messages_BeforeExecution_ReturnsEmptyList()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var messages = rule.Messages;

        // Assert
        Assert.IsNotNull(messages);
        Assert.AreEqual(0, messages.Count);
    }
}

#endregion

#region RuleMessage Property Name Tests

/// <summary>
/// Tests verifying that the PropertyName in RuleMessage matches the trigger property.
/// </summary>
[TestClass]
public class RequiredRulePropertyNameInMessageTests
{
    private RequiredRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RequiredRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_ErrorMessage_ContainsCorrectPropertyName_StringProperty()
    {
        // Arrange
        _target.StringProperty = null;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.StringProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual("StringProperty", result[0].PropertyName);
    }

    [TestMethod]
    public async Task RunRule_ErrorMessage_ContainsCorrectPropertyName_NullableIntProperty()
    {
        // Arrange
        _target.NullableIntProperty = null;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.NullableIntProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual("NullableIntProperty", result[0].PropertyName);
    }

    [TestMethod]
    public async Task RunRule_ErrorMessage_ContainsCorrectPropertyName_ObjectProperty()
    {
        // Arrange
        _target.ObjectProperty = null;

        var triggerProperty = new TriggerProperty<RequiredRuleTestTarget>(t => t.ObjectProperty);
        var rule = RequiredRuleTestHelper.CreateRequiredRule(triggerProperty, new RequiredAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual("ObjectProperty", result[0].PropertyName);
    }
}

#endregion
