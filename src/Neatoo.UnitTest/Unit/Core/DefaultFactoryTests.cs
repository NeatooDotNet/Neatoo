using System.ComponentModel;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using Neatoo.Internal;

namespace Neatoo.UnitTest.Unit.Core;

#region Test POCO Classes

/// <summary>
/// Simple POCO with public getter and setter for standard property tests.
/// </summary>
public class SimpleTestPoco
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public Guid? NullableGuid { get; set; }
    public DateTime DateOfBirth { get; set; }
    public decimal Price { get; set; }
    public List<Dictionary<string, int>> ComplexProperty { get; set; } = new();
    public int[] ArrayProperty { get; set; } = Array.Empty<int>();
    public DayOfWeek DayOfWeek { get; set; }
    public int? NullableInt { get; set; }
    public DateTime? NullableDateTime { get; set; }
    public decimal? NullableDecimal { get; set; }
}

/// <summary>
/// POCO with private setter for read-only property tests.
/// </summary>
public class ReadOnlyTestPoco
{
    public string ReadOnlyProperty { get; private set; } = string.Empty;
    public string WritableProperty { get; set; } = string.Empty;

    public void SetReadOnlyProperty(string value) => ReadOnlyProperty = value;
}

/// <summary>
/// POCO with DisplayNameAttribute for display name tests.
/// </summary>
public class DisplayNameTestPoco
{
    [DisplayName("Custom Display Name")]
    public string PropertyWithDisplayName { get; set; } = string.Empty;

    public string PropertyWithoutDisplayName { get; set; } = string.Empty;
}

/// <summary>
/// POCO for testing properties with different names.
/// </summary>
public class MultiPropertyPoco
{
    public string FirstProperty { get; set; } = string.Empty;
    public string SecondProperty { get; set; } = string.Empty;
    public string ValidateFirst { get; set; } = string.Empty;
    public string ValidateSecond { get; set; } = string.Empty;
    public string EntityFirst { get; set; } = string.Empty;
    public string EntitySecond { get; set; } = string.Empty;
}

#endregion

/// <summary>
/// Unit tests for the DefaultFactory class.
/// Tests factory methods for creating Property, ValidateProperty, and EntityProperty instances.
/// Verifies correct configuration, interface implementation, and instance uniqueness.
/// Uses real PropertyInfoWrapper instances instead of mocks for more realistic testing.
/// </summary>
[TestClass]
public class DefaultFactoryTests
{
    private DefaultFactory _factory = null!;
    private IPropertyInfo _namePropertyInfo = null!;

    /// <summary>
    /// Helper method to create a PropertyInfoWrapper from a POCO type and property name.
    /// </summary>
    private static IPropertyInfo CreatePropertyInfoWrapper<TPoco>(string propertyName)
    {
        var propertyInfo = typeof(TPoco).GetProperty(propertyName)
            ?? throw new ArgumentException($"Property '{propertyName}' not found on type '{typeof(TPoco).Name}'");
        return new PropertyInfoWrapper(propertyInfo);
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _factory = new DefaultFactory();
        _namePropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("Name");
    }

    #region IFactory Interface Implementation Tests

    [TestMethod]
    public void DefaultFactory_ImplementsIFactory()
    {
        // Arrange & Act
        var factory = new DefaultFactory();

        // Assert
        Assert.IsInstanceOfType(factory, typeof(IFactory));
    }

    [TestMethod]
    public void DefaultFactory_DefaultConstructor_CreatesInstance()
    {
        // Arrange & Act
        var factory = new DefaultFactory();

        // Assert
        Assert.IsNotNull(factory);
    }

    #endregion

    #region CreateProperty Tests

    [TestMethod]
    public void CreateProperty_WithStringType_ReturnsPropertyOfString()
    {
        // Arrange & Act
        var property = _factory.CreateProperty<string>(_namePropertyInfo);

        // Assert
        Assert.IsNotNull(property);
        Assert.IsInstanceOfType(property, typeof(Property<string>));
    }

    [TestMethod]
    public void CreateProperty_WithIntType_ReturnsPropertyOfInt()
    {
        // Arrange
        var agePropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("Age");

        // Act
        var property = _factory.CreateProperty<int>(agePropertyInfo);

        // Assert
        Assert.IsNotNull(property);
        Assert.IsInstanceOfType(property, typeof(Property<int>));
    }

    [TestMethod]
    public void CreateProperty_WithNullableGuidType_ReturnsPropertyOfNullableGuid()
    {
        // Arrange
        var nullableGuidPropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("NullableGuid");

        // Act
        var property = _factory.CreateProperty<Guid?>(nullableGuidPropertyInfo);

        // Assert
        Assert.IsNotNull(property);
        Assert.IsInstanceOfType(property, typeof(Property<Guid?>));
    }

    [TestMethod]
    public void CreateProperty_SetsNameFromPropertyInfo()
    {
        // Arrange - Name property from SimpleTestPoco

        // Act
        var property = _factory.CreateProperty<string>(_namePropertyInfo);

        // Assert
        Assert.AreEqual("Name", property.Name);
    }

    [TestMethod]
    public void CreateProperty_SetsIsReadOnlyFromPropertyInfo_True()
    {
        // Arrange
        var readOnlyPropertyInfo = CreatePropertyInfoWrapper<ReadOnlyTestPoco>("ReadOnlyProperty");

        // Act
        var property = _factory.CreateProperty<string>(readOnlyPropertyInfo);

        // Assert
        Assert.IsTrue(property.IsReadOnly);
    }

    [TestMethod]
    public void CreateProperty_SetsIsReadOnlyFromPropertyInfo_False()
    {
        // Arrange
        var writablePropertyInfo = CreatePropertyInfoWrapper<ReadOnlyTestPoco>("WritableProperty");

        // Act
        var property = _factory.CreateProperty<string>(writablePropertyInfo);

        // Assert
        Assert.IsFalse(property.IsReadOnly);
    }

    [TestMethod]
    public void CreateProperty_ReturnsNewInstanceEachTime()
    {
        // Arrange & Act
        var property1 = _factory.CreateProperty<string>(_namePropertyInfo);
        var property2 = _factory.CreateProperty<string>(_namePropertyInfo);

        // Assert
        Assert.AreNotSame(property1, property2);
    }

    [TestMethod]
    public void CreateProperty_WithComplexType_ReturnsPropertyOfComplexType()
    {
        // Arrange
        var complexPropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("ComplexProperty");

        // Act
        var property = _factory.CreateProperty<List<Dictionary<string, int>>>(complexPropertyInfo);

        // Assert
        Assert.IsNotNull(property);
        Assert.IsInstanceOfType(property, typeof(Property<List<Dictionary<string, int>>>));
        Assert.AreEqual(typeof(List<Dictionary<string, int>>), property.Type);
    }

    [TestMethod]
    public void CreateProperty_TypeProperty_ReturnsCorrectGenericType()
    {
        // Arrange
        var namePropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("Name");
        var agePropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("Age");
        var dateOfBirthPropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("DateOfBirth");

        // Act
        var stringProperty = _factory.CreateProperty<string>(namePropertyInfo);
        var intProperty = _factory.CreateProperty<int>(agePropertyInfo);
        var dateTimeProperty = _factory.CreateProperty<DateTime>(dateOfBirthPropertyInfo);

        // Assert
        Assert.AreEqual(typeof(string), stringProperty.Type);
        Assert.AreEqual(typeof(int), intProperty.Type);
        Assert.AreEqual(typeof(DateTime), dateTimeProperty.Type);
    }

    #endregion

    #region CreateValidateProperty Tests

    [TestMethod]
    public void CreateValidateProperty_WithStringType_ReturnsValidatePropertyOfString()
    {
        // Arrange & Act
        var property = _factory.CreateValidateProperty<string>(_namePropertyInfo);

        // Assert
        Assert.IsNotNull(property);
        Assert.IsInstanceOfType(property, typeof(ValidateProperty<string>));
    }

    [TestMethod]
    public void CreateValidateProperty_WithIntType_ReturnsValidatePropertyOfInt()
    {
        // Arrange
        var agePropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("Age");

        // Act
        var property = _factory.CreateValidateProperty<int>(agePropertyInfo);

        // Assert
        Assert.IsNotNull(property);
        Assert.IsInstanceOfType(property, typeof(ValidateProperty<int>));
    }

    [TestMethod]
    public void CreateValidateProperty_WithDecimalType_ReturnsValidatePropertyOfDecimal()
    {
        // Arrange
        var pricePropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("Price");

        // Act
        var property = _factory.CreateValidateProperty<decimal>(pricePropertyInfo);

        // Assert
        Assert.IsNotNull(property);
        Assert.IsInstanceOfType(property, typeof(ValidateProperty<decimal>));
    }

    [TestMethod]
    public void CreateValidateProperty_SetsNameFromPropertyInfo()
    {
        // Arrange
        var validateFirstPropertyInfo = CreatePropertyInfoWrapper<MultiPropertyPoco>("ValidateFirst");

        // Act
        var property = _factory.CreateValidateProperty<string>(validateFirstPropertyInfo);

        // Assert
        Assert.AreEqual("ValidateFirst", property.Name);
    }

    [TestMethod]
    public void CreateValidateProperty_SetsIsReadOnlyFromPropertyInfo_True()
    {
        // Arrange
        var readOnlyPropertyInfo = CreatePropertyInfoWrapper<ReadOnlyTestPoco>("ReadOnlyProperty");

        // Act
        var property = _factory.CreateValidateProperty<string>(readOnlyPropertyInfo);

        // Assert
        Assert.IsTrue(property.IsReadOnly);
    }

    [TestMethod]
    public void CreateValidateProperty_SetsIsReadOnlyFromPropertyInfo_False()
    {
        // Arrange
        var writablePropertyInfo = CreatePropertyInfoWrapper<ReadOnlyTestPoco>("WritableProperty");

        // Act
        var property = _factory.CreateValidateProperty<string>(writablePropertyInfo);

        // Assert
        Assert.IsFalse(property.IsReadOnly);
    }

    [TestMethod]
    public void CreateValidateProperty_ReturnsNewInstanceEachTime()
    {
        // Arrange & Act
        var property1 = _factory.CreateValidateProperty<string>(_namePropertyInfo);
        var property2 = _factory.CreateValidateProperty<string>(_namePropertyInfo);

        // Assert
        Assert.AreNotSame(property1, property2);
    }

    [TestMethod]
    public void CreateValidateProperty_InheritsFromProperty()
    {
        // Arrange & Act
        var property = _factory.CreateValidateProperty<string>(_namePropertyInfo);

        // Assert
        Assert.IsInstanceOfType(property, typeof(Property<string>));
    }

    [TestMethod]
    public void CreateValidateProperty_ImplementsIValidateProperty()
    {
        // Arrange & Act
        var property = _factory.CreateValidateProperty<string>(_namePropertyInfo);

        // Assert
        Assert.IsInstanceOfType(property, typeof(IValidateProperty<string>));
    }

    [TestMethod]
    public void CreateValidateProperty_InitiallyIsValid()
    {
        // Arrange & Act
        var property = _factory.CreateValidateProperty<string>(_namePropertyInfo);

        // Assert
        Assert.IsTrue(property.IsValid);
        Assert.IsTrue(property.IsSelfValid);
    }

    [TestMethod]
    public void CreateValidateProperty_InitiallyHasEmptyRuleMessages()
    {
        // Arrange & Act
        var property = _factory.CreateValidateProperty<string>(_namePropertyInfo);

        // Assert
        Assert.AreEqual(0, property.RuleMessages.Count);
    }

    #endregion

    #region CreateEntityProperty Tests

    [TestMethod]
    public void CreateEntityProperty_WithStringType_ReturnsEntityPropertyOfString()
    {
        // Arrange & Act
        var property = _factory.CreateEntityProperty<string>(_namePropertyInfo);

        // Assert
        Assert.IsNotNull(property);
        Assert.IsInstanceOfType(property, typeof(EntityProperty<string>));
    }

    [TestMethod]
    public void CreateEntityProperty_WithIntType_ReturnsEntityPropertyOfInt()
    {
        // Arrange
        var agePropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("Age");

        // Act
        var property = _factory.CreateEntityProperty<int>(agePropertyInfo);

        // Assert
        Assert.IsNotNull(property);
        Assert.IsInstanceOfType(property, typeof(EntityProperty<int>));
    }

    [TestMethod]
    public void CreateEntityProperty_WithDateTimeType_ReturnsEntityPropertyOfDateTime()
    {
        // Arrange
        var dateOfBirthPropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("DateOfBirth");

        // Act
        var property = _factory.CreateEntityProperty<DateTime>(dateOfBirthPropertyInfo);

        // Assert
        Assert.IsNotNull(property);
        Assert.IsInstanceOfType(property, typeof(EntityProperty<DateTime>));
    }

    [TestMethod]
    public void CreateEntityProperty_SetsNameFromPropertyInfo()
    {
        // Arrange
        var entityFirstPropertyInfo = CreatePropertyInfoWrapper<MultiPropertyPoco>("EntityFirst");

        // Act
        var property = _factory.CreateEntityProperty<string>(entityFirstPropertyInfo);

        // Assert
        Assert.AreEqual("EntityFirst", property.Name);
    }

    [TestMethod]
    public void CreateEntityProperty_SetsIsReadOnlyFromPropertyInfo_True()
    {
        // Arrange
        var readOnlyPropertyInfo = CreatePropertyInfoWrapper<ReadOnlyTestPoco>("ReadOnlyProperty");

        // Act
        var property = _factory.CreateEntityProperty<string>(readOnlyPropertyInfo);

        // Assert
        Assert.IsTrue(property.IsReadOnly);
    }

    [TestMethod]
    public void CreateEntityProperty_SetsIsReadOnlyFromPropertyInfo_False()
    {
        // Arrange
        var writablePropertyInfo = CreatePropertyInfoWrapper<ReadOnlyTestPoco>("WritableProperty");

        // Act
        var property = _factory.CreateEntityProperty<string>(writablePropertyInfo);

        // Assert
        Assert.IsFalse(property.IsReadOnly);
    }

    [TestMethod]
    public void CreateEntityProperty_ReturnsNewInstanceEachTime()
    {
        // Arrange & Act
        var property1 = _factory.CreateEntityProperty<string>(_namePropertyInfo);
        var property2 = _factory.CreateEntityProperty<string>(_namePropertyInfo);

        // Assert
        Assert.AreNotSame(property1, property2);
    }

    [TestMethod]
    public void CreateEntityProperty_InheritsFromValidateProperty()
    {
        // Arrange & Act
        var property = _factory.CreateEntityProperty<string>(_namePropertyInfo);

        // Assert
        Assert.IsInstanceOfType(property, typeof(ValidateProperty<string>));
    }

    [TestMethod]
    public void CreateEntityProperty_InheritsFromProperty()
    {
        // Arrange & Act
        var property = _factory.CreateEntityProperty<string>(_namePropertyInfo);

        // Assert
        Assert.IsInstanceOfType(property, typeof(Property<string>));
    }

    [TestMethod]
    public void CreateEntityProperty_ImplementsIEntityProperty()
    {
        // Arrange & Act
        var property = _factory.CreateEntityProperty<string>(_namePropertyInfo);

        // Assert
        Assert.IsInstanceOfType(property, typeof(IEntityProperty<string>));
    }

    [TestMethod]
    public void CreateEntityProperty_InitiallyIsNotModified()
    {
        // Arrange & Act
        var property = _factory.CreateEntityProperty<string>(_namePropertyInfo);

        // Assert
        Assert.IsFalse(property.IsModified);
        Assert.IsFalse(property.IsSelfModified);
    }

    [TestMethod]
    public void CreateEntityProperty_InitiallyIsNotPaused()
    {
        // Arrange & Act
        var property = _factory.CreateEntityProperty<string>(_namePropertyInfo);

        // Assert
        Assert.IsFalse(property.IsPaused);
    }

    [TestMethod]
    public void CreateEntityProperty_WithoutDisplayNameAttribute_UsesPropertyName()
    {
        // Arrange
        var propertyWithoutDisplayNameInfo = CreatePropertyInfoWrapper<DisplayNameTestPoco>("PropertyWithoutDisplayName");

        // Act
        var property = _factory.CreateEntityProperty<string>(propertyWithoutDisplayNameInfo);

        // Assert
        Assert.AreEqual("PropertyWithoutDisplayName", property.DisplayName);
    }

    [TestMethod]
    public void CreateEntityProperty_WithDisplayNameAttribute_UsesAttributeDisplayName()
    {
        // Arrange
        var propertyWithDisplayNameInfo = CreatePropertyInfoWrapper<DisplayNameTestPoco>("PropertyWithDisplayName");

        // Act
        var property = _factory.CreateEntityProperty<string>(propertyWithDisplayNameInfo);

        // Assert
        Assert.AreEqual("Custom Display Name", property.DisplayName);
    }

    #endregion

    #region Factory Instance Uniqueness Tests

    [TestMethod]
    public void CreateProperty_DifferentGenericTypes_ReturnsDistinctInstances()
    {
        // Arrange
        var namePropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("Name");
        var agePropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("Age");
        var nullableGuidPropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("NullableGuid");

        // Act
        var stringProperty = _factory.CreateProperty<string>(namePropertyInfo);
        var intProperty = _factory.CreateProperty<int>(agePropertyInfo);
        var guidProperty = _factory.CreateProperty<Guid?>(nullableGuidPropertyInfo);

        // Assert - Cast to object for comparing different generic types
        Assert.AreNotSame((object)stringProperty, (object)intProperty);
        Assert.AreNotSame((object)intProperty, (object)guidProperty);
        Assert.AreNotSame((object)stringProperty, (object)guidProperty);
    }

    [TestMethod]
    public void CreateValidateProperty_DifferentGenericTypes_ReturnsDistinctInstances()
    {
        // Arrange
        var namePropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("Name");
        var agePropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("Age");
        var pricePropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("Price");

        // Act
        var stringProperty = _factory.CreateValidateProperty<string>(namePropertyInfo);
        var intProperty = _factory.CreateValidateProperty<int>(agePropertyInfo);
        var decimalProperty = _factory.CreateValidateProperty<decimal>(pricePropertyInfo);

        // Assert - Cast to object for comparing different generic types
        Assert.AreNotSame((object)stringProperty, (object)intProperty);
        Assert.AreNotSame((object)intProperty, (object)decimalProperty);
        Assert.AreNotSame((object)stringProperty, (object)decimalProperty);
    }

    [TestMethod]
    public void CreateEntityProperty_DifferentGenericTypes_ReturnsDistinctInstances()
    {
        // Arrange
        var namePropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("Name");
        var agePropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("Age");
        var dateOfBirthPropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("DateOfBirth");

        // Act
        var stringProperty = _factory.CreateEntityProperty<string>(namePropertyInfo);
        var intProperty = _factory.CreateEntityProperty<int>(agePropertyInfo);
        var dateTimeProperty = _factory.CreateEntityProperty<DateTime>(dateOfBirthPropertyInfo);

        // Assert - Cast to object for comparing different generic types
        Assert.AreNotSame((object)stringProperty, (object)intProperty);
        Assert.AreNotSame((object)intProperty, (object)dateTimeProperty);
        Assert.AreNotSame((object)stringProperty, (object)dateTimeProperty);
    }

    [TestMethod]
    public void Factory_MultipleCallsWithSamePropertyInfo_ReturnsNewInstancesEachTime()
    {
        // Arrange & Act
        var property1 = _factory.CreateProperty<string>(_namePropertyInfo);
        var validateProperty1 = _factory.CreateValidateProperty<string>(_namePropertyInfo);
        var entityProperty1 = _factory.CreateEntityProperty<string>(_namePropertyInfo);

        var property2 = _factory.CreateProperty<string>(_namePropertyInfo);
        var validateProperty2 = _factory.CreateValidateProperty<string>(_namePropertyInfo);
        var entityProperty2 = _factory.CreateEntityProperty<string>(_namePropertyInfo);

        // Assert
        Assert.AreNotSame(property1, property2);
        Assert.AreNotSame(validateProperty1, validateProperty2);
        Assert.AreNotSame(entityProperty1, entityProperty2);
    }

    #endregion

    #region Factory with Different PropertyInfo Tests

    [TestMethod]
    public void CreateProperty_WithDifferentPropertyInfoNames_SetsCorrectNames()
    {
        // Arrange
        var firstPropertyInfo = CreatePropertyInfoWrapper<MultiPropertyPoco>("FirstProperty");
        var secondPropertyInfo = CreatePropertyInfoWrapper<MultiPropertyPoco>("SecondProperty");

        // Act
        var property1 = _factory.CreateProperty<string>(firstPropertyInfo);
        var property2 = _factory.CreateProperty<string>(secondPropertyInfo);

        // Assert
        Assert.AreEqual("FirstProperty", property1.Name);
        Assert.AreEqual("SecondProperty", property2.Name);
    }

    [TestMethod]
    public void CreateValidateProperty_WithDifferentPropertyInfoNames_SetsCorrectNames()
    {
        // Arrange
        var validateFirstPropertyInfo = CreatePropertyInfoWrapper<MultiPropertyPoco>("ValidateFirst");
        var validateSecondPropertyInfo = CreatePropertyInfoWrapper<MultiPropertyPoco>("ValidateSecond");

        // Act
        var property1 = _factory.CreateValidateProperty<string>(validateFirstPropertyInfo);
        var property2 = _factory.CreateValidateProperty<string>(validateSecondPropertyInfo);

        // Assert
        Assert.AreEqual("ValidateFirst", property1.Name);
        Assert.AreEqual("ValidateSecond", property2.Name);
    }

    [TestMethod]
    public void CreateEntityProperty_WithDifferentPropertyInfoNames_SetsCorrectNames()
    {
        // Arrange
        var entityFirstPropertyInfo = CreatePropertyInfoWrapper<MultiPropertyPoco>("EntityFirst");
        var entitySecondPropertyInfo = CreatePropertyInfoWrapper<MultiPropertyPoco>("EntitySecond");

        // Act
        var property1 = _factory.CreateEntityProperty<string>(entityFirstPropertyInfo);
        var property2 = _factory.CreateEntityProperty<string>(entitySecondPropertyInfo);

        // Assert
        Assert.AreEqual("EntityFirst", property1.Name);
        Assert.AreEqual("EntitySecond", property2.Name);
    }

    [TestMethod]
    public void CreateProperty_WithMixedReadOnlySettings_SetsCorrectReadOnlyState()
    {
        // Arrange
        var readOnlyPropertyInfo = CreatePropertyInfoWrapper<ReadOnlyTestPoco>("ReadOnlyProperty");
        var writablePropertyInfo = CreatePropertyInfoWrapper<ReadOnlyTestPoco>("WritableProperty");

        // Act
        var readOnlyProp = _factory.CreateProperty<string>(readOnlyPropertyInfo);
        var writableProp = _factory.CreateProperty<string>(writablePropertyInfo);

        // Assert
        Assert.IsTrue(readOnlyProp.IsReadOnly);
        Assert.IsFalse(writableProp.IsReadOnly);
    }

    #endregion

    #region Interface Through Factory Tests

    [TestMethod]
    public void CreateProperty_ThroughIFactoryInterface_ReturnsSameTypeAsDirectCall()
    {
        // Arrange
        IFactory factoryInterface = _factory;

        // Act
        var propertyThroughInterface = factoryInterface.CreateProperty<string>(_namePropertyInfo);
        var propertyDirect = _factory.CreateProperty<string>(_namePropertyInfo);

        // Assert
        Assert.AreEqual(propertyThroughInterface.GetType(), propertyDirect.GetType());
    }

    [TestMethod]
    public void CreateValidateProperty_ThroughIFactoryInterface_ReturnsSameTypeAsDirectCall()
    {
        // Arrange
        IFactory factoryInterface = _factory;

        // Act
        var propertyThroughInterface = factoryInterface.CreateValidateProperty<string>(_namePropertyInfo);
        var propertyDirect = _factory.CreateValidateProperty<string>(_namePropertyInfo);

        // Assert
        Assert.AreEqual(propertyThroughInterface.GetType(), propertyDirect.GetType());
    }

    [TestMethod]
    public void CreateEntityProperty_ThroughIFactoryInterface_ReturnsSameTypeAsDirectCall()
    {
        // Arrange
        IFactory factoryInterface = _factory;

        // Act
        var propertyThroughInterface = factoryInterface.CreateEntityProperty<string>(_namePropertyInfo);
        var propertyDirect = _factory.CreateEntityProperty<string>(_namePropertyInfo);

        // Assert
        Assert.AreEqual(propertyThroughInterface.GetType(), propertyDirect.GetType());
    }

    #endregion

    #region Edge Case Tests

    [TestMethod]
    public void CreateProperty_WithNullableValueType_ReturnsPropertyOfNullableType()
    {
        // Arrange
        var nullableIntPropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("NullableInt");

        // Act
        var property = _factory.CreateProperty<int?>(nullableIntPropertyInfo);

        // Assert
        Assert.IsNotNull(property);
        Assert.IsInstanceOfType(property, typeof(Property<int?>));
        Assert.AreEqual(typeof(int?), property.Type);
    }

    [TestMethod]
    public void CreateValidateProperty_WithNullableValueType_ReturnsValidatePropertyOfNullableType()
    {
        // Arrange
        var nullableDateTimePropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("NullableDateTime");

        // Act
        var property = _factory.CreateValidateProperty<DateTime?>(nullableDateTimePropertyInfo);

        // Assert
        Assert.IsNotNull(property);
        Assert.IsInstanceOfType(property, typeof(ValidateProperty<DateTime?>));
    }

    [TestMethod]
    public void CreateEntityProperty_WithNullableValueType_ReturnsEntityPropertyOfNullableType()
    {
        // Arrange
        var nullableDecimalPropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("NullableDecimal");

        // Act
        var property = _factory.CreateEntityProperty<decimal?>(nullableDecimalPropertyInfo);

        // Assert
        Assert.IsNotNull(property);
        Assert.IsInstanceOfType(property, typeof(EntityProperty<decimal?>));
    }

    [TestMethod]
    public void CreateProperty_WithArrayType_ReturnsPropertyOfArrayType()
    {
        // Arrange
        var arrayPropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("ArrayProperty");

        // Act
        var property = _factory.CreateProperty<int[]>(arrayPropertyInfo);

        // Assert
        Assert.IsNotNull(property);
        Assert.IsInstanceOfType(property, typeof(Property<int[]>));
        Assert.AreEqual(typeof(int[]), property.Type);
    }

    [TestMethod]
    public void CreateProperty_WithEnumType_ReturnsPropertyOfEnumType()
    {
        // Arrange
        var dayOfWeekPropertyInfo = CreatePropertyInfoWrapper<SimpleTestPoco>("DayOfWeek");

        // Act
        var property = _factory.CreateProperty<DayOfWeek>(dayOfWeekPropertyInfo);

        // Assert
        Assert.IsNotNull(property);
        Assert.IsInstanceOfType(property, typeof(Property<DayOfWeek>));
        Assert.AreEqual(typeof(DayOfWeek), property.Type);
    }

    #endregion
}
