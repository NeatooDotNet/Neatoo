// -----------------------------------------------------------------------------
// Design.Tests - Custom Property Type Tests
// -----------------------------------------------------------------------------
// Behavioral contracts for the custom property type pattern documented in
// Design.Domain/PropertySystem/CustomPropertyType.cs.
// -----------------------------------------------------------------------------

using Design.Domain.PropertySystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory.Internal;

namespace Design.Tests.PropertyTests;

[TestClass]
public class CustomPropertyTypeTests
{
    private IServiceScope _scope = null!;
    private IMeasurementDemoFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IMeasurementDemoFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    private IMeasurementDemo RoundTrip(IMeasurementDemo entity)
    {
        var serializer = _scope.GetRequiredService<NeatooJsonSerializer>();
        return serializer.Deserialize<IMeasurementDemo>(serializer.Serialize(entity));
    }

    [TestMethod]
    public void Create_PlausibleProperty_ExposesRangeAndIsPlausible()
    {
        var entity = _factory.Create();
        entity.LengthCm = 95;

        var property = (PlausibleProperty<double>)entity[nameof(IMeasurementDemo.LengthCm)];

        Assert.AreEqual(80, property.Range!.Max);
        Assert.IsFalse(property.IsPlausible);
    }

    [TestMethod]
    public void RoundTrip_PlausibleProperty_KeepsTypeModificationAndRange()
    {
        var entity = _factory.Create();
        entity.LengthCm = 42;

        var result = RoundTrip(entity);

        var property = (PlausibleProperty<double>)result[nameof(IMeasurementDemo.LengthCm)];
        Assert.AreEqual(42, property.Value);
        Assert.IsTrue(property.IsSelfModified, "IsSelfModified crosses the wire");
        Assert.AreEqual(10, property.Range!.Min, "Range is restored by ApplyPropertyInfo");
        Assert.IsTrue(property.IsPlausible);
    }

    [TestMethod]
    public void RoundTrip_PropertyWithoutAttribute_StaysPlainEntityProperty()
    {
        var entity = _factory.Create();
        entity.Notes = "note";

        var result = RoundTrip(entity);

        Assert.IsNotInstanceOfType<PlausibleProperty<string?>>(result[nameof(IMeasurementDemo.Notes)]);
        Assert.IsTrue(result[nameof(IMeasurementDemo.Notes)].IsSelfModified);
    }
}
