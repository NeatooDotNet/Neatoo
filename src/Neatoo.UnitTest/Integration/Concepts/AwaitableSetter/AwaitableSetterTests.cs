using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.AwaitableSetter;

/// <summary>
/// The generated Set{Name} returns the Task of the property's whole rule cascade.
/// </summary>
[TestClass]
public class AwaitableSetterTests : IntegrationTestBase
{
    private IAwaitParent parent = null!;
    private IAwaitChild child = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        InitializeScope();
        parent = GetRequiredService<IAwaitParentFactory>().Create();
        child = GetRequiredService<IAwaitChildFactory>().Create();
        await parent.SetChild(child);
    }

    [TestMethod]
    public async Task SetX_Awaited_ReturnsAfterChildAndParentAsyncRules()
    {
        await child.SetQuantity(6);

        Assert.AreEqual(60, child.LineTotal, "the child's async rule finished");
        Assert.AreEqual(60, parent.Total, "the parent's async rule on the child's property finished");
        Assert.AreEqual("High", parent.Band, "the async rule the parent's write triggered finished");
        Assert.AreEqual("Total 60", parent.Note, "the class-based rule's write finished");
        Assert.IsFalse(parent.IsBusy, "nothing is left running");
    }

    [TestMethod]
    public async Task Setter_Assigned_ReturnsBeforeAsyncConsequencesLand()
    {
        // The generated setter cannot await: assignment forks the cascade and returns.
        child.Quantity = 6;

        Assert.AreEqual(0, child.LineTotal, "the child's async rule has not finished when assignment returns");
        Assert.IsTrue(parent.IsBusy, "the forked cascade is tracked");

        await parent.WaitForTasks();
        Assert.AreEqual(60, parent.Total);
    }

    [TestMethod]
    public async Task SetX_InsideAsyncRule_CompletesNestedCascadeBeforeNextStatement()
    {
        await child.SetQuantity(6);

        // The total rule awaited SetTotal, whose cascade ran the async Band rule, then read Band.
        Assert.AreEqual("High", parent.BandSeenByTotalRule);
    }

    [TestMethod]
    public async Task SetX_PrivateSet_WritesPastIsReadOnly()
    {
        Assert.IsTrue(child[nameof(IAwaitChild.LineTotal)].IsReadOnly, "a private-set property is read-only to the outside");
        await Assert.ThrowsExactlyAsync<PropertyReadOnlyException>(
            () => child[nameof(IAwaitChild.LineTotal)].SetValue(5));

        await child.SetQuantity(2);

        Assert.AreEqual(20, child.LineTotal, "the rule's SetLineTotal wrote past IsReadOnly");
    }

    [TestMethod]
    public async Task SetX_ThroughInterface_IsTheTypedWrite()
    {
        IAwaitParent root = parent;

        await root.SetNote("typed");

        Assert.AreEqual("typed", root.Note);
        Assert.IsTrue(root.IsModified);
    }
}
