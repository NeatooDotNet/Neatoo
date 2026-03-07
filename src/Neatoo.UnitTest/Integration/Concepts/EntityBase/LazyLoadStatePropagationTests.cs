using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.Serialization.LazyLoadTests;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.EntityBase;

/// <summary>
/// Tests that state changes in a child entity loaded via LazyLoad&lt;T&gt;
/// propagate up to the parent entity, just like regular partial properties.
/// </summary>
[TestClass]
public class LazyLoadStatePropagationTests : IntegrationTestBase
{
    private ILazyLoadEntityObject parent = null!;
    private ILazyLoadEntityObject child = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        InitializeScope();

        var factory = GetRequiredService<ILazyLoadEntityObjectFactory>();

        // Fetch creates entities in a persisted (not new, not modified) state
        parent = await factory.Fetch(Guid.NewGuid(), "Parent", "parent desc");
        child = await factory.Fetch(Guid.NewGuid(), "Child", "child desc");

        // Set child into parent's LazyLoad property (pre-loaded)
        parent.LazyChild = new LazyLoad<ILazyLoadEntityObject>(child);
    }

    [TestMethod]
    public void LazyLoadChild_InitialState_ParentNotModified()
    {
        // After Fetch, the partial properties are not modified.
        // Verify child is not modified.
        Assert.IsFalse(child.IsModified, "Child should not be modified after Fetch");
    }

    [TestMethod]
    public async Task LazyLoadChild_ModifyChild_ParentIsModified()
    {
        // Act - modify the child entity inside LazyLoad
        child.Name = "Modified Child";
        await parent.WaitForTasks();

        // Assert - parent should reflect child's modification
        Assert.IsTrue(child.IsModified, "Child should be modified after Name change");
        Assert.IsTrue(parent.IsModified, "Parent should be modified when LazyLoad child is modified");
    }

    [TestMethod]
    public async Task LazyLoadChild_ModifyChild_ParentNotSelfModified()
    {
        // Act
        child.Name = "Modified Child";
        await parent.WaitForTasks();

        // Assert - parent itself wasn't modified, only its child was
        Assert.IsFalse(parent.IsSelfModified, "Parent should not be self-modified when only child changed");
        Assert.IsTrue(child.IsSelfModified, "Child should be self-modified");
    }

    [TestMethod]
    public async Task LazyLoadChild_ModifyChild_ParentIsSavable()
    {
        // Act
        child.Name = "Modified Child";
        await parent.WaitForTasks();

        // Assert - parent should be savable since it has modified children
        Assert.IsTrue(((IEntityRoot)parent).IsSavable, "Parent should be savable when LazyLoad child is modified");
    }
}
