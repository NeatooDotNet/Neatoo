using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Samples.DomainModel.Authorization;

namespace Neatoo.Samples.DomainModel.Tests.Authorization;

/// <summary>
/// Tests for AuthorizationSamples.cs code snippets.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("Authorization")]
public class AuthorizationSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region Document Tests

    [TestMethod]
    public void Document_Create_WhenAuthorized_Succeeds()
    {
        // Arrange - Set up authorized user
        var mockUser = GetRequiredService<ICurrentUser>() as MockCurrentUser;
        mockUser!.IsAuthenticated = true;
        mockUser.Permissions = Permission.All;

        var factory = GetRequiredService<IDocumentFactory>();

        // Act
        var document = factory.Create();

        // Assert
        Assert.IsNotNull(document);
        Assert.AreNotEqual(Guid.Empty, document.Id);
    }

    [TestMethod]
    public void Document_Create_WhenNotAuthorized_ReturnsNull()
    {
        // Arrange - No permissions
        var mockUser = GetRequiredService<ICurrentUser>() as MockCurrentUser;
        mockUser!.IsAuthenticated = false;
        mockUser.Permissions = Permission.None;

        var factory = GetRequiredService<IDocumentFactory>();

        // Act
        var document = factory.Create();

        // Assert - Create returns null when not authorized
        Assert.IsNull(document);
    }

    [TestMethod]
    public void Document_Fetch_WhenAuthorized_Succeeds()
    {
        // Arrange
        var mockUser = GetRequiredService<ICurrentUser>() as MockCurrentUser;
        mockUser!.IsAuthenticated = true;
        mockUser.Permissions = Permission.Read;

        var factory = GetRequiredService<IDocumentFactory>();
        var id = Guid.NewGuid();

        // Act
        var document = factory.Fetch(id, "Test Title", "Test Content");

        // Assert
        Assert.IsNotNull(document);
        Assert.AreEqual(id, document.Id);
        Assert.AreEqual("Test Title", document.Title);
    }

    #endregion

    #region Role-Based Tests

    [TestMethod]
    public void Article_Create_AsEditor_Succeeds()
    {
        // Arrange - Editor role
        var mockUser = GetRequiredService<ICurrentUser>() as MockCurrentUser;
        mockUser!.IsAuthenticated = true;
        mockUser.Roles.Add("Editor");

        var factory = GetRequiredService<IArticleFactory>();

        // Act
        var article = factory.Create();

        // Assert
        Assert.IsNotNull(article);
    }

    [TestMethod]
    public void Article_Create_AsUser_ReturnsNull()
    {
        // Arrange - User role (no create permission)
        var mockUser = GetRequiredService<ICurrentUser>() as MockCurrentUser;
        mockUser!.IsAuthenticated = true;
        mockUser.Roles.Clear();
        mockUser.Roles.Add("User");

        var factory = GetRequiredService<IArticleFactory>();

        // Act
        var article = factory.Create();

        // Assert - User cannot create
        Assert.IsNull(article);
    }

    [TestMethod]
    public void Article_Create_AsAdmin_Succeeds()
    {
        // Arrange - Admin role
        var mockUser = GetRequiredService<ICurrentUser>() as MockCurrentUser;
        mockUser!.IsAuthenticated = true;
        mockUser.Roles.Clear();
        mockUser.Roles.Add("Admin");

        var factory = GetRequiredService<IArticleFactory>();

        // Act
        var article = factory.Create();

        // Assert
        Assert.IsNotNull(article);
    }

    #endregion

    #region Authorization Interface Tests

    [TestMethod]
    public void DocumentAuth_HasAccess_WhenAuthenticated_ReturnsTrue()
    {
        // Arrange
        var mockUser = GetRequiredService<ICurrentUser>() as MockCurrentUser;
        mockUser!.IsAuthenticated = true;

        var auth = new DocumentAuth(mockUser);

        // Act & Assert
        Assert.IsTrue(auth.HasAccess());
    }

    [TestMethod]
    public void DocumentAuth_HasAccess_WhenNotAuthenticated_ReturnsFalse()
    {
        // Arrange
        var mockUser = GetRequiredService<ICurrentUser>() as MockCurrentUser;
        mockUser!.IsAuthenticated = false;

        var auth = new DocumentAuth(mockUser);

        // Act & Assert
        Assert.IsFalse(auth.HasAccess());
    }

    [TestMethod]
    public void DocumentAuth_CanDelete_RequiresDeletePermission()
    {
        // Arrange
        var mockUser = GetRequiredService<ICurrentUser>() as MockCurrentUser;
        mockUser!.Permissions = Permission.Read | Permission.Create | Permission.Update;
        // Note: No Delete permission

        var auth = new DocumentAuth(mockUser);

        // Act & Assert - Delete should fail without permission
        Assert.IsFalse(auth.CanDelete());

        // Add delete permission
        mockUser.Permissions |= Permission.Delete;
        Assert.IsTrue(auth.CanDelete());
    }

    [TestMethod]
    public void RoleBasedAuth_HierarchicalPermissions_WorkCorrectly()
    {
        // Arrange
        var mockUser = GetRequiredService<ICurrentUser>() as MockCurrentUser;
        mockUser!.Roles.Clear();
        mockUser.Roles.Add("User");

        var auth = new RoleBasedAuth(mockUser);

        // Assert - User can only read
        Assert.IsTrue(auth.HasReadAccess());
        Assert.IsFalse(auth.HasCreateAccess());
        Assert.IsFalse(auth.HasUpdateAccess());
        Assert.IsFalse(auth.HasDeleteAccess());

        // Add Editor role
        mockUser.Roles.Add("Editor");

        // Assert - Editor can read, create, update
        Assert.IsTrue(auth.HasReadAccess());
        Assert.IsTrue(auth.HasCreateAccess());
        Assert.IsTrue(auth.HasUpdateAccess());
        Assert.IsFalse(auth.HasDeleteAccess());

        // Add Admin role
        mockUser.Roles.Add("Admin");

        // Assert - Admin can do everything
        Assert.IsTrue(auth.HasReadAccess());
        Assert.IsTrue(auth.HasCreateAccess());
        Assert.IsTrue(auth.HasUpdateAccess());
        Assert.IsTrue(auth.HasDeleteAccess());
    }

    #endregion
}
