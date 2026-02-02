// -----------------------------------------------------------------------------
// Design.Tests - [Fetch] Factory Operation Tests
// -----------------------------------------------------------------------------
// Tests demonstrating [Fetch] patterns for loading existing objects.
// -----------------------------------------------------------------------------

using Design.Domain.FactoryOperations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.FactoryTests;

[TestClass]
public class FetchTests
{
    private IServiceScope _scope = null!;
    private IFetchDemoFactory _factory = null!;
    private IFetchWithChildrenDemoFactory _parentFactory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IFetchDemoFactory>();
        _parentFactory = _scope.GetRequiredService<IFetchWithChildrenDemoFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public async Task Fetch_ById_LoadsEntity()
    {
        // Arrange & Act
        var entity = await _factory.Fetch(42);

        // Assert
        Assert.IsNotNull(entity);
        Assert.AreEqual(42, entity.Id);
        Assert.AreEqual("Fetched-42", entity.Name);
    }

    [TestMethod]
    public async Task Fetch_SetsIsNewFalse()
    {
        // Arrange & Act
        var entity = await _factory.Fetch(1);

        // Assert
        Assert.IsFalse(entity.IsNew, "Fetched entity should not be new");
    }

    [TestMethod]
    public async Task Fetch_SetsIsModifiedFalse()
    {
        // Arrange & Act
        var entity = await _factory.Fetch(1);

        // Assert
        Assert.IsFalse(entity.IsModified, "Fetched entity should not be modified");
        Assert.IsFalse(entity.IsSelfModified, "Fetched entity should not be self-modified");
    }

    [TestMethod]
    public async Task Fetch_WithChildren_LoadsChildCollection()
    {
        // Arrange & Act
        var parent = await _parentFactory.Fetch(1);

        // Assert
        Assert.IsNotNull(parent.Items);
        Assert.AreEqual(2, parent.Items.Count, "Should load 2 child items");
    }
}
