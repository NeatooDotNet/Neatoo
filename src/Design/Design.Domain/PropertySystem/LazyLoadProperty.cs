// -----------------------------------------------------------------------------
// Design.Domain - LazyLoad Property on Entities
// -----------------------------------------------------------------------------
// Demonstrates LazyLoad<T> properties on EntityBase and ValidateBase entities.
// LazyLoad<T> properties are regular C# properties (not partial properties).
// Their loaded values participate in PropertyManager via look-through property
// subclasses (LazyLoadValidateProperty<T>, LazyLoadEntityProperty<T>).
//
// DESIGN DECISION: LazyLoad<T> is declared as a regular property because:
// - It wraps a child entity/value, not a scalar property value
// - It has its own lifecycle (IsLoaded, IsLoading, LoadAsync)
// - It implements IValidateMetaProperties and IEntityMetaProperties
//   for delegation to the loaded value
// The LazyLoad<T> C# property itself is NOT a partial property and NOT
// processed by the source generator. However, at runtime, a look-through
// property subclass is registered with PropertyManager that delegates
// RunRules, PropertyMessages, IsValid, IsBusy, IsModified, WaitForTasks,
// and ClearAllMessages to the inner entity loaded by LazyLoad.
//
// DESIGN DECISION: Accessing Value auto-triggers a fire-and-forget load
// when the value hasn't been loaded, no load is in progress, and a loader
// delegate is present. The getter returns null synchronously; PropertyChanged
// fires when the load completes. IsLoading and IsLoaded access does NOT
// trigger a load. This eliminates the manual "await" boilerplate that was
// consistently needed in Blazor Razor databinding.
//
// DESIGN DECISION: ValidateBase.WaitForTasks() awaits in-progress LazyLoad
// children via PropertyManager.WaitForTasks(). This ensures that
// "await entity.WaitForTasks()" before Save waits for any auto-triggered
// loads to complete. WaitForTasks does NOT trigger loads on unaccessed
// LazyLoad children (uses BoxedValue, not Value getter).
//
// DESIGN DECISION: The generic constraint is `where T : class?` (not `where T : class`)
// to support nullable reference types. This allows declarations like
// `LazyLoad<IOrderItemList?>` when the entity interface property is nullable.
// The same `class?` constraint applies to ILazyLoadFactory and LazyLoadFactory.
//
// GENERATOR BEHAVIOR: The generators do NOT process LazyLoad<T> properties
// because they are not partial properties. No backing field is generated.
// Registration with PropertyManager happens at runtime in
// RegisterLazyLoadProperties() (called from FactoryComplete/OnDeserialized).
//
// SERIALIZATION: LazyLoad<T> has [JsonInclude] on Value/IsLoaded and
// [JsonConstructor] for deserialization. The NeatooBaseJsonTypeConverter
// detects LazyLoad<> properties via reflection and serializes them
// separately from PropertyManager entries. LazyLoad property subclasses
// (ILazyLoadProperty) are skipped in the PropertyManager serialization
// array to avoid double-serialization.
// When LazyLoad<T>.Value contains a Neatoo entity (IValidateBase),
// the NeatooBaseJsonConverterFactory claims the inner type, ensuring
// proper $id/$ref and PropertyManager serialization for the value.
//
// STATE PROPAGATION: LazyLoad property subclasses look through the
// LazyLoad wrapper to the inner entity for all state delegation:
// - IsValid, RunRules, PropertyMessages cascade to inner entity
// - IsBusy includes LazyLoad.IsLoading and inner entity busy state
// - IsModified (EntityProperty) delegates to inner entity
// - WaitForTasks delegates to LazyLoad.WaitForTasks (handles both
//   load tasks and inner child tasks)
// - ClearAllMessages cascades through ValueIsValidateBase
// This is unified through PropertyManager -- no parallel helper methods.
//
// REGISTRATION LIFECYCLE: RegisterLazyLoadProperties() is called at
// FactoryComplete() and OnDeserialized(). It uses cached-per-type
// reflection to discover LazyLoad properties and creates look-through
// property subclass instances registered with PropertyManager.
// If a LazyLoad property is assigned after these points (e.g., in
// a custom property setter), call RegisterLazyLoadProperties() to
// register or re-register the LazyLoad instance with PropertyManager.
//
// COMMON MISTAKE: Assigning a LazyLoad<T> property after FactoryComplete()
// without calling RegisterLazyLoadProperties() in the setter. The
// LazyLoad property will not be tracked by PropertyManager, so RunRules
// cascading, PropertyMessages aggregation, and reactive state propagation
// will not work for that property.
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
// state. This is automatic via the look-through property subclass registered
// with PropertyManager at FactoryComplete/OnDeserialized.
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
//           RegisterLazyLoadProperties();
//       }
//   }
//
// This registers (or re-registers) the LazyLoad instance with PropertyManager,
// enabling RunRules cascading, PropertyMessages aggregation, and reactive state
// propagation for the inner entity.
