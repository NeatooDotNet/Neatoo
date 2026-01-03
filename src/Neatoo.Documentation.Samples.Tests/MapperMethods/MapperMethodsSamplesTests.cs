using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Documentation.Samples.MapperMethods;

namespace Neatoo.Documentation.Samples.Tests.MapperMethods;

/// <summary>
/// Tests for MapperMethodsSamples.cs code snippets.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("MapperMethods")]
public class MapperMethodsSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region PersonWithMapper Tests (Overview)

    [TestMethod]
    public void PersonWithMapper_Create_IsNew()
    {
        // Arrange
        var factory = GetRequiredService<IPersonWithMapperFactory>();

        // Act
        var person = factory.Create();

        // Assert
        Assert.IsTrue(person.IsNew);
        Assert.AreEqual(0, person.Id);
    }

    [TestMethod]
    public void PersonWithMapper_MapFrom_CopiesAllProperties()
    {
        // Arrange
        var factory = GetRequiredService<IPersonWithMapperFactory>();
        var entity = new PersonEntity
        {
            Id = 42,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };

        // Act
        var person = factory.Fetch(entity);

        // Assert
        Assert.AreEqual(42, person.Id);
        Assert.AreEqual("John", person.FirstName);
        Assert.AreEqual("Doe", person.LastName);
        Assert.AreEqual("john@example.com", person.Email);
    }

    [TestMethod]
    public void PersonWithMapper_MapTo_CopiesAllProperties()
    {
        // Arrange
        var factory = GetRequiredService<IPersonWithMapperFactory>();
        var person = factory.Create();
        person.FirstName = "Jane";
        person.LastName = "Smith";
        person.Email = "jane@example.com";

        // Act
        var entity = new PersonEntity();
        person.MapTo(entity);

        // Assert
        Assert.AreEqual("Jane", entity.FirstName);
        Assert.AreEqual("Smith", entity.LastName);
        Assert.AreEqual("jane@example.com", entity.Email);
    }

    [TestMethod]
    public void PersonWithMapper_FetchedEntity_NotModified()
    {
        // Arrange
        var factory = GetRequiredService<IPersonWithMapperFactory>();
        var entity = new PersonEntity
        {
            Id = 1,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };

        // Act
        var person = factory.Fetch(entity);

        // Assert - Factory operations don't mark properties as modified
        Assert.IsFalse(person.IsModified);
    }

    #endregion

    #region FetchableProduct Tests (MapFrom)

    [TestMethod]
    public void FetchableProduct_Fetch_MapsFromEntity()
    {
        // Arrange
        var factory = GetRequiredService<IFetchableProductFactory>();
        var entity = new ProductEntity
        {
            Id = 100,
            Name = "Widget",
            Price = 29.99m,
            StockQuantity = 50
        };

        // Act
        var product = factory.Fetch(entity);

        // Assert
        Assert.AreEqual(100, product.Id);
        Assert.AreEqual("Widget", product.Name);
        Assert.AreEqual(29.99m, product.Price);
        Assert.AreEqual(50, product.StockQuantity);
    }

    [TestMethod]
    public void FetchableProduct_Fetch_NotModified()
    {
        // Arrange
        var factory = GetRequiredService<IFetchableProductFactory>();
        var entity = new ProductEntity { Id = 1, Name = "Test" };

        // Act
        var product = factory.Fetch(entity);

        // Assert - During Fetch, rules are paused and properties not marked modified
        Assert.IsFalse(product.IsModified);
        Assert.IsFalse(product.IsNew);
    }

    #endregion

    #region InsertableItem Tests (MapTo)

    [TestMethod]
    public void InsertableItem_MapTo_CopiesProperties()
    {
        // Arrange
        var factory = GetRequiredService<IInsertableItemFactory>();
        var item = factory.Create();
        item.Description = "Test Item";
        item.Amount = 99.99m;

        // Act
        var entity = new ItemEntity();
        item.MapTo(entity);

        // Assert
        Assert.AreEqual("Test Item", entity.Description);
        Assert.AreEqual(99.99m, entity.Amount);
    }

    [TestMethod]
    public async Task InsertableItem_Insert_SetsGeneratedId()
    {
        // Arrange
        var factory = GetRequiredService<IInsertableItemFactory>();
        var item = factory.Create();
        item.Description = "New Item";
        item.Amount = 50.00m;

        // Act
        await factory.Save(item);

        // Assert - ID should be set after insert
        Assert.AreEqual(1, item.Id);
    }

    #endregion

    #region UpdatableRecord Tests (MapModifiedTo)

    [TestMethod]
    public void UpdatableRecord_Fetch_NotModified()
    {
        // Arrange
        var factory = GetRequiredService<IUpdatableRecordFactory>();
        var entity = new RecordEntity
        {
            Id = 1,
            Title = "Original Title",
            Content = "Original Content"
        };

        // Act
        var record = factory.Fetch(entity);

        // Assert
        Assert.IsFalse(record.IsModified);
    }

    [TestMethod]
    public void UpdatableRecord_ModifyProperty_IsModified()
    {
        // Arrange
        var factory = GetRequiredService<IUpdatableRecordFactory>();
        var entity = new RecordEntity { Id = 1, Title = "Original", Content = "Content" };
        var record = factory.Fetch(entity);

        // Act
        record.Title = "Updated Title";

        // Assert
        Assert.IsTrue(record.IsModified);
        Assert.IsTrue(record[nameof(IUpdatableRecord.Title)].IsModified);
        Assert.IsFalse(record[nameof(IUpdatableRecord.Content)].IsModified);
    }

    [TestMethod]
    public async Task UpdatableRecord_MapModifiedTo_OnlyUpdatesChanged()
    {
        // Arrange
        var factory = GetRequiredService<IUpdatableRecordFactory>();
        var originalEntity = new RecordEntity
        {
            Id = 1,
            Title = "Original Title",
            Content = "Original Content",
            LastModified = new DateTime(2024, 1, 1)
        };
        var record = factory.Fetch(originalEntity);

        // Modify only Title
        record.Title = "New Title";

        // Create target entity with different values
        var targetEntity = new RecordEntity
        {
            Id = 1,
            Title = "Should be overwritten",
            Content = "Should NOT be overwritten",
            LastModified = new DateTime(2024, 6, 1)
        };

        // Act
        await factory.Save(record, targetEntity);

        // Assert - Only modified property should change
        Assert.AreEqual("New Title", targetEntity.Title);
        Assert.AreEqual("Should NOT be overwritten", targetEntity.Content);
        Assert.AreEqual(new DateTime(2024, 6, 1), targetEntity.LastModified);
    }

    #endregion

    #region EmployeeWithComputed Tests (Custom Mapping)

    [TestMethod]
    public void EmployeeWithComputed_Fetch_SetsComputedProperty()
    {
        // Arrange
        var factory = GetRequiredService<IEmployeeWithComputedFactory>();
        var entity = new EmployeeEntity
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Johnson",
            PhoneTypeString = "Mobile"
        };

        // Act
        var employee = factory.Fetch(entity);

        // Assert
        Assert.AreEqual("Alice", employee.FirstName);
        Assert.AreEqual("Johnson", employee.LastName);
        Assert.AreEqual("Alice Johnson", employee.FullName); // Computed
        Assert.AreEqual("Mobile", employee.PhoneType);
    }

    [TestMethod]
    public void EmployeeWithComputed_FullName_UpdatesWhenPropertiesChange()
    {
        // Arrange
        var factory = GetRequiredService<IEmployeeWithComputedFactory>();
        var employee = factory.Create();

        // Act
        employee.FirstName = "Bob";
        employee.LastName = "Wilson";

        // Assert
        Assert.AreEqual("Bob Wilson", employee.FullName);
    }

    #endregion

    #region CustomerWithAddress Tests (Different Shapes)

    [TestMethod]
    public void CustomerWithAddress_MapFromEntity_FlattensNestedAddress()
    {
        // Arrange
        var factory = GetRequiredService<ICustomerWithAddressFactory>();
        var entity = new CustomerEntity
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Address = new AddressEntity
            {
                Street = "123 Main St",
                City = "Springfield",
                State = "IL",
                ZipCode = "62701"
            }
        };

        // Act
        var customer = factory.Fetch(entity);

        // Assert - Nested structure flattened
        Assert.AreEqual(1, customer.Id);
        Assert.AreEqual("John", customer.FirstName);
        Assert.AreEqual("Doe", customer.LastName);
        Assert.AreEqual("123 Main St", customer.StreetAddress);
        Assert.AreEqual("Springfield", customer.City);
        Assert.AreEqual("IL", customer.State);
        Assert.AreEqual("62701", customer.ZipCode);
    }

    [TestMethod]
    public void CustomerWithAddress_MapToEntity_CreatesNestedAddress()
    {
        // Arrange
        var factory = GetRequiredService<ICustomerWithAddressFactory>();
        var customer = factory.Create();
        customer.FirstName = "Jane";
        customer.LastName = "Smith";
        customer.StreetAddress = "456 Oak Ave";
        customer.City = "Chicago";
        customer.State = "IL";
        customer.ZipCode = "60601";

        // Act
        var entity = new CustomerEntity();
        customer.MapToEntity(entity);

        // Assert - Flat structure converted to nested
        Assert.AreEqual("Jane", entity.FirstName);
        Assert.AreEqual("Smith", entity.LastName);
        Assert.IsNotNull(entity.Address);
        Assert.AreEqual("456 Oak Ave", entity.Address.Street);
        Assert.AreEqual("Chicago", entity.Address.City);
        Assert.AreEqual("IL", entity.Address.State);
        Assert.AreEqual("60601", entity.Address.ZipCode);
    }

    [TestMethod]
    public void CustomerWithAddress_MapFromEntity_HandlesNullAddress()
    {
        // Arrange
        var factory = GetRequiredService<ICustomerWithAddressFactory>();
        var entity = new CustomerEntity
        {
            Id = 2,
            FirstName = "No",
            LastName = "Address",
            Address = null
        };

        // Act
        var customer = factory.Fetch(entity);

        // Assert - Null address handled gracefully
        Assert.AreEqual("No", customer.FirstName);
        Assert.IsNull(customer.StreetAddress);
        Assert.IsNull(customer.City);
    }

    #endregion
}
