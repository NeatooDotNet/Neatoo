// -----------------------------------------------------------------------------
// Design.Tests - Property Basics Tests
// -----------------------------------------------------------------------------
// Tests demonstrating partial property behavior and property system.
// -----------------------------------------------------------------------------

using Design.Domain.PropertySystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.PropertyTests;

[TestClass]
public class PropertyBasicsTests
{
    private IServiceScope _scope = null!;
    private IPropertyBasicsDemoFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IPropertyBasicsDemoFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void Property_GetReturnsSetValue()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity.Name = "Test";

        // Assert
        Assert.AreEqual("Test", entity.Name);
    }

    #region skill-property-changed
    [TestMethod]
    public void Property_SetTriggersPropertyChanged()
    {
        // Arrange
        var entity = _factory.Create();
        var changedProperties = new List<string>();
        entity.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

        // Act
        entity.Name = "Test";

        // Assert
        Assert.IsTrue(changedProperties.Contains("Name"));
    }
    #endregion

    #region skill-neatoo-property-changed
    [TestMethod]
    public async Task NeatooPropertyChanged_CarriesFullNameAndReason()
    {
        var entity = _factory.Create();
        var received = new List<Neatoo.NeatooPropertyChangedEventArgs>();
        entity.NeatooPropertyChanged += args =>
        {
            received.Add(args);
            return Task.CompletedTask;
        };

        entity.Name = "Test";
        await entity.WaitForTasks();

        var nameEvent = received.Single(e => e.PropertyName == "Name");
        Assert.AreEqual("Name", nameEvent.FullPropertyName, "A dotted path for descendants; the bare name here");
        Assert.AreEqual(Neatoo.ChangeReason.UserEdit, nameEvent.Reason, "A setter outside a factory operation is a user edit");
    }
    #endregion

    #region skill-property-metadata
    [TestMethod]
    public void Indexer_ExposesPropertyMetadata()
    {
        var entity = _factory.Create();

        // Each partial property is backed by its own property object
        var nameProperty = entity["Name"];

        entity.Name = "";  // Name is required
        Assert.IsFalse(nameProperty.IsValid);
        Assert.IsTrue(nameProperty.PropertyMessages.Count > 0);
        Assert.IsFalse(nameProperty.IsBusy);
        Assert.IsFalse(nameProperty.IsReadOnly);

        // The object aggregates every property's messages
        Assert.IsTrue(entity.PropertyMessages.Any(m => m.Property.Name == "Name"));

        entity.Name = "Set";
        Assert.IsTrue(nameProperty.IsValid);
        Assert.AreEqual(0, nameProperty.PropertyMessages.Count);

        // Strongly typed access by casting
        var typed = (Neatoo.IValidateProperty<string?>)nameProperty;
        Assert.AreEqual("Set", typed.Value);
    }
    #endregion

    #region skill-set-value
    [TestMethod]
    public async Task SetValue_IsTheAwaitablePath()
    {
        var entity = _factory.Create();

        // The property setter runs the same rules but returns no Task.
        // A component that needs to await the rules calls SetValue.
        await entity["Name"].SetValue("Manual Value");

        Assert.AreEqual("Manual Value", entity.Name);
        Assert.IsTrue(entity["Name"].IsValid);
    }
    #endregion

    [TestMethod]
    public void Property_Indexer_ReturnsPropertyInterface()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        var nameProperty = entity["Name"];

        // Assert
        Assert.IsNotNull(nameProperty);
    }

    [TestMethod]
    public void Property_Indexer_CanLoadValue()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity["Name"].LoadValue("Loaded");

        // Assert
        Assert.AreEqual("Loaded", entity.Name);
    }
}

// =============================================================================
// Private Set Property Tests
// =============================================================================
// Behavioral contracts for private-set partial properties.
// These verify that the generator correctly routes through SetPrivateValue.
// =============================================================================

[TestClass]
public class PrivateSetPropertyTests
{
    private IServiceScope _scope = null!;
    private IPrivateSetPropertyDemoFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IPrivateSetPropertyDemoFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    #region docs-private-set-rule-computes
    [TestMethod]
    public void PrivateSet_RuleComputesValue()
    {
        // Scenario 8: Private-set property set internally via rule
        // WHEN Quantity and UnitPrice are set, THEN ComputedTotal is updated by AddAction rule

        // Arrange
        var entity = _factory.Create();

        // Act
        entity.Quantity = 5;
        entity.UnitPrice = 10.00m;

        // Assert
        Assert.AreEqual(50.00m, entity.ComputedTotal);
    }
    #endregion

    [TestMethod]
    public void PrivateSet_TriggersPropertyChanged()
    {
        // Scenario 8: Private-set property set internally triggers PropertyChanged
        // WHEN a rule updates ComputedTotal, THEN PropertyChanged fires for "ComputedTotal"

        // Arrange
        var entity = _factory.Create();
        var changedProperties = new List<string>();
        entity.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

        // Act
        entity.Quantity = 3;
        entity.UnitPrice = 7.00m;

        // Assert
        Assert.IsTrue(changedProperties.Contains("ComputedTotal"),
            "PropertyChanged should fire for private-set property when set by a rule");
    }

    #region docs-private-set-read-only
    [TestMethod]
    public void PrivateSet_IsReadOnlyTrue()
    {
        // Scenario 8/11: Private-set property has IsReadOnly=true
        // WHEN a property has private set, THEN its IsReadOnly is true

        // Arrange
        var entity = _factory.Create();

        // Act
        var totalProperty = entity["ComputedTotal"];

        // Assert
        Assert.IsTrue(totalProperty.IsReadOnly,
            "Private-set property should have IsReadOnly=true");
    }
    #endregion

    [TestMethod]
    public void PrivateSet_PublicPropertyIsReadOnlyFalse()
    {
        // Negative case: public set property has IsReadOnly=false

        // Arrange
        var entity = _factory.Create();

        // Act
        var quantityProperty = entity["Quantity"];

        // Assert
        Assert.IsFalse(quantityProperty.IsReadOnly,
            "Public-set property should have IsReadOnly=false");
    }

    #region docs-private-set-set-value-throws
    [TestMethod]
    public void PrivateSet_SetValueThrows()
    {
        // Scenario 9: SetValue on private-set property throws
        // WHEN entity["ComputedTotal"].SetValue(x) is called, THEN a PropertyException is thrown
        // (PropertyReadOnlyException is internal; verify via the public base class)

        // Arrange
        var entity = _factory.Create();

        // Act & Assert
        try
        {
            entity["ComputedTotal"].SetValue(99.99m);
            Assert.Fail("Expected PropertyException to be thrown for read-only property");
        }
        catch (Exception ex) when (ex is Neatoo.PropertyException)
        {
            // Expected: PropertyReadOnlyException (derives from PropertyException)
            StringAssert.Contains(ex.Message, "read-only");
        }
    }
    #endregion

    [TestMethod]
    public void PrivateSet_LoadValueSucceeds()
    {
        // Scenario 10: LoadValue on private-set property succeeds
        // WHEN entity["ComputedTotal"].LoadValue(x) is called, THEN value is set

        // Arrange
        var entity = _factory.Create();

        // Act
        entity["ComputedTotal"].LoadValue(123.45m);

        // Assert
        Assert.AreEqual(123.45m, entity.ComputedTotal);
    }

    [TestMethod]
    public void PrivateSet_InterfaceExposesGetOnly()
    {
        // Scenario 2: Interface has get-only for private-set property
        // WHEN IPrivateSetPropertyDemo is used, THEN ComputedTotal has no setter

        // This is a compile-time verification: the interface declares `decimal ComputedTotal { get; }`
        // If this test compiles, the interface is correct.
        // We verify the value is readable through the interface.

        // Arrange
        IPrivateSetPropertyDemo entity = _factory.Create();

        // Act
        entity.Quantity = 5;
        entity.UnitPrice = 10.00m;

        // Assert - can read through interface
        Assert.AreEqual(50.00m, entity.ComputedTotal);

        // Note: entity.ComputedTotal = 50.00m would be a compile error here
        // because the interface only exposes { get; }
    }

    [TestMethod]
    public async Task PrivateSet_SetPrivateValueOnInterface()
    {
        // Scenario 11/13: SetPrivateValue callable on IValidateProperty interface
        // WHEN SetPrivateValue is called on the IValidateProperty, THEN value is set

        // Arrange
        var entity = _factory.Create();
        var totalProperty = entity["ComputedTotal"];

        // Act
        await totalProperty.SetPrivateValue(77.77m);

        // Assert
        Assert.AreEqual(77.77m, entity.ComputedTotal);
    }
}
