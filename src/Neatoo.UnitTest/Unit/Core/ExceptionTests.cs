using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using Neatoo.Internal;
using Neatoo.Rules;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// Unit tests for custom exception classes in the Neatoo project.
/// Tests construction, message preservation, inner exception handling, and inheritance.
/// </summary>
[TestClass]
public class ExceptionTests
{
    #region PropertyMissingException Tests

    [TestMethod]
    public void PropertyMissingException_DefaultConstructor_CreatesInstance()
    {
        // Arrange & Act
        var exception = new PropertyMissingException();

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    [TestMethod]
    public void PropertyMissingException_MessageConstructor_PreservesMessage()
    {
        // Arrange
        const string expectedMessage = "Property 'Name' is missing from the object.";

        // Act
        var exception = new PropertyMissingException(expectedMessage);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
    }

    [TestMethod]
    public void PropertyMissingException_MessageAndInnerExceptionConstructor_PreservesBoth()
    {
        // Arrange
        const string expectedMessage = "Property 'Name' is missing from the object.";
        var innerException = new InvalidOperationException("Inner exception message");

        // Act
        var exception = new PropertyMissingException(expectedMessage, innerException);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
    }

    [TestMethod]
    public void PropertyMissingException_InheritsFromPropertyException_True()
    {
        // Arrange & Act
        var exception = new PropertyMissingException();

        // Assert
        Assert.IsTrue(exception is PropertyException);
        Assert.IsTrue(exception is NeatooException);
        Assert.AreEqual(typeof(PropertyException), typeof(PropertyMissingException).BaseType);
    }

    [TestMethod]
    public void PropertyMissingException_HasSerializableAttribute_True()
    {
        // Arrange & Act
        var attributes = typeof(PropertyMissingException).GetCustomAttributes(typeof(SerializableAttribute), false);

        // Assert
        Assert.AreEqual(1, attributes.Length);
    }

    #endregion

    #region PropertyTypeMismatchException Tests

    [TestMethod]
    public void PropertyTypeMismatchException_DefaultConstructor_CreatesInstance()
    {
        // Arrange & Act
        var exception = new PropertyTypeMismatchException();

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    [TestMethod]
    public void PropertyTypeMismatchException_MessageConstructor_PreservesMessage()
    {
        // Arrange
        const string expectedMessage = "Expected type 'String' but got 'Int32'.";

        // Act
        var exception = new PropertyTypeMismatchException(expectedMessage);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
    }

    [TestMethod]
    public void PropertyTypeMismatchException_MessageAndInnerExceptionConstructor_PreservesBoth()
    {
        // Arrange
        const string expectedMessage = "Expected type 'String' but got 'Int32'.";
        var innerException = new ArgumentException("Invalid argument");

        // Act
        var exception = new PropertyTypeMismatchException(expectedMessage, innerException);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
    }

    [TestMethod]
    public void PropertyTypeMismatchException_InheritsFromPropertyException_True()
    {
        // Arrange & Act
        var exception = new PropertyTypeMismatchException();

        // Assert
        Assert.IsTrue(exception is PropertyException);
        Assert.IsTrue(exception is NeatooException);
        Assert.AreEqual(typeof(PropertyException), typeof(PropertyTypeMismatchException).BaseType);
    }

    [TestMethod]
    public void PropertyTypeMismatchException_HasSerializableAttribute_True()
    {
        // Arrange & Act
        var attributes = typeof(PropertyTypeMismatchException).GetCustomAttributes(typeof(SerializableAttribute), false);

        // Assert
        Assert.AreEqual(1, attributes.Length);
    }

    #endregion

    #region PropertyNotFoundException Tests

    [TestMethod]
    public void PropertyNotFoundException_DefaultConstructor_CreatesInstance()
    {
        // Arrange & Act
        var exception = new PropertyNotFoundException();

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    [TestMethod]
    public void PropertyNotFoundException_MessageConstructor_PreservesMessage()
    {
        // Arrange
        const string expectedMessage = "Property 'FirstName' not found in 'PersonEntity'.";

        // Act
        var exception = new PropertyNotFoundException(expectedMessage);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
    }

    [TestMethod]
    public void PropertyNotFoundException_MessageAndInnerExceptionConstructor_PreservesBoth()
    {
        // Arrange
        const string expectedMessage = "Property 'FirstName' not found in 'PersonEntity'.";
        var innerException = new KeyNotFoundException("Key not found");

        // Act
        var exception = new PropertyNotFoundException(expectedMessage, innerException);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
    }

    [TestMethod]
    public void PropertyNotFoundException_InheritsFromPropertyException_True()
    {
        // Arrange & Act
        var exception = new PropertyNotFoundException();

        // Assert
        Assert.IsTrue(exception is PropertyException);
        Assert.IsTrue(exception is NeatooException);
        Assert.AreEqual(typeof(PropertyException), typeof(PropertyNotFoundException).BaseType);
    }

    [TestMethod]
    public void PropertyNotFoundException_HasSerializableAttribute_True()
    {
        // Arrange & Act
        var attributes = typeof(PropertyNotFoundException).GetCustomAttributes(typeof(SerializableAttribute), false);

        // Assert
        Assert.AreEqual(1, attributes.Length);
    }

    #endregion

    #region GlobalFactoryException Tests

    [TestMethod]
    public void GlobalFactoryException_DefaultConstructor_CreatesInstance()
    {
        // Arrange & Act
        var exception = new GlobalFactoryException();

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    [TestMethod]
    public void GlobalFactoryException_MessageConstructor_PreservesMessage()
    {
        // Arrange
        const string expectedMessage = "Failed to create factory for type 'PersonEntity'.";

        // Act
        var exception = new GlobalFactoryException(expectedMessage);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
    }

    [TestMethod]
    public void GlobalFactoryException_MessageAndInnerExceptionConstructor_PreservesBoth()
    {
        // Arrange
        const string expectedMessage = "Failed to create factory for type 'PersonEntity'.";
        var innerException = new TypeLoadException("Type could not be loaded");

        // Act
        var exception = new GlobalFactoryException(expectedMessage, innerException);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
    }

    [TestMethod]
    public void GlobalFactoryException_InheritsFromConfigurationException_True()
    {
        // Arrange & Act
        var exception = new GlobalFactoryException();

        // Assert
        Assert.IsTrue(exception is ConfigurationException);
        Assert.IsTrue(exception is NeatooException);
        Assert.AreEqual(typeof(ConfigurationException), typeof(GlobalFactoryException).BaseType);
    }

    [TestMethod]
    public void GlobalFactoryException_HasSerializableAttribute_True()
    {
        // Arrange & Act
        var attributes = typeof(GlobalFactoryException).GetCustomAttributes(typeof(SerializableAttribute), false);

        // Assert
        Assert.AreEqual(1, attributes.Length);
    }

    #endregion

    #region PropertyInfoEntityChildDataWrongTypeException Tests

    [TestMethod]
    public void PropertyInfoEntityChildDataWrongTypeException_DefaultConstructor_CreatesInstance()
    {
        // Arrange & Act
        var exception = new PropertyInfoEntityChildDataWrongTypeException();

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    [TestMethod]
    public void PropertyInfoEntityChildDataWrongTypeException_MessageConstructor_PreservesMessage()
    {
        // Arrange
        const string expectedMessage = "Entity child data has wrong type for property 'Child'.";

        // Act
        var exception = new PropertyInfoEntityChildDataWrongTypeException(expectedMessage);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
    }

    [TestMethod]
    public void PropertyInfoEntityChildDataWrongTypeException_MessageAndInnerExceptionConstructor_PreservesBoth()
    {
        // Arrange
        const string expectedMessage = "Entity child data has wrong type for property 'Child'.";
        var innerException = new InvalidCastException("Cannot cast to target type");

        // Act
        var exception = new PropertyInfoEntityChildDataWrongTypeException(expectedMessage, innerException);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
    }

    [TestMethod]
    public void PropertyInfoEntityChildDataWrongTypeException_InheritsFromPropertyException_True()
    {
        // Arrange & Act
        var exception = new PropertyInfoEntityChildDataWrongTypeException();

        // Assert
        Assert.IsTrue(exception is PropertyException);
        Assert.IsTrue(exception is NeatooException);
        Assert.AreEqual(typeof(PropertyException), typeof(PropertyInfoEntityChildDataWrongTypeException).BaseType);
    }

    [TestMethod]
    public void PropertyInfoEntityChildDataWrongTypeException_HasSerializableAttribute_True()
    {
        // Arrange & Act
        var attributes = typeof(PropertyInfoEntityChildDataWrongTypeException).GetCustomAttributes(typeof(SerializableAttribute), false);

        // Assert
        Assert.AreEqual(1, attributes.Length);
    }

    #endregion

    #region PropertyValidateChildDataWrongTypeException Tests

    [TestMethod]
    public void PropertyValidateChildDataWrongTypeException_DefaultConstructor_CreatesInstance()
    {
        // Arrange & Act
        var exception = new PropertyValidateChildDataWrongTypeException();

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    [TestMethod]
    public void PropertyValidateChildDataWrongTypeException_MessageConstructor_PreservesMessage()
    {
        // Arrange
        const string expectedMessage = "Validate child data has wrong type for property 'Child'.";

        // Act
        var exception = new PropertyValidateChildDataWrongTypeException(expectedMessage);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
    }

    [TestMethod]
    public void PropertyValidateChildDataWrongTypeException_MessageAndInnerExceptionConstructor_PreservesBoth()
    {
        // Arrange
        const string expectedMessage = "Validate child data has wrong type for property 'Child'.";
        var innerException = new InvalidCastException("Cannot cast to target type");

        // Act
        var exception = new PropertyValidateChildDataWrongTypeException(expectedMessage, innerException);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
    }

    [TestMethod]
    public void PropertyValidateChildDataWrongTypeException_InheritsFromPropertyException_True()
    {
        // Arrange & Act
        var exception = new PropertyValidateChildDataWrongTypeException();

        // Assert
        Assert.IsTrue(exception is PropertyException);
        Assert.IsTrue(exception is NeatooException);
        Assert.AreEqual(typeof(PropertyException), typeof(PropertyValidateChildDataWrongTypeException).BaseType);
    }

    [TestMethod]
    public void PropertyValidateChildDataWrongTypeException_HasSerializableAttribute_True()
    {
        // Arrange & Act
        var attributes = typeof(PropertyValidateChildDataWrongTypeException).GetCustomAttributes(typeof(SerializableAttribute), false);

        // Assert
        Assert.AreEqual(1, attributes.Length);
    }

    #endregion

    #region TargetRulePropertyChangeException Tests

    [TestMethod]
    public void TargetRulePropertyChangeException_DefaultConstructor_CreatesInstance()
    {
        // Arrange & Act
        var exception = new TargetRulePropertyChangeException();

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    [TestMethod]
    public void TargetRulePropertyChangeException_MessageConstructor_PreservesMessage()
    {
        // Arrange
        const string expectedMessage = "Target rule property change failed for 'Name'.";

        // Act
        var exception = new TargetRulePropertyChangeException(expectedMessage);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
    }

    [TestMethod]
    public void TargetRulePropertyChangeException_MessageAndInnerExceptionConstructor_PreservesBoth()
    {
        // Arrange
        const string expectedMessage = "Target rule property change failed for 'Name'.";
        var innerException = new InvalidOperationException("Property is read-only");

        // Act
        var exception = new TargetRulePropertyChangeException(expectedMessage, innerException);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
    }

    [TestMethod]
    public void TargetRulePropertyChangeException_InheritsFromRuleException_True()
    {
        // Arrange & Act
        var exception = new TargetRulePropertyChangeException();

        // Assert
        Assert.IsTrue(exception is RuleException);
        Assert.IsTrue(exception is NeatooException);
        Assert.AreEqual(typeof(RuleException), typeof(TargetRulePropertyChangeException).BaseType);
    }

    [TestMethod]
    public void TargetRulePropertyChangeException_HasSerializableAttribute_True()
    {
        // Arrange & Act
        var attributes = typeof(TargetRulePropertyChangeException).GetCustomAttributes(typeof(SerializableAttribute), false);

        // Assert
        Assert.AreEqual(1, attributes.Length);
    }

    #endregion

    #region InvalidRuleTypeException Tests

    [TestMethod]
    public void InvalidRuleTypeException_DefaultConstructor_CreatesInstance()
    {
        // Arrange & Act
        var exception = new InvalidRuleTypeException();

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    [TestMethod]
    public void InvalidRuleTypeException_MessageConstructor_PreservesMessage()
    {
        // Arrange
        const string expectedMessage = "Rule type 'CustomRule' cannot be executed for 'PersonEntity'.";

        // Act
        var exception = new InvalidRuleTypeException(expectedMessage);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
    }

    [TestMethod]
    public void InvalidRuleTypeException_MessageAndInnerExceptionConstructor_PreservesBoth()
    {
        // Arrange
        const string expectedMessage = "Rule type 'CustomRule' cannot be executed for 'PersonEntity'.";
        var innerException = new NotSupportedException("Rule type not supported");

        // Act
        var exception = new InvalidRuleTypeException(expectedMessage, innerException);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
    }

    [TestMethod]
    public void InvalidRuleTypeException_InheritsFromRuleException_True()
    {
        // Arrange & Act
        var exception = new InvalidRuleTypeException();

        // Assert
        Assert.IsTrue(exception is RuleException);
        Assert.IsTrue(exception is NeatooException);
        Assert.AreEqual(typeof(RuleException), typeof(InvalidRuleTypeException).BaseType);
    }

    [TestMethod]
    public void InvalidRuleTypeException_HasSerializableAttribute_True()
    {
        // Arrange & Act
        var attributes = typeof(InvalidRuleTypeException).GetCustomAttributes(typeof(SerializableAttribute), false);

        // Assert
        Assert.AreEqual(1, attributes.Length);
    }

    #endregion

    #region InvalidTargetTypeException Tests

    [TestMethod]
    public void InvalidTargetTypeException_DefaultConstructor_CreatesInstance()
    {
        // Arrange & Act
        var exception = new InvalidTargetTypeException();

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    [TestMethod]
    public void InvalidTargetTypeException_MessageConstructor_PreservesMessage()
    {
        // Arrange
        const string expectedMessage = "Type 'PersonDto' is not assignable from 'PersonEntity'.";

        // Act
        var exception = new InvalidTargetTypeException(expectedMessage);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
    }

    [TestMethod]
    public void InvalidTargetTypeException_MessageAndInnerExceptionConstructor_PreservesBoth()
    {
        // Arrange
        const string expectedMessage = "Type 'PersonDto' is not assignable from 'PersonEntity'.";
        var innerException = new InvalidCastException("Cannot cast to target type");

        // Act
        var exception = new InvalidTargetTypeException(expectedMessage, innerException);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
    }

    [TestMethod]
    public void InvalidTargetTypeException_InheritsFromRuleException_True()
    {
        // Arrange & Act
        var exception = new InvalidTargetTypeException();

        // Assert
        Assert.IsTrue(exception is RuleException);
        Assert.IsTrue(exception is NeatooException);
        Assert.AreEqual(typeof(RuleException), typeof(InvalidTargetTypeException).BaseType);
    }

    [TestMethod]
    public void InvalidTargetTypeException_HasSerializableAttribute_True()
    {
        // Arrange & Act
        var attributes = typeof(InvalidTargetTypeException).GetCustomAttributes(typeof(SerializableAttribute), false);

        // Assert
        Assert.AreEqual(1, attributes.Length);
    }

    #endregion

    #region TargetIsNullException Tests

    [TestMethod]
    public void TargetIsNullException_DefaultConstructor_CreatesInstance()
    {
        // Arrange & Act
        var exception = new TargetIsNullException();

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    [TestMethod]
    public void TargetIsNullException_MessageConstructor_PreservesMessage()
    {
        // Arrange
        const string expectedMessage = "Target cannot be null when creating a RuleManager.";

        // Act
        var exception = new TargetIsNullException(expectedMessage);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
    }

    [TestMethod]
    public void TargetIsNullException_MessageAndInnerExceptionConstructor_PreservesBoth()
    {
        // Arrange
        const string expectedMessage = "Target cannot be null when creating a RuleManager.";
        var innerException = new ArgumentNullException("target");

        // Act
        var exception = new TargetIsNullException(expectedMessage, innerException);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
    }

    [TestMethod]
    public void TargetIsNullException_InheritsFromRuleException_True()
    {
        // Arrange & Act
        var exception = new TargetIsNullException();

        // Assert
        Assert.IsTrue(exception is RuleException);
        Assert.IsTrue(exception is NeatooException);
        Assert.AreEqual(typeof(RuleException), typeof(TargetIsNullException).BaseType);
    }

    [TestMethod]
    public void TargetIsNullException_HasSerializableAttribute_True()
    {
        // Arrange & Act
        var attributes = typeof(TargetIsNullException).GetCustomAttributes(typeof(SerializableAttribute), false);

        // Assert
        Assert.AreEqual(1, attributes.Length);
    }

    #endregion

    #region AddRulesNotDefinedException Tests

    [TestMethod]
    public void AddRulesNotDefinedException_DefaultConstructor_CreatesInstanceWithDefaultMessage()
    {
        // Arrange & Act
        var exception = new AddRulesNotDefinedException<string>();

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception, typeof(Exception));
        Assert.AreEqual("AddRules not defined for String", exception.Message);
    }

    [TestMethod]
    public void AddRulesNotDefinedException_DefaultConstructor_IncludesTypeName()
    {
        // Arrange & Act
        var exception = new AddRulesNotDefinedException<int>();

        // Assert
        Assert.IsTrue(exception.Message.Contains("Int32"));
    }

    [TestMethod]
    public void AddRulesNotDefinedException_MessageConstructor_PreservesMessage()
    {
        // Arrange
        const string expectedMessage = "Custom error message for AddRules.";

        // Act
        var exception = new AddRulesNotDefinedException<object>(expectedMessage);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
    }

    [TestMethod]
    public void AddRulesNotDefinedException_MessageAndInnerExceptionConstructor_PreservesBoth()
    {
        // Arrange
        const string expectedMessage = "Custom error message for AddRules.";
        var innerException = new NotImplementedException("Method not implemented");

        // Act
        var exception = new AddRulesNotDefinedException<object>(expectedMessage, innerException);

        // Assert
        Assert.AreEqual(expectedMessage, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
    }

    [TestMethod]
    public void AddRulesNotDefinedException_InheritsFromConfigurationException_True()
    {
        // Arrange & Act
        var exception = new AddRulesNotDefinedException<string>();

        // Assert
        Assert.IsTrue(exception is ConfigurationException);
        Assert.IsTrue(exception is NeatooException);
        Assert.AreEqual(typeof(ConfigurationException), typeof(AddRulesNotDefinedException<string>).BaseType);
    }

    [TestMethod]
    public void AddRulesNotDefinedException_HasSerializableAttribute_True()
    {
        // Arrange & Act
        var attributes = typeof(AddRulesNotDefinedException<string>).GetCustomAttributes(typeof(SerializableAttribute), false);

        // Assert
        Assert.AreEqual(1, attributes.Length);
    }

    [TestMethod]
    public void AddRulesNotDefinedException_DifferentGenericTypes_HaveDifferentMessages()
    {
        // Arrange & Act
        var stringException = new AddRulesNotDefinedException<string>();
        var intException = new AddRulesNotDefinedException<int>();
        var guidException = new AddRulesNotDefinedException<Guid>();

        // Assert
        Assert.AreNotEqual(stringException.Message, intException.Message);
        Assert.AreNotEqual(intException.Message, guidException.Message);
        Assert.IsTrue(stringException.Message.Contains("String"));
        Assert.IsTrue(intException.Message.Contains("Int32"));
        Assert.IsTrue(guidException.Message.Contains("Guid"));
    }

    #endregion

    #region Exception Throwing and Catching Tests

    [TestMethod]
    public void PropertyMissingException_CanBeThrownAndCaught()
    {
        // Arrange
        const string expectedMessage = "Test property missing exception";

        // Act & Assert
        var caughtException = Assert.ThrowsException<PropertyMissingException>(() =>
        {
            throw new PropertyMissingException(expectedMessage);
        });

        Assert.AreEqual(expectedMessage, caughtException.Message);
    }

    [TestMethod]
    public void PropertyTypeMismatchException_CanBeThrownAndCaught()
    {
        // Arrange
        const string expectedMessage = "Test property type mismatch exception";

        // Act & Assert
        var caughtException = Assert.ThrowsException<PropertyTypeMismatchException>(() =>
        {
            throw new PropertyTypeMismatchException(expectedMessage);
        });

        Assert.AreEqual(expectedMessage, caughtException.Message);
    }

    [TestMethod]
    public void PropertyNotFoundException_CanBeThrownAndCaught()
    {
        // Arrange
        const string expectedMessage = "Test property not found exception";

        // Act & Assert
        var caughtException = Assert.ThrowsException<PropertyNotFoundException>(() =>
        {
            throw new PropertyNotFoundException(expectedMessage);
        });

        Assert.AreEqual(expectedMessage, caughtException.Message);
    }

    [TestMethod]
    public void GlobalFactoryException_CanBeThrownAndCaught()
    {
        // Arrange
        const string expectedMessage = "Test global factory exception";

        // Act & Assert
        var caughtException = Assert.ThrowsException<GlobalFactoryException>(() =>
        {
            throw new GlobalFactoryException(expectedMessage);
        });

        Assert.AreEqual(expectedMessage, caughtException.Message);
    }

    [TestMethod]
    public void InvalidRuleTypeException_CanBeThrownAndCaught()
    {
        // Arrange
        const string expectedMessage = "Test invalid rule type exception";

        // Act & Assert
        var caughtException = Assert.ThrowsException<InvalidRuleTypeException>(() =>
        {
            throw new InvalidRuleTypeException(expectedMessage);
        });

        Assert.AreEqual(expectedMessage, caughtException.Message);
    }

    [TestMethod]
    public void InvalidTargetTypeException_CanBeThrownAndCaught()
    {
        // Arrange
        const string expectedMessage = "Test invalid target type exception";

        // Act & Assert
        var caughtException = Assert.ThrowsException<InvalidTargetTypeException>(() =>
        {
            throw new InvalidTargetTypeException(expectedMessage);
        });

        Assert.AreEqual(expectedMessage, caughtException.Message);
    }

    [TestMethod]
    public void TargetIsNullException_CanBeThrownAndCaught()
    {
        // Arrange
        const string expectedMessage = "Test target is null exception";

        // Act & Assert
        var caughtException = Assert.ThrowsException<TargetIsNullException>(() =>
        {
            throw new TargetIsNullException(expectedMessage);
        });

        Assert.AreEqual(expectedMessage, caughtException.Message);
    }

    [TestMethod]
    public void TargetRulePropertyChangeException_CanBeThrownAndCaught()
    {
        // Arrange
        const string expectedMessage = "Test target rule property change exception";

        // Act & Assert
        var caughtException = Assert.ThrowsException<TargetRulePropertyChangeException>(() =>
        {
            throw new TargetRulePropertyChangeException(expectedMessage);
        });

        Assert.AreEqual(expectedMessage, caughtException.Message);
    }

    [TestMethod]
    public void PropertyInfoEntityChildDataWrongTypeException_CanBeThrownAndCaught()
    {
        // Arrange
        const string expectedMessage = "Test entity child data wrong type exception";

        // Act & Assert
        var caughtException = Assert.ThrowsException<PropertyInfoEntityChildDataWrongTypeException>(() =>
        {
            throw new PropertyInfoEntityChildDataWrongTypeException(expectedMessage);
        });

        Assert.AreEqual(expectedMessage, caughtException.Message);
    }

    [TestMethod]
    public void PropertyValidateChildDataWrongTypeException_CanBeThrownAndCaught()
    {
        // Arrange
        const string expectedMessage = "Test validate child data wrong type exception";

        // Act & Assert
        var caughtException = Assert.ThrowsException<PropertyValidateChildDataWrongTypeException>(() =>
        {
            throw new PropertyValidateChildDataWrongTypeException(expectedMessage);
        });

        Assert.AreEqual(expectedMessage, caughtException.Message);
    }

    [TestMethod]
    public void AddRulesNotDefinedException_CanBeThrownAndCaught()
    {
        // Act & Assert
        var caughtException = Assert.ThrowsException<AddRulesNotDefinedException<string>>(() =>
        {
            throw new AddRulesNotDefinedException<string>();
        });

        Assert.IsTrue(caughtException.Message.Contains("String"));
    }

    #endregion

    #region Exception Chaining Tests

    [TestMethod]
    public void AllExceptions_SupportExceptionChaining()
    {
        // Arrange
        var rootException = new InvalidOperationException("Root cause");
        var level1Exception = new PropertyNotFoundException("Level 1", rootException);
        var level2Exception = new GlobalFactoryException("Level 2", level1Exception);

        // Assert
        Assert.AreSame(level1Exception, level2Exception.InnerException);
        Assert.AreSame(rootException, level2Exception.InnerException!.InnerException);
    }

    [TestMethod]
    public void AllExceptions_InnerExceptionCanBeNull()
    {
        // Arrange & Act
        var exceptions = new Exception[]
        {
            new PropertyMissingException("test"),
            new PropertyTypeMismatchException("test"),
            new PropertyNotFoundException("test"),
            new GlobalFactoryException("test"),
            new PropertyInfoEntityChildDataWrongTypeException("test"),
            new PropertyValidateChildDataWrongTypeException("test"),
            new TargetRulePropertyChangeException("test"),
            new InvalidRuleTypeException("test"),
            new InvalidTargetTypeException("test"),
            new TargetIsNullException("test"),
            new AddRulesNotDefinedException<string>("test")
        };

        // Assert
        foreach (var exception in exceptions)
        {
            Assert.IsNull(exception.InnerException, $"InnerException should be null for {exception.GetType().Name}");
        }
    }

    #endregion

    #region Empty and Null Message Tests

    [TestMethod]
    public void PropertyMissingException_EmptyMessage_IsAccepted()
    {
        // Arrange & Act
        var exception = new PropertyMissingException(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, exception.Message);
    }

    [TestMethod]
    public void PropertyTypeMismatchException_EmptyMessage_IsAccepted()
    {
        // Arrange & Act
        var exception = new PropertyTypeMismatchException(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, exception.Message);
    }

    [TestMethod]
    public void PropertyNotFoundException_EmptyMessage_IsAccepted()
    {
        // Arrange & Act
        var exception = new PropertyNotFoundException(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, exception.Message);
    }

    [TestMethod]
    public void GlobalFactoryException_EmptyMessage_IsAccepted()
    {
        // Arrange & Act
        var exception = new GlobalFactoryException(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, exception.Message);
    }

    #endregion

    #region Stack Trace and Source Tests

    [TestMethod]
    public void AllExceptions_HaveStackTraceWhenThrown()
    {
        // Arrange & Act
        Exception? caughtException = null;
        try
        {
            throw new PropertyNotFoundException("Test exception");
        }
        catch (PropertyNotFoundException ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.IsNotNull(caughtException);
        Assert.IsNotNull(caughtException.StackTrace);
        Assert.IsTrue(caughtException.StackTrace.Contains("ExceptionTests"));
    }

    #endregion
}
