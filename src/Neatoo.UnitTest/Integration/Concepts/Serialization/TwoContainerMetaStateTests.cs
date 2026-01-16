using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.Serialization.EntityTests;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

/// <summary>
/// Integration tests that verify entity meta property state (IsNew, IsModified, IsSavable)
/// after factory operations that cross the client-server serialization boundary.
/// These tests simulate the full two-container scenario where:
/// 1. Client calls a remote factory method
/// 2. Server creates/fetches the entity and populates it
/// 3. Entity is serialized and sent to client
/// 4. Client deserializes and uses the entity
/// </summary>
[TestClass]
public class TwoContainerMetaStateTests : ClientServerTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScopes();
    }

    #region Create Operation Tests

    /// <summary>
    /// Verifies that after a Create operation through two containers,
    /// IsNew is true (entity hasn't been persisted yet).
    /// </summary>
    [TestMethod]
    public void Create_TwoContainer_IsNew_ReturnsTrue()
    {
        // Arrange
        var factory = GetClientService<IEntityObjectFactory>();
        var id = Guid.NewGuid();
        var name = "Test Entity";

        // Act
        var entity = factory.Create(id, name);

        // Assert
        Assert.IsTrue(entity.IsNew, "Entity should be new after Create operation");
    }

    /// <summary>
    /// Verifies that after a Create operation through two containers,
    /// IsModified is true (new entities are considered modified).
    /// </summary>
    [TestMethod]
    public void Create_TwoContainer_IsModified_ReturnsTrue()
    {
        // Arrange
        var factory = GetClientService<IEntityObjectFactory>();
        var id = Guid.NewGuid();
        var name = "Test Entity";

        // Act
        var entity = factory.Create(id, name);

        // Assert
        Assert.IsTrue(entity.IsModified, "Entity should be modified after Create operation (new entities are modified)");
    }

    /// <summary>
    /// Verifies that after a Create operation through two containers,
    /// IsSavable is true (new, modified, valid, not busy, not child).
    /// </summary>
    [TestMethod]
    public async Task Create_TwoContainer_IsSavable_ReturnsTrue()
    {
        // Arrange
        var factory = GetClientService<IEntityObjectFactory>();
        var id = Guid.NewGuid();
        var name = "Test Entity";

        // Act
        var entity = factory.Create(id, name);
        await entity.WaitForTasks(); // Ensure validation is complete

        // Assert
        Assert.IsTrue(entity.IsValid, "Entity should be valid");
        Assert.IsFalse(entity.IsBusy, "Entity should not be busy");
        Assert.IsFalse(entity.IsChild, "Entity should not be a child");
        Assert.IsTrue(entity.IsSavable, "Entity should be savable after Create operation");
    }

    #endregion

    #region Fetch Operation Tests

    /// <summary>
    /// Verifies that after a Fetch operation through two containers,
    /// IsNew is false (fetched entities are existing/old).
    /// </summary>
    [TestMethod]
    public async Task Fetch_TwoContainer_IsNew_ReturnsFalse()
    {
        // Arrange
        var factory = GetClientService<IEntityObjectFactory>();
        var id = Guid.NewGuid();
        var name = "Fetched Entity";

        // Act
        var entity = await factory.Fetch(id, name);

        // Assert
        Assert.IsFalse(entity.IsNew, "Entity should not be new after Fetch operation");
    }

    /// <summary>
    /// Verifies that after a Fetch operation through two containers,
    /// IsModified is false (fetched entities have no pending changes).
    /// </summary>
    [TestMethod]
    public async Task Fetch_TwoContainer_IsModified_ReturnsFalse()
    {
        // Arrange
        var factory = GetClientService<IEntityObjectFactory>();
        var id = Guid.NewGuid();
        var name = "Fetched Entity";

        // Act
        var entity = await factory.Fetch(id, name);

        // Assert
        Assert.IsFalse(entity.IsModified, "Entity should not be modified after Fetch operation");
    }

    /// <summary>
    /// Verifies that after a Fetch operation through two containers,
    /// IsSelfModified is false (no properties have been changed).
    /// </summary>
    [TestMethod]
    public async Task Fetch_TwoContainer_IsSelfModified_ReturnsFalse()
    {
        // Arrange
        var factory = GetClientService<IEntityObjectFactory>();
        var id = Guid.NewGuid();
        var name = "Fetched Entity";

        // Act
        var entity = await factory.Fetch(id, name);

        // Assert
        Assert.IsFalse(entity.IsSelfModified, "Entity should not be self-modified after Fetch operation");
    }

    /// <summary>
    /// Verifies that after a Fetch operation through two containers,
    /// IsSavable is false (not modified, so nothing to save).
    /// </summary>
    [TestMethod]
    public async Task Fetch_TwoContainer_IsSavable_ReturnsFalse()
    {
        // Arrange
        var factory = GetClientService<IEntityObjectFactory>();
        var id = Guid.NewGuid();
        var name = "Fetched Entity";

        // Act
        var entity = await factory.Fetch(id, name);
        await entity.WaitForTasks(); // Ensure validation is complete

        // Assert
        Assert.IsFalse(entity.IsSavable, "Entity should not be savable after Fetch operation (not modified)");
    }

    /// <summary>
    /// Verifies that ModifiedProperties is empty after a Fetch operation.
    /// </summary>
    [TestMethod]
    public async Task Fetch_TwoContainer_ModifiedProperties_IsEmpty()
    {
        // Arrange
        var factory = GetClientService<IEntityObjectFactory>();
        var id = Guid.NewGuid();
        var name = "Fetched Entity";

        // Act
        var entity = await factory.Fetch(id, name);

        // Assert
        Assert.AreEqual(0, entity.ModifiedProperties.Count(), "ModifiedProperties should be empty after Fetch operation");
    }

    #endregion

    #region Post-Fetch Modification Tests

    /// <summary>
    /// Verifies that modifying a fetched entity correctly sets IsModified to true.
    /// </summary>
    [TestMethod]
    public async Task Fetch_ThenModify_IsModified_ReturnsTrue()
    {
        // Arrange
        var factory = GetClientService<IEntityObjectFactory>();
        var id = Guid.NewGuid();
        var entity = await factory.Fetch(id, "Original Name");
        await entity.WaitForTasks();

        // Verify initial state
        Assert.IsFalse(entity.IsModified, "Precondition: Entity should not be modified after Fetch");

        // Act
        entity.Name = "Modified Name";
        await entity.WaitForTasks();

        // Assert
        Assert.IsTrue(entity.IsModified, "Entity should be modified after property change");
        Assert.IsTrue(entity.IsSelfModified, "Entity should be self-modified after property change");
        Assert.IsTrue(entity.IsSavable, "Entity should be savable after modification");
    }

    /// <summary>
    /// Verifies that ModifiedProperties tracks which properties were changed after Fetch.
    /// </summary>
    [TestMethod]
    public async Task Fetch_ThenModify_ModifiedProperties_ContainsChangedProperty()
    {
        // Arrange
        var factory = GetClientService<IEntityObjectFactory>();
        var id = Guid.NewGuid();
        var entity = await factory.Fetch(id, "Original Name");
        await entity.WaitForTasks();

        // Act
        entity.Name = "Modified Name";

        // Assert
        var modifiedProps = entity.ModifiedProperties.ToList();
        Assert.IsTrue(modifiedProps.Contains("Name"), "ModifiedProperties should contain 'Name'");
    }

    #endregion

    #region Diagnostic Tests - Server Side Check

    /// <summary>
    /// DIAGNOSTIC: Checks if IsModified is already true on the SERVER side
    /// right after Fetch completes, BEFORE any serialization to the client.
    /// This helps determine if the bug is in the factory/fetch logic or in serialization.
    /// </summary>
    [TestMethod]
    public async Task Fetch_ServerSideOnly_IsModified_ReturnsFalse()
    {
        // Arrange - Use SERVER scope directly, bypassing serialization entirely
        var factory = GetServerService<IEntityObjectFactory>();
        var id = Guid.NewGuid();
        var name = "Server Fetched Entity";

        // Act - Fetch directly on server (no client-server boundary crossed)
        var entity = await factory.Fetch(id, name);

        // Assert - If this fails, the bug is in FactoryComplete/Fetch, not serialization
        Assert.IsFalse(entity.IsNew, "Server-side entity should not be new after Fetch");
        Assert.IsFalse(entity.IsModified, $"Server-side entity should not be modified after Fetch. ModifiedProperties: [{string.Join(", ", entity.ModifiedProperties)}]");
        Assert.IsFalse(entity.IsSelfModified, "Server-side entity should not be self-modified after Fetch");
        Assert.AreEqual(0, entity.ModifiedProperties.Count(), $"Server-side entity should have no modified properties. Found: [{string.Join(", ", entity.ModifiedProperties)}]");
    }

    /// <summary>
    /// DIAGNOSTIC: Checks if IsModified is already true on the SERVER side
    /// after Create operation.
    /// </summary>
    [TestMethod]
    public void Create_ServerSideOnly_IsModified_ReturnsTrue()
    {
        // Arrange - Use SERVER scope directly
        var factory = GetServerService<IEntityObjectFactory>();
        var id = Guid.NewGuid();
        var name = "Server Created Entity";

        // Act - Create directly on server
        var entity = factory.Create(id, name);

        // Assert - Create should result in IsNew=true, IsModified=true
        Assert.IsTrue(entity.IsNew, "Server-side entity should be new after Create");
        Assert.IsTrue(entity.IsModified, "Server-side entity should be modified after Create");
    }

    #endregion
}
