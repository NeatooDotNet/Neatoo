using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;

namespace Neatoo.UnitTest.Unit.Core;

#region Test Attributes

/// <summary>
/// Custom attribute for testing GetCustomAttribute functionality.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class TestDescriptionAttribute : Attribute
{
    public string Description { get; }

    public TestDescriptionAttribute(string description)
    {
        Description = description;
    }
}

/// <summary>
/// Another custom attribute for testing multiple attribute scenarios.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class TestValidationAttribute : Attribute
{
    public int MaxLength { get; }

    public TestValidationAttribute(int maxLength)
    {
        MaxLength = maxLength;
    }
}

/// <summary>
/// Attribute that allows multiple instances for testing GetCustomAttributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class TestTagAttribute : Attribute
{
    public string Tag { get; }

    public TestTagAttribute(string tag)
    {
        Tag = tag;
    }
}

#endregion

#region Test POCO Classes

/// <summary>
/// Simple POCO with various property configurations for testing.
/// </summary>
public class SimpleTestClass
{
    public string PublicProperty { get; set; } = string.Empty;

    public string PublicGetPrivateSet { get; private set; } = string.Empty;

    public string ReadOnlyProperty { get; } = "ReadOnly";

    public int ValueTypeProperty { get; set; }

    public DateTime? NullableValueTypeProperty { get; set; }

    public List<string>? ReferenceTypeProperty { get; set; }
}

/// <summary>
/// POCO with custom attributes on properties.
/// </summary>
public class AttributedTestClass
{
    [TestDescription("This is a test property")]
    public string PropertyWithDescription { get; set; } = string.Empty;

    [TestDescription("Validated property")]
    [TestValidation(100)]
    public string PropertyWithMultipleAttributes { get; set; } = string.Empty;

    [TestTag("Primary")]
    [TestTag("Important")]
    [TestTag("Required")]
    public string PropertyWithMultipleSameAttributes { get; set; } = string.Empty;

    public string PropertyWithoutAttributes { get; set; } = string.Empty;
}

/// <summary>
/// POCO with different access modifier combinations.
/// </summary>
public class AccessModifierTestClass
{
    public string FullyPublic { get; set; } = string.Empty;

    public string PublicGetProtectedSet { get; protected set; } = string.Empty;

    public string PublicGetInternalSet { get; internal set; } = string.Empty;

    public string PublicGetPrivateSet { get; private set; } = string.Empty;

    public string InitOnlyProperty { get; init; } = string.Empty;
}

/// <summary>
/// POCO with various type properties for testing Type property.
/// </summary>
public class TypeVarietyTestClass
{
    public int IntProperty { get; set; }

    public double DoubleProperty { get; set; }

    public bool BoolProperty { get; set; }

    public Guid GuidProperty { get; set; }

    public string StringProperty { get; set; } = string.Empty;

    public object? ObjectProperty { get; set; }

    public int[]? ArrayProperty { get; set; }

    public Dictionary<string, int>? DictionaryProperty { get; set; }

    public IEnumerable<string>? EnumerableProperty { get; set; }
}

#endregion

/// <summary>
/// Unit tests for PropertyInfoWrapper class.
/// Tests construction, property access, attribute retrieval, and caching behavior.
/// </summary>
[TestClass]
public class PropertyInfoWrapperTests
{
    #region Construction and Basic Properties Tests

    [TestMethod]
    public void Constructor_WithPropertyInfo_SetsPropertyInfoProperty()
    {
        // Arrange
        var propertyInfo = typeof(SimpleTestClass).GetProperty(nameof(SimpleTestClass.PublicProperty))!;

        // Act
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Assert
        Assert.AreSame(propertyInfo, wrapper.PropertyInfo);
    }

    [TestMethod]
    public void Name_ReturnsPropertyName()
    {
        // Arrange
        var propertyInfo = typeof(SimpleTestClass).GetProperty(nameof(SimpleTestClass.PublicProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var name = wrapper.Name;

        // Assert
        Assert.AreEqual("PublicProperty", name);
    }

    [TestMethod]
    public void Type_ReturnsPropertyType_ForValueType()
    {
        // Arrange
        var propertyInfo = typeof(SimpleTestClass).GetProperty(nameof(SimpleTestClass.ValueTypeProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var type = wrapper.Type;

        // Assert
        Assert.AreEqual(typeof(int), type);
    }

    [TestMethod]
    public void Type_ReturnsPropertyType_ForReferenceType()
    {
        // Arrange
        var propertyInfo = typeof(SimpleTestClass).GetProperty(nameof(SimpleTestClass.PublicProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var type = wrapper.Type;

        // Assert
        Assert.AreEqual(typeof(string), type);
    }

    [TestMethod]
    public void Type_ReturnsPropertyType_ForNullableValueType()
    {
        // Arrange
        var propertyInfo = typeof(SimpleTestClass).GetProperty(nameof(SimpleTestClass.NullableValueTypeProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var type = wrapper.Type;

        // Assert
        Assert.AreEqual(typeof(DateTime?), type);
    }

    [TestMethod]
    public void Type_ReturnsPropertyType_ForGenericCollectionType()
    {
        // Arrange
        var propertyInfo = typeof(SimpleTestClass).GetProperty(nameof(SimpleTestClass.ReferenceTypeProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var type = wrapper.Type;

        // Assert
        Assert.AreEqual(typeof(List<string>), type);
    }

    [TestMethod]
    public void Key_ReturnsPropertyName()
    {
        // Arrange
        var propertyInfo = typeof(SimpleTestClass).GetProperty(nameof(SimpleTestClass.PublicProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var key = wrapper.Key;

        // Assert
        Assert.AreEqual("PublicProperty", key);
    }

    [TestMethod]
    public void Key_EqualToName()
    {
        // Arrange
        var propertyInfo = typeof(SimpleTestClass).GetProperty(nameof(SimpleTestClass.ValueTypeProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act & Assert
        Assert.AreEqual(wrapper.Name, wrapper.Key);
    }

    #endregion

    #region IsPrivateSetter Tests

    [TestMethod]
    public void IsPrivateSetter_PublicSetter_ReturnsFalse()
    {
        // Arrange
        var propertyInfo = typeof(SimpleTestClass).GetProperty(nameof(SimpleTestClass.PublicProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var isPrivate = wrapper.IsPrivateSetter;

        // Assert
        Assert.IsFalse(isPrivate);
    }

    [TestMethod]
    public void IsPrivateSetter_PrivateSetter_ReturnsTrue()
    {
        // Arrange
        var propertyInfo = typeof(SimpleTestClass).GetProperty(nameof(SimpleTestClass.PublicGetPrivateSet))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var isPrivate = wrapper.IsPrivateSetter;

        // Assert
        Assert.IsTrue(isPrivate);
    }

    [TestMethod]
    public void IsPrivateSetter_ReadOnlyProperty_ReturnsTrue()
    {
        // Arrange
        var propertyInfo = typeof(SimpleTestClass).GetProperty(nameof(SimpleTestClass.ReadOnlyProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var isPrivate = wrapper.IsPrivateSetter;

        // Assert
        Assert.IsTrue(isPrivate);
    }

    [TestMethod]
    public void IsPrivateSetter_ProtectedSetter_ReturnsFalse()
    {
        // Arrange
        var propertyInfo = typeof(AccessModifierTestClass).GetProperty(nameof(AccessModifierTestClass.PublicGetProtectedSet))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var isPrivate = wrapper.IsPrivateSetter;

        // Assert
        Assert.IsFalse(isPrivate);
    }

    [TestMethod]
    public void IsPrivateSetter_InternalSetter_ReturnsFalse()
    {
        // Arrange
        var propertyInfo = typeof(AccessModifierTestClass).GetProperty(nameof(AccessModifierTestClass.PublicGetInternalSet))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var isPrivate = wrapper.IsPrivateSetter;

        // Assert
        Assert.IsFalse(isPrivate);
    }

    [TestMethod]
    public void IsPrivateSetter_InitOnlyProperty_ReturnsFalse()
    {
        // Arrange
        var propertyInfo = typeof(AccessModifierTestClass).GetProperty(nameof(AccessModifierTestClass.InitOnlyProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var isPrivate = wrapper.IsPrivateSetter;

        // Assert
        // init accessors are not private, they are just restricted to initialization
        Assert.IsFalse(isPrivate);
    }

    #endregion

    #region GetCustomAttribute Tests

    [TestMethod]
    public void GetCustomAttribute_PropertyHasAttribute_ReturnsAttribute()
    {
        // Arrange
        var propertyInfo = typeof(AttributedTestClass).GetProperty(nameof(AttributedTestClass.PropertyWithDescription))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var attribute = wrapper.GetCustomAttribute<TestDescriptionAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("This is a test property", attribute.Description);
    }

    [TestMethod]
    public void GetCustomAttribute_PropertyDoesNotHaveAttribute_ReturnsNull()
    {
        // Arrange
        var propertyInfo = typeof(AttributedTestClass).GetProperty(nameof(AttributedTestClass.PropertyWithoutAttributes))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var attribute = wrapper.GetCustomAttribute<TestDescriptionAttribute>();

        // Assert
        Assert.IsNull(attribute);
    }

    [TestMethod]
    public void GetCustomAttribute_MultipleAttributesOnProperty_ReturnsRequestedAttribute()
    {
        // Arrange
        var propertyInfo = typeof(AttributedTestClass).GetProperty(nameof(AttributedTestClass.PropertyWithMultipleAttributes))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var descriptionAttr = wrapper.GetCustomAttribute<TestDescriptionAttribute>();
        var validationAttr = wrapper.GetCustomAttribute<TestValidationAttribute>();

        // Assert
        Assert.IsNotNull(descriptionAttr);
        Assert.AreEqual("Validated property", descriptionAttr.Description);
        Assert.IsNotNull(validationAttr);
        Assert.AreEqual(100, validationAttr.MaxLength);
    }

    [TestMethod]
    public void GetCustomAttribute_RequestDifferentAttributeType_ReturnsNull()
    {
        // Arrange
        var propertyInfo = typeof(AttributedTestClass).GetProperty(nameof(AttributedTestClass.PropertyWithDescription))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var attribute = wrapper.GetCustomAttribute<TestValidationAttribute>();

        // Assert
        Assert.IsNull(attribute);
    }

    #endregion

    #region GetCustomAttributes Tests

    [TestMethod]
    public void GetCustomAttributes_PropertyWithNoAttributes_ReturnsEmptyCollection()
    {
        // Arrange
        var propertyInfo = typeof(AttributedTestClass).GetProperty(nameof(AttributedTestClass.PropertyWithoutAttributes))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var attributes = wrapper.GetCustomAttributes();

        // Assert
        Assert.IsNotNull(attributes);
        Assert.AreEqual(0, attributes.Count());
    }

    [TestMethod]
    public void GetCustomAttributes_PropertyWithSingleAttribute_ReturnsAttribute()
    {
        // Arrange
        var propertyInfo = typeof(AttributedTestClass).GetProperty(nameof(AttributedTestClass.PropertyWithDescription))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var attributes = wrapper.GetCustomAttributes().ToList();

        // Assert
        Assert.AreEqual(1, attributes.Count);
        Assert.IsInstanceOfType(attributes[0], typeof(TestDescriptionAttribute));
    }

    [TestMethod]
    public void GetCustomAttributes_PropertyWithMultipleAttributes_ReturnsAllAttributes()
    {
        // Arrange
        var propertyInfo = typeof(AttributedTestClass).GetProperty(nameof(AttributedTestClass.PropertyWithMultipleAttributes))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var attributes = wrapper.GetCustomAttributes().ToList();

        // Assert
        Assert.AreEqual(2, attributes.Count);
        Assert.IsTrue(attributes.Any(a => a is TestDescriptionAttribute));
        Assert.IsTrue(attributes.Any(a => a is TestValidationAttribute));
    }

    [TestMethod]
    public void GetCustomAttributes_PropertyWithMultipleSameTypeAttributes_ReturnsAllInstances()
    {
        // Arrange
        var propertyInfo = typeof(AttributedTestClass).GetProperty(nameof(AttributedTestClass.PropertyWithMultipleSameAttributes))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var attributes = wrapper.GetCustomAttributes().ToList();
        var tagAttributes = attributes.OfType<TestTagAttribute>().ToList();

        // Assert
        Assert.AreEqual(3, tagAttributes.Count);
        Assert.IsTrue(tagAttributes.Any(t => t.Tag == "Primary"));
        Assert.IsTrue(tagAttributes.Any(t => t.Tag == "Important"));
        Assert.IsTrue(tagAttributes.Any(t => t.Tag == "Required"));
    }

    #endregion

    #region Attribute Caching Tests

    [TestMethod]
    public void GetCustomAttribute_CalledTwice_ReturnsSameInstance()
    {
        // Arrange
        var propertyInfo = typeof(AttributedTestClass).GetProperty(nameof(AttributedTestClass.PropertyWithDescription))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var firstCall = wrapper.GetCustomAttribute<TestDescriptionAttribute>();
        var secondCall = wrapper.GetCustomAttribute<TestDescriptionAttribute>();

        // Assert
        Assert.IsNotNull(firstCall);
        Assert.IsNotNull(secondCall);
        Assert.AreSame(firstCall, secondCall);
    }

    [TestMethod]
    public void GetCustomAttribute_CalledMultipleTimes_ReturnsCachedValue()
    {
        // Arrange
        var propertyInfo = typeof(AttributedTestClass).GetProperty(nameof(AttributedTestClass.PropertyWithDescription))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var call1 = wrapper.GetCustomAttribute<TestDescriptionAttribute>();
        var call2 = wrapper.GetCustomAttribute<TestDescriptionAttribute>();
        var call3 = wrapper.GetCustomAttribute<TestDescriptionAttribute>();

        // Assert
        Assert.AreSame(call1, call2);
        Assert.AreSame(call2, call3);
    }

    [TestMethod]
    public void GetCustomAttribute_NullResult_IsCached()
    {
        // Arrange
        var propertyInfo = typeof(AttributedTestClass).GetProperty(nameof(AttributedTestClass.PropertyWithoutAttributes))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var firstCall = wrapper.GetCustomAttribute<TestDescriptionAttribute>();
        var secondCall = wrapper.GetCustomAttribute<TestDescriptionAttribute>();

        // Assert
        Assert.IsNull(firstCall);
        Assert.IsNull(secondCall);
        // Both should be null, and the caching mechanism should prevent redundant lookups
    }

    [TestMethod]
    public void GetCustomAttribute_DifferentAttributeTypes_CachedSeparately()
    {
        // Arrange
        var propertyInfo = typeof(AttributedTestClass).GetProperty(nameof(AttributedTestClass.PropertyWithMultipleAttributes))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var descAttr1 = wrapper.GetCustomAttribute<TestDescriptionAttribute>();
        var valAttr1 = wrapper.GetCustomAttribute<TestValidationAttribute>();
        var descAttr2 = wrapper.GetCustomAttribute<TestDescriptionAttribute>();
        var valAttr2 = wrapper.GetCustomAttribute<TestValidationAttribute>();

        // Assert
        Assert.AreSame(descAttr1, descAttr2);
        Assert.AreSame(valAttr1, valAttr2);
        // Verify different attribute types are cached separately (they are different instances)
        Assert.AreNotEqual((object?)descAttr1, (object?)valAttr1);
    }

    [TestMethod]
    public void GetCustomAttributes_CalledTwice_ReturnsSameCollection()
    {
        // Arrange
        var propertyInfo = typeof(AttributedTestClass).GetProperty(nameof(AttributedTestClass.PropertyWithMultipleAttributes))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var firstCall = wrapper.GetCustomAttributes();
        var secondCall = wrapper.GetCustomAttributes();

        // Assert
        Assert.AreSame(firstCall, secondCall);
    }

    [TestMethod]
    public void GetCustomAttributes_CalledMultipleTimes_ReturnsCachedCollection()
    {
        // Arrange
        var propertyInfo = typeof(AttributedTestClass).GetProperty(nameof(AttributedTestClass.PropertyWithMultipleSameAttributes))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var call1 = wrapper.GetCustomAttributes();
        var call2 = wrapper.GetCustomAttributes();
        var call3 = wrapper.GetCustomAttributes();

        // Assert
        Assert.AreSame(call1, call2);
        Assert.AreSame(call2, call3);
    }

    #endregion

    #region Type Property Variety Tests

    [TestMethod]
    public void Type_IntProperty_ReturnsIntType()
    {
        // Arrange
        var propertyInfo = typeof(TypeVarietyTestClass).GetProperty(nameof(TypeVarietyTestClass.IntProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act & Assert
        Assert.AreEqual(typeof(int), wrapper.Type);
    }

    [TestMethod]
    public void Type_DoubleProperty_ReturnsDoubleType()
    {
        // Arrange
        var propertyInfo = typeof(TypeVarietyTestClass).GetProperty(nameof(TypeVarietyTestClass.DoubleProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act & Assert
        Assert.AreEqual(typeof(double), wrapper.Type);
    }

    [TestMethod]
    public void Type_BoolProperty_ReturnsBoolType()
    {
        // Arrange
        var propertyInfo = typeof(TypeVarietyTestClass).GetProperty(nameof(TypeVarietyTestClass.BoolProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act & Assert
        Assert.AreEqual(typeof(bool), wrapper.Type);
    }

    [TestMethod]
    public void Type_GuidProperty_ReturnsGuidType()
    {
        // Arrange
        var propertyInfo = typeof(TypeVarietyTestClass).GetProperty(nameof(TypeVarietyTestClass.GuidProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act & Assert
        Assert.AreEqual(typeof(Guid), wrapper.Type);
    }

    [TestMethod]
    public void Type_StringProperty_ReturnsStringType()
    {
        // Arrange
        var propertyInfo = typeof(TypeVarietyTestClass).GetProperty(nameof(TypeVarietyTestClass.StringProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act & Assert
        Assert.AreEqual(typeof(string), wrapper.Type);
    }

    [TestMethod]
    public void Type_ObjectProperty_ReturnsObjectType()
    {
        // Arrange
        var propertyInfo = typeof(TypeVarietyTestClass).GetProperty(nameof(TypeVarietyTestClass.ObjectProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act & Assert
        Assert.AreEqual(typeof(object), wrapper.Type);
    }

    [TestMethod]
    public void Type_ArrayProperty_ReturnsArrayType()
    {
        // Arrange
        var propertyInfo = typeof(TypeVarietyTestClass).GetProperty(nameof(TypeVarietyTestClass.ArrayProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act & Assert
        Assert.AreEqual(typeof(int[]), wrapper.Type);
    }

    [TestMethod]
    public void Type_DictionaryProperty_ReturnsDictionaryType()
    {
        // Arrange
        var propertyInfo = typeof(TypeVarietyTestClass).GetProperty(nameof(TypeVarietyTestClass.DictionaryProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act & Assert
        Assert.AreEqual(typeof(Dictionary<string, int>), wrapper.Type);
    }

    [TestMethod]
    public void Type_EnumerableProperty_ReturnsEnumerableType()
    {
        // Arrange
        var propertyInfo = typeof(TypeVarietyTestClass).GetProperty(nameof(TypeVarietyTestClass.EnumerableProperty))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act & Assert
        Assert.AreEqual(typeof(IEnumerable<string>), wrapper.Type);
    }

    #endregion

    #region IPropertyInfo Interface Implementation Tests

    [TestMethod]
    public void PropertyInfoWrapper_ImplementsIPropertyInfo()
    {
        // Arrange
        var propertyInfo = typeof(SimpleTestClass).GetProperty(nameof(SimpleTestClass.PublicProperty))!;

        // Act
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Assert
        Assert.IsInstanceOfType(wrapper, typeof(IPropertyInfo));
    }

    [TestMethod]
    public void IPropertyInfo_AccessThroughInterface_WorksCorrectly()
    {
        // Arrange
        var propertyInfo = typeof(SimpleTestClass).GetProperty(nameof(SimpleTestClass.PublicProperty))!;
        IPropertyInfo wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act & Assert
        Assert.AreEqual("PublicProperty", wrapper.Name);
        Assert.AreEqual(typeof(string), wrapper.Type);
        Assert.AreEqual("PublicProperty", wrapper.Key);
        Assert.IsFalse(wrapper.IsPrivateSetter);
        Assert.AreSame(propertyInfo, wrapper.PropertyInfo);
    }

    #endregion
}
