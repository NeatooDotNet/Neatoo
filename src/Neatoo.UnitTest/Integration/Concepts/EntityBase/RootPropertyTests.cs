using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.UnitTest.Integration.Concepts.EntityBase;

[TestClass]
public class RootPropertyTests
{
    [TestMethod]
    public void Root_StandaloneEntity_IsNull()
    {
        // Arrange
        var entity = new EntityPerson();

        // Act & Assert
        Assert.IsNull(entity.Root);
    }

    [TestMethod]
    public void Root_AggregateRoot_IsNull()
    {
        // Arrange - Entity with no parent is the aggregate root
        IEntityPerson aggregateRoot = new EntityPerson();
        aggregateRoot.MarkUnmodified();

        // Act & Assert
        Assert.IsNull(aggregateRoot.Parent);
        Assert.IsNull(aggregateRoot.Root);
    }

    [TestMethod]
    public void Root_ChildEntity_ReturnsAggregateRoot()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var childList = new EntityPersonList();
        var child = new EntityPerson();

        // Act - Build the hierarchy
        aggregateRoot.ChildList = childList;
        childList.Add(child);

        // Assert
        Assert.AreEqual(aggregateRoot, child.Parent);
        Assert.AreEqual(aggregateRoot, child.Root);
    }

    [TestMethod]
    public void Root_DeeplyNestedEntity_ReturnsAggregateRoot()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var level1List = new EntityPersonList();
        var level1Child = new EntityPerson();
        var level2List = new EntityPersonList();
        var level2Child = new EntityPerson();

        // Act - Build deep hierarchy: root -> list1 -> child1 -> list2 -> child2
        aggregateRoot.ChildList = level1List;
        level1List.Add(level1Child);
        level1Child.ChildList = level2List;
        level2List.Add(level2Child);

        // Assert
        Assert.AreEqual(aggregateRoot, level1Child.Root);
        Assert.AreEqual(aggregateRoot, level2Child.Root);
    }

    [TestMethod]
    public void Root_EntityList_ReturnsAggregateRoot()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var childList = new EntityPersonList();

        // Act
        aggregateRoot.ChildList = childList;

        // Assert
        Assert.AreEqual(aggregateRoot, childList.Parent);
        Assert.AreEqual(aggregateRoot, childList.Root);
    }

    [TestMethod]
    public void Root_NestedEntityList_ReturnsAggregateRoot()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var level1List = new EntityPersonList();
        var level1Child = new EntityPerson();
        var level2List = new EntityPersonList();

        // Act - Build: root -> list1 -> child1 -> list2
        aggregateRoot.ChildList = level1List;
        level1List.Add(level1Child);
        level1Child.ChildList = level2List;

        // Assert
        Assert.AreEqual(aggregateRoot, level2List.Root);
    }

    [TestMethod]
    public void AddToList_BrandNewItem_Succeeds()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list = new EntityPersonList();
        aggregateRoot.ChildList = list;

        var newItem = new EntityPerson(); // Brand new, Root is null

        // Act & Assert - Should not throw
        Assert.IsNull(newItem.Root);
        list.Add(newItem);
        Assert.AreEqual(aggregateRoot, newItem.Root);
    }

    [TestMethod]
    public void AddToList_SameAggregate_Succeeds()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list1 = new EntityPersonList();
        var list2 = new EntityPersonList();
        aggregateRoot.ChildList = list1;

        var item = new EntityPerson();
        list1.Add(item);

        // Create second list in same aggregate
        var childWithList = new EntityPerson();
        list1.Add(childWithList);
        childWithList.ChildList = list2;

        // Act - Remove from list1 and add to list2 (same aggregate)
        list1.Remove(item);
        Assert.AreEqual(aggregateRoot, item.Root); // Still has same root

        // Should succeed since same aggregate
        list2.Add(item);

        // Assert
        Assert.AreEqual(aggregateRoot, item.Root);
    }

    [TestMethod]
    public void AddToList_DifferentAggregate_Throws()
    {
        // Arrange
        var aggregate1 = new EntityPerson();
        var list1 = new EntityPersonList();
        aggregate1.ChildList = list1;

        var aggregate2 = new EntityPerson();
        var list2 = new EntityPersonList();
        aggregate2.ChildList = list2;

        var item = new EntityPerson();
        list1.Add(item);

        // Act & Assert
        Assert.AreEqual(aggregate1, item.Root);
        var exception = Assert.ThrowsException<InvalidOperationException>(() => list2.Add(item));
        Assert.IsTrue(exception.Message.Contains("Cannot add"));
        Assert.IsTrue(exception.Message.Contains("belongs to aggregate"));
    }

    [TestMethod]
    public void AddToList_WhenPaused_SkipsCrossAggregateCheck()
    {
        // Arrange
        var aggregate1 = new EntityPerson();
        var list1 = new EntityPersonList();
        aggregate1.ChildList = list1;

        var aggregate2 = new EntityPerson();
        var list2 = new EntityPersonList();
        aggregate2.ChildList = list2;

        var item = new EntityPerson();
        list1.Add(item);

        // Act - Pause and add (simulates deserialization)
        list2.FactoryStart(Neatoo.RemoteFactory.FactoryOperation.Fetch);

        // Should not throw when paused
        list2.Add(item);

        list2.FactoryComplete(Neatoo.RemoteFactory.FactoryOperation.Fetch);

        // Assert - Item was added
        Assert.IsTrue(list2.Contains(item));
    }

    [TestMethod]
    public void AddToList_RootLevelList_AllowsNewItems()
    {
        // Arrange - A list that has no parent (standalone list)
        var list = new EntityPersonList();
        var item = new EntityPerson();

        // Act & Assert - Should succeed since list.Root is null
        Assert.IsNull(list.Root);
        list.Add(item);
        Assert.IsNull(item.Root); // Item's root is also null since list has no parent
    }

    [TestMethod]
    public void Root_AfterRemoveFromList_StillPointsToPreviousAggregate()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list = new EntityPersonList();
        aggregateRoot.ChildList = list;

        var item = new EntityPerson();
        list.Add(item);

        // Act
        list.Remove(item);

        // Assert - Parent still points to the aggregate root
        // (This is expected behavior - Parent is set when added, not cleared on remove)
        Assert.AreEqual(aggregateRoot, item.Root);
    }
}
