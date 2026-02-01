// -----------------------------------------------------------------------------
// Design.Tests - Common Gotcha Tests
// -----------------------------------------------------------------------------
// Tests that verify the gotcha behaviors documented in Design.Domain/CommonGotchas.cs.
// These tests demonstrate both the incorrect assumption and the correct behavior.
// -----------------------------------------------------------------------------

using Design.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;

namespace Design.Tests.GotchaTests;

[TestClass]
public class CommonGotchaTests
{
    private IServiceScope _scope = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    // =========================================================================
    // GOTCHA 1: Rules don't fire during [Create]
    // =========================================================================

    [TestMethod]
    public void Gotcha1_RulesDoNotFireDuringCreate()
    {
        // Arrange
        var factory = _scope.GetRequiredService<IGotcha1DemoFactory>();

        // Act - Create sets Quantity=10 and Price=5.00
        var entity = factory.Create();

        // Assert - Total is NOT calculated because rules were paused during Create
        Assert.AreEqual(10, entity.Quantity);
        Assert.AreEqual(5.00m, entity.Price);
        Assert.AreEqual(0m, entity.Total, "Total should be 0 - rule did not fire during Create");
        Assert.IsFalse(entity.RuleHasRun, "Rule should NOT have run during Create");
    }

    [TestMethod]
    public async Task Gotcha1_RulesFireAfterCreate_WithWaitForTasks()
    {
        // Arrange
        var factory = _scope.GetRequiredService<IGotcha1DemoFactory>();

        // Act
        var entity = factory.Create();

        // After Create completes, we can trigger rules by modifying a property
        // OR by calling RunRules explicitly
        await entity.RunRules(RunRulesFlag.All);

        // Assert - Now the rule has run
        Assert.AreEqual(50.00m, entity.Total, "Total should be calculated after RunRules");
        Assert.IsTrue(entity.RuleHasRun, "Rule should have run after explicit RunRules call");
    }

    [TestMethod]
    public void Gotcha1_ExplicitCalculationInCreate_WorksCorrectly()
    {
        // Arrange
        var factory = _scope.GetRequiredService<IGotcha1DemoFactory>();

        // Act - Use the Create overload that calculates explicitly
        var entity = factory.CreateWithExplicitCalculation();

        // Assert - Total is set explicitly in Create, no rule needed
        Assert.AreEqual(10, entity.Quantity);
        Assert.AreEqual(5.00m, entity.Price);
        Assert.AreEqual(50.00m, entity.Total, "Total should be 50 from explicit calculation");
    }

    [TestMethod]
    public async Task Gotcha1_RulesFireOnPropertyChange_AfterCreate()
    {
        // Arrange
        var factory = _scope.GetRequiredService<IGotcha1DemoFactory>();
        var entity = factory.Create();

        // Act - Change a property AFTER Create
        entity.Quantity = 20;  // This triggers the rule
        await entity.WaitForTasks();

        // Assert - Rule fired due to property change
        Assert.IsTrue(entity.RuleHasRun, "Rule should fire on property change after Create");
        Assert.AreEqual(100.00m, entity.Total, "Total should be recalculated: 20 * 5.00 = 100");
    }

    // =========================================================================
    // GOTCHA 2: DeletedList only tracks non-new items
    // =========================================================================

    [TestMethod]
    public void Gotcha2_NewItemRemoved_NotInDeletedList()
    {
        // Arrange
        var parentFactory = _scope.GetRequiredService<IGotcha2ParentFactory>();
        var itemFactory = _scope.GetRequiredService<IGotcha2ItemFactory>();

        var parent = parentFactory.Create();

        // Create a NEW item (IsNew=true)
        var newItem = itemFactory.Create();
        Assert.IsTrue(newItem.IsNew, "Newly created item should have IsNew=true");

        parent.Items!.Add(newItem);
        Assert.AreEqual(1, parent.Items.Count);

        // Act - Remove the NEW item
        parent.Items.Remove(newItem);

        // Assert - NEW items are DISCARDED, not added to DeletedList
        Assert.AreEqual(0, parent.Items.Count);
        Assert.AreEqual(0, parent.Items.DeletedCount, "New item should NOT be in DeletedList");
    }

    [TestMethod]
    public async Task Gotcha2_FetchedItemRemoved_InDeletedList()
    {
        // Arrange
        var parentFactory = _scope.GetRequiredService<IGotcha2ParentFactory>();

        // Fetch parent with existing items (IsNew=false on items)
        var parent = await parentFactory.Fetch(1);
        Assert.AreEqual(2, parent.Items!.Count, "Fetched parent should have 2 items");

        // Get a fetched item (IsNew should be false)
        var fetchedItem = parent.Items[0];
        Assert.IsFalse(fetchedItem.IsNew, "Fetched item should have IsNew=false");

        // Act - Remove the FETCHED item
        parent.Items.Remove(fetchedItem);

        // Assert - Fetched items (IsNew=false) ARE added to DeletedList
        Assert.AreEqual(1, parent.Items.Count);
        Assert.AreEqual(1, parent.Items.DeletedCount, "Fetched item SHOULD be in DeletedList");
    }

    // =========================================================================
    // GOTCHA 3: Method-injected [Service] needs [Remote]
    // =========================================================================
    // This gotcha is demonstrated through documentation rather than a runtime test,
    // because we can't easily simulate client-side DI container behavior in this test.
    // The test verifies the pattern works correctly when called server-side.

    [TestMethod]
    public async Task Gotcha3_RemoteMethodWithService_WorksOnServer()
    {
        // Arrange
        var factory = _scope.GetRequiredService<IGotcha3DemoFactory>();

        // Act - Fetch uses [Service] with [Remote] - works on server
        var entity = await factory.Fetch(1);

        // Assert - Method executed successfully with injected service
        Assert.IsNotNull(entity.Name);
    }

    // =========================================================================
    // GOTCHA 4: PauseAllActions breaks rule calculations
    // =========================================================================

    [TestMethod]
    public void Gotcha4_PausedPropertyChanges_DoNotTriggerRules()
    {
        // Arrange
        var factory = _scope.GetRequiredService<IGotcha4DemoFactory>();
        var entity = factory.Create();

        // Act - Modify properties while paused
        using (entity.PauseAllActions())
        {
            entity.Quantity = 10;
            entity.Price = 5.00m;
        }
        // ResumeAllActions() is called, but rules don't automatically run

        // Assert - Total is NOT calculated
        Assert.AreEqual(0m, entity.Total, "Total should be 0 - rules did not run while paused");
    }

    [TestMethod]
    public async Task Gotcha4_RunRulesAfterResume_CalculatesCorrectly()
    {
        // Arrange
        var factory = _scope.GetRequiredService<IGotcha4DemoFactory>();
        var entity = factory.Create();

        // Modify while paused
        using (entity.PauseAllActions())
        {
            entity.Quantity = 10;
            entity.Price = 5.00m;
        }

        // Act - Explicitly run rules after resume
        await entity.RunRules(RunRulesFlag.All);

        // Assert - Now Total is calculated
        Assert.AreEqual(50.00m, entity.Total, "Total should be calculated after explicit RunRules");
    }

    [TestMethod]
    public async Task Gotcha4_PropertyChangeWithoutPause_TriggersRules()
    {
        // Arrange
        var factory = _scope.GetRequiredService<IGotcha4DemoFactory>();
        var entity = factory.Create();

        // Act - Set properties WITHOUT pausing
        entity.Quantity = 10;
        entity.Price = 5.00m;
        await entity.WaitForTasks();  // Wait for any async rules

        // Assert - Rules fire on each property change
        Assert.AreEqual(50.00m, entity.Total, "Total should be calculated when not paused");
    }

    // =========================================================================
    // GOTCHA 5: IsModified includes child modifications
    // =========================================================================

    [TestMethod]
    public async Task Gotcha5_ChildModification_SetsParentIsModified()
    {
        // Arrange
        var factory = _scope.GetRequiredService<IGotcha5ParentFactory>();

        // Fetch parent (starts as unmodified)
        var parent = await factory.Fetch(1);
        Assert.IsFalse(parent.IsModified, "Freshly fetched parent should not be modified");
        Assert.IsFalse(parent.IsSelfModified, "Freshly fetched parent should not be self-modified");

        // Act - Modify only the CHILD
        parent.Child!.Value = "Changed Value";
        await parent.WaitForTasks();

        // Assert
        Assert.IsTrue(parent.Child.IsSelfModified, "Child should be self-modified");
        Assert.IsTrue(parent.Child.IsModified, "Child should be modified");
        Assert.IsFalse(parent.IsSelfModified, "Parent itself is NOT modified - only child changed");
        Assert.IsTrue(parent.IsModified, "Parent.IsModified should be TRUE because child is modified");
    }

    [TestMethod]
    public async Task Gotcha5_ParentModification_SetsParentIsSelfModified()
    {
        // Arrange
        var factory = _scope.GetRequiredService<IGotcha5ParentFactory>();

        // Fetch parent
        var parent = await factory.Fetch(1);

        // Act - Modify only the PARENT
        parent.Name = "New Name";
        await parent.WaitForTasks();

        // Assert
        Assert.IsTrue(parent.IsSelfModified, "Parent should be self-modified");
        Assert.IsTrue(parent.IsModified, "Parent should be modified");
        Assert.IsFalse(parent.Child!.IsSelfModified, "Child should NOT be modified");
    }

    [TestMethod]
    public async Task Gotcha5_BothModified_BothFlagsTrue()
    {
        // Arrange
        var factory = _scope.GetRequiredService<IGotcha5ParentFactory>();

        // Fetch parent
        var parent = await factory.Fetch(1);

        // Act - Modify both parent and child
        parent.Name = "New Parent Name";
        parent.Child!.Value = "New Child Value";
        await parent.WaitForTasks();

        // Assert
        Assert.IsTrue(parent.IsSelfModified, "Parent should be self-modified");
        Assert.IsTrue(parent.IsModified, "Parent should be modified");
        Assert.IsTrue(parent.Child.IsSelfModified, "Child should be self-modified");
        Assert.IsTrue(parent.Child.IsModified, "Child should be modified");
    }
}
