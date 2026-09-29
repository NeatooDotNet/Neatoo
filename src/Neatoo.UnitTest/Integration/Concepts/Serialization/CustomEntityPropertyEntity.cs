using Microsoft.Extensions.Logging;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

/// <summary>
/// Test attribute carrying state that a custom property type derives from its IPropertyInfo.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PropertyTagAttribute : Attribute
{
    public PropertyTagAttribute(string tag) { Tag = tag; }

    public string Tag { get; }
}

/// <summary>
/// Custom EntityProperty subclass. Its Tag comes from [PropertyTag] and is not serialized;
/// it is restored after deserialization through the ApplyPropertyInfo override.
/// </summary>
public class TaggedEntityProperty<T> : EntityProperty<T>
{
    public TaggedEntityProperty(IPropertyInfo propertyInfo) : base(propertyInfo)
    {
        Tag = propertyInfo.GetCustomAttribute<PropertyTagAttribute>()?.Tag;
    }

    [JsonConstructor]
    public TaggedEntityProperty(string name, T value, bool isSelfModified, bool isReadOnly, IRuleMessage[] serializedRuleMessages)
        : base(name, value, isSelfModified, isReadOnly, serializedRuleMessages)
    {
    }

    [JsonIgnore]
    public string? Tag { get; private set; }

    public override void ApplyPropertyInfo(IPropertyInfo propertyInfo)
    {
        base.ApplyPropertyInfo(propertyInfo);
        Tag = propertyInfo.GetCustomAttribute<PropertyTagAttribute>()?.Tag;
    }
}

/// <summary>
/// Creates TaggedEntityProperty for properties carrying [PropertyTag]; everything else
/// goes to the framework's factory, so untagged properties stay plain EntityProperty.
/// </summary>
internal sealed class TaggedPropertyFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] TOwner>
    : IPropertyFactory<TOwner>
    where TOwner : IValidateBase
{
    private readonly IPropertyInfoList<TOwner> _propertyInfoList;
    private readonly IPropertyFactory<TOwner> _inner;

    public TaggedPropertyFactory(IPropertyInfoList<TOwner> propertyInfoList, IPropertyFactory<TOwner> inner)
    {
        _propertyInfoList = propertyInfoList;
        _inner = inner;
    }

    public IValidateProperty<TProperty> Create<TProperty>(TOwner owner, string propertyName)
    {
        var propertyInfo = _propertyInfoList.GetPropertyInfo(propertyName);
        if (propertyInfo?.GetCustomAttribute<PropertyTagAttribute>() != null)
        {
            return new TaggedEntityProperty<TProperty>(propertyInfo);
        }
        return _inner.Create<TProperty>(owner, propertyName);
    }

    public IValidateProperty CreateEntityLazyLoad<TInner>(TOwner owner, string propertyName) where TInner : class?
        => _inner.CreateEntityLazyLoad<TInner>(owner, propertyName);
}

/// <summary>
/// Services wrapper that substitutes TaggedPropertyFactory. EntityBaseServices constructs its
/// own EntityPropertyFactory, so the entity wraps the injected services instead.
/// </summary>
internal sealed class TaggedEntityBaseServices<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] T>
    : IEntityBaseServices<T>
    where T : EntityBase<T>
{
    private readonly IEntityBaseServices<T> _inner;

    public TaggedEntityBaseServices(IEntityBaseServices<T> inner)
    {
        _inner = inner;
        PropertyFactory = new TaggedPropertyFactory<T>(inner.PropertyInfoList, inner.PropertyFactory);
    }

    public IPropertyFactory<T> PropertyFactory { get; }
    public IPropertyInfoList<T> PropertyInfoList => _inner.PropertyInfoList;
    public IValidatePropertyManager<IValidateProperty> ValidatePropertyManager => _inner.ValidatePropertyManager;
    public ILogger<T> Logger => _inner.Logger;
    public IEntityPropertyManager EntityPropertyManager => _inner.EntityPropertyManager;
    public IFactorySave<T>? Factory => _inner.Factory;
    public IRuleManager<T> CreateRuleManager(T target) => _inner.CreateRuleManager(target);
}

public interface ICustomEntityPropertyEntity : IEntityBase
{
    string? Tagged { get; set; }

    string? Plain { get; set; }
}

[Factory]
internal partial class CustomEntityPropertyEntity : EntityBase<CustomEntityPropertyEntity>, ICustomEntityPropertyEntity
{
    public CustomEntityPropertyEntity(IEntityBaseServices<CustomEntityPropertyEntity> services)
        : base(new TaggedEntityBaseServices<CustomEntityPropertyEntity>(services))
    {
    }

    [PropertyTag("limb")]
    public partial string? Tagged { get; set; }

    public partial string? Plain { get; set; }
}
