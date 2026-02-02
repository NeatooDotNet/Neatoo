// -----------------------------------------------------------------------------
// Design.Tests - [Create] Factory Operation Tests
// -----------------------------------------------------------------------------
// Tests demonstrating [Create] patterns for object initialization.
// -----------------------------------------------------------------------------

using Design.Domain.FactoryOperations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.FactoryTests;

[TestClass]
public class CreateTests
{
    private IServiceScope _scope = null!;
    private ICreateDemoFactory _factory = null!;
    private ICreateWithChildrenDemoFactory _parentFactory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<ICreateDemoFactory>();
        _parentFactory = _scope.GetRequiredService<ICreateWithChildrenDemoFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void Create_Parameterless_ReturnsNewEntity()
    {
        // Arrange & Act
        var entity = _factory.Create();

        // Assert
        Assert.IsNotNull(entity);
        Assert.IsTrue(entity.IsNew, "Created entity should be new");
    }

    [TestMethod]
    public void Create_WithName_SetsProperty()
    {
        // Arrange & Act
        var entity = _factory.Create("Test Entity");

        // Assert
        Assert.AreEqual("Test Entity", entity.Name);
        Assert.AreEqual(1, entity.Priority, "Default priority should be set");
    }

    [TestMethod]
    public void Create_WithNameAndPriority_SetsBothProperties()
    {
        // Arrange & Act
        var entity = _factory.Create("Test Entity", 10);

        // Assert
        Assert.AreEqual("Test Entity", entity.Name);
        Assert.AreEqual(10, entity.Priority);
    }

    [TestMethod]
    public void Create_WithChildren_InitializesChildCollection()
    {
        // Arrange & Act
        var parent = _parentFactory.Create();

        // Assert
        Assert.IsNotNull(parent.Items, "Child collection should be initialized");
        Assert.AreEqual(0, parent.Items.Count, "Child collection should be empty");
    }
}
