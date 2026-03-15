// -----------------------------------------------------------------------------
// Design.Domain - LazyLoad Property on Entities
// -----------------------------------------------------------------------------
// Demonstrates LazyLoad<T> properties on EntityBase and ValidateBase entities.
// LazyLoad<T> properties are regular C# properties (not partial properties).
// Their loaded values participate in PropertyManager via look-through property
// subclasses (LazyLoadValidateProperty<T>, LazyLoadEntityProperty<T>).
//
// DESIGN DECISION: LazyLoad<T> is declared as a partial property, matching
// how every other Neatoo property works. The generator detects LazyLoad<T>
// type and generates:
// - Backing field accessor (IValidateProperty<LazyLoad<T>>)
// - Setter using LoadValue (no rule triggering, no task tracking)
// - Registration using factory.CreateLazyLoad<TInner> in InitializePropertyBackingFields
// The factory creates a look-through property subclass that delegates
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
// GENERATOR BEHAVIOR: The generators detect LazyLoad<T> partial properties
// via OriginalDefinition check. They generate backing fields, LoadValue-based
// setters (no task tracking), and CreateLazyLoad<TInner> registration calls
// in InitializePropertyBackingFields.
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
// REGISTRATION LIFECYCLE: LazyLoad properties are registered with
// PropertyManager during InitializePropertyBackingFields (in the constructor),
// via the generated CreateLazyLoad<TInner> call. The generated setter uses
// LoadValue to connect/disconnect inner child events when the LazyLoad
// wrapper is assigned. After deserialization, OnDeserialized calls
// ReconnectAfterDeserialization on each ILazyLoadProperty to re-establish
// inner child event subscriptions after ApplyDeserializedState.
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

    // LazyLoad<T> is a partial property -- the generator handles backing field,
    // setter (uses LoadValue), and PropertyManager registration via CreateLazyLoad<TInner>.
    public partial LazyLoad<string> LazyDescription { get; set; }

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

    // LazyLoad<T> on ValidateBase -- partial, same pattern as EntityBase
    public partial LazyLoad<string> LazyContent { get; set; }

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
// with PropertyManager during InitializePropertyBackingFields.
//
// LazyLoad properties are partial, just like every other Neatoo property:
//
//   public partial LazyLoad<IChildEntity> LazyChild { get; set; }
//
// The generated setter uses LoadValue, which handles connecting/disconnecting
// inner child events whenever the LazyLoad wrapper is reassigned.
