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
// DESIGN DECISION: Accessing Value auto-triggers a fire-and-forget load
// when the value hasn't been loaded, no load is in progress, and a loader
// delegate is present. The getter returns null synchronously; PropertyChanged
// fires when the load completes. IsLoading and IsLoaded access does NOT
// trigger a load. This eliminates the manual "await" boilerplate that was
// consistently needed in Blazor Razor databinding.
//
// DESIGN DECISION: ValidateBase.WaitForTasks() awaits in-progress LazyLoad
// children. This ensures that "await entity.WaitForTasks()" before Save
// waits for any auto-triggered loads to complete. WaitForTasks does NOT
// trigger loads on unaccessed LazyLoad children.
//
// DESIGN DECISION: The generic constraint is `where T : class?` (not `where T : class`)
// to support nullable reference types. This allows declarations like
// `LazyLoad<IOrderItemList?>` when the entity interface property is nullable.
// The same `class?` constraint applies to ILazyLoadFactory and LazyLoadFactory.
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
//
// STATE PROPAGATION: LazyLoad<T> forwards PropertyChanged events from
// its wrapped value. The parent entity includes LazyLoad children in
// its IsModified, IsValid, and IsBusy calculations via cached reflection.
// This ensures that modifying a child entity inside LazyLoad<T> causes
// the parent's IsSavable to return true, matching the behavior of
// regular partial property children managed by PropertyManager.
//
// SUBSCRIPTION LIFECYCLE: The parent subscribes to LazyLoad instances'
// PropertyChanged events at FactoryComplete() and OnDeserialized().
// If a LazyLoad property is assigned after these points (e.g., in test
// setup or runtime code), use a custom property setter that calls
// SubscribeToLazyLoadProperties() to ensure reactive event propagation.
// Even without subscriptions, the polling overrides (IsModified, IsValid,
// IsBusy) return correct values when queried directly.
//
// COMMON MISTAKE: Assigning a LazyLoad<T> property after FactoryComplete()
// without calling SubscribeToLazyLoadProperties() in the setter. The
// polling override still returns correct values, but UI bindings won't
// update reactively because the parent isn't subscribed to the new
// LazyLoad instance's PropertyChanged events.
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

// =============================================================================
// LazyLoad with entity child -- state propagation pattern
// =============================================================================
//
// DESIGN DECISION: When a LazyLoad<T> wraps a child entity (not a string),
// the parent's IsModified, IsValid, IsBusy, and IsSavable include the child's
// state. This is automatic via cached reflection in EntityBase/ValidateBase.
//
// If the LazyLoad property is assigned after FactoryComplete() (e.g., in
// test setup or application code), use a custom setter:
//
//   private LazyLoad<IChildEntity> _lazyChild = null!;
//   public LazyLoad<IChildEntity> LazyChild
//   {
//       get => _lazyChild;
//       set
//       {
//           _lazyChild = value;
//           SubscribeToLazyLoadProperties();
//       }
//   }
//
// This ensures the parent subscribes to the LazyLoad instance's PropertyChanged
// events for reactive UI updates. Without the custom setter, polling (IsModified
// etc.) still returns correct values, but PropertyChanged events won't fire
// on the parent when the LazyLoad child's state changes.
