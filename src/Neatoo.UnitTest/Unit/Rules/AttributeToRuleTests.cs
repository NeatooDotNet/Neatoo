using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Neatoo.UnitTest.Unit.Rules;

#region Test Helper Classes

/// <summary>
/// Mock implementation of IPropertyInfo for testing AttributeToRule.
/// </summary>
public class MockPropertyInfo : IPropertyInfo
{
    public MockPropertyInfo(string name, Type type)
    {
        Name = name;
        Type = type;
        Key = name;
    }

    public PropertyInfo PropertyInfo => null!;
    public string Name { get; }
    public Type Type { get; }
    public string Key { get; }
    public bool IsPrivateSetter => false;

    public T? GetCustomAttribute<T>() where T : Attribute => null;
    public IEnumerable<Attribute> GetCustomAttributes() => Enumerable.Empty<Attribute>();
}

/// <summary>
/// Simple test object that inherits from ValidateBase for testing rule creation.
/// Uses SuppressFactory to avoid requiring the full factory infrastructure.
/// </summary>
[SuppressFactory]
public class TestValidateObject : ValidateBase<TestValidateObject>
{
    public TestValidateObject() : base(new ValidateBaseServices<TestValidateObject>())
    {
    }

    public string? StringProperty { get => Getter<string>(); set => Setter(value); }
    public int IntProperty { get => Getter<int>(); set => Setter(value); }
    public bool BoolProperty { get => Getter<bool>(); set => Setter(value); }
    public DateTime? DateTimeProperty { get => Getter<DateTime?>(); set => Setter(value); }
    public object? ObjectProperty { get => Getter<object?>(); set => Setter(value); }
    public List<string>? ListProperty { get => Getter<List<string>>(); set => Setter(value); }
    public decimal DecimalProperty { get => Getter<decimal>(); set => Setter(value); }
}

/// <summary>
/// Another test object with different properties for testing generic type parameters.
/// </summary>
[SuppressFactory]
public class AnotherTestValidateObject : ValidateBase<AnotherTestValidateObject>
{
    public AnotherTestValidateObject() : base(new ValidateBaseServices<AnotherTestValidateObject>())
    {
    }

    public string? Name { get => Getter<string>(); set => Setter(value); }
    public decimal Amount { get => Getter<decimal>(); set => Setter(value); }
}

#endregion

/// <summary>
/// Comprehensive unit tests for the AttributeToRule class.
/// Tests the conversion of data annotation attributes to Neatoo rules.
/// </summary>
[TestClass]
public class AttributeToRuleTests
{
    private AttributeToRule _attributeToRule = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _attributeToRule = new AttributeToRule();
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_Default_CreatesInstance()
    {
        // Arrange & Act
        var attributeToRule = new AttributeToRule();

        // Assert
        Assert.IsNotNull(attributeToRule);
    }

    [TestMethod]
    public void AttributeToRule_ImplementsIAttributeToRule()
    {
        // Arrange & Act
        var attributeToRule = new AttributeToRule();

        // Assert
        Assert.IsInstanceOfType(attributeToRule, typeof(IAttributeToRule));
    }

    #endregion

    #region RequiredAttribute Tests - Returns RequiredRule

    [TestMethod]
    public void GetRule_RequiredAttribute_ReturnsRequiredRule()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        Assert.IsInstanceOfType(rule, typeof(IRequiredRule));
    }

    [TestMethod]
    public void GetRule_RequiredAttribute_ReturnsIRuleInstance()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        Assert.IsInstanceOfType(rule, typeof(IRule));
    }

    #endregion

    #region Non-RequiredAttribute Tests - Returns Null

    [TestMethod]
    public void GetRule_NullAttribute_ReturnsNull()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, null);

        // Assert
        Assert.IsNull(rule);
    }

    [TestMethod]
    public void GetRule_StringLengthAttribute_ReturnsNull()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var stringLengthAttribute = new StringLengthAttribute(100);

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, stringLengthAttribute);

        // Assert
        Assert.IsNull(rule);
    }

    [TestMethod]
    public void GetRule_RangeAttribute_ReturnsNull()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("IntProperty", typeof(int));
        var rangeAttribute = new RangeAttribute(1, 100);

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, rangeAttribute);

        // Assert
        Assert.IsNull(rule);
    }

    [TestMethod]
    public void GetRule_EmailAddressAttribute_ReturnsNull()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var emailAttribute = new EmailAddressAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, emailAttribute);

        // Assert
        Assert.IsNull(rule);
    }

    [TestMethod]
    public void GetRule_RegularExpressionAttribute_ReturnsNull()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var regexAttribute = new RegularExpressionAttribute(@"^\d+$");

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, regexAttribute);

        // Assert
        Assert.IsNull(rule);
    }

    [TestMethod]
    public void GetRule_MaxLengthAttribute_ReturnsNull()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var maxLengthAttribute = new MaxLengthAttribute(50);

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, maxLengthAttribute);

        // Assert
        Assert.IsNull(rule);
    }

    [TestMethod]
    public void GetRule_MinLengthAttribute_ReturnsNull()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var minLengthAttribute = new MinLengthAttribute(5);

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, minLengthAttribute);

        // Assert
        Assert.IsNull(rule);
    }

    [TestMethod]
    public void GetRule_NonValidationObject_ReturnsNull()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var randomObject = new object();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, randomObject);

        // Assert
        Assert.IsNull(rule);
    }

    [TestMethod]
    public void GetRule_StringObject_ReturnsNull()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var stringObject = "not an attribute";

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, stringObject);

        // Assert
        Assert.IsNull(rule);
    }

    #endregion

    #region Trigger Property Name Tests

    [TestMethod]
    public void GetRule_RequiredAttribute_HasCorrectTriggerPropertyName()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        Assert.AreEqual(1, rule.TriggerProperties.Count);
        Assert.AreEqual("StringProperty", rule.TriggerProperties[0].PropertyName);
    }

    [TestMethod]
    public void GetRule_RequiredAttribute_IntProperty_HasCorrectTriggerPropertyName()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("IntProperty", typeof(int));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        Assert.AreEqual(1, rule.TriggerProperties.Count);
        Assert.AreEqual("IntProperty", rule.TriggerProperties[0].PropertyName);
    }

    [TestMethod]
    public void GetRule_RequiredAttribute_BoolProperty_HasCorrectTriggerPropertyName()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("BoolProperty", typeof(bool));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        Assert.AreEqual(1, rule.TriggerProperties.Count);
        Assert.AreEqual("BoolProperty", rule.TriggerProperties[0].PropertyName);
    }

    [TestMethod]
    public void GetRule_RequiredAttribute_ObjectProperty_HasCorrectTriggerPropertyName()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("ObjectProperty", typeof(object));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        Assert.AreEqual(1, rule.TriggerProperties.Count);
        Assert.AreEqual("ObjectProperty", rule.TriggerProperties[0].PropertyName);
    }

    [TestMethod]
    public void GetRule_RequiredAttribute_ListProperty_HasCorrectTriggerPropertyName()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("ListProperty", typeof(List<string>));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        Assert.AreEqual(1, rule.TriggerProperties.Count);
        Assert.AreEqual("ListProperty", rule.TriggerProperties[0].PropertyName);
    }

    [TestMethod]
    public void GetRule_RequiredAttribute_DateTimeProperty_HasCorrectTriggerPropertyName()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("DateTimeProperty", typeof(DateTime?));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        Assert.AreEqual(1, rule.TriggerProperties.Count);
        Assert.AreEqual("DateTimeProperty", rule.TriggerProperties[0].PropertyName);
    }

    #endregion

    #region Custom Error Message Tests

    [TestMethod]
    public void GetRule_RequiredAttribute_CustomErrorMessage_PreservesMessage()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var customMessage = "This field is mandatory and cannot be empty.";
        var requiredAttribute = new RequiredAttribute { ErrorMessage = customMessage };

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        var requiredRule = rule as IRequiredRule;
        Assert.IsNotNull(requiredRule);
        Assert.AreEqual(customMessage, requiredRule.ErrorMessage);
    }

    [TestMethod]
    public void GetRule_RequiredAttribute_EmptyErrorMessage_UsesEmptyString()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var requiredAttribute = new RequiredAttribute { ErrorMessage = string.Empty };

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        var requiredRule = rule as IRequiredRule;
        Assert.IsNotNull(requiredRule);
        // Empty string is treated as a valid message, not null
        Assert.AreEqual(string.Empty, requiredRule.ErrorMessage);
    }

    [TestMethod]
    public void GetRule_RequiredAttribute_SpecialCharactersInErrorMessage_PreservesMessage()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var specialMessage = "Error: <field> must be set! @#$%^&*()";
        var requiredAttribute = new RequiredAttribute { ErrorMessage = specialMessage };

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        var requiredRule = rule as IRequiredRule;
        Assert.IsNotNull(requiredRule);
        Assert.AreEqual(specialMessage, requiredRule.ErrorMessage);
    }

    [TestMethod]
    public void GetRule_RequiredAttribute_UnicodeErrorMessage_PreservesMessage()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var unicodeMessage = "Campo requerido. Champ requis.";
        var requiredAttribute = new RequiredAttribute { ErrorMessage = unicodeMessage };

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        var requiredRule = rule as IRequiredRule;
        Assert.IsNotNull(requiredRule);
        Assert.AreEqual(unicodeMessage, requiredRule.ErrorMessage);
    }

    #endregion

    #region Default Error Message Tests

    [TestMethod]
    public void GetRule_RequiredAttribute_NoErrorMessage_UsesDefaultMessage()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        var requiredRule = rule as IRequiredRule;
        Assert.IsNotNull(requiredRule);
        Assert.AreEqual("StringProperty is required.", requiredRule.ErrorMessage);
    }

    [TestMethod]
    public void GetRule_RequiredAttribute_NullErrorMessage_UsesDefaultMessage()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("IntProperty", typeof(int));
        var requiredAttribute = new RequiredAttribute { ErrorMessage = null };

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        var requiredRule = rule as IRequiredRule;
        Assert.IsNotNull(requiredRule);
        Assert.AreEqual("IntProperty is required.", requiredRule.ErrorMessage);
    }

    [TestMethod]
    public void GetRule_RequiredAttribute_DefaultMessage_IncludesPropertyName()
    {
        // Arrange - Use an existing property on the test object
        var propertyInfo = new MockPropertyInfo("DecimalProperty", typeof(decimal));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        var requiredRule = rule as IRequiredRule;
        Assert.IsNotNull(requiredRule);
        Assert.IsTrue(requiredRule.ErrorMessage.Contains("DecimalProperty"));
        Assert.AreEqual("DecimalProperty is required.", requiredRule.ErrorMessage);
    }

    #endregion

    #region Different Generic Type Parameters Tests

    [TestMethod]
    public void GetRule_RequiredAttribute_DifferentType_CreatesCorrectRule()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("Name", typeof(string));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<AnotherTestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        Assert.IsInstanceOfType(rule, typeof(IRequiredRule));
        Assert.AreEqual("Name", rule.TriggerProperties[0].PropertyName);
    }

    [TestMethod]
    public void GetRule_RequiredAttribute_DifferentType_DecimalProperty_CreatesCorrectRule()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("Amount", typeof(decimal));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<AnotherTestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        Assert.IsInstanceOfType(rule, typeof(IRequiredRule));
        Assert.AreEqual("Amount", rule.TriggerProperties[0].PropertyName);
    }

    [TestMethod]
    public void GetRule_SameAttributeToDifferentTypes_CreatesSeparateRules()
    {
        // Arrange
        var propertyInfo1 = new MockPropertyInfo("StringProperty", typeof(string));
        var propertyInfo2 = new MockPropertyInfo("Name", typeof(string));
        var requiredAttribute = new RequiredAttribute { ErrorMessage = "Field is required" };

        // Act
        var rule1 = _attributeToRule.GetRule<TestValidateObject>(propertyInfo1, requiredAttribute);
        var rule2 = _attributeToRule.GetRule<AnotherTestValidateObject>(propertyInfo2, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule1);
        Assert.IsNotNull(rule2);
        Assert.AreNotSame(rule1, rule2);
        Assert.AreEqual("StringProperty", rule1.TriggerProperties[0].PropertyName);
        Assert.AreEqual("Name", rule2.TriggerProperties[0].PropertyName);
    }

    #endregion

    #region Multiple Attribute Types Tests

    [TestMethod]
    public void GetRule_MultipleDifferentAttributes_OnlyRequiredReturnsRule()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var requiredAttribute = new RequiredAttribute();
        var stringLengthAttribute = new StringLengthAttribute(100);
        var emailAttribute = new EmailAddressAttribute();

        // Act
        var requiredRule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);
        var stringLengthRule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, stringLengthAttribute);
        var emailRule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, emailAttribute);

        // Assert
        Assert.IsNotNull(requiredRule);
        Assert.IsNull(stringLengthRule);
        Assert.IsNull(emailRule);
    }

    [TestMethod]
    public void GetRule_CompareAttribute_ReturnsNull()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("Password", typeof(string));
        var compareAttribute = new CompareAttribute("ConfirmPassword");

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, compareAttribute);

        // Assert
        Assert.IsNull(rule);
    }

    [TestMethod]
    public void GetRule_PhoneAttribute_ReturnsNull()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("PhoneNumber", typeof(string));
        var phoneAttribute = new PhoneAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, phoneAttribute);

        // Assert
        Assert.IsNull(rule);
    }

    [TestMethod]
    public void GetRule_UrlAttribute_ReturnsNull()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("Website", typeof(string));
        var urlAttribute = new UrlAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, urlAttribute);

        // Assert
        Assert.IsNull(rule);
    }

    [TestMethod]
    public void GetRule_CreditCardAttribute_ReturnsNull()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("CreditCardNumber", typeof(string));
        var creditCardAttribute = new CreditCardAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, creditCardAttribute);

        // Assert
        Assert.IsNull(rule);
    }

    #endregion

    #region Edge Cases Tests

    [TestMethod]
    public void GetRule_NonExistentProperty_ThrowsArgumentException()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("NonExistentProperty", typeof(string));
        var requiredAttribute = new RequiredAttribute();

        // Act & Assert
        // The AttributeToRule.GetRule method uses Expression.Property which requires
        // the property to exist on the type T
        Assert.ThrowsException<ArgumentException>(() =>
            _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute));
    }

    [TestMethod]
    public void GetRule_VeryLongErrorMessage_PreservesEntireMessage()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var longMessage = new string('X', 1000) + " This is a very long error message for testing purposes.";
        var requiredAttribute = new RequiredAttribute { ErrorMessage = longMessage };

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        var requiredRule = rule as IRequiredRule;
        Assert.IsNotNull(requiredRule);
        Assert.AreEqual(longMessage, requiredRule.ErrorMessage);
    }

    #endregion

    #region Interface Usage Tests

    [TestMethod]
    public void GetRule_ViaInterface_ReturnsCorrectRule()
    {
        // Arrange
        IAttributeToRule attributeToRule = new AttributeToRule();
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        Assert.IsInstanceOfType(rule, typeof(IRequiredRule));
    }

    [TestMethod]
    public void GetRule_ViaInterface_UnsupportedAttribute_ReturnsNull()
    {
        // Arrange
        IAttributeToRule attributeToRule = new AttributeToRule();
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var stringLengthAttribute = new StringLengthAttribute(100);

        // Act
        var rule = attributeToRule.GetRule<TestValidateObject>(propertyInfo, stringLengthAttribute);

        // Assert
        Assert.IsNull(rule);
    }

    #endregion

    #region Consistency Tests

    [TestMethod]
    public void GetRule_SameInputs_ProducesSameOutputProperties()
    {
        // Arrange - Use existing property on TestValidateObject
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var requiredAttribute = new RequiredAttribute { ErrorMessage = "Test message" };

        // Act
        var rule1 = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);
        var rule2 = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule1);
        Assert.IsNotNull(rule2);
        Assert.AreEqual(rule1.TriggerProperties[0].PropertyName, rule2.TriggerProperties[0].PropertyName);
        Assert.AreEqual(((IRequiredRule)rule1).ErrorMessage, ((IRequiredRule)rule2).ErrorMessage);
    }

    [TestMethod]
    public void GetRule_NewInstances_ProducesSameResults()
    {
        // Arrange - Use existing property on TestValidateObject
        var attributeToRule1 = new AttributeToRule();
        var attributeToRule2 = new AttributeToRule();
        var propertyInfo = new MockPropertyInfo("IntProperty", typeof(int));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule1 = attributeToRule1.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);
        var rule2 = attributeToRule2.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule1);
        Assert.IsNotNull(rule2);
        Assert.AreEqual(rule1.TriggerProperties[0].PropertyName, rule2.TriggerProperties[0].PropertyName);
    }

    #endregion

    #region Rule Properties Tests

    [TestMethod]
    public void GetRule_RequiredAttribute_RuleHasSingleTriggerProperty()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        Assert.AreEqual(1, rule.TriggerProperties.Count);
    }

    [TestMethod]
    public void GetRule_RequiredAttribute_RuleNotYetExecuted()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        Assert.IsFalse(rule.Executed);
    }

    [TestMethod]
    public void GetRule_RequiredAttribute_RuleMessagesInitiallyEmpty()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        Assert.AreEqual(0, rule.Messages.Count);
    }

    #endregion

    #region TriggerProperty Match Tests

    [TestMethod]
    public void GetRule_RequiredAttribute_TriggerPropertyMatchesPropertyName()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        var triggerProperty = rule.TriggerProperties[0];
        Assert.IsTrue(triggerProperty.IsMatch("StringProperty"));
        Assert.IsFalse(triggerProperty.IsMatch("OtherProperty"));
    }

    [TestMethod]
    public void GetRule_RequiredAttribute_TriggerPropertyCaseSensitive()
    {
        // Arrange
        var propertyInfo = new MockPropertyInfo("StringProperty", typeof(string));
        var requiredAttribute = new RequiredAttribute();

        // Act
        var rule = _attributeToRule.GetRule<TestValidateObject>(propertyInfo, requiredAttribute);

        // Assert
        Assert.IsNotNull(rule);
        var triggerProperty = rule.TriggerProperties[0];
        Assert.IsFalse(triggerProperty.IsMatch("stringproperty"));
        Assert.IsFalse(triggerProperty.IsMatch("STRINGPROPERTY"));
    }

    #endregion
}
