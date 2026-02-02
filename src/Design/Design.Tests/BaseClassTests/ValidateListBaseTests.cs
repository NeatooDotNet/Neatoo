// -----------------------------------------------------------------------------
// Design.Tests - ValidateListBase Tests
// -----------------------------------------------------------------------------
// Tests demonstrating ValidateListBase<I> behavior including validation
// aggregation and parent-child relationships.
// -----------------------------------------------------------------------------

using Design.Domain.BaseClasses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.BaseClassTests;

[TestClass]
public class ValidateListBaseTests
{
    private IServiceScope _scope = null!;
    private IDemoValueObjectListFactory _listFactory = null!;
    private IDemoValueObjectFactory _itemFactory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _listFactory = _scope.GetRequiredService<IDemoValueObjectListFactory>();
        _itemFactory = _scope.GetRequiredService<IDemoValueObjectFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void Create_InitializesEmptyList()
    {
        // Arrange & Act
        var list = _listFactory.Create();

        // Assert
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void Add_ItemBecomesPartOfList()
    {
        // Arrange
        var list = _listFactory.Create();
        var item = _itemFactory.Create("Test Item");

        // Act
        list.Add(item);

        // Assert
        Assert.AreEqual(1, list.Count);
        Assert.AreSame(item, list[0]);
    }

    [TestMethod]
    public async Task Add_InvalidItem_ListBecomesInvalid()
    {
        // Arrange
        var list = _listFactory.Create();
        var validItem = _itemFactory.Create("Valid");
        list.Add(validItem);
        Assert.IsTrue(list.IsValid);

        // Act - Add item then make it invalid
        var itemToInvalidate = _itemFactory.Create("Initially Valid");
        list.Add(itemToInvalidate);
        itemToInvalidate.Name = ""; // Make invalid
        await itemToInvalidate.WaitForTasks();

        // Assert
        Assert.IsFalse(list.IsValid, "List should be invalid if any child is invalid");
    }

    [TestMethod]
    public void AllValidItems_ListIsValid()
    {
        // Arrange
        var list = _listFactory.Create();
        var item1 = _itemFactory.Create("Valid 1");
        var item2 = _itemFactory.Create("Valid 2");

        // Act
        list.Add(item1);
        list.Add(item2);

        // Assert
        Assert.IsTrue(list.IsValid, "List should be valid when all children are valid");
    }
}
