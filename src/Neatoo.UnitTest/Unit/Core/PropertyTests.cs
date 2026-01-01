using System.ComponentModel;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using Neatoo.Internal;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// Test POCO class with various property configurations for testing Property{T}.
/// </summary>
public class TestPoco
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; private set; }
    public string? NullableString { get; set; }
    public Guid Id { get; set; }
    public Guid? NullableId { get; set; }
    public List<int> Numbers { get; set; } = new();
    public object? GenericObject { get; set; }
}

/// <summary>
/// Unit tests for the Property{T} class.
/// Tests construction, value management, property change notifications, busy state, and read-only behavior.
/// </summary>
[TestClass]
public class PropertyTests
{
    private PropertyInfoWrapper _stringPropertyInfo = null!;
    private PropertyInfoWrapper _privateSetterPropertyInfo = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var nameProperty = typeof(TestPoco).GetProperty(nameof(TestPoco.Name))!;
        _stringPropertyInfo = new PropertyInfoWrapper(nameProperty);

        var ageProperty = typeof(TestPoco).GetProperty(nameof(TestPoco.Age))!;
        _privateSetterPropertyInfo = new PropertyInfoWrapper(ageProperty);
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithPropertyInfo_SetsNameFromPropertyInfo()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Name))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var property = new Property<string>(wrapper);

        // Assert
        Assert.AreEqual("Name", property.Name);
    }

    [TestMethod]
    public void Constructor_WithPropertyInfo_SetsIsReadOnlyFromIsPrivateSetter()
    {
        // Arrange - Age has a private setter
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Age))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var property = new Property<int>(wrapper);

        // Assert
        Assert.IsTrue(property.IsReadOnly);
    }

    [TestMethod]
    public void Constructor_WithPropertyInfoNotPrivateSetter_SetsIsReadOnlyFalse()
    {
        // Arrange - Name has a public setter
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Name))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);

        // Act
        var property = new Property<string>(wrapper);

        // Assert
        Assert.IsFalse(property.IsReadOnly);
    }

    [TestMethod]
    public void Constructor_WithPropertyInfo_InitializesValueToDefault()
    {
        // Arrange & Act
        var stringProperty = new Property<string>(_stringPropertyInfo);

        var intPropertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Age))!;
        var intWrapper = new PropertyInfoWrapper(intPropertyInfo);
        var intProperty = new Property<int>(intWrapper);

        // Assert
        Assert.IsNull(stringProperty.Value);
        Assert.AreEqual(0, intProperty.Value);
    }

    [TestMethod]
    public void JsonConstructor_WithParameters_SetsAllProperties()
    {
        // Arrange & Act
        var property = new Property<string>("PropertyName", "TestValue", true);

        // Assert
        Assert.AreEqual("PropertyName", property.Name);
        Assert.AreEqual("TestValue", property.Value);
        Assert.IsTrue(property.IsReadOnly);
    }

    [TestMethod]
    public void JsonConstructor_WithValueTypeAndFalseReadOnly_SetsAllProperties()
    {
        // Arrange & Act
        var property = new Property<int>("IntProperty", 42, false);

        // Assert
        Assert.AreEqual("IntProperty", property.Name);
        Assert.AreEqual(42, property.Value);
        Assert.IsFalse(property.IsReadOnly);
    }

    #endregion

    #region Type Property Tests

    [TestMethod]
    public void Type_ForStringProperty_ReturnsStringType()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);

        // Act
        var type = property.Type;

        // Assert
        Assert.AreEqual(typeof(string), type);
    }

    [TestMethod]
    public void Type_ForIntProperty_ReturnsIntType()
    {
        // Arrange
        var property = new Property<int>(_privateSetterPropertyInfo);

        // Act
        var type = property.Type;

        // Assert
        Assert.AreEqual(typeof(int), type);
    }

    [TestMethod]
    public void Type_ForNullableGuidProperty_ReturnsNullableGuidType()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.NullableId))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        var property = new Property<Guid?>(wrapper);

        // Act
        var type = property.Type;

        // Assert
        Assert.AreEqual(typeof(Guid?), type);
    }

    [TestMethod]
    public void Type_ForListProperty_ReturnsListType()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Numbers))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        var property = new Property<List<int>>(wrapper);

        // Act
        var type = property.Type;

        // Assert
        Assert.AreEqual(typeof(List<int>), type);
    }

    #endregion

    #region SetValue and PropertyChanged Event Tests

    [TestMethod]
    public void SetValue_ChangingValue_RaisesPropertyChangedEvent()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        var eventRaised = false;
        string? propertyName = null;

        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaised = true;
                propertyName = e.PropertyName;
            }
        };

        // Act
        property.Value = "NewValue";

        // Assert
        Assert.IsTrue(eventRaised);
        Assert.AreEqual("Value", propertyName);
    }

    [TestMethod]
    public void SetValue_SameReferenceValue_DoesNotRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.GenericObject))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        var testObject = new object();
        var property = new Property<object>(wrapper);
        property.Value = testObject;

        var eventRaisedCount = 0;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaisedCount++;
            }
        };

        // Act
        property.Value = testObject; // Same reference

        // Assert
        Assert.AreEqual(0, eventRaisedCount);
    }

    [TestMethod]
    public void SetValue_SameValueTypeValue_DoesNotRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Id))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        var property = new Property<int>(wrapper);
        property.Value = 42;

        var eventRaisedCount = 0;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaisedCount++;
            }
        };

        // Act
        property.Value = 42; // Same value

        // Assert
        Assert.AreEqual(0, eventRaisedCount);
    }

    [TestMethod]
    public void SetValue_DifferentValueTypeValue_RaisesPropertyChangedEvent()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Id))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        var property = new Property<int>(wrapper);
        property.Value = 42;

        var eventRaised = false;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaised = true;
            }
        };

        // Act
        property.Value = 100;

        // Assert
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public void SetValue_ToNull_RaisesPropertyChangedEvent()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        property.Value = "InitialValue";

        var eventRaised = false;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaised = true;
            }
        };

        // Act
        property.Value = null;

        // Assert
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public void SetValue_FromNullToValue_RaisesPropertyChangedEvent()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        var eventRaised = false;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaised = true;
            }
        };

        // Act
        property.Value = "NewValue";

        // Assert
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public void SetValue_NullToNull_DoesNotRaisePropertyChangedEvent()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        // Value is already null by default

        var eventRaisedCount = 0;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaisedCount++;
            }
        };

        // Act
        property.Value = null;

        // Assert
        Assert.AreEqual(0, eventRaisedCount);
    }

    #endregion

    #region NeatooPropertyChanged Event Tests

    [TestMethod]
    public async Task SetValue_ChangingValue_RaisesNeatooPropertyChangedEvent()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        var eventRaised = false;
        NeatooPropertyChangedEventArgs? receivedArgs = null;

        property.NeatooPropertyChanged += (args) =>
        {
            eventRaised = true;
            receivedArgs = args;
            return Task.CompletedTask;
        };

        // Act
        await property.SetValue("NewValue");

        // Assert
        Assert.IsTrue(eventRaised);
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual("Name", receivedArgs.PropertyName);
    }

    [TestMethod]
    public async Task SetValue_SameValue_DoesNotRaiseNeatooPropertyChangedEvent()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Id))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        var property = new Property<int>(wrapper);
        await property.SetValue(42);

        var eventRaisedCount = 0;
        property.NeatooPropertyChanged += (args) =>
        {
            eventRaisedCount++;
            return Task.CompletedTask;
        };

        // Act
        await property.SetValue(42);

        // Assert
        Assert.AreEqual(0, eventRaisedCount);
    }

    #endregion

    #region AreSame Tests

    [TestMethod]
    public void AreSame_BothNullReferenceTypes_ReturnsTrue()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        // Value is null, setting to null should not raise event (AreSame returns true)

        var eventRaisedCount = 0;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaisedCount++;
            }
        };

        // Act
        property.Value = null;

        // Assert
        Assert.AreEqual(0, eventRaisedCount); // AreSame(null, null) == true
    }

    [TestMethod]
    public void AreSame_OneNullOneNotNull_ReturnsFalse()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        // Start with null

        var eventRaised = false;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaised = true;
            }
        };

        // Act
        property.Value = "NotNull";

        // Assert
        Assert.IsTrue(eventRaised); // AreSame(null, "NotNull") == false
    }

    [TestMethod]
    public void AreSame_SameReferenceTypes_ReturnsTrue()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.GenericObject))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        var testObject = new TestReferenceClass { Id = 1 };
        var property = new Property<TestReferenceClass>(wrapper);
        property.Value = testObject;

        var eventRaisedCount = 0;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaisedCount++;
            }
        };

        // Act
        property.Value = testObject; // Same reference

        // Assert
        Assert.AreEqual(0, eventRaisedCount);
    }

    [TestMethod]
    public void AreSame_DifferentReferenceTypesWithSameContent_ReturnsFalse()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.GenericObject))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        var testObject1 = new TestReferenceClass { Id = 1 };
        var testObject2 = new TestReferenceClass { Id = 1 }; // Same content, different reference
        var property = new Property<TestReferenceClass>(wrapper);
        property.Value = testObject1;

        var eventRaised = false;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaised = true;
            }
        };

        // Act
        property.Value = testObject2; // Different reference

        // Assert
        Assert.IsTrue(eventRaised); // ReferenceEquals returns false
    }

    [TestMethod]
    public void AreSame_ValueTypes_UsesEqualsComparison()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Id))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        var property = new Property<int>(wrapper);
        property.Value = 42;

        var eventRaisedCount = 0;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaisedCount++;
            }
        };

        // Act
        property.Value = 42; // Same value

        // Assert
        Assert.AreEqual(0, eventRaisedCount); // Value types use Equals
    }

    [TestMethod]
    public void AreSame_DifferentValueTypes_ReturnsFalse()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Id))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        var property = new Property<int>(wrapper);
        property.Value = 42;

        var eventRaised = false;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaised = true;
            }
        };

        // Act
        property.Value = 100;

        // Assert
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public void AreSame_SameStructValues_ReturnsTrue()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Id))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        var guid = Guid.NewGuid();
        var property = new Property<Guid>(wrapper);
        property.Value = guid;

        var eventRaisedCount = 0;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaisedCount++;
            }
        };

        // Act
        property.Value = guid; // Same Guid value

        // Assert
        Assert.AreEqual(0, eventRaisedCount);
    }

    #endregion

    #region LoadValue Tests

    [TestMethod]
    public void LoadValue_SetsValueWithoutTriggeringPropertyChangedEvent()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        var eventRaisedCount = 0;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaisedCount++;
            }
        };

        // Act
        property.LoadValue("LoadedValue");

        // Assert
        Assert.AreEqual("LoadedValue", property.Value);
        Assert.AreEqual(0, eventRaisedCount);
    }

    [TestMethod]
    public void LoadValue_SetsValueWithoutTriggeringNeatooPropertyChangedEvent()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        var eventRaisedCount = 0;
        property.NeatooPropertyChanged += (args) =>
        {
            eventRaisedCount++;
            return Task.CompletedTask;
        };

        // Act
        property.LoadValue("LoadedValue");

        // Assert
        Assert.AreEqual("LoadedValue", property.Value);
        Assert.AreEqual(0, eventRaisedCount);
    }

    [TestMethod]
    public void LoadValue_WithValueType_SetsValue()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Id))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        var property = new Property<int>(wrapper);

        // Act
        property.LoadValue(42);

        // Assert
        Assert.AreEqual(42, property.Value);
    }

    [TestMethod]
    public void LoadValue_WithNull_SetsValueToNull()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        property.Value = "InitialValue";

        // Act
        property.LoadValue(null);

        // Assert
        Assert.IsNull(property.Value);
    }

    #endregion

    #region IsReadOnly Tests

    [TestMethod]
    public void SetValue_WhenIsReadOnlyTrue_ThrowsPropertyReadOnlyException()
    {
        // Arrange - Age has private setter
        var property = new Property<int>(_privateSetterPropertyInfo);

        // Act & Assert
        Assert.ThrowsException<PropertyReadOnlyException>(() => property.Value = 25);
    }

    [TestMethod]
    public async Task SetValue_MethodWhenIsReadOnlyTrue_ThrowsPropertyReadOnlyException()
    {
        // Arrange - Age has private setter
        var property = new Property<int>(_privateSetterPropertyInfo);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PropertyReadOnlyException>(async () => await property.SetValue(25));
    }

    [TestMethod]
    public void SetValue_WhenIsReadOnlyFalse_SetsValue()
    {
        // Arrange - Name has public setter
        var property = new Property<string>(_stringPropertyInfo);

        // Act
        property.Value = "NewValue";

        // Assert
        Assert.AreEqual("NewValue", property.Value);
    }

    [TestMethod]
    public void IsReadOnly_InitializedFromPropertyInfo_IsTrue()
    {
        // Arrange - Age has private setter

        // Act
        var property = new Property<int>(_privateSetterPropertyInfo);

        // Assert
        Assert.IsTrue(property.IsReadOnly);
    }

    [TestMethod]
    public void IsReadOnly_InitializedFromPropertyInfo_IsFalse()
    {
        // Arrange - Name has public setter

        // Act
        var property = new Property<string>(_stringPropertyInfo);

        // Assert
        Assert.IsFalse(property.IsReadOnly);
    }

    #endregion

    #region IsBusy Tests

    [TestMethod]
    public void IsBusy_InitialState_ReturnsFalse()
    {
        // Arrange & Act
        var property = new Property<string>(_stringPropertyInfo);

        // Assert
        Assert.IsFalse(property.IsBusy);
    }

    [TestMethod]
    public void IsBusy_AfterAddMarkedBusy_ReturnsTrue()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);

        // Act
        property.AddMarkedBusy(1);

        // Assert
        Assert.IsTrue(property.IsBusy);
    }

    [TestMethod]
    public void IsBusy_AfterAddAndRemoveMarkedBusy_ReturnsFalse()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        property.AddMarkedBusy(1);

        // Act
        property.RemoveMarkedBusy(1);

        // Assert
        Assert.IsFalse(property.IsBusy);
    }

    #endregion

    #region AddMarkedBusy / RemoveMarkedBusy Tests

    [TestMethod]
    public void AddMarkedBusy_AddsIdToIsMarkedBusyList()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);

        // Act
        property.AddMarkedBusy(123);

        // Assert
        Assert.IsTrue(property.IsMarkedBusy.Contains(123));
    }

    [TestMethod]
    public void AddMarkedBusy_SameIdTwice_OnlyAddsOnce()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);

        // Act
        property.AddMarkedBusy(123);
        property.AddMarkedBusy(123);

        // Assert
        Assert.AreEqual(1, property.IsMarkedBusy.Count);
    }

    [TestMethod]
    public void AddMarkedBusy_MultipleIds_AddsAll()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);

        // Act
        property.AddMarkedBusy(1);
        property.AddMarkedBusy(2);
        property.AddMarkedBusy(3);

        // Assert
        Assert.AreEqual(3, property.IsMarkedBusy.Count);
        Assert.IsTrue(property.IsMarkedBusy.Contains(1));
        Assert.IsTrue(property.IsMarkedBusy.Contains(2));
        Assert.IsTrue(property.IsMarkedBusy.Contains(3));
    }

    [TestMethod]
    public void AddMarkedBusy_RaisesPropertyChangedForIsMarkedBusy()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        var propertyChangedProperties = new List<string>();
        property.PropertyChanged += (sender, e) =>
        {
            propertyChangedProperties.Add(e.PropertyName!);
        };

        // Act
        property.AddMarkedBusy(1);

        // Assert
        Assert.IsTrue(propertyChangedProperties.Contains("IsMarkedBusy"));
    }

    [TestMethod]
    public void AddMarkedBusy_RaisesPropertyChangedForIsBusy()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        var propertyChangedProperties = new List<string>();
        property.PropertyChanged += (sender, e) =>
        {
            propertyChangedProperties.Add(e.PropertyName!);
        };

        // Act
        property.AddMarkedBusy(1);

        // Assert
        Assert.IsTrue(propertyChangedProperties.Contains("IsBusy"));
    }

    [TestMethod]
    public void RemoveMarkedBusy_RemovesIdFromList()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        property.AddMarkedBusy(123);

        // Act
        property.RemoveMarkedBusy(123);

        // Assert
        Assert.IsFalse(property.IsMarkedBusy.Contains(123));
    }

    [TestMethod]
    public void RemoveMarkedBusy_NonExistentId_DoesNotThrow()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);

        // Act & Assert (should not throw)
        property.RemoveMarkedBusy(999);
    }

    [TestMethod]
    public void RemoveMarkedBusy_RaisesPropertyChangedForIsMarkedBusy()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        property.AddMarkedBusy(1);

        var propertyChangedProperties = new List<string>();
        property.PropertyChanged += (sender, e) =>
        {
            propertyChangedProperties.Add(e.PropertyName!);
        };

        // Act
        property.RemoveMarkedBusy(1);

        // Assert
        Assert.IsTrue(propertyChangedProperties.Contains("IsMarkedBusy"));
    }

    [TestMethod]
    public void RemoveMarkedBusy_RaisesPropertyChangedForIsBusy()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        property.AddMarkedBusy(1);

        var propertyChangedProperties = new List<string>();
        property.PropertyChanged += (sender, e) =>
        {
            propertyChangedProperties.Add(e.PropertyName!);
        };

        // Act
        property.RemoveMarkedBusy(1);

        // Assert
        Assert.IsTrue(propertyChangedProperties.Contains("IsBusy"));
    }

    #endregion

    #region GetValue / SetValue through IProperty Interface Tests

    [TestMethod]
    public void IPropertyValue_Get_ReturnsCorrectValue()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        property.Value = "TestValue";
        IProperty iproperty = property;

        // Act
        var value = iproperty.Value;

        // Assert
        Assert.AreEqual("TestValue", value);
    }

    [TestMethod]
    public void IPropertyValue_Set_SetsValueCorrectly()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        IProperty iproperty = property;

        // Act
        iproperty.Value = "NewValue";

        // Assert
        Assert.AreEqual("NewValue", property.Value);
    }

    [TestMethod]
    public async Task IPropertySetValue_SetsValueCorrectly()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        IProperty iproperty = property;

        // Act
        await iproperty.SetValue("NewValue");

        // Assert
        Assert.AreEqual("NewValue", property.Value);
    }

    [TestMethod]
    public void IPropertyValue_WithValueType_GetReturnsCorrectValue()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Id))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        var property = new Property<int>(wrapper);
        property.Value = 42;
        IProperty iproperty = property;

        // Act
        var value = iproperty.Value;

        // Assert
        Assert.AreEqual(42, value);
    }

    [TestMethod]
    public void IPropertyValue_WithValueType_SetSetsCorrectValue()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Id))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        var property = new Property<int>(wrapper);
        IProperty iproperty = property;

        // Act
        iproperty.Value = 100;

        // Assert
        Assert.AreEqual(100, property.Value);
    }

    #endregion

    #region PropertyTypeMismatch Tests

    [TestMethod]
    public async Task SetValue_WrongType_ThrowsPropertyTypeMismatchException()
    {
        // Arrange
        var propertyInfo = typeof(TestPoco).GetProperty(nameof(TestPoco.Id))!;
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        var property = new Property<int>(wrapper);

        // Act & Assert
        var ex = await Assert.ThrowsExceptionAsync<PropertyTypeMismatchException>(
            async () => await property.SetValue("wrong type"));

        Assert.IsTrue(ex.Message.Contains("String"));
        Assert.IsTrue(ex.Message.Contains("Int32"));
    }

    [TestMethod]
    public async Task SetPrivateValue_WrongType_ThrowsPropertyTypeMismatchException()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PropertyTypeMismatchException>(
            async () => await property.SetPrivateValue(123));
    }

    #endregion

    #region Task Property Tests

    [TestMethod]
    public void Task_InitialState_IsCompletedTask()
    {
        // Arrange & Act
        var property = new Property<string>(_stringPropertyInfo);

        // Assert
        Assert.IsTrue(property.Task.IsCompleted);
    }

    [TestMethod]
    public async Task WaitForTasks_WhenNoValueAsBase_CompletesImmediately()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);

        // Act & Assert (should complete without hanging)
        await property.WaitForTasks();
    }

    #endregion

    #region IsSelfBusy Tests

    [TestMethod]
    public void IsSelfBusy_InitialState_ReturnsFalse()
    {
        // Arrange & Act
        var property = new Property<string>(_stringPropertyInfo);

        // Assert
        Assert.IsFalse(property.IsSelfBusy);
    }

    #endregion

    #region SetPrivateValue Tests

    [TestMethod]
    public async Task SetPrivateValue_WithQuietlyTrue_DoesNotRaisePropertyChangedEvent()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        var eventRaisedCount = 0;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaisedCount++;
            }
        };

        // Act
        await property.SetPrivateValue("NewValue", quietly: true);

        // Assert
        Assert.AreEqual("NewValue", property.Value);
        Assert.AreEqual(0, eventRaisedCount);
    }

    [TestMethod]
    public async Task SetPrivateValue_WithQuietlyFalse_RaisesPropertyChangedEvent()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        var eventRaised = false;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaised = true;
            }
        };

        // Act
        await property.SetPrivateValue("NewValue", quietly: false);

        // Assert
        Assert.AreEqual("NewValue", property.Value);
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public async Task SetPrivateValue_BothNull_ReturnsCompletedTask()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        // Value starts as null

        // Act
        await property.SetPrivateValue(null);

        // Assert - just verify no exception and value is still null
        Assert.IsNull(property.Value);
    }

    #endregion

    #region OnDeserialized Tests

    [TestMethod]
    public void OnDeserialized_WithNonNotifyPropertyChangedValue_DoesNotThrow()
    {
        // Arrange
        var property = new Property<string>("TestProp", "TestValue", false);

        // Act & Assert (should not throw)
        property.OnDeserialized();
    }

    #endregion

    #region INotifyPropertyChanged Implementation Tests

    [TestMethod]
    public void Property_ImplementsINotifyPropertyChanged()
    {
        // Arrange & Act
        var property = new Property<string>(_stringPropertyInfo);

        // Assert
        Assert.IsInstanceOfType(property, typeof(INotifyPropertyChanged));
    }

    [TestMethod]
    public void Property_ImplementsINotifyNeatooPropertyChanged()
    {
        // Arrange & Act
        var property = new Property<string>(_stringPropertyInfo);

        // Assert
        Assert.IsInstanceOfType(property, typeof(INotifyNeatooPropertyChanged));
    }

    [TestMethod]
    public void Property_ImplementsIProperty()
    {
        // Arrange & Act
        var property = new Property<string>(_stringPropertyInfo);

        // Assert
        Assert.IsInstanceOfType(property, typeof(IProperty));
    }

    [TestMethod]
    public void Property_ImplementsIPropertyOfT()
    {
        // Arrange & Act
        var property = new Property<string>(_stringPropertyInfo);

        // Assert
        Assert.IsInstanceOfType(property, typeof(IProperty<string>));
    }

    #endregion

    #region Edge Cases and Thread Safety Hints

    [TestMethod]
    public void AddMarkedBusy_CalledConcurrently_HandlesCorrectly()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        var tasks = new List<Task>();

        // Act - Simulate concurrent access (note: this is a basic test, not a full stress test)
        for (int i = 0; i < 100; i++)
        {
            var id = i;
            tasks.Add(Task.Run(() => property.AddMarkedBusy(id)));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - All unique IDs should be present
        Assert.AreEqual(100, property.IsMarkedBusy.Count);
    }

    [TestMethod]
    public void RemoveMarkedBusy_CalledConcurrently_HandlesCorrectly()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        for (int i = 0; i < 100; i++)
        {
            property.AddMarkedBusy(i);
        }

        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var id = i;
            tasks.Add(Task.Run(() => property.RemoveMarkedBusy(id)));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.AreEqual(0, property.IsMarkedBusy.Count);
    }

    [TestMethod]
    public async Task IsMarkedBusy_ConcurrentReadWrite_NoException()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        var tasks = new List<Task>();
        var cts = new CancellationTokenSource();

        // Act - Concurrent writes
        for (int i = 0; i < 100; i++)
        {
            var id = i;
            tasks.Add(Task.Run(() => property.AddMarkedBusy(id)));
        }

        // Concurrent reads while writing
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var snapshot = property.IsMarkedBusy;
                // Enumerate the snapshot - should not throw InvalidOperationException
                foreach (var item in snapshot) { _ = item; }
                return snapshot.Count;
            }));
        }

        // Should not throw InvalidOperationException from concurrent modification
        await Task.WhenAll(tasks);

        // Assert - All 100 unique IDs should be present
        Assert.AreEqual(100, property.IsMarkedBusy.Count);
    }

    [TestMethod]
    public void IsMarkedBusy_ReturnsSnapshot_NotLiveReference()
    {
        // Arrange
        var property = new Property<string>(_stringPropertyInfo);
        property.AddMarkedBusy(1);
        property.AddMarkedBusy(2);

        // Act - Get a snapshot
        var snapshot1 = property.IsMarkedBusy;

        // Modify the property
        property.AddMarkedBusy(3);

        // Get another snapshot
        var snapshot2 = property.IsMarkedBusy;

        // Assert - First snapshot should not include the new ID
        Assert.AreEqual(2, snapshot1.Count);
        Assert.AreEqual(3, snapshot2.Count);
        Assert.IsFalse(snapshot1.Contains(3));
        Assert.IsTrue(snapshot2.Contains(3));
    }

    #endregion
}

/// <summary>
/// Helper class for testing reference type comparisons.
/// </summary>
public class TestReferenceClass
{
    public int Id { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is TestReferenceClass other)
        {
            return Id == other.Id;
        }
        return false;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
