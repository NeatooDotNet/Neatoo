using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using Neatoo.Internal;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// Unit tests for the PropertyMessage record class.
/// Tests construction, equality, properties, and edge cases.
/// Uses real Property&lt;T&gt; and PropertyInfoWrapper instances instead of mocks.
/// </summary>
[TestClass]
public class PropertyMessageTests
{
    /// <summary>
    /// Simple test POCO class used to create real Property instances.
    /// </summary>
    private class TestPoco
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    private IValidateProperty _property = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Name))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        _property = new ValidateProperty<string>(wrapper);
    }

    /// <summary>
    /// Helper method to create a ValidateProperty instance from a TestPoco property.
    /// </summary>
    private static ValidateProperty<T> CreateProperty<T>(string propertyName)
    {
        var propertyInfo = typeof(TestPoco).GetProperty(propertyName)!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        return new ValidateProperty<T>(wrapper);
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithPropertyAndMessage_SetsPropertyCorrectly()
    {
        // Arrange
        var expectedProperty = _property;
        var message = "Test message";

        // Act
        var propertyMessage = new PropertyMessage(expectedProperty, message);

        // Assert
        Assert.AreSame(expectedProperty, propertyMessage.Property);
    }

    [TestMethod]
    public void Constructor_WithPropertyAndMessage_SetsMessageCorrectly()
    {
        // Arrange
        var property = _property;
        var expectedMessage = "Expected error message";

        // Act
        var propertyMessage = new PropertyMessage(property, expectedMessage);

        // Assert
        Assert.AreEqual(expectedMessage, propertyMessage.Message);
    }

    [TestMethod]
    public void Constructor_WithNullMessage_SetsMessageToNull()
    {
        // Arrange
        var property = _property;

        // Act
        var propertyMessage = new PropertyMessage(property, null!);

        // Assert
        Assert.IsNull(propertyMessage.Message);
    }

    [TestMethod]
    public void Constructor_WithEmptyMessage_SetsMessageToEmpty()
    {
        // Arrange
        var property = _property;

        // Act
        var propertyMessage = new PropertyMessage(property, string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, propertyMessage.Message);
    }

    [TestMethod]
    public void Constructor_WithWhitespaceMessage_PreservesWhitespace()
    {
        // Arrange
        var property = _property;
        var whitespaceMessage = "   ";

        // Act
        var propertyMessage = new PropertyMessage(property, whitespaceMessage);

        // Assert
        Assert.AreEqual(whitespaceMessage, propertyMessage.Message);
    }

    #endregion

    #region Property Accessor Tests

    [TestMethod]
    public void Property_Get_ReturnsIPropertyInstance()
    {
        // Arrange
        var property = _property;
        var propertyMessage = new PropertyMessage(property, "Message");

        // Act
        var result = propertyMessage.Property;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IValidateProperty));
    }

    [TestMethod]
    public void Property_Set_UpdatesProperty()
    {
        // Arrange
        var originalProperty = _property;
        var newProperty = CreateProperty<string>(nameof(TestPoco.Description));
        var propertyMessage = new PropertyMessage(originalProperty, "Message");

        // Act
        propertyMessage.Property = newProperty;

        // Assert
        Assert.AreSame(newProperty, propertyMessage.Property);
    }

    [TestMethod]
    public void Message_Get_ReturnsMessageString()
    {
        // Arrange
        var property = _property;
        var expectedMessage = "Validation error message";
        var propertyMessage = new PropertyMessage(property, expectedMessage);

        // Act
        var result = propertyMessage.Message;

        // Assert
        Assert.AreEqual(expectedMessage, result);
    }

    [TestMethod]
    public void Message_Set_UpdatesMessage()
    {
        // Arrange
        var property = _property;
        var propertyMessage = new PropertyMessage(property, "Original message");
        var newMessage = "Updated message";

        // Act
        propertyMessage.Message = newMessage;

        // Assert
        Assert.AreEqual(newMessage, propertyMessage.Message);
    }

    #endregion

    #region Record Equality Tests

    [TestMethod]
    public void Equals_SamePropertyAndMessage_ReturnsTrue()
    {
        // Arrange
        var property = _property;
        var message = "Same message";
        var propertyMessage1 = new PropertyMessage(property, message);
        var propertyMessage2 = new PropertyMessage(property, message);

        // Act
        var result = propertyMessage1.Equals(propertyMessage2);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Equals_DifferentMessage_ReturnsFalse()
    {
        // Arrange
        var property = _property;
        var propertyMessage1 = new PropertyMessage(property, "Message 1");
        var propertyMessage2 = new PropertyMessage(property, "Message 2");

        // Act
        var result = propertyMessage1.Equals(propertyMessage2);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Equals_DifferentProperty_ReturnsFalse()
    {
        // Arrange
        var property1 = _property;
        var property2 = CreateProperty<string>(nameof(TestPoco.Description));
        var message = "Same message";
        var propertyMessage1 = new PropertyMessage(property1, message);
        var propertyMessage2 = new PropertyMessage(property2, message);

        // Act
        var result = propertyMessage1.Equals(propertyMessage2);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Equals_BothPropertiesNullSameMessage_ReturnsTrue()
    {
        // Arrange
        var message = "Same message";
        var propertyMessage1 = new PropertyMessage(null!, message);
        var propertyMessage2 = new PropertyMessage(null!, message);

        // Act
        var result = propertyMessage1.Equals(propertyMessage2);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Equals_OnePropertyNullOneNot_ReturnsFalse()
    {
        // Arrange
        var property = _property;
        var message = "Same message";
        var propertyMessage1 = new PropertyMessage(property, message);
        var propertyMessage2 = new PropertyMessage(null!, message);

        // Act
        var result = propertyMessage1.Equals(propertyMessage2);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Equals_BothMessagesNullSameProperty_ReturnsTrue()
    {
        // Arrange
        var property = _property;
        var propertyMessage1 = new PropertyMessage(property, null!);
        var propertyMessage2 = new PropertyMessage(property, null!);

        // Act
        var result = propertyMessage1.Equals(propertyMessage2);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Equals_NullObject_ReturnsFalse()
    {
        // Arrange
        var property = _property;
        var propertyMessage = new PropertyMessage(property, "Message");

        // Act
        var result = propertyMessage.Equals(null);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void EqualsOperator_SamePropertyAndMessage_ReturnsTrue()
    {
        // Arrange
        var property = _property;
        var message = "Same message";
        var propertyMessage1 = new PropertyMessage(property, message);
        var propertyMessage2 = new PropertyMessage(property, message);

        // Act
        var result = propertyMessage1 == propertyMessage2;

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void NotEqualsOperator_DifferentMessage_ReturnsTrue()
    {
        // Arrange
        var property = _property;
        var propertyMessage1 = new PropertyMessage(property, "Message 1");
        var propertyMessage2 = new PropertyMessage(property, "Message 2");

        // Act
        var result = propertyMessage1 != propertyMessage2;

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void EqualsOperator_BothNull_ReturnsTrue()
    {
        // Arrange
        PropertyMessage? propertyMessage1 = null;
        PropertyMessage? propertyMessage2 = null;

        // Act
        var result = propertyMessage1 == propertyMessage2;

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void EqualsOperator_OneNull_ReturnsFalse()
    {
        // Arrange
        var property = _property;
        PropertyMessage? propertyMessage1 = new PropertyMessage(property, "Message");
        PropertyMessage? propertyMessage2 = null;

        // Act
        var result = propertyMessage1 == propertyMessage2;

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region GetHashCode Tests

    [TestMethod]
    public void GetHashCode_SamePropertyAndMessage_ReturnsSameHashCode()
    {
        // Arrange
        var property = _property;
        var message = "Same message";
        var propertyMessage1 = new PropertyMessage(property, message);
        var propertyMessage2 = new PropertyMessage(property, message);

        // Act
        var hashCode1 = propertyMessage1.GetHashCode();
        var hashCode2 = propertyMessage2.GetHashCode();

        // Assert
        Assert.AreEqual(hashCode1, hashCode2);
    }

    [TestMethod]
    public void GetHashCode_DifferentMessage_ReturnsDifferentHashCode()
    {
        // Arrange
        var property = _property;
        var propertyMessage1 = new PropertyMessage(property, "Message 1");
        var propertyMessage2 = new PropertyMessage(property, "Message 2");

        // Act
        var hashCode1 = propertyMessage1.GetHashCode();
        var hashCode2 = propertyMessage2.GetHashCode();

        // Assert
        // Note: Different values should typically have different hash codes,
        // though this is not guaranteed
        Assert.AreNotEqual(hashCode1, hashCode2);
    }

    [TestMethod]
    public void GetHashCode_DifferentProperty_ReturnsDifferentHashCode()
    {
        // Arrange
        var property1 = _property;
        var property2 = CreateProperty<string>(nameof(TestPoco.Description));
        var message = "Same message";
        var propertyMessage1 = new PropertyMessage(property1, message);
        var propertyMessage2 = new PropertyMessage(property2, message);

        // Act
        var hashCode1 = propertyMessage1.GetHashCode();
        var hashCode2 = propertyMessage2.GetHashCode();

        // Assert
        Assert.AreNotEqual(hashCode1, hashCode2);
    }

    #endregion

    #region ToString Tests

    [TestMethod]
    public void ToString_ReturnsRecordStringRepresentation()
    {
        // Arrange
        var property = _property;
        var message = "Test message";
        var propertyMessage = new PropertyMessage(property, message);

        // Act
        var result = propertyMessage.ToString();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("PropertyMessage"));
        Assert.IsTrue(result.Contains("Test message"));
    }

    [TestMethod]
    public void ToString_WithNullMessage_DoesNotThrow()
    {
        // Arrange
        var property = _property;
        var propertyMessage = new PropertyMessage(property, null!);

        // Act
        var result = propertyMessage.ToString();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("PropertyMessage"));
    }

    #endregion

    #region IPropertyMessage Interface Tests

    [TestMethod]
    public void PropertyMessage_ImplementsIPropertyMessage()
    {
        // Arrange & Act
        var property = _property;
        var propertyMessage = new PropertyMessage(property, "Message");

        // Assert
        Assert.IsInstanceOfType(propertyMessage, typeof(IPropertyMessage));
    }

    [TestMethod]
    public void IPropertyMessage_Property_ReturnsCorrectValue()
    {
        // Arrange
        var property = _property;
        IPropertyMessage propertyMessage = new PropertyMessage(property, "Message");

        // Act
        var result = propertyMessage.Property;

        // Assert
        Assert.AreSame(property, result);
    }

    [TestMethod]
    public void IPropertyMessage_Message_ReturnsCorrectValue()
    {
        // Arrange
        var property = _property;
        var expectedMessage = "Test message via interface";
        IPropertyMessage propertyMessage = new PropertyMessage(property, expectedMessage);

        // Act
        var result = propertyMessage.Message;

        // Assert
        Assert.AreEqual(expectedMessage, result);
    }

    [TestMethod]
    public void IPropertyMessage_PropertySet_UpdatesProperty()
    {
        // Arrange
        var originalProperty = _property;
        var newProperty = CreateProperty<string>(nameof(TestPoco.Description));
        IPropertyMessage propertyMessage = new PropertyMessage(originalProperty, "Message");

        // Act
        propertyMessage.Property = newProperty;

        // Assert
        Assert.AreSame(newProperty, propertyMessage.Property);
    }

    [TestMethod]
    public void IPropertyMessage_MessageSet_UpdatesMessage()
    {
        // Arrange
        var property = _property;
        IPropertyMessage propertyMessage = new PropertyMessage(property, "Original");
        var newMessage = "Updated via interface";

        // Act
        propertyMessage.Message = newMessage;

        // Assert
        Assert.AreEqual(newMessage, propertyMessage.Message);
    }

    #endregion

    #region Different IProperty Instances With Same Message Tests

    [TestMethod]
    public void Equality_DifferentIPropertyInstancesSameMessage_ReturnsFalse()
    {
        // Arrange
        // Create two separate Property instances for the same underlying property
        var property1 = CreateProperty<string>(nameof(TestPoco.Name));
        var property2 = CreateProperty<string>(nameof(TestPoco.Name));
        var message = "Same error message";
        var propertyMessage1 = new PropertyMessage(property1, message);
        var propertyMessage2 = new PropertyMessage(property2, message);

        // Act
        var result = propertyMessage1.Equals(propertyMessage2);

        // Assert
        // Different IProperty instances should not be equal even with same message
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Equality_SameIPropertyInstanceSameMessage_ReturnsTrue()
    {
        // Arrange
        var sharedProperty = _property;
        var message = "Shared error message";
        var propertyMessage1 = new PropertyMessage(sharedProperty, message);
        var propertyMessage2 = new PropertyMessage(sharedProperty, message);

        // Act
        var result = propertyMessage1.Equals(propertyMessage2);

        // Assert
        Assert.IsTrue(result);
    }

    #endregion

    #region Record With Expression Tests

    [TestMethod]
    public void WithExpression_CreatesCopyWithModifiedProperty()
    {
        // Arrange
        var originalProperty = _property;
        var newProperty = CreateProperty<string>(nameof(TestPoco.Description));
        var message = "Original message";
        var original = new PropertyMessage(originalProperty, message);

        // Act
        var modified = original with { Property = newProperty };

        // Assert
        Assert.AreSame(newProperty, modified.Property);
        Assert.AreEqual(message, modified.Message);
        Assert.AreNotSame(original, modified);
    }

    [TestMethod]
    public void WithExpression_CreatesCopyWithModifiedMessage()
    {
        // Arrange
        var property = _property;
        var originalMessage = "Original message";
        var newMessage = "Modified message";
        var original = new PropertyMessage(property, originalMessage);

        // Act
        var modified = original with { Message = newMessage };

        // Assert
        Assert.AreSame(property, modified.Property);
        Assert.AreEqual(newMessage, modified.Message);
        Assert.AreNotSame(original, modified);
    }

    [TestMethod]
    public void WithExpression_OriginalRemainsUnchanged()
    {
        // Arrange
        var property = _property;
        var originalMessage = "Original message";
        var original = new PropertyMessage(property, originalMessage);

        // Act
        _ = original with { Message = "Modified message" };

        // Assert
        Assert.AreEqual(originalMessage, original.Message);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void PropertyMessage_WithVeryLongMessage_HandlesCorrectly()
    {
        // Arrange
        var property = _property;
        var longMessage = new string('A', 10000);

        // Act
        var propertyMessage = new PropertyMessage(property, longMessage);

        // Assert
        Assert.AreEqual(longMessage, propertyMessage.Message);
        Assert.AreEqual(10000, propertyMessage.Message.Length);
    }

    [TestMethod]
    public void PropertyMessage_WithSpecialCharactersInMessage_HandlesCorrectly()
    {
        // Arrange
        var property = _property;
        var specialMessage = "Message with special chars: !@#$%^&*()_+{}|:<>?\n\t\r";

        // Act
        var propertyMessage = new PropertyMessage(property, specialMessage);

        // Assert
        Assert.AreEqual(specialMessage, propertyMessage.Message);
    }

    [TestMethod]
    public void PropertyMessage_WithUnicodeMessage_HandlesCorrectly()
    {
        // Arrange
        var property = _property;
        var unicodeMessage = "Unicode test: \u4e2d\u6587 \u65e5\u672c\u8a9e \ud55c\uad6d\uc5b4";

        // Act
        var propertyMessage = new PropertyMessage(property, unicodeMessage);

        // Assert
        Assert.AreEqual(unicodeMessage, propertyMessage.Message);
    }

    [TestMethod]
    public void PropertyMessage_MessageCaseSensitivity_DifferentCaseNotEqual()
    {
        // Arrange
        var property = _property;
        var propertyMessage1 = new PropertyMessage(property, "Message");
        var propertyMessage2 = new PropertyMessage(property, "message");

        // Act
        var result = propertyMessage1.Equals(propertyMessage2);

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region Collection Usage Tests

    [TestMethod]
    public void PropertyMessage_CanBeUsedInHashSet()
    {
        // Arrange
        var property = _property;
        var propertyMessage1 = new PropertyMessage(property, "Message 1");
        var propertyMessage2 = new PropertyMessage(property, "Message 2");
        var propertyMessage3 = new PropertyMessage(property, "Message 1"); // Duplicate of 1

        // Act
        var hashSet = new HashSet<PropertyMessage> { propertyMessage1, propertyMessage2, propertyMessage3 };

        // Assert
        Assert.AreEqual(2, hashSet.Count);
    }

    [TestMethod]
    public void PropertyMessage_CanBeUsedAsDictionaryKey()
    {
        // Arrange
        var property = _property;
        var propertyMessage1 = new PropertyMessage(property, "Key Message");
        var propertyMessage2 = new PropertyMessage(property, "Key Message"); // Same as 1
        var dictionary = new Dictionary<PropertyMessage, int>();

        // Act
        dictionary[propertyMessage1] = 1;
        dictionary[propertyMessage2] = 2; // Should overwrite

        // Assert
        Assert.AreEqual(1, dictionary.Count);
        Assert.AreEqual(2, dictionary[propertyMessage1]);
    }

    #endregion

    #region Real Property Behavior Tests

    [TestMethod]
    public void Property_Name_ReturnsCorrectPropertyName()
    {
        // Arrange
        var property = _property;

        // Act
        var name = property.Name;

        // Assert
        Assert.AreEqual(nameof(TestPoco.Name), name);
    }

    [TestMethod]
    public void Property_WithDifferentTypes_WorksCorrectly()
    {
        // Arrange
        var stringProperty = CreateProperty<string>(nameof(TestPoco.Name));
        var intProperty = CreateProperty<int>(nameof(TestPoco.Age));

        // Act
        var stringPropertyMessage = new PropertyMessage(stringProperty, "String property error");
        var intPropertyMessage = new PropertyMessage(intProperty, "Int property error");

        // Assert
        Assert.AreEqual(nameof(TestPoco.Name), stringPropertyMessage.Property.Name);
        Assert.AreEqual(nameof(TestPoco.Age), intPropertyMessage.Property.Name);
    }

    #endregion
}
