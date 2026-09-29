// -----------------------------------------------------------------------------
// Design.Domain - Custom Property Types
// -----------------------------------------------------------------------------
// Demonstrates an EntityProperty<T> subclass that carries extra per-property
// metadata (a plausible range read from an attribute) and exposes it beside
// IsValid/IsBusy/IsReadOnly, so UI binds it like any other property metadata.
//
// Three seams make this work:
//   1. Construction: ValidateBase's constructor calls the generated
//      InitializePropertyBackingFields(services.PropertyFactory). Substitute
//      the IPropertyFactory<T> and the generated properties are your type.
//   2. Serialization: the converter writes the property's open generic
//      definition as $type and on read does MakeGenericType(valueType), then
//      calls the five-argument [JsonConstructor]. Any IEntityProperty type
//      takes the entity branch, so IsSelfModified survives the trip.
//   3. Restoration: EntityPropertyManager.OnDeserialized calls
//      ApplyPropertyInfo on every property. Override it to rebuild
//      attribute-derived state; nothing extra goes on the wire.
//
// DESIGN DECISION: Attribute-derived state is re-read, not serialized. The
// attribute is on the declaring property on both tiers, so shipping it would
// only add payload and a second source of truth. DisplayName works the same way.
//
// DID NOT DO THIS: Call ApplyPropertyInfo from the EntityProperty constructor.
// It is virtual, and a virtual call from a base constructor runs the override
// before the subclass's fields are initialized. The subclass reads its
// attribute in its own IPropertyInfo constructor and again in the override.
//
// COMMON MISTAKE: Registering IPropertyFactory<T> in DI for an EntityBase.
// EntityBaseServices<T> always constructs EntityPropertyFactory<T> itself and
// never resolves IPropertyFactory<T> (only ValidateBaseServices<T> does). Wrap
// the injected services and replace PropertyFactory, as below.
//
// CONTRACT for a custom property type:
//   - Open generic with exactly one type parameter (MakeGenericType(valueType)).
//   - Declares [JsonConstructor] (string name, T value, bool isSelfModified,
//     bool isReadOnly, IRuleMessage[] serializedRuleMessages).
//   - Lives in an assembly passed to AddNeatooServices; the $type is resolved
//     by full name through IServiceAssemblies.FindType.
//   - Trimmed WASM client: root the JsonConstructor. Only
//     Activator.CreateInstance reaches it, so the linker cannot see it.
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Design.Domain.PropertySystem;

/// <summary>
/// Declares the plausible range for a measurement property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PlausibleAttribute : Attribute
{
    public PlausibleAttribute(double min, double max)
    {
        Min = min;
        Max = max;
    }

    public double Min { get; }
    public double Max { get; }
}

/// <summary>
/// Property type exposing IsPlausible and the declared range beside the standard property metadata.
/// </summary>
public class PlausibleProperty<T> : EntityProperty<T>
{
    public PlausibleProperty(IPropertyInfo propertyInfo) : base(propertyInfo)
    {
        Range = propertyInfo.GetCustomAttribute<PlausibleAttribute>();
    }

    // Required shape: the deserializer calls exactly this constructor.
    [JsonConstructor]
    public PlausibleProperty(string name, T value, bool isSelfModified, bool isReadOnly, IRuleMessage[] serializedRuleMessages)
        : base(name, value, isSelfModified, isReadOnly, serializedRuleMessages)
    {
    }

    /// <summary>Declared range; null when the property has no [Plausible].</summary>
    [JsonIgnore]
    public PlausibleAttribute? Range { get; private set; }

    [JsonIgnore]
    public bool IsPlausible =>
        Range is null
        || Value is not IConvertible c
        || (c.ToDouble(null) is var d && d >= Range.Min && d <= Range.Max);

    // Called after deserialization; the range is not on the wire.
    public override void ApplyPropertyInfo(IPropertyInfo propertyInfo)
    {
        base.ApplyPropertyInfo(propertyInfo);
        Range = propertyInfo.GetCustomAttribute<PlausibleAttribute>();
    }
}

/// <summary>
/// Creates PlausibleProperty for [Plausible] properties; defers everything else to the framework factory.
/// </summary>
internal sealed class PlausiblePropertyFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] TOwner>
    : IPropertyFactory<TOwner>
    where TOwner : IValidateBase
{
    private readonly IPropertyInfoList<TOwner> _propertyInfoList;
    private readonly IPropertyFactory<TOwner> _inner;

    public PlausiblePropertyFactory(IPropertyInfoList<TOwner> propertyInfoList, IPropertyFactory<TOwner> inner)
    {
        _propertyInfoList = propertyInfoList;
        _inner = inner;
    }

    public IValidateProperty<TProperty> Create<TProperty>(TOwner owner, string propertyName)
    {
        var propertyInfo = _propertyInfoList.GetPropertyInfo(propertyName);
        return propertyInfo?.GetCustomAttribute<PlausibleAttribute>() != null
            ? new PlausibleProperty<TProperty>(propertyInfo)
            : _inner.Create<TProperty>(owner, propertyName);
    }

    public IValidateProperty CreateEntityLazyLoad<TInner>(TOwner owner, string propertyName) where TInner : class?
        => _inner.CreateEntityLazyLoad<TInner>(owner, propertyName);
}

/// <summary>
/// Services wrapper: delegates everything to the injected services except PropertyFactory.
/// </summary>
internal sealed class PlausibleEntityServices<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] T>
    : IEntityBaseServices<T>
    where T : EntityBase<T>
{
    private readonly IEntityBaseServices<T> _inner;

    public PlausibleEntityServices(IEntityBaseServices<T> inner)
    {
        _inner = inner;
        PropertyFactory = new PlausiblePropertyFactory<T>(inner.PropertyInfoList, inner.PropertyFactory);
    }

    public IPropertyFactory<T> PropertyFactory { get; }
    public IPropertyInfoList<T> PropertyInfoList => _inner.PropertyInfoList;
    public IValidatePropertyManager<IValidateProperty> ValidatePropertyManager => _inner.ValidatePropertyManager;
    public ILogger<T> Logger => _inner.Logger;
    public IEntityPropertyManager EntityPropertyManager => _inner.EntityPropertyManager;
    public IFactorySave<T>? Factory => _inner.Factory;
    public IRuleManager<T> CreateRuleManager(T target) => _inner.CreateRuleManager(target);
}

/// <summary>
/// Demonstrates: an aggregate whose [Plausible] properties are PlausibleProperty instances.
/// </summary>
[Factory]
internal partial class MeasurementDemo : EntityBase<MeasurementDemo>, IMeasurementDemo
{
    // Wrap the injected services; generated backing fields come from PlausiblePropertyFactory.
    public MeasurementDemo(IEntityBaseServices<MeasurementDemo> services)
        : base(new PlausibleEntityServices<MeasurementDemo>(services)) { }

    [Plausible(10, 80)]
    public partial double LengthCm { get; set; }

    public partial string? Notes { get; set; }

    [Create]
    public void Create() { }
}

public interface IMeasurementDemo : IEntityRoot
{
    double LengthCm { get; set; }
    string? Notes { get; set; }
}
