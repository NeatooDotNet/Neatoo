using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

#region Test Entity Definitions

/// <summary>
/// Interface for testing fetch-returns-false behavior across client-server boundary.
/// When a [Fetch] method returns false, the factory should return null to the client.
/// </summary>
public interface IFetchReturnsFalseEntity : IEntityRoot
{
    string Name { get; set; }
}

/// <summary>
/// Entity with a [Fetch] method that returns Task&lt;bool&gt;.
/// When shouldExist is false, Fetch returns false to signal "not found",
/// and the factory should return null to the caller.
/// </summary>
[Factory]
internal partial class FetchReturnsFalseEntity : EntityBase<FetchReturnsFalseEntity>, IFetchReturnsFalseEntity
{
    public FetchReturnsFalseEntity(IEntityBaseServices<FetchReturnsFalseEntity> services) : base(services)
    {
    }

    public partial string Name { get; set; }

    [Remote]
    [Fetch]
    internal Task<bool> Fetch(bool shouldExist)
    {
        if (!shouldExist)
        {
            return Task.FromResult(false);
        }

        this.Name = "Found";
        return Task.FromResult(true);
    }
}

#endregion

/// <summary>
/// Integration tests verifying that when a [Fetch] method returns false,
/// the client receives null instead of an exception.
///
/// This is a regression test: the expected behavior is that a bool-returning
/// fetch method signals "not found" by returning false, and the generated
/// factory returns null to the caller. If the generated factory calls
/// ForDelegate instead of ForDelegateNullable, an InvalidOperationException
/// is thrown instead.
/// </summary>
[TestClass]
public class FetchReturnsFalseTests : ClientServerTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScopes();
    }

    /// <summary>
    /// When a [Fetch] method returns true, the factory should return the populated entity.
    /// This is the happy path baseline.
    /// </summary>
    [TestMethod]
    public async Task Fetch_ReturnsTrue_EntityIsReturned()
    {
        // Arrange
        var factory = GetClientService<IFetchReturnsFalseEntityFactory>();

        // Act
        var entity = await factory.Fetch(shouldExist: true);

        // Assert
        Assert.IsNotNull(entity, "Factory should return a non-null entity when fetch returns true");
        Assert.AreEqual("Found", entity.Name, "Entity should have the name set by the fetch method");
    }

    /// <summary>
    /// When a [Fetch] method returns false, the factory should return null to the client.
    /// This is the core regression test: previously, this scenario threw
    /// InvalidOperationException("Remote call failed") because the generated factory
    /// called ForDelegate (which throws on null) instead of ForDelegateNullable.
    /// </summary>
    [TestMethod]
    public async Task Fetch_ReturnsFalse_ClientReceivesNull()
    {
        // Arrange
        var factory = GetClientService<IFetchReturnsFalseEntityFactory>();

        // Act
        var entity = await factory.Fetch(shouldExist: false);

        // Assert
        Assert.IsNull(entity, "Factory should return null when fetch returns false");
    }

    /// <summary>
    /// Verifies server-side behavior directly: when fetch returns true,
    /// the server factory returns a non-null entity.
    /// </summary>
    [TestMethod]
    public async Task Fetch_ServerSide_ReturnsTrue_EntityIsReturned()
    {
        // Arrange
        var factory = GetServerService<IFetchReturnsFalseEntityFactory>();

        // Act
        var entity = await factory.Fetch(shouldExist: true);

        // Assert
        Assert.IsNotNull(entity, "Server-side factory should return a non-null entity when fetch returns true");
        Assert.AreEqual("Found", entity.Name);
    }

    /// <summary>
    /// Verifies server-side behavior directly: when fetch returns false,
    /// the server factory returns null (no serialization boundary involved).
    /// If this test passes but Fetch_ReturnsFalse_ClientReceivesNull fails,
    /// the bug is in the generated remote factory code (ForDelegate vs ForDelegateNullable).
    /// </summary>
    [TestMethod]
    public async Task Fetch_ServerSide_ReturnsFalse_ReturnsNull()
    {
        // Arrange
        var factory = GetServerService<IFetchReturnsFalseEntityFactory>();

        // Act
        var entity = await factory.Fetch(shouldExist: false);

        // Assert
        Assert.IsNull(entity, "Server-side factory should return null when fetch returns false");
    }
}
