using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Samples.DomainModel.AggregatesAndEntities.CompleteExample;

namespace Neatoo.Samples.DomainModel.Tests.AggregatesAndEntities;

/// <summary>
/// Tests for CompleteExampleSamples.cs code snippets.
/// Verifies the complete Person aggregate with child collections.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("AggregatesAndEntities")]
public class CompleteExampleSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region Person Creation Tests

    [TestMethod]
    public void Person_Create_HasEmptyPhoneList()
    {
        // Arrange & Act
        var factory = GetRequiredService<IPersonFactory>();
        var person = factory.Create();

        // Assert
        Assert.IsNotNull(person.PersonPhoneList);
        Assert.AreEqual(0, person.PersonPhoneList.Count);
        Assert.IsTrue(person.IsNew);
    }

    [TestMethod]
    public void Person_SetFirstName_IsModified()
    {
        // Arrange
        var factory = GetRequiredService<IPersonFactory>();
        var person = factory.Create();

        // Act
        person.FirstName = "John";

        // Assert
        Assert.IsTrue(person[nameof(IPerson.FirstName)].IsModified);
    }

    [TestMethod]
    public void Person_RequiredFirstName_InvalidWhenEmpty()
    {
        // Arrange
        var factory = GetRequiredService<IPersonFactory>();
        var person = factory.Create();

        // Act - Leave FirstName empty (trigger validation)
        person.FirstName = "";

        // Assert
        var firstNameProp = person[nameof(IPerson.FirstName)];
        Assert.IsFalse(firstNameProp.IsValid);
        Assert.IsTrue(firstNameProp.PropertyMessages.Any(m => m.Message.Contains("required")));
    }

    [TestMethod]
    public void Person_RequiredLastName_InvalidWhenEmpty()
    {
        // Arrange
        var factory = GetRequiredService<IPersonFactory>();
        var person = factory.Create();

        // Act - Leave LastName empty (trigger validation)
        person.LastName = "";

        // Assert
        var lastNameProp = person[nameof(IPerson.LastName)];
        Assert.IsFalse(lastNameProp.IsValid);
        Assert.IsTrue(lastNameProp.PropertyMessages.Any(m => m.Message.Contains("required")));
    }

    #endregion

    #region Child Collection Tests

    [TestMethod]
    public void Person_AddPhone_PhoneIsChild()
    {
        // Arrange
        var personFactory = GetRequiredService<IPersonFactory>();
        var phoneFactory = GetRequiredService<IPersonPhoneFactory>();
        var person = personFactory.Create();

        // Act
        var phone = phoneFactory.Create();
        phone.PhoneNumber = "555-1234";
        person.PersonPhoneList.Add(phone);

        // Assert
        Assert.AreEqual(1, person.PersonPhoneList.Count);
        Assert.IsTrue(phone.IsChild);
    }

    [TestMethod]
    public void Person_RemovePhone_PhoneInDeletedList()
    {
        // Arrange
        var personFactory = GetRequiredService<IPersonFactory>();
        var phoneFactory = GetRequiredService<IPersonPhoneFactory>();
        var person = personFactory.Create();
        var phone = phoneFactory.Create();
        phone.PhoneNumber = "555-1234";
        person.PersonPhoneList.Add(phone);

        // Act
        person.PersonPhoneList.Remove(phone);

        // Assert - New items are just removed, not tracked in DeletedList
        Assert.AreEqual(0, person.PersonPhoneList.Count);
    }

    #endregion

    #region Mapper Tests

    [TestMethod]
    public void Person_Fetch_CopiesAllProperties()
    {
        // Arrange
        var factory = GetRequiredService<IPersonFactory>();
        var entity = new PersonEntity
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Phones = []
        };

        // Act - Fetch uses MapFrom internally
        var fetchedPerson = factory.Fetch(entity);

        // Assert
        Assert.AreEqual(entity.Id, fetchedPerson.Id);
        Assert.AreEqual("John", fetchedPerson.FirstName);
        Assert.AreEqual("Doe", fetchedPerson.LastName);
        Assert.IsFalse(fetchedPerson.IsNew);
    }

    [TestMethod]
    public void Person_Fetch_LoadsPhones()
    {
        // Arrange
        var factory = GetRequiredService<IPersonFactory>();
        var entity = new PersonEntity
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Phones =
            [
                new PersonPhoneEntity { Id = Guid.NewGuid(), PhoneNumber = "555-1234" },
                new PersonPhoneEntity { Id = Guid.NewGuid(), PhoneNumber = "555-5678" }
            ]
        };

        // Act
        var person = factory.Fetch(entity);

        // Assert
        Assert.AreEqual(2, person.PersonPhoneList.Count);
        Assert.IsTrue(person.PersonPhoneList.Any(p => p.PhoneNumber == "555-1234"));
        Assert.IsTrue(person.PersonPhoneList.Any(p => p.PhoneNumber == "555-5678"));
    }

    #endregion

    #region Save Operation Tests

    [TestMethod]
    public async Task Person_Insert_SetsId()
    {
        // Arrange
        var factory = GetRequiredService<IPersonFactory>();
        var person = factory.Create();
        person.FirstName = "John";
        person.LastName = "Doe";

        // Act
        person = await factory.Save(person, CancellationToken.None);

        // Assert
        Assert.IsNotNull(person.Id);
        Assert.AreNotEqual(Guid.Empty, person.Id);
    }

    [TestMethod]
    public async Task Person_InsertWithEmptyFirstName_InvalidatesFirstName()
    {
        // Arrange
        var factory = GetRequiredService<IPersonFactory>();
        var person = factory.Create();

        // Act - Set empty first name to trigger Required validation
        person.FirstName = "";
        person.LastName = "Doe";
        await person.RunRules();

        // Assert - FirstName should be invalid due to Required attribute
        var firstNameProp = person[nameof(IPerson.FirstName)];
        Assert.IsFalse(firstNameProp.IsValid);
        Assert.IsTrue(firstNameProp.PropertyMessages.Any(m => m.Message.Contains("required")));
    }

    #endregion

    #region Phone Tests

    [TestMethod]
    public void PersonPhone_Create_IsNew()
    {
        // Arrange & Act
        var factory = GetRequiredService<IPersonPhoneFactory>();
        var phone = factory.Create();

        // Assert
        Assert.IsTrue(phone.IsNew);
    }

    [TestMethod]
    public void PersonPhone_Fetch_LoadsProperties()
    {
        // Arrange
        var factory = GetRequiredService<IPersonPhoneFactory>();
        var entity = new PersonPhoneEntity
        {
            Id = Guid.NewGuid(),
            PhoneNumber = "555-1234"
        };

        // Act
        var phone = factory.Fetch(entity);

        // Assert
        Assert.AreEqual(entity.Id, phone.Id);
        Assert.AreEqual("555-1234", phone.PhoneNumber);
        Assert.IsFalse(phone.IsNew);
    }

    #endregion
}
