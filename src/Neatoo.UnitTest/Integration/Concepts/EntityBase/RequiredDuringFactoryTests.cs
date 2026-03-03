using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Integration.Concepts.EntityBase;

/// <summary>
/// Entity with [Required] properties for testing validation during factory operations.
/// Reproduces the bug where RunRules() inside a paused factory operation
/// (e.g., [Insert]) leaves IsValid/IsSavable stale because
/// ValidatePropertyManager.IsValid is cached and events are dropped while paused.
/// </summary>
[Factory]
public class RequiredEntity : EntityBase<RequiredEntity>
{
    public RequiredEntity() : base(new EntityBaseServices<RequiredEntity>())
    {
    }

    [Required(ErrorMessage = "First Name is required")]
    public string FirstName { get => Getter<string>(); set => Setter(value); }

    [Required(ErrorMessage = "Last Name is required")]
    public string LastName { get => Getter<string>(); set => Setter(value); }

    // Expose FactoryStart/FactoryComplete for testing
    public void SimulateFactoryStart(FactoryOperation op) => this.FactoryStart(op);
    public void SimulateFactoryComplete(FactoryOperation op) => this.FactoryComplete(op);
}

[TestClass]
public class RequiredDuringFactoryTests
{
    /// <summary>
    /// Baseline: RunRules() correctly sets IsValid = false when NOT paused.
    /// This test passes — proves RequiredRule works in the normal case.
    /// </summary>
    [TestMethod]
    public async Task RunRules_NotPaused_RequiredFieldsEmpty_IsValidFalse()
    {
        var entity = new RequiredEntity();

        await entity.RunRules();

        Assert.IsFalse(entity.IsValid, "IsValid should be false when [Required] fields are empty");
    }

    /// <summary>
    /// BUG: RunRules() during a factory operation (paused) leaves IsValid = true
    /// because ValidatePropertyManager drops PropertyChanged events while paused.
    ///
    /// This is the core reproduction of the reported regression:
    /// [Insert] method calls RunRules() then checks IsSavable — but IsValid
    /// is stale (true) because the property manager is paused.
    /// </summary>
    [TestMethod]
    public async Task RunRules_DuringFactoryInsert_RequiredFieldsEmpty_IsValidFalse()
    {
        var entity = new RequiredEntity();

        // Simulate what the framework does before [Insert] runs
        entity.SimulateFactoryStart(FactoryOperation.Insert);

        Assert.IsTrue(entity.IsPaused, "Entity should be paused during factory operation");

        await entity.RunRules();

        // This is the bug: IsValid remains true because the cached value
        // in ValidatePropertyManager is never updated while paused
        Assert.IsFalse(entity.IsValid, "IsValid should be false after RunRules() even while paused");
    }

    /// <summary>
    /// BUG: IsSelfValid also stale during factory operation.
    /// </summary>
    [TestMethod]
    public async Task RunRules_DuringFactoryInsert_RequiredFieldsEmpty_IsSelfValidFalse()
    {
        var entity = new RequiredEntity();

        entity.SimulateFactoryStart(FactoryOperation.Insert);

        await entity.RunRules();

        Assert.IsFalse(entity.IsSelfValid, "IsSelfValid should be false after RunRules() even while paused");
    }

    /// <summary>
    /// BUG: The real-world scenario — user calls RunRules() then checks IsSavable
    /// inside an [Insert] method. IsSavable stays true because IsValid is stale.
    ///
    /// Scenario: Entity created with one required field set, other left empty.
    /// During Create factory, Setter doesn't trigger rules (paused), so IsValid stays true.
    /// Then during Insert factory, RunRules() discovers the empty required field,
    /// but IsValid stays stale at true.
    /// </summary>
    [TestMethod]
    public async Task RunRules_DuringFactoryInsert_RequiredFieldEmpty_IsSavableFalse()
    {
        var entity = new RequiredEntity();

        // Simulate Create factory — Setter during pause doesn't run rules
        entity.SimulateFactoryStart(FactoryOperation.Create);
        entity.FirstName = "John";
        // LastName intentionally left empty — the [Required] violation
        entity.SimulateFactoryComplete(FactoryOperation.Create);

        // Entity is now: New, Modified, IsValid=true (no rules ever ran for LastName)
        Assert.IsTrue(entity.IsNew, "Should be new after Create");
        Assert.IsTrue(entity.IsModified, "Should be modified after setting FirstName");
        Assert.IsTrue(entity.IsValid, "IsValid is true because no rules ran for LastName yet");
        Assert.IsTrue(entity.IsSavable, "IsSavable appears true — no validation errors yet");

        // Simulate factory Insert — this is what happens before [Insert] method runs
        entity.SimulateFactoryStart(FactoryOperation.Insert);

        // Run rules inside the factory operation (what the user does in [Insert])
        await entity.RunRules();

        // The [Insert] method would check IsSavable here
        Assert.IsFalse(entity.IsValid, "IsValid should be false — RunRules() found empty LastName");
        Assert.IsFalse(entity.IsSavable, "IsSavable should be false — entity is invalid");
    }

    /// <summary>
    /// BUG: Same issue applies to [Update] factory operations.
    /// </summary>
    [TestMethod]
    public async Task RunRules_DuringFactoryUpdate_RequiredFieldsEmpty_IsValidFalse()
    {
        var entity = new RequiredEntity();

        entity.SimulateFactoryStart(FactoryOperation.Update);

        await entity.RunRules();

        Assert.IsFalse(entity.IsValid, "IsValid should be false after RunRules() during factory update");
    }

    /// <summary>
    /// Verify that after FactoryComplete, the validity is eventually correct.
    /// This test should pass — ResumeAllActions recalculates cached values.
    /// </summary>
    [TestMethod]
    public async Task RunRules_AfterFactoryComplete_RequiredFieldsEmpty_IsValidFalse()
    {
        var entity = new RequiredEntity();

        entity.SimulateFactoryStart(FactoryOperation.Insert);
        await entity.RunRules();
        entity.SimulateFactoryComplete(FactoryOperation.Insert);

        Assert.IsFalse(entity.IsValid, "IsValid should be false after FactoryComplete recalculates");
    }

    /// <summary>
    /// Verify that valid fields remain valid during factory operation.
    /// </summary>
    [TestMethod]
    public async Task RunRules_DuringFactoryInsert_RequiredFieldsSet_IsValidTrue()
    {
        var entity = new RequiredEntity();
        entity.FirstName = "John";
        entity.LastName = "Doe";

        entity.SimulateFactoryStart(FactoryOperation.Insert);

        await entity.RunRules();

        Assert.IsTrue(entity.IsValid, "IsValid should be true when all required fields are set");
    }
}
