using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

/// <summary>
/// Round-trip tests for a custom EntityProperty&lt;T&gt; subclass supplied through IPropertyFactory&lt;T&gt;.
/// </summary>
[TestClass]
public class CustomEntityPropertySerializationTests : IntegrationTestBase
{
    private ICustomEntityPropertyEntity _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
        _target = GetRequiredService<ICustomEntityPropertyEntity>();
    }

    private ICustomEntityPropertyEntity RoundTrip()
    {
        return Deserialize<ICustomEntityPropertyEntity>(Serialize(_target));
    }

    [TestMethod]
    public void CustomProperty_BeforeSerialization_IsCreatedByCustomFactory()
    {
        var property = _target[nameof(ICustomEntityPropertyEntity.Tagged)];

        Assert.IsInstanceOfType<TaggedEntityProperty<string?>>(property);
        Assert.AreEqual("limb", ((TaggedEntityProperty<string?>)property).Tag);
    }

    [TestMethod]
    public void CustomProperty_RoundTrip_KeepsSubclassType()
    {
        _target.Tagged = "value";

        var deserialized = RoundTrip();

        Assert.IsInstanceOfType<TaggedEntityProperty<string?>>(deserialized[nameof(ICustomEntityPropertyEntity.Tagged)]);
        Assert.AreEqual("value", deserialized.Tagged);
    }

    [TestMethod]
    public void CustomProperty_RoundTrip_PreservesIsSelfModified()
    {
        _target.Tagged = "value";
        Assert.IsTrue(_target[nameof(ICustomEntityPropertyEntity.Tagged)].IsSelfModified);

        var deserialized = RoundTrip();

        var property = deserialized[nameof(ICustomEntityPropertyEntity.Tagged)];
        Assert.IsTrue(property.IsSelfModified);
        Assert.IsTrue(property.IsModified);
        Assert.IsTrue(deserialized.IsModified);
    }

    [TestMethod]
    public void CustomProperty_RoundTrip_UnmodifiedStaysUnmodified()
    {
        _target.Plain = "value";

        var deserialized = RoundTrip();

        Assert.IsFalse(deserialized[nameof(ICustomEntityPropertyEntity.Tagged)].IsSelfModified);
    }

    [TestMethod]
    public void CustomProperty_RoundTrip_RestoresAttributeStateViaApplyPropertyInfo()
    {
        _target.Tagged = "value";

        var deserialized = RoundTrip();

        var property = (TaggedEntityProperty<string?>)deserialized[nameof(ICustomEntityPropertyEntity.Tagged)];
        Assert.AreEqual("limb", property.Tag);
        Assert.AreEqual(nameof(ICustomEntityPropertyEntity.Tagged), property.DisplayName);
    }

    [TestMethod]
    public void PlainProperty_RoundTrip_StaysEntityPropertyAndPreservesIsSelfModified()
    {
        _target.Plain = "value";

        var deserialized = RoundTrip();

        var property = deserialized[nameof(ICustomEntityPropertyEntity.Plain)];
        Assert.AreEqual(typeof(EntityProperty<string?>), property.GetType());
        Assert.IsTrue(property.IsSelfModified);
        Assert.AreEqual("value", deserialized.Plain);
    }
}
