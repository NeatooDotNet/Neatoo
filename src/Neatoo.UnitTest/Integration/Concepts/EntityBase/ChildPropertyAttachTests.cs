using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Concepts.EntityBase;

/// <summary>
/// Characterization of the entity-child-PROPERTY dirt channel (ISNEW-004).
/// </summary>
/// <remarks>
/// Written BEFORE the flip, deliberately. The ISNEW-004 plan review found that
/// the design document was wrong about this path: assigning a child entity to a
/// parent property was described as "never dirties the parent — a quirk this
/// fixes", when in fact it dirties the parent today for NEW children, which is
/// the common case.
///
/// The chain: the property itself refuses to self-dirty while holding a Neatoo
/// object, but EntityProperty.IsModified is
/// `IsSelfModified || EntityChild?.IsModified`, and a new child's IsModified is
/// true ONLY because IsNew is welded into it. So this channel is a second half
/// of the same job attach-marking replaces for lists — losing it would be silent
/// data loss, not a tidied-up quirk.
///
/// These tests must pass BOTH before the flip (via the weld) and after it (via
/// attach-marking on child-property assignment). That parity is the whole point;
/// a failure here after ISNEW-004 means the property channel was missed.
/// </remarks>
[TestClass]
public class ChildPropertyAttachTests
{
    private static EntityPerson CreateFetchedParent()
    {
        // A parent in the state a [Fetch] would leave it: old and clean
        var parent = new EntityPerson();
        parent.FirstName = "Parent";
        ((IEntityPerson)parent).MarkOld();
        ((IEntityPerson)parent).MarkUnmodified();
        return parent;
    }

    private static EntityPerson CreateNewChild()
    {
        var child = new EntityPerson();
        ((IEntityPerson)child).MarkNew();
        return child;
    }

    [TestMethod]
    public void AssignNewChildToCleanParent_DirtiesParent()
    {
        // Arrange
        var parent = CreateFetchedParent();
        Assert.IsFalse(parent.IsModified, "Precondition: parent starts clean");

        // Act - assign a new child to the parent's entity-child property
        parent.Child = CreateNewChild();

        // Assert - the parent is dirtied by the attach. Pre-flip this arrives
        // through the child's welded IsModified; post-flip through the mark
        // applied when the child is assigned to a live parent property.
        Assert.IsTrue(parent.IsModified,
            "Attaching a new child through a property must dirty the parent");
        Assert.IsTrue(parent.IsSavable,
            "A parent dirtied by a child attach must be savable");
    }

    [TestMethod]
    public void AssignNewChildToCleanParent_DoesNotSelfDirtyTheParent()
    {
        // The property deliberately never self-dirties while holding a Neatoo
        // object; the dirt is the CHILD's, aggregating upward. This distinction
        // is what the design doc originally got wrong, so pin it explicitly.
        var parent = CreateFetchedParent();

        parent.Child = CreateNewChild();

        Assert.IsFalse(parent.IsSelfModified,
            "Holding a Neatoo child is not the parent's own modification");
        Assert.IsTrue(parent.IsModified,
            "But the graph containing it IS modified");
    }

    [TestMethod]
    public void AssignChildDuringPausedFactoryOperation_LeavesParentClean()
    {
        // Baseline population: a [Create]/[Fetch] body assigning a child must
        // not produce dirt. This is the constraint the flip must not violate
        // when it starts marking on assignment.
        var parent = new EntityPerson();
        var child = CreateNewChild();
        parent.FactoryStart(FactoryOperation.Fetch);

        parent.Child = child;

        parent.FactoryComplete(FactoryOperation.Fetch);

        // Assert BEFORE any Mark* call - otherwise the assertions would be
        // guaranteed by their own setup rather than by the framework.
        //
        // IsModified is the property that matters here: the mark lands on the
        // CHILD, and a child's dirt reaches the parent only through
        // EntityProperty.IsModified => IsSelfModified || EntityChild?.IsModified.
        // It can never surface as parent.IsSelfModified, because the property
        // deliberately refuses to self-dirty while holding a Neatoo object - so
        // asserting IsSelfModified alone would pass even if the mark leaked
        // through the paused guard.
        Assert.IsFalse(child.IsModified,
            "A child assigned during a paused factory op must not be marked");
        Assert.IsFalse(parent.IsModified,
            "...so the parent stays clean");
        Assert.IsFalse(parent.IsSelfModified,
            "...and the property never self-dirties for a Neatoo child");
    }

    [TestMethod]
    public void AssignFactoryPopulatedListToLiveParent_DirtiesParent()
    {
        // The channel the first version of the flip missed. A list built by its
        // own factory operation added its children while PAUSED, so they carry
        // no attach-mark; post-flip their IsNew no longer makes them modified.
        // Assigning such a list to a live parent must still dirty it, or the
        // parent is unsavable and the children's inserts never run - which is
        // what the weld used to prevent.
        var parent = CreateFetchedParent();
        Assert.IsFalse(parent.IsModified, "Precondition: parent starts clean");

        var list = new EntityPersonList();
        list.FactoryStart(FactoryOperation.Create);
        list.Add(CreateNewChild());   // paused add - deliberately unmarked
        list.FactoryComplete(FactoryOperation.Create);
        Assert.IsFalse(list.IsModified,
            "Precondition: a factory-populated list is clean on its own");

        // Act - hand the populated list to a live parent
        parent.ChildList = list;

        // Assert - the new children are marked on attach, so dirt reaches the parent
        Assert.IsTrue(list.IsModified, "The list's new children are now marked");
        Assert.IsTrue(parent.IsModified,
            "Assigning a populated list to a live parent dirties it");
        Assert.IsTrue(parent.IsSavable, "...and makes it savable");
    }

    [TestMethod]
    public void AssignListValuedChildProperty_DirtIsCarriedByItsChildren()
    {
        // A list-valued child property needs no marking of its own: entity
        // lists have no MarkModified, and they do not need one - a list's
        // IsModified aggregates from its children, which are attach-marked as
        // they are added. Pin the two directions.
        var parent = CreateFetchedParent();
        var emptyList = new EntityPersonList();

        // An empty new list carries no work
        parent.ChildList = emptyList;
        Assert.IsFalse(parent.IsModified,
            "Assigning an empty list is not user work - nothing to save");

        // Adding a child to the attached list dirties the graph
        parent.ChildList.Add(CreateNewChild());
        Assert.IsTrue(parent.IsModified,
            "A child added to the attached list dirties the parent through the list");
    }
}
