// -----------------------------------------------------------------------------
// Design.Domain - LazyLoad Property on Entities
// -----------------------------------------------------------------------------
// Demonstrates EntityLazyLoad<T> properties on EntityBase and ValidateBase entities.
// Their loaded values participate in PropertyManager via look-through property
// subclasses (LazyLoadValidateProperty<T>, LazyLoadEntityProperty<T>).
//
// DESIGN DECISION: EntityLazyLoad<T> is declared as a partial property, matching
// how every other Neatoo property works. The generator detects EntityLazyLoad<T>
// type and generates:
// - Backing field accessor (IValidateProperty<EntityLazyLoad<T>>)
// - Setter using LoadValue (no rule triggering, no task tracking)
// - Registration using factory.CreateEntityLazyLoad<TInner> in InitializePropertyBackingFields
// The factory creates a look-through property subclass that delegates
// RunRules, PropertyMessages, IsValid, IsBusy, IsModified, WaitForTasks,
// and ClearAllMessages to the inner entity loaded by LazyLoad.
//
// DESIGN DECISION: Value is a passive read. It returns the current state
// (null if not loaded, the loaded value otherwise) with no side effects.
// Call LoadAsync() explicitly to trigger loading. PropertyChanged fires
// when the load completes. Two patterns: LoadAsync() for imperative code
// (tests, domain logic, OnInitializedAsync), .Value for binding.
//
// DESIGN DECISION: ValidateBase.WaitForTasks() awaits in-progress LazyLoad
// children via PropertyManager.WaitForTasks(). This ensures that
// "await entity.WaitForTasks()" before Save waits for any explicitly
// triggered loads to complete. WaitForTasks does NOT trigger loads on
// unaccessed LazyLoad children.
//
// DESIGN DECISION: The generic constraint is `where T : class?` (not `where T : class`)
// to support nullable reference types. This allows declarations like
// `EntityLazyLoad<IOrderItemList?>` when the entity interface property is nullable.
// The same `class?` constraint applies to IEntityLazyLoadFactory and EntityLazyLoadFactory.
//
// GENERATOR BEHAVIOR: The generators detect EntityLazyLoad<T> partial properties
// via OriginalDefinition check. They generate backing fields, LoadValue-based
// setters (no task tracking), and CreateEntityLazyLoad<TInner> registration calls
// in InitializePropertyBackingFields.
//
// SERIALIZATION: EntityLazyLoad<T> has [JsonInclude] on Value/IsLoaded and
// [JsonConstructor] for deserialization. The NeatooBaseJsonTypeConverter
// detects EntityLazyLoad<> properties via reflection and serializes them
// separately from PropertyManager entries. LazyLoad property subclasses
// (ILazyLoadProperty) are skipped in the PropertyManager serialization
// array to avoid double-serialization.
// When EntityLazyLoad<T>.Value contains a Neatoo entity (IValidateBase),
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
// via the generated CreateEntityLazyLoad<TInner> call. The generated setter uses
// LoadValue to connect/disconnect inner child events when the EntityLazyLoad
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

public interface ILazyLoadEntityDemo : IEntityRoot
{
    string? Name { get; set; }
    EntityLazyLoad<string> LazyDescription { get; }
}

/// <summary>
/// Demonstrates: LazyLoad property on an EntityBase entity.
/// The LazyLoad property holds a string value for simplicity.
/// </summary>
[Factory]
internal partial class LazyLoadEntityDemo : EntityBase<LazyLoadEntityDemo>, ILazyLoadEntityDemo
{
    public partial string? Name { get; set; }

    // EntityLazyLoad<T> is a partial property -- the generator handles backing field,
    // setter (uses LoadValue), and PropertyManager registration via CreateEntityLazyLoad<TInner>.
    public partial EntityLazyLoad<string> LazyDescription { get; set; }

    public LazyLoadEntityDemo(IEntityBaseServices<LazyLoadEntityDemo> services) : base(services)
    {
    }

    [Create]
    public void Create([Service] IEntityLazyLoadFactory lazyLoadFactory)
    {
        LazyDescription = lazyLoadFactory.Create<string>("Default description");
    }

    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IEntityLazyLoadFactory lazyLoadFactory)
    {
        Name = $"Entity-{id}";
        LazyDescription = lazyLoadFactory.Create<string>($"Description for {id}");
    }
}

// =============================================================================
// LazyLoad on ValidateBase
// =============================================================================

public interface ILazyLoadValidateDemo : IValidateBase
{
    string? Label { get; set; }
    EntityLazyLoad<string> LazyContent { get; }
}

/// <summary>
/// Demonstrates: LazyLoad property on a ValidateBase entity.
/// Verifies that LazyLoad serialization works for both base class hierarchies.
/// </summary>
[Factory]
internal partial class LazyLoadValidateDemo : ValidateBase<LazyLoadValidateDemo>, ILazyLoadValidateDemo
{
    public partial string? Label { get; set; }

    // EntityLazyLoad<T> on ValidateBase -- partial, same pattern as EntityBase
    public partial EntityLazyLoad<string> LazyContent { get; set; }

    public LazyLoadValidateDemo(IValidateBaseServices<LazyLoadValidateDemo> services) : base(services)
    {
    }

    [Create]
    public void Create([Service] IEntityLazyLoadFactory lazyLoadFactory)
    {
        LazyContent = lazyLoadFactory.Create<string>("Default content");
    }
}

// =============================================================================
// LazyLoad with entity child -- the constructor pattern
// =============================================================================
//
// DESIGN DECISION: When an EntityLazyLoad<T> wraps a child entity (not a string),
// the parent's IsModified, IsValid, IsBusy, and IsSavable include the child's
// state. This is automatic via the look-through property subclass registered
// with PropertyManager during InitializePropertyBackingFields.
//
// DESIGN DECISION: The EntityLazyLoad is created in the CONSTRUCTOR, with a
// loader lambda. The constructor runs on every tier that builds the object -
// the client included, when the object is deserialized - so the loader is
// always present. The loader delegate is not serialized; the JSON converter
// merges the deserialized Value/IsLoaded into the constructor-created instance.
//
// The lambda captures the child factory from DI and reads this.Id when it
// RUNS, not when it is created, so it works even though [Fetch] sets Id later.
//
// COMMON MISTAKE: Creating the EntityLazyLoad inside [Fetch]. The loader is
// not serialized, so the client's copy has none, and LoadAsync() throws.
//
// The child's [Fetch] is the one child operation the client calls directly
// (the loader runs on the client), so it carries [Remote]. It is still
// internal: only the loader, not application code, calls it.
// =============================================================================

public interface ILazyLoadChildDemo : IEntityBase
{
    Guid ParentId { get; }
    string? Notes { get; set; }
}

public interface ILazyLoadParentDemo : IEntityRoot
{
    Guid Id { get; }
    string? Name { get; set; }
    EntityLazyLoad<ILazyLoadChildDemo> Details { get; }
}

/// <summary>
/// Child loaded on demand through the parent's EntityLazyLoad.
/// </summary>
[Factory]
internal partial class LazyLoadChildDemo : EntityBase<LazyLoadChildDemo>, ILazyLoadChildDemo
{
    public partial Guid ParentId { get; set; }
    public partial string? Notes { get; set; }

    public LazyLoadChildDemo(IEntityBaseServices<LazyLoadChildDemo> services) : base(services) { }

    [Create]
    public void Create(Guid parentId)
    {
        ParentId = parentId;
    }

    // The loader on the client calls this, so it is a client entry point
    [Remote]
    [Fetch]
    internal void Fetch(Guid parentId, [Service] ILazyLoadChildRepository repository)
    {
        ParentId = parentId;
        Notes = repository.GetNotes(parentId);
    }
}

#region skill-lazy-load-constructor
/// <summary>
/// Demonstrates: EntityLazyLoad created in the constructor with a loader that
/// reads this.Id at load time.
/// </summary>
[Factory]
internal partial class LazyLoadParentDemo : EntityBase<LazyLoadParentDemo>, ILazyLoadParentDemo
{
    public partial Guid Id { get; set; }
    public partial string? Name { get; set; }

    // Partial, like every other Neatoo property. The generator creates a
    // look-through backing field, so the child's IsValid/IsModified/IsBusy
    // flow into this entity's once the child is loaded.
    public partial EntityLazyLoad<ILazyLoadChildDemo> Details { get; set; }

    public LazyLoadParentDemo(
        IEntityBaseServices<LazyLoadParentDemo> services,
        ILazyLoadChildDemoFactory childFactory,
        IEntityLazyLoadFactory lazyLoadFactory) : base(services)
    {
        // Created here, on every tier. this.Id is read when the loader runs.
        Details = lazyLoadFactory.Create<ILazyLoadChildDemo>(
            async () => await childFactory.Fetch(this.Id));
    }

    #region skill-lazy-load-set-value
    [Create]
    public void Create([Service] ILazyLoadChildDemoFactory childFactory)
    {
        Id = Guid.NewGuid();

        // A new parent's details exist from the start, so there is nothing to
        // load: SetValue bypasses the loader and marks the EntityLazyLoad loaded.
        Details.SetValue(childFactory.Create(Id));
    }
    #endregion

    [Remote]
    [Fetch]
    internal void Fetch(Guid id, [Service] ILazyLoadParentRepository repository)
    {
        Id = id;
        Name = repository.GetName(id);
        // Details keeps the loader from the constructor; nothing to do here
    }
}
#endregion

public interface ILazyLoadParentRepository
{
    string GetName(Guid id);
}

public interface ILazyLoadChildRepository
{
    string GetNotes(Guid parentId);
}
