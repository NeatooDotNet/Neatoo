// -----------------------------------------------------------------------------
// Design.Domain - LazyLoad Property on Entities
// -----------------------------------------------------------------------------
// Demonstrates LazyLoad<T> properties on EntityBase and ValidateBase entities.
// LazyLoad<T> properties are regular C# properties (not partial properties),
// meaning they are NOT managed by PropertyManager.
//
// DESIGN DECISION: LazyLoad<T> is declared as a regular property because:
// - It wraps a child entity/value, not a scalar property value
// - It has its own lifecycle (IsLoaded, IsLoading, LoadAsync)
// - It implements IValidateMetaProperties and IEntityMetaProperties
//   for delegation to the loaded value
//
// GENERATOR BEHAVIOR: The generators do NOT process LazyLoad<T> properties
// because they are not partial properties. No backing field is generated.
//
// SERIALIZATION: LazyLoad<T> has [JsonInclude] on Value/IsLoaded and
// [JsonConstructor] for deserialization. The NeatooBaseJsonTypeConverter
// detects LazyLoad<> properties via reflection and serializes them
// separately from PropertyManager entries.
// When LazyLoad<T>.Value contains a Neatoo entity (IValidateBase),
// the NeatooBaseJsonConverterFactory claims the inner type, ensuring
// proper $id/$ref and PropertyManager serialization for the value.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.PropertySystem;

// =============================================================================
// LazyLoad on EntityBase
// =============================================================================

/// <summary>
/// Demonstrates: LazyLoad property on an EntityBase entity.
/// The LazyLoad property holds a string value for simplicity.
/// </summary>
[Factory]
public partial class LazyLoadEntityDemo : EntityBase<LazyLoadEntityDemo>
{
    public partial string? Name { get; set; }

    // LazyLoad<T> is a regular property -- not partial, not in PropertyManager
    public LazyLoad<string> LazyDescription { get; set; } = null!;

    public LazyLoadEntityDemo(IEntityBaseServices<LazyLoadEntityDemo> services) : base(services)
    {
    }

    [Create]
    public void Create([Service] ILazyLoadFactory lazyLoadFactory)
    {
        LazyDescription = lazyLoadFactory.Create<string>("Default description");
    }

    [Fetch]
    public void Fetch(int id, [Service] ILazyLoadFactory lazyLoadFactory)
    {
        using (PauseAllActions())
        {
            this["Name"].LoadValue($"Entity-{id}");
        }
        LazyDescription = lazyLoadFactory.Create<string>($"Description for {id}");
    }
}

// =============================================================================
// LazyLoad on ValidateBase
// =============================================================================

/// <summary>
/// Demonstrates: LazyLoad property on a ValidateBase entity.
/// Verifies that LazyLoad serialization works for both base class hierarchies.
/// </summary>
[Factory]
public partial class LazyLoadValidateDemo : ValidateBase<LazyLoadValidateDemo>
{
    public partial string? Label { get; set; }

    // LazyLoad<T> on ValidateBase -- same pattern as EntityBase
    public LazyLoad<string> LazyContent { get; set; } = null!;

    public LazyLoadValidateDemo(IValidateBaseServices<LazyLoadValidateDemo> services) : base(services)
    {
    }

    [Create]
    public void Create([Service] ILazyLoadFactory lazyLoadFactory)
    {
        LazyContent = lazyLoadFactory.Create<string>("Default content");
    }
}
