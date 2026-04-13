using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;

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

// Eight single-allowed attribute types used by the concurrent multi-type stress test (Scenario 2).
// TestAttr1..TestAttr4 are applied to ThreadSafetyTestClass.MixedProperty; TestAttr5..TestAttr8 are not.
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)] public class TestAttr1Attribute : Attribute { }
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)] public class TestAttr2Attribute : Attribute { }
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)] public class TestAttr3Attribute : Attribute { }
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)] public class TestAttr4Attribute : Attribute { }
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)] public class TestAttr5Attribute : Attribute { }
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)] public class TestAttr6Attribute : Attribute { }
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)] public class TestAttr7Attribute : Attribute { }
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)] public class TestAttr8Attribute : Attribute { }

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
/// POCO used by thread-safety tests. MixedProperty carries TestAttr1..TestAttr4 but not TestAttr5..TestAttr8.
/// </summary>
public class ThreadSafetyTestClass
{
    [TestAttr1]
    [TestAttr2]
    [TestAttr3]
    [TestAttr4]
    [TestDescription("Thread safety test property")]
    public string MixedProperty { get; set; } = string.Empty;
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

    #region Thread Safety Tests

    // Test-only subclass that counts reflection invocations. Used to verify the
    // "exactly once per type" and "exactly once for all-attrs" guarantees that the
    // lock buys over ConcurrentDictionary.GetOrAdd. Detecting >1 invocations is a
    // deterministic race indicator — the InvalidOperationException from Dictionary
    // corruption is a rarer downstream consequence of the same root cause, so we
    // assert on the more reliable signal.
    private class CountingPropertyInfoWrapper : PropertyInfoWrapper
    {
        public ConcurrentDictionary<Type, int> ReflectCount { get; } = new();
        public int ReflectAllCount;

        public CountingPropertyInfoWrapper(PropertyInfo propertyInfo) : base(propertyInfo) { }

        protected override Attribute? ReflectCustomAttribute(Type attrType)
        {
            ReflectCount.AddOrUpdate(attrType, 1, (_, v) => v + 1);
            return base.ReflectCustomAttribute(attrType);
        }

        protected override List<Attribute> ReflectAllCustomAttributes()
        {
            Interlocked.Increment(ref ReflectAllCount);
            return base.ReflectAllCustomAttributes();
        }
    }

    private const int StressThreadCount = 64;
    private const int StressIterationsPerThread = 10_000;

    /// <summary>
    /// Scenario 1: N threads repeatedly call GetCustomAttribute{T}() on the same wrapper for the same T.
    /// Against unmodified PropertyInfoWrapper, the internal Dictionary corrupts within a few hundred
    /// iterations and throws InvalidOperationException. Against the locked version, all calls succeed
    /// and return the same attribute instance.
    /// </summary>
    [TestMethod]
    public void GetCustomAttribute_ConcurrentSingleType_NoCorruption()
    {
        var propertyInfo = typeof(ThreadSafetyTestClass).GetProperty(nameof(ThreadSafetyTestClass.MixedProperty))!;
        var wrapper = new CountingPropertyInfoWrapper(propertyInfo);
        var gate = new ManualResetEventSlim(false);
        var exceptions = new ConcurrentBag<Exception>();
        var observedInstances = new ConcurrentBag<TestDescriptionAttribute?>();

        var tasks = Enumerable.Range(0, StressThreadCount).Select(_ => Task.Run(() =>
        {
            gate.Wait();
            for (int i = 0; i < StressIterationsPerThread; i++)
            {
                try
                {
                    var attr = wrapper.GetCustomAttribute<TestDescriptionAttribute>();
                    if (i == StressIterationsPerThread - 1)
                    {
                        observedInstances.Add(attr);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    return;
                }
            }
        })).ToArray();

        gate.Set();
        Task.WaitAll(tasks);

        Assert.AreEqual(0, exceptions.Count,
            $"Expected zero exceptions across {StressThreadCount * StressIterationsPerThread} concurrent calls. " +
            $"First exception: {exceptions.FirstOrDefault()?.GetType().Name}: {exceptions.FirstOrDefault()?.Message}");

        Assert.IsTrue(wrapper.ReflectCount.TryGetValue(typeof(TestDescriptionAttribute), out var count),
            "TestDescriptionAttribute must have been queried.");
        Assert.AreEqual(1, count,
            $"Reflection for TestDescriptionAttribute must be invoked exactly once; observed {count}. " +
            "More than one invocation indicates a cold-cache race between threads.");

        var distinctInstances = observedInstances.Where(a => a != null).Distinct().ToList();
        Assert.AreEqual(1, distinctInstances.Count, "All threads must observe the same cached attribute instance.");
    }

    /// <summary>
    /// Scenario 2: N threads concurrently lookup randomly-chosen TAttr from 8 types (4 present, 4 absent).
    /// Verifies zero exceptions AND that reflection is invoked exactly once per attribute type —
    /// the core property that the lock provides over ConcurrentDictionary.GetOrAdd.
    /// </summary>
    [TestMethod]
    public void GetCustomAttribute_ConcurrentMultiType_ReflectsOncePerType()
    {
        var propertyInfo = typeof(ThreadSafetyTestClass).GetProperty(nameof(ThreadSafetyTestClass.MixedProperty))!;
        var wrapper = new CountingPropertyInfoWrapper(propertyInfo);

        var lookupActions = new Action[]
        {
            () => wrapper.GetCustomAttribute<TestAttr1Attribute>(),
            () => wrapper.GetCustomAttribute<TestAttr2Attribute>(),
            () => wrapper.GetCustomAttribute<TestAttr3Attribute>(),
            () => wrapper.GetCustomAttribute<TestAttr4Attribute>(),
            () => wrapper.GetCustomAttribute<TestAttr5Attribute>(),
            () => wrapper.GetCustomAttribute<TestAttr6Attribute>(),
            () => wrapper.GetCustomAttribute<TestAttr7Attribute>(),
            () => wrapper.GetCustomAttribute<TestAttr8Attribute>(),
        };

        var gate = new ManualResetEventSlim(false);
        var exceptions = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, StressThreadCount).Select(threadIndex => Task.Run(() =>
        {
            // Per-thread deterministic seeding. System.Random is not thread-safe; sharing it
            // across threads would itself throw and produce a confusing failure.
            var rng = new Random(12345 + threadIndex);
            gate.Wait();
            for (int i = 0; i < StressIterationsPerThread; i++)
            {
                try
                {
                    lookupActions[rng.Next(lookupActions.Length)]();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    return;
                }
            }
        })).ToArray();

        gate.Set();
        Task.WaitAll(tasks);

        Assert.AreEqual(0, exceptions.Count,
            $"Expected zero exceptions. First: {exceptions.FirstOrDefault()?.GetType().Name}: {exceptions.FirstOrDefault()?.Message}");

        Assert.AreEqual(8, wrapper.ReflectCount.Count, "All 8 attribute types must have been queried at least once.");
        foreach (var entry in wrapper.ReflectCount)
        {
            Assert.AreEqual(1, entry.Value,
                $"Reflection for {entry.Key.Name} must be invoked exactly once; observed {entry.Value}.");
        }
    }

    /// <summary>
    /// Scenario 3: N threads concurrently call GetCustomAttributes() on the same wrapper.
    /// Verifies zero exceptions and that all callers observe the same cached List reference.
    /// </summary>
    [TestMethod]
    public void GetCustomAttributes_ConcurrentAccess_NoCorruption()
    {
        var propertyInfo = typeof(ThreadSafetyTestClass).GetProperty(nameof(ThreadSafetyTestClass.MixedProperty))!;
        var wrapper = new CountingPropertyInfoWrapper(propertyInfo);
        var gate = new ManualResetEventSlim(false);
        var exceptions = new ConcurrentBag<Exception>();
        var observedReferences = new ConcurrentBag<IEnumerable<Attribute>>();

        var tasks = Enumerable.Range(0, StressThreadCount).Select(_ => Task.Run(() =>
        {
            gate.Wait();
            for (int i = 0; i < StressIterationsPerThread; i++)
            {
                try
                {
                    var attrs = wrapper.GetCustomAttributes();
                    if (i == StressIterationsPerThread - 1)
                    {
                        observedReferences.Add(attrs);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    return;
                }
            }
        })).ToArray();

        gate.Set();
        Task.WaitAll(tasks);

        Assert.AreEqual(0, exceptions.Count,
            $"Expected zero exceptions. First: {exceptions.FirstOrDefault()?.GetType().Name}: {exceptions.FirstOrDefault()?.Message}");

        var distinctRefs = observedReferences.Distinct(ReferenceEqualityComparer.Instance).ToList();
        Assert.AreEqual(1, distinctRefs.Count, "All threads must observe the same cached list reference.");

        Assert.AreEqual(1, wrapper.ReflectAllCount,
            $"ReflectAllCustomAttributes must be invoked exactly once; observed {wrapper.ReflectAllCount}.");

        var baseline = wrapper.GetCustomAttributes().ToList();
        Assert.AreEqual(5, baseline.Count, "Expected 5 attributes on MixedProperty (TestAttr1..4 + TestDescription).");
    }

    /// <summary>
    /// Scenario 8: Shared-wrapper sanity. Resolving IPropertyInfoList{T} twice from DI must yield
    /// the same list instance, and calling GetPropertyInfo on each must return the same wrapper.
    /// Codifies the invariant that makes thread-safety necessary. A future refactor that makes
    /// wrappers per-scope or per-instance would fail this test and flag the contract change.
    /// </summary>
    [TestMethod]
    public void PropertyInfoList_ResolvedTwiceFromDI_SharesSameWrappers()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(IPropertyInfoList<>), typeof(PropertyInfoList<>));
        services.AddTransient<CreatePropertyInfoWrapper>(_ => pi => new PropertyInfoWrapper(pi));
        using var sp = services.BuildServiceProvider();

        var list1 = sp.GetRequiredService<IPropertyInfoList<Neatoo.UnitTest.Unit.Rules.TestValidateObject>>();
        var list2 = sp.GetRequiredService<IPropertyInfoList<Neatoo.UnitTest.Unit.Rules.TestValidateObject>>();

        Assert.AreSame(list1, list2, "Singleton registration must yield the same PropertyInfoList instance.");

        var info1 = list1.GetPropertyInfo(nameof(Neatoo.UnitTest.Unit.Rules.TestValidateObject.StringProperty));
        var info2 = list2.GetPropertyInfo(nameof(Neatoo.UnitTest.Unit.Rules.TestValidateObject.StringProperty));

        Assert.IsNotNull(info1);
        Assert.IsNotNull(info2);
        Assert.AreSame(info1, info2, "PropertyInfoList<T> must return the same IPropertyInfo on repeat resolutions.");
        Assert.IsInstanceOfType(info1, typeof(PropertyInfoWrapper));
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
