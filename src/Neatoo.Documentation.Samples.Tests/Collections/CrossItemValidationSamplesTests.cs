using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Documentation.Samples.Collections;

namespace Neatoo.Documentation.Samples.Tests.Collections;

/// <summary>
/// Tests for CrossItemValidationSamples.cs code snippets.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("Collections")]
public class CrossItemValidationSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region ContactPhone Tests

    [TestMethod]
    public void ContactPhone_Create_InitializesId()
    {
        // Arrange
        var factory = GetRequiredService<IContactPhoneFactory>();

        // Act
        var phone = factory.Create();

        // Assert
        Assert.AreNotEqual(Guid.Empty, phone.Id);
        Assert.IsTrue(phone.IsNew);
    }

    [TestMethod]
    public void ContactPhone_CanSetProperties()
    {
        // Arrange
        var factory = GetRequiredService<IContactPhoneFactory>();
        var phone = factory.Create();

        // Act
        phone.PhoneNumber = "555-1234";
        phone.PhoneType = "Mobile";

        // Assert
        Assert.AreEqual("555-1234", phone.PhoneNumber);
        Assert.AreEqual("Mobile", phone.PhoneType);
        Assert.IsTrue(phone.IsModified);
    }

    #endregion

    #region ContactPhoneList Tests

    [TestMethod]
    public void ContactPhoneList_Create_IsEmpty()
    {
        // Arrange
        var factory = GetRequiredService<IContactPhoneListFactory>();

        // Act
        var list = factory.Create();

        // Assert
        Assert.IsNotNull(list);
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void ContactPhoneList_AddPhone_CreatesItem()
    {
        // Arrange
        var factory = GetRequiredService<IContactPhoneListFactory>();
        var list = factory.Create();

        // Act
        var phone = list.AddPhone();

        // Assert
        Assert.AreEqual(1, list.Count);
        Assert.IsNotNull(phone);
        Assert.IsTrue(phone.IsChild);
    }

    [TestMethod]
    public void ContactPhoneList_AddMultiplePhones_AllTracked()
    {
        // Arrange
        var factory = GetRequiredService<IContactPhoneListFactory>();
        var list = factory.Create();

        // Act
        var phone1 = list.AddPhone();
        phone1.PhoneNumber = "555-1111";
        phone1.PhoneType = "Home";

        var phone2 = list.AddPhone();
        phone2.PhoneNumber = "555-2222";
        phone2.PhoneType = "Work";

        var phone3 = list.AddPhone();
        phone3.PhoneNumber = "555-3333";
        phone3.PhoneType = "Mobile";

        // Assert
        Assert.AreEqual(3, list.Count);
        Assert.IsTrue(list.IsModified);
    }

    [TestMethod]
    public async Task ContactPhoneList_HandleNeatooPropertyChanged_TriggersOnPropertyChange()
    {
        // Arrange
        var factory = GetRequiredService<IContactPhoneListFactory>();
        var list = factory.Create();

        var phone1 = list.AddPhone();
        phone1.PhoneNumber = "555-1111";
        phone1.PhoneType = "Home";

        var phone2 = list.AddPhone();
        phone2.PhoneNumber = "555-2222";
        phone2.PhoneType = "Work";

        // Act - change a property, which triggers HandleNeatooPropertyChanged
        phone1.PhoneType = "Mobile";

        // Allow async handlers to complete
        await Task.Delay(50);

        // Assert - the list should still be valid (no validation rule on uniqueness)
        Assert.IsTrue(list.IsValid);
    }

    #endregion
}
