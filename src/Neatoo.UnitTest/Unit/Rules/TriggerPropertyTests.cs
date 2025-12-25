using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Rules;
using System.Collections.Generic;

namespace Neatoo.UnitTest.Unit.Rules;

#region Test POCO Classes

/// <summary>
/// Simple POCO for testing basic property expressions.
/// </summary>
public class Person
{
    public string? Name { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public double Salary { get; set; }
    public DateTime? BirthDate { get; set; }
    public Address? Address { get; set; }
    public List<Order>? Orders { get; set; }
}

/// <summary>
/// Nested POCO for testing nested property expressions.
/// </summary>
public class Address
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? ZipCode { get; set; }
    public Country? Country { get; set; }
}

/// <summary>
/// Third-level nested POCO for testing deep nesting.
/// </summary>
public class Country
{
    public string? Name { get; set; }
    public string? Code { get; set; }
}

/// <summary>
/// POCO for testing collection indexer expressions.
/// </summary>
public class Order
{
    public int OrderId { get; set; }
    public string? ProductName { get; set; }
    public decimal Price { get; set; }
    public OrderDetails? Details { get; set; }
}

/// <summary>
/// Nested POCO within collection item for testing collection indexer with nested properties.
/// </summary>
public class OrderDetails
{
    public string? Description { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// Legacy test subject class for backward compatibility with existing tests.
/// </summary>
public class TriggerPropertyTestSubject
{
    public TriggerPropertyTestSubjectChild? Child { get; set; }
}

/// <summary>
/// Legacy child class for backward compatibility with existing tests.
/// </summary>
public class TriggerPropertyTestSubjectChild
{
    public string? ChildProperty { get; set; }
}

#endregion

/// <summary>
/// Comprehensive unit tests for the TriggerProperty class.
/// Tests the property path matching functionality used by the rules system.
/// </summary>
[TestClass]
public class TriggerPropertyTests
{
    #region Constructor Tests

    [TestMethod]
    public void Constructor_SimplePropertyExpression_ExtractsPropertyName()
    {
        // Arrange & Act
        var triggerProperty = new TriggerProperty<Person>(p => p.Name);

        // Assert
        Assert.AreEqual("Name", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void Constructor_NestedPropertyExpression_ExtractsFullPropertyPath()
    {
        // Arrange & Act
        var triggerProperty = new TriggerProperty<Person>(p => p.Address!.City);

        // Assert
        Assert.AreEqual("Address.City", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void Constructor_DeeplyNestedPropertyExpression_ExtractsFullPropertyPath()
    {
        // Arrange & Act
        var triggerProperty = new TriggerProperty<Person>(p => p.Address!.Country!.Name);

        // Assert
        Assert.AreEqual("Address.Country.Name", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void Constructor_ValueTypeProperty_ExtractsPropertyNameThroughUnaryExpression()
    {
        // Arrange & Act
        // Value types (int) get boxed via UnaryExpression when cast to object?
        var triggerProperty = new TriggerProperty<Person>(p => p.Age);

        // Assert
        Assert.AreEqual("Age", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void Constructor_BooleanProperty_ExtractsPropertyNameThroughUnaryExpression()
    {
        // Arrange & Act
        var triggerProperty = new TriggerProperty<Person>(p => p.IsActive);

        // Assert
        Assert.AreEqual("IsActive", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void Constructor_DoubleProperty_ExtractsPropertyNameThroughUnaryExpression()
    {
        // Arrange & Act
        var triggerProperty = new TriggerProperty<Person>(p => p.Salary);

        // Assert
        Assert.AreEqual("Salary", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void Constructor_NullableDateTimeProperty_ExtractsPropertyName()
    {
        // Arrange & Act
        var triggerProperty = new TriggerProperty<Person>(p => p.BirthDate);

        // Assert
        Assert.AreEqual("BirthDate", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void Constructor_NestedValueTypeProperty_ExtractsFullPropertyPath()
    {
        // Arrange & Act
        var triggerProperty = new TriggerProperty<Person>(p => p.Orders![0].OrderId);

        // Assert
        // Collection indexer is handled via MethodCallExpression
        Assert.AreEqual("Orders.OrderId", triggerProperty.PropertyName);
    }

    #endregion

    #region PropertyName Tests

    [TestMethod]
    public void PropertyName_SimpleProperty_ReturnsPropertyName()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Name);

        // Act
        var result = triggerProperty.PropertyName;

        // Assert
        Assert.AreEqual("Name", result);
    }

    [TestMethod]
    public void PropertyName_NestedProperty_ReturnsFullPath()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Address!.Street);

        // Act
        var result = triggerProperty.PropertyName;

        // Assert
        Assert.AreEqual("Address.Street", result);
    }

    [TestMethod]
    public void PropertyName_ThreeLevelNesting_ReturnsFullPath()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Address!.Country!.Code);

        // Act
        var result = triggerProperty.PropertyName;

        // Assert
        Assert.AreEqual("Address.Country.Code", result);
    }

    #endregion

    #region IsMatch Tests - Simple Properties

    [TestMethod]
    public void IsMatch_SimplePropertyExactMatch_ReturnsTrue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Name);

        // Act
        var result = triggerProperty.IsMatch("Name");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsMatch_SimplePropertyNoMatch_ReturnsFalse()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Name);

        // Act
        var result = triggerProperty.IsMatch("Age");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsMatch_SimplePropertyCaseMismatch_ReturnsFalse()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Name);

        // Act
        var result = triggerProperty.IsMatch("name");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsMatch_SimplePropertyCaseMismatchUpperCase_ReturnsFalse()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Name);

        // Act
        var result = triggerProperty.IsMatch("NAME");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsMatch_ValueTypePropertyExactMatch_ReturnsTrue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Age);

        // Act
        var result = triggerProperty.IsMatch("Age");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsMatch_EmptyString_ReturnsFalse()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Name);

        // Act
        var result = triggerProperty.IsMatch("");

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region IsMatch Tests - Nested Properties

    [TestMethod]
    public void IsMatch_NestedPropertyExactMatch_ReturnsTrue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Address!.City);

        // Act
        var result = triggerProperty.IsMatch("Address.City");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsMatch_NestedPropertyPartialMatchFirstLevel_ReturnsFalse()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Address!.City);

        // Act
        var result = triggerProperty.IsMatch("Address");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsMatch_NestedPropertyPartialMatchSecondLevel_ReturnsFalse()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Address!.City);

        // Act
        var result = triggerProperty.IsMatch("City");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsMatch_NestedPropertyWrongPath_ReturnsFalse()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Address!.City);

        // Act
        var result = triggerProperty.IsMatch("Address.Street");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsMatch_DeeplyNestedPropertyExactMatch_ReturnsTrue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Address!.Country!.Name);

        // Act
        var result = triggerProperty.IsMatch("Address.Country.Name");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsMatch_DeeplyNestedPropertyPartialMatch_ReturnsFalse()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Address!.Country!.Name);

        // Act
        var result = triggerProperty.IsMatch("Address.Country");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsMatch_WithNestedPropertyPath_ReturnsTrue()
    {
        // Arrange - Legacy test for backward compatibility
        var triggerProperty = new TriggerProperty<TriggerPropertyTestSubject>(t => t.Child!.ChildProperty);

        // Act
        var result = triggerProperty.IsMatch("Child.ChildProperty");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsMatch_WithNonMatchingPath_ReturnsFalse()
    {
        // Arrange - Legacy test for backward compatibility
        var triggerProperty = new TriggerProperty<TriggerPropertyTestSubject>(t => t.Child!.ChildProperty);

        // Act
        var result = triggerProperty.IsMatch("Other.Property");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsMatch_WithPartialPath_ReturnsFalse()
    {
        // Arrange - Legacy test for backward compatibility
        var triggerProperty = new TriggerProperty<TriggerPropertyTestSubject>(t => t.Child!.ChildProperty);

        // Act
        var result = triggerProperty.IsMatch("Child");

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region IsMatch Tests - Collection Indexer

    [TestMethod]
    public void IsMatch_CollectionIndexerProperty_ReturnsTrue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Orders![0].ProductName);

        // Act
        // The indexer is handled by MethodCallExpression, so the path is "Orders.ProductName"
        var result = triggerProperty.IsMatch("Orders.ProductName");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsMatch_CollectionIndexerNestedProperty_ReturnsTrue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Orders![0].Details!.Description);

        // Act
        var result = triggerProperty.IsMatch("Orders.Details.Description");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsMatch_CollectionIndexerValueTypeProperty_ReturnsTrue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Orders![0].Details!.Quantity);

        // Act
        var result = triggerProperty.IsMatch("Orders.Details.Quantity");

        // Assert
        Assert.IsTrue(result);
    }

    #endregion

    #region GetValue Tests - Simple Properties

    [TestMethod]
    public void GetValue_SimpleStringProperty_ReturnsValue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Name);
        var person = new Person { Name = "John Doe" };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreEqual("John Doe", result);
    }

    [TestMethod]
    public void GetValue_SimpleStringPropertyNull_ReturnsNull()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Name);
        var person = new Person { Name = null };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetValue_ValueTypeProperty_ReturnsBoxedValue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Age);
        var person = new Person { Age = 30 };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreEqual(30, result);
    }

    [TestMethod]
    public void GetValue_ValueTypePropertyDefaultValue_ReturnsDefault()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Age);
        var person = new Person(); // Age defaults to 0

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void GetValue_BooleanProperty_ReturnsBoxedValue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.IsActive);
        var person = new Person { IsActive = true };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreEqual(true, result);
    }

    [TestMethod]
    public void GetValue_DoubleProperty_ReturnsBoxedValue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Salary);
        var person = new Person { Salary = 75000.50 };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreEqual(75000.50, result);
    }

    [TestMethod]
    public void GetValue_NullableDateTimePropertyWithValue_ReturnsValue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.BirthDate);
        var birthDate = new DateTime(1990, 5, 15);
        var person = new Person { BirthDate = birthDate };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreEqual(birthDate, result);
    }

    [TestMethod]
    public void GetValue_NullableDateTimePropertyNull_ReturnsNull()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.BirthDate);
        var person = new Person { BirthDate = null };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region GetValue Tests - Nested Properties

    [TestMethod]
    public void GetValue_NestedProperty_ReturnsValue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Address!.City);
        var person = new Person
        {
            Address = new Address { City = "New York" }
        };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreEqual("New York", result);
    }

    [TestMethod]
    public void GetValue_DeeplyNestedProperty_ReturnsValue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Address!.Country!.Name);
        var person = new Person
        {
            Address = new Address
            {
                Country = new Country { Name = "United States" }
            }
        };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreEqual("United States", result);
    }

    [TestMethod]
    public void GetValue_NestedObjectProperty_ReturnsEntireObject()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Address);
        var address = new Address { City = "Boston", Street = "Main St" };
        var person = new Person { Address = address };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreSame(address, result);
    }

    #endregion

    #region GetValue Tests - Collection Indexer

    [TestMethod]
    public void GetValue_CollectionIndexerProperty_ReturnsValue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Orders![0].ProductName);
        var person = new Person
        {
            Orders = new List<Order>
            {
                new Order { ProductName = "Widget" }
            }
        };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreEqual("Widget", result);
    }

    [TestMethod]
    public void GetValue_CollectionIndexerDifferentIndex_ReturnsCorrectValue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Orders![1].ProductName);
        var person = new Person
        {
            Orders = new List<Order>
            {
                new Order { ProductName = "First" },
                new Order { ProductName = "Second" }
            }
        };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreEqual("Second", result);
    }

    [TestMethod]
    public void GetValue_CollectionIndexerNestedProperty_ReturnsValue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Orders![0].Details!.Description);
        var person = new Person
        {
            Orders = new List<Order>
            {
                new Order
                {
                    Details = new OrderDetails { Description = "Test Description" }
                }
            }
        };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreEqual("Test Description", result);
    }

    [TestMethod]
    public void GetValue_CollectionIndexerValueTypeProperty_ReturnsBoxedValue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Orders![0].Details!.Quantity);
        var person = new Person
        {
            Orders = new List<Order>
            {
                new Order
                {
                    Details = new OrderDetails { Quantity = 5 }
                }
            }
        };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreEqual(5, result);
    }

    [TestMethod]
    public void GetValue_CollectionIndexerDecimalProperty_ReturnsBoxedValue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Orders![0].Price);
        var person = new Person
        {
            Orders = new List<Order>
            {
                new Order { Price = 99.99m }
            }
        };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreEqual(99.99m, result);
    }

    #endregion

    #region FromExpression Static Factory Method Tests

    [TestMethod]
    public void FromExpression_SimpleProperty_CreatesTriggerProperty()
    {
        // Arrange & Act
        var triggerProperty = TriggerProperty<Person>.FromExpression<Person>(p => p.Name);

        // Assert
        Assert.IsNotNull(triggerProperty);
        Assert.AreEqual("Name", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void FromExpression_NestedProperty_CreatesTriggerProperty()
    {
        // Arrange & Act
        var triggerProperty = TriggerProperty<Person>.FromExpression<Person>(p => p.Address!.City);

        // Assert
        Assert.IsNotNull(triggerProperty);
        Assert.AreEqual("Address.City", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void FromExpression_ValueTypeProperty_CreatesTriggerProperty()
    {
        // Arrange & Act
        var triggerProperty = TriggerProperty<Person>.FromExpression<Person>(p => p.Age);

        // Assert
        Assert.IsNotNull(triggerProperty);
        Assert.AreEqual("Age", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void FromExpression_CollectionIndexerProperty_CreatesTriggerProperty()
    {
        // Arrange & Act
        var triggerProperty = TriggerProperty<Person>.FromExpression<Person>(p => p.Orders![0].ProductName);

        // Assert
        Assert.IsNotNull(triggerProperty);
        Assert.AreEqual("Orders.ProductName", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void FromExpression_DifferentGenericType_CreatesTriggerProperty()
    {
        // Arrange & Act
        // The static method allows specifying a different type
        var triggerProperty = TriggerProperty<Person>.FromExpression<Address>(a => a.City);

        // Assert
        Assert.IsNotNull(triggerProperty);
        Assert.AreEqual("City", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void FromExpression_GetValueWorks_ReturnsCorrectValue()
    {
        // Arrange
        var triggerProperty = TriggerProperty<Person>.FromExpression<Person>(p => p.Name);
        var person = new Person { Name = "Jane" };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreEqual("Jane", result);
    }

    [TestMethod]
    public void FromExpression_IsMatchWorks_ReturnsTrue()
    {
        // Arrange
        var triggerProperty = TriggerProperty<Person>.FromExpression<Person>(p => p.Name);

        // Act
        var result = triggerProperty.IsMatch("Name");

        // Assert
        Assert.IsTrue(result);
    }

    #endregion

    #region Interface Implementation Tests

    [TestMethod]
    public void TriggerProperty_ImplementsITriggerProperty()
    {
        // Arrange & Act
        var triggerProperty = new TriggerProperty<Person>(p => p.Name);

        // Assert
        Assert.IsInstanceOfType(triggerProperty, typeof(ITriggerProperty));
    }

    [TestMethod]
    public void TriggerProperty_ImplementsITriggerPropertyOfT()
    {
        // Arrange & Act
        var triggerProperty = new TriggerProperty<Person>(p => p.Name);

        // Assert
        Assert.IsInstanceOfType(triggerProperty, typeof(ITriggerProperty<Person>));
    }

    [TestMethod]
    public void ITriggerProperty_PropertyName_ReturnsCorrectValue()
    {
        // Arrange
        ITriggerProperty triggerProperty = new TriggerProperty<Person>(p => p.Name);

        // Act
        var result = triggerProperty.PropertyName;

        // Assert
        Assert.AreEqual("Name", result);
    }

    [TestMethod]
    public void ITriggerProperty_IsMatch_WorksCorrectly()
    {
        // Arrange
        ITriggerProperty triggerProperty = new TriggerProperty<Person>(p => p.Address!.City);

        // Act
        var matchResult = triggerProperty.IsMatch("Address.City");
        var noMatchResult = triggerProperty.IsMatch("Address.Street");

        // Assert
        Assert.IsTrue(matchResult);
        Assert.IsFalse(noMatchResult);
    }

    [TestMethod]
    public void ITriggerPropertyOfT_GetValue_WorksCorrectly()
    {
        // Arrange
        ITriggerProperty<Person> triggerProperty = new TriggerProperty<Person>(p => p.Age);
        var person = new Person { Age = 25 };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreEqual(25, result);
    }

    #endregion

    #region Edge Cases and Special Scenarios

    [TestMethod]
    public void IsMatch_WithWhitespace_ReturnsFalse()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Name);

        // Act
        var result = triggerProperty.IsMatch(" Name ");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsMatch_WithLeadingDot_ReturnsFalse()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Address!.City);

        // Act
        var result = triggerProperty.IsMatch(".Address.City");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsMatch_WithTrailingDot_ReturnsFalse()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Address!.City);

        // Act
        var result = triggerProperty.IsMatch("Address.City.");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GetValue_MultipleCallsOnSameInstance_ReturnsSameCompiledResult()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Name);
        var person1 = new Person { Name = "First" };
        var person2 = new Person { Name = "Second" };

        // Act
        var result1 = triggerProperty.GetValue(person1);
        var result2 = triggerProperty.GetValue(person2);
        var result3 = triggerProperty.GetValue(person1);

        // Assert
        Assert.AreEqual("First", result1);
        Assert.AreEqual("Second", result2);
        Assert.AreEqual("First", result3);
    }

    [TestMethod]
    public void PropertyName_CalledMultipleTimes_ReturnsSameValue()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Address!.City);

        // Act
        var result1 = triggerProperty.PropertyName;
        var result2 = triggerProperty.PropertyName;
        var result3 = triggerProperty.PropertyName;

        // Assert
        Assert.AreEqual("Address.City", result1);
        Assert.AreEqual("Address.City", result2);
        Assert.AreEqual("Address.City", result3);
        Assert.AreSame(result1, result2);
        Assert.AreSame(result2, result3);
    }

    [TestMethod]
    public void TriggerProperty_DifferentExpressionsForSameProperty_HaveSamePropertyName()
    {
        // Arrange & Act
        var trigger1 = new TriggerProperty<Person>(p => p.Name);
        var trigger2 = new TriggerProperty<Person>(x => x.Name);

        // Assert
        Assert.AreEqual(trigger1.PropertyName, trigger2.PropertyName);
    }

    [TestMethod]
    public void TriggerProperty_SameExpressionDifferentTypes_HaveSamePropertyName()
    {
        // Arrange & Act
        var personTrigger = new TriggerProperty<Person>(p => p.Name);
        var countryTrigger = new TriggerProperty<Country>(c => c.Name);

        // Assert
        Assert.AreEqual("Name", personTrigger.PropertyName);
        Assert.AreEqual("Name", countryTrigger.PropertyName);
    }

    #endregion

    #region Expression Pattern Coverage Tests

    [TestMethod]
    public void Constructor_ReferenceTypeProperty_NoUnaryExpression()
    {
        // Arrange & Act
        // Reference types don't need boxing, so no UnaryExpression
        var triggerProperty = new TriggerProperty<Person>(p => p.Name);

        // Assert
        Assert.AreEqual("Name", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void Constructor_IntProperty_HandlesUnaryExpression()
    {
        // Arrange & Act
        var triggerProperty = new TriggerProperty<Person>(p => p.Age);

        // Assert
        Assert.AreEqual("Age", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void Constructor_NestedIntProperty_HandlesUnaryExpressionWithMemberExpression()
    {
        // Arrange & Act
        var triggerProperty = new TriggerProperty<Person>(p => p.Orders![0].OrderId);

        // Assert
        Assert.AreEqual("Orders.OrderId", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void Constructor_CollectionProperty_ReturnsCollectionName()
    {
        // Arrange & Act
        var triggerProperty = new TriggerProperty<Person>(p => p.Orders);

        // Assert
        Assert.AreEqual("Orders", triggerProperty.PropertyName);
    }

    [TestMethod]
    public void GetValue_CollectionProperty_ReturnsEntireCollection()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<Person>(p => p.Orders);
        var orders = new List<Order>
        {
            new Order { ProductName = "Item1" },
            new Order { ProductName = "Item2" }
        };
        var person = new Person { Orders = orders };

        // Act
        var result = triggerProperty.GetValue(person);

        // Assert
        Assert.AreSame(orders, result);
    }

    #endregion
}
