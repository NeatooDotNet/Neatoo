using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Samples.DomainModel.ValidationAndRules.RuleUsage;

namespace Neatoo.Samples.DomainModel.Tests.ValidationAndRules;

/// <summary>
/// Tests for RuleUsageSamples.cs code snippets.
/// Verifies rule registration, async actions, pause actions, and other rule patterns.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("ValidationAndRules")]
public class RuleUsageSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region Rule Registration Tests

    [TestMethod]
    public void RuleRegistrationPerson_NegativeAge_ReturnsError()
    {
        // Arrange
        var factory = GetRequiredService<IRuleRegistrationPersonFactory>();
        var person = factory.Create();

        // Act
        person.Age = -5;

        // Assert
        Assert.IsFalse(person.IsValid);
        var ageProp = person[nameof(IRuleRegistrationPerson.Age)];
        Assert.IsFalse(ageProp.IsValid);
        Assert.IsTrue(ageProp.PropertyMessages.Any(m => m.Message.Contains("negative")));
    }

    [TestMethod]
    public void RuleRegistrationPerson_ValidAge_NoError()
    {
        // Arrange
        var factory = GetRequiredService<IRuleRegistrationPersonFactory>();
        var person = factory.Create();

        // Act
        person.Age = 25;

        // Assert
        var ageProp = person[nameof(IRuleRegistrationPerson.Age)];
        Assert.IsTrue(ageProp.IsValid);
    }

    #endregion

    #region Async Action Rule Tests

    [TestMethod]
    public async Task AsyncActionPerson_ZipCodeStartsWith9_HigherTaxRate()
    {
        // Arrange
        var factory = GetRequiredService<IAsyncActionPersonFactory>();
        var person = factory.Create();

        // Act
        person.ZipCode = "90210";
        await person.WaitForTasks();

        // Assert - California zip codes get 8.25% tax rate
        Assert.AreEqual(0.0825m, person.TaxRate);
    }

    [TestMethod]
    public async Task AsyncActionPerson_ZipCodeNotStartsWith9_StandardTaxRate()
    {
        // Arrange
        var factory = GetRequiredService<IAsyncActionPersonFactory>();
        var person = factory.Create();

        // Act
        person.ZipCode = "10001";
        await person.WaitForTasks();

        // Assert - Non-CA zip codes get 6% tax rate
        Assert.AreEqual(0.06m, person.TaxRate);
    }

    #endregion

    #region Pause All Actions Tests

    [TestMethod]
    public async Task PauseActionsPerson_SetProperties_Valid()
    {
        // Arrange
        var factory = GetRequiredService<IPauseActionsPersonFactory>();
        var person = factory.Create();

        // Act - Set all properties
        person.FirstName = "John";
        person.LastName = "Doe";
        person.Email = "john@example.com";
        await person.RunRules();

        // Assert
        Assert.AreEqual("John", person.FirstName);
        Assert.AreEqual("Doe", person.LastName);
        Assert.AreEqual("john@example.com", person.Email);
        Assert.IsTrue(person.IsValid);
    }

    [TestMethod]
    public async Task PauseActionsPerson_EmptyFirstName_Invalid()
    {
        // Arrange
        var factory = GetRequiredService<IPauseActionsPersonFactory>();
        var person = factory.Create();

        // Act
        person.FirstName = "";
        await person.RunRules();

        // Assert
        Assert.IsFalse(person.IsValid);
        var firstNameProp = person[nameof(IPauseActionsPerson.FirstName)];
        Assert.IsFalse(firstNameProp.IsValid);
    }

    #endregion

    #region Manual Execution Tests

    [TestMethod]
    public async Task ManualExecutionEntity_InsertWithValidName_SetsId()
    {
        // Arrange
        var factory = GetRequiredService<IManualExecutionEntityFactory>();
        var entity = factory.Create();
        entity.Name = "Test Entity";

        // Act
        await factory.Save(entity, CancellationToken.None);

        // Assert
        Assert.IsNotNull(entity.Id);
        Assert.AreNotEqual(Guid.Empty, entity.Id);
    }

    [TestMethod]
    public async Task ManualExecutionEntity_InsertWithEmptyName_DoesNotSetId()
    {
        // Arrange
        var factory = GetRequiredService<IManualExecutionEntityFactory>();
        var entity = factory.Create();
        entity.Name = "";

        // Act
        entity = await factory.Save(entity, CancellationToken.None);

        // Assert - Id should not be set because validation failed
        Assert.IsNull(entity.Id);
        Assert.IsFalse(entity.IsValid);
    }

    #endregion

    #region Parent-Child Validation Tests

    [TestMethod]
    public async Task ParentContact_DuplicatePhoneTypes_ReturnsError()
    {
        // Arrange
        var contactFactory = GetRequiredService<IParentContactFactory>();
        var phoneFactory = GetRequiredService<IContactPhoneFactory>();
        var contact = contactFactory.Create();

        // Act - Add two phones with same type
        var phone1 = phoneFactory.Create();
        contact.PhoneList.Add(phone1);
        phone1.PhoneType = PhoneType.Mobile;
        phone1.Number = "555-1234";

        var phone2 = phoneFactory.Create();
        contact.PhoneList.Add(phone2);
        phone2.PhoneType = PhoneType.Mobile; // Duplicate!
        phone2.Number = "555-5678";

        // Run rules to trigger validation
        await contact.RunRules();

        // Assert - Second phone should have validation error
        var phoneTypeProp = phone2[nameof(IContactPhone.PhoneType)];
        Assert.IsFalse(phoneTypeProp.IsValid);
        Assert.IsTrue(phoneTypeProp.PropertyMessages.Any(m => m.Message.Contains("unique")));
    }

    [TestMethod]
    public void ParentContact_UniquePhoneTypes_NoError()
    {
        // Arrange
        var contactFactory = GetRequiredService<IParentContactFactory>();
        var phoneFactory = GetRequiredService<IContactPhoneFactory>();
        var contact = contactFactory.Create();

        // Act - Add phones with different types
        var phone1 = phoneFactory.Create();
        phone1.PhoneType = PhoneType.Mobile;
        phone1.Number = "555-1234";
        contact.PhoneList.Add(phone1);

        var phone2 = phoneFactory.Create();
        phone2.PhoneType = PhoneType.Home; // Different type
        phone2.Number = "555-5678";
        contact.PhoneList.Add(phone2);

        // Assert - Both phones should be valid
        Assert.IsTrue(phone1[nameof(IContactPhone.PhoneType)].IsValid);
        Assert.IsTrue(phone2[nameof(IContactPhone.PhoneType)].IsValid);
    }

    #endregion

    #region LoadProperty Tests

    [TestMethod]
    public void LoadPropertyPerson_SetFirstAndLastName_ComputesFullName()
    {
        // Arrange
        var factory = GetRequiredService<ILoadPropertyPersonFactory>();
        var person = factory.Create();

        // Act
        person.FirstName = "John";
        person.LastName = "Doe";

        // Assert - FullName should be computed by the rule
        Assert.AreEqual("John Doe", person.FullName);
    }

    [TestMethod]
    public void LoadPropertyPerson_ChangeFirstName_UpdatesFullName()
    {
        // Arrange
        var factory = GetRequiredService<ILoadPropertyPersonFactory>();
        var person = factory.Create();
        person.FirstName = "John";
        person.LastName = "Doe";

        // Act
        person.FirstName = "Jane";

        // Assert
        Assert.AreEqual("Jane Doe", person.FullName);
    }

    #endregion

    #region Validation Messages Tests

    [TestMethod]
    public void ValidationMessagesPerson_EmptyFields_HasMessages()
    {
        // Arrange
        var factory = GetRequiredService<IValidationMessagesPersonFactory>();
        var person = factory.Create();

        // Act - Leave Email and Name empty (trigger validation)
        person.Email = "";
        person.Name = "";

        // Assert
        Assert.IsFalse(person.IsValid);
        Assert.IsTrue(person.PropertyMessages.Count >= 2);

        var emailProp = person[nameof(IValidationMessagesPerson.Email)];
        Assert.IsTrue(emailProp.PropertyMessages.Any(m => m.Message.Contains("required")));

        var nameProp = person[nameof(IValidationMessagesPerson.Name)];
        Assert.IsTrue(nameProp.PropertyMessages.Any(m => m.Message.Contains("required")));
    }

    [TestMethod]
    public void ValidationMessagesPerson_ValidFields_NoMessages()
    {
        // Arrange
        var factory = GetRequiredService<IValidationMessagesPersonFactory>();
        var person = factory.Create();

        // Act
        person.Email = "test@example.com";
        person.Name = "Test User";

        // Assert
        Assert.IsTrue(person.IsValid);
        Assert.IsFalse(person.PropertyMessages.Any());
    }

    #endregion

    #region IsModified Check Tests

    [TestMethod]
    public async Task IsModifiedCheckUser_ChangeEmail_RuleExecutes()
    {
        // Arrange
        var factory = GetRequiredService<IIsModifiedCheckUserFactory>();
        var user = factory.Create();

        // Act
        user.Email = "test@example.com";
        await user.WaitForTasks();

        // Assert - Rule should have executed (no errors in this simple case)
        var emailProp = user[nameof(IIsModifiedCheckUser.Email)];
        Assert.IsTrue(emailProp.IsModified);
    }

    #endregion
}
