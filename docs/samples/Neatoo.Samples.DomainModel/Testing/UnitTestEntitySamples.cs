/// <summary>
/// Code samples for docs/testing.md - Unit testing entities directly.
///
/// Snippets in this file:
/// - docs:testing:entity-unit-test-caution
/// - docs:testing:entity-unit-test-class
/// - docs:testing:entity-unit-test-usage
///
/// Corresponding tests: UnitTestEntitySamplesTests.cs
/// </summary>

using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;

namespace Neatoo.Samples.DomainModel.Testing;

#region entity-unit-test-class
/// <summary>
/// Entity class designed for direct unit testing.
/// Uses [SuppressFactory] to prevent factory generation.
/// </summary>
[SuppressFactory]
public class TestableProduct : EntityBase<TestableProduct>
{
    /// <summary>
    /// Parameterless constructor using EntityBaseServices for unit testing.
    /// WARNING: This bypasses DI and disables Save() functionality.
    /// </summary>
    public TestableProduct() : base(new EntityBaseServices<TestableProduct>())
    {
    }

    public string? Name { get => Getter<string>(); set => Setter(value); }
    public decimal Price { get => Getter<decimal>(); set => Setter(value); }
    public int Quantity { get => Getter<int>(); set => Setter(value); }

    /// <summary>
    /// Calculated property - tests business logic without needing factories.
    /// </summary>
    public decimal TotalValue => Price * Quantity;

    /// <summary>
    /// Expose MarkNew for testing state transitions.
    /// </summary>
    public void SetAsNew() => MarkNew();

    /// <summary>
    /// Expose MarkOld for testing existing entity scenarios.
    /// </summary>
    public void SetAsExisting() => MarkOld();

    /// <summary>
    /// Expose MarkAsChild for testing child entity behavior.
    /// </summary>
    public void SetAsChild() => MarkAsChild();
}
#endregion

#region entity-unit-test-usage
/// <summary>
/// Example showing how to create and test an entity directly.
/// </summary>
public static class EntityUnitTestExample
{
    /// <summary>
    /// Tests entity state tracking without DI or factories.
    /// </summary>
    public static void TestEntityStateTracking()
    {
        // Create entity directly - no factory needed
        var product = new TestableProduct();

        // Test property changes trigger IsModified
        product.Name = "Widget";
        var isModified = product.IsModified; // true

        // Test state transitions
        product.SetAsNew();
        var isNew = product.IsNew; // true

        // Test calculated properties
        product.Price = 10.00m;
        product.Quantity = 5;
        var total = product.TotalValue; // 50.00
    }
}
#endregion
