using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Samples.DomainModel.DatabaseValidation;

namespace Neatoo.Samples.DomainModel.Tests.DatabaseValidation;

/// <summary>
/// Tests for AsyncValidationSamples.cs code snippets.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("DatabaseValidation")]
public class AsyncValidationSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region UserWithEmail Tests

    [TestMethod]
    public void UserWithEmail_Create_InitializesId()
    {
        // Arrange
        var factory = GetRequiredService<IUserWithEmailFactory>();

        // Act
        var user = factory.Create();

        // Assert
        Assert.AreNotEqual(Guid.Empty, user.Id);
        Assert.IsTrue(user.IsNew);
    }

    [TestMethod]
    public async Task UserWithEmail_UniqueEmail_IsValid()
    {
        // Arrange
        var factory = GetRequiredService<IUserWithEmailFactory>();
        var user = factory.Create();

        // Act - set a unique email
        user.Email = "unique@example.com";
        user.Name = "Test User";
        await user.RunRules();

        // Assert
        var emailProp = user[nameof(IUserWithEmail.Email)];
        Assert.IsTrue(emailProp.IsValid);
    }

    [TestMethod]
    public async Task UserWithEmail_ExistingEmail_ReturnsError()
    {
        // Arrange
        var factory = GetRequiredService<IUserWithEmailFactory>();
        var user = factory.Create();

        // Act - set an email that exists (per MockUserRepository)
        user.Email = "existing@example.com";
        user.Name = "Test User";
        await user.RunRules();

        // Assert
        var emailProp = user[nameof(IUserWithEmail.Email)];
        Assert.IsFalse(emailProp.IsValid);
        Assert.IsTrue(emailProp.PropertyMessages.Any(m => m.Message.Contains("already in use")));
    }

    [TestMethod]
    public async Task UserWithEmail_TakenEmail_ReturnsError()
    {
        // Arrange
        var factory = GetRequiredService<IUserWithEmailFactory>();
        var user = factory.Create();

        // Act - set another taken email
        user.Email = "taken@example.com";
        await user.RunRules();

        // Assert
        Assert.IsFalse(user[nameof(IUserWithEmail.Email)].IsValid);
    }

    [TestMethod]
    public async Task UserWithEmail_IsSavable_WhenValid()
    {
        // Arrange
        var factory = GetRequiredService<IUserWithEmailFactory>();
        var user = factory.Create();

        // Act
        user.Email = "new-unique@example.com";
        user.Name = "Valid User";
        await user.RunRules();

        // Assert
        Assert.IsTrue(user.IsValid);
        Assert.IsTrue(user.IsSavable);
    }

    [TestMethod]
    public async Task UserWithEmail_NotSavable_WhenInvalid()
    {
        // Arrange
        var factory = GetRequiredService<IUserWithEmailFactory>();
        var user = factory.Create();

        // Act
        user.Email = "existing@example.com"; // Taken
        await user.RunRules();

        // Assert
        Assert.IsFalse(user.IsValid);
        Assert.IsFalse(user.IsSavable);
    }

    #endregion
}
