// -----------------------------------------------------------------------------
// Design.Domain - Property System Basics
// -----------------------------------------------------------------------------
// This file demonstrates the Neatoo property system: partial properties,
// IValidateProperty/IEntityProperty, and how assignment behaves inside and
// outside a factory operation.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Design.Domain.PropertySystem;

// =============================================================================
// Partial Properties - The Modern Pattern
// =============================================================================
// Neatoo uses C# partial properties with source generation. You declare the
// property signature; the generator provides the implementation.
//
// DESIGN DECISION: Partial properties are the ONLY supported pattern.
// The old Getter<T>()/Setter() methods are deprecated.
//
// For this declaration:
//   public partial string? Name { get; set; }
//
// GENERATOR BEHAVIOR: Neatoo.BaseGenerator produces the same shape for
// ValidateBase and EntityBase (from DemoEntity.g.cs):
//
//   protected IValidateProperty<string?> NameProperty
//       => (IValidateProperty<string?>)PropertyManager[nameof(Name)]!;
//
//   public partial string? Name
//   {
//       get => NameProperty.Value;
//       set
//       {
//           NameProperty.Value = value;
//           if (!NameProperty.Task.IsCompleted)
//           {
//               Parent?.AddChildTask(NameProperty.Task);
//               RunningTasks.AddTask(NameProperty.Task);
//           }
//       }
//   }
//
//   protected override void InitializePropertyBackingFields(IPropertyFactory<T> factory)
//   {
//       PropertyManager.Register(factory.Create<string?>(this, nameof(Name)));
//   }
//
// On an EntityBase the factory creates an entity property (modification
// tracking); the accessor is still typed IValidateProperty<T>. The real output
// is on disk under Generated/Neatoo.BaseGenerator/.
// =============================================================================

/// <summary>
/// Demonstrates: Partial property patterns and property system basics.
/// </summary>
[Factory]
internal partial class PropertyBasicsDemo : EntityBase<PropertyBasicsDemo>, IPropertyBasicsDemo
{
    // =========================================================================
    // Pattern 1: Simple Properties
    // =========================================================================
    // Just declare the property as partial - generator does the rest.
    // The property participates in:
    // - Change tracking (IsModified)
    // - Validation rule triggering
    // - PropertyChanged notifications
    // =========================================================================

    public partial string? Name { get; set; }

    public partial int Count { get; set; }

    public partial decimal Price { get; set; }

    // =========================================================================
    // Pattern 2: Properties with Validation Attributes
    // =========================================================================
    // Standard DataAnnotations work on partial properties.
    // The generator respects attributes and passes them to IValidateProperty.
    // =========================================================================

    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public partial string? Title { get; set; }

    [Range(0, 999999, ErrorMessage = "Quantity must be between 0 and 999999")]
    public partial int Quantity { get; set; }

    // =========================================================================
    // Pattern 3: Reference Type Properties (Child Objects)
    // =========================================================================
    // When a property holds another Neatoo object, the property system:
    // - Sets Parent reference on the child
    // - Bubbles PropertyChanged/NeatooPropertyChanged events
    // - Includes child's IsValid/IsBusy in parent's aggregated state
    //
    // COMMON MISTAKE: Assigning null to clear a child relationship.
    // This is allowed but be aware it affects parent-child tracking.
    // =========================================================================

    public partial IPropertyChildDemo? Child { get; set; }

    public PropertyBasicsDemo(IEntityBaseServices<PropertyBasicsDemo> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name is required" : string.Empty,
            t => t.Name);
    }

    [Create]
    public void Create() { }
}

[Factory]
internal partial class PropertyChildDemo : ValidateBase<PropertyChildDemo>, IPropertyChildDemo
{
    public partial string? Value { get; set; }

    public PropertyChildDemo(IValidateBaseServices<PropertyChildDemo> services) : base(services) { }

    [Create]
    public void Create() { }
}

// =============================================================================
// Private Setter Properties
// =============================================================================
// DESIGN DECISION: When a partial property has `private set`, the generator:
// 1. Emits `private set` on the implementation (preserving accessibility)
// 2. Emits `get;` only on the interface (no setter exposed)
// 3. Uses SetPrivateValue() in the setter body (bypasses IsReadOnly check)
//
// This enables computed/derived properties that:
// - Are read-only to external consumers (interface exposes only getter)
// - Can be set by internal rules (AddAction lambda calls the private setter)
// - Have IsReadOnly=true (PropertyInfoWrapper detects private setter)
// - Render as ReadOnly in MudNeatoo components automatically
//
// GENERATOR BEHAVIOR for `public partial decimal ComputedTotal { get; private set; }`:
//
//   protected IValidateProperty<decimal> ComputedTotalProperty => ...;
//   public partial decimal ComputedTotal
//   {
//       get => ComputedTotalProperty.Value;
//       private set
//       {
//           ComputedTotalProperty.SetPrivateValue(value);
//           if (!ComputedTotalProperty.Task.IsCompleted)
//           {
//               Parent?.AddChildTask(ComputedTotalProperty.Task);
//               RunningTasks.AddTask(ComputedTotalProperty.Task);
//           }
//       }
//   }
//
// On the interface:
//   decimal ComputedTotal { get; }  // No setter exposed
//
// COMMON MISTAKE: Using .Value = value for private-set properties.
// .Value = value routes to SetValue() which throws PropertyReadOnlyException
// because IsReadOnly is true for private-set properties. Always use
// SetPrivateValue() which bypasses the IsReadOnly check.
// =============================================================================

/// <summary>
/// Demonstrates: Private setter properties with computed values via rules.
/// </summary>
[Factory]
internal partial class PrivateSetPropertyDemo : EntityBase<PrivateSetPropertyDemo>, IPrivateSetPropertyDemo
{
    // Writable properties - external consumers can set these
    public partial int Quantity { get; set; }
    public partial decimal UnitPrice { get; set; }

    // Private-set property - only settable from within the entity
    // The interface exposes only `get;` - consumers see this as read-only
    // MudNeatoo components automatically bind ReadOnly="true"
    public partial decimal ComputedTotal { get; private set; }

    public PrivateSetPropertyDemo(IEntityBaseServices<PrivateSetPropertyDemo> services) : base(services)
    {
        // Rule: when Quantity or UnitPrice changes, recompute Total
        // The lambda sets the private setter, which calls SetPrivateValue internally
        RuleManager.AddAction(
            t => t.ComputedTotal = t.Quantity * t.UnitPrice,
            t => t.Quantity,
            t => t.UnitPrice);
    }

    [Create]
    public void Create() { }
}

// =============================================================================
// Assignment and Pause State
// =============================================================================
// Application code sets a value one way: the property setter. What the setter
// does depends on whether the object is paused.
//
// Not paused (a ViewModel, a Razor binding, an entity method, after a factory
// operation has returned):
//   entity.Name = "New";
//   // Result: IsSelfModified=true, rules triggered, PropertyChanged raised
//
// Paused (inside every factory operation, and during deserialization):
//   Name = data.Name;
//   // Result: IsSelfModified unchanged, no rules, no PropertyChanged
//
// DESIGN DECISION: Every factory operation is paused for the length of its
// body (FactoryStart before, FactoryComplete after), so a [Create] or [Fetch]
// loads its baseline by plain assignment. LoadValue, PauseAllActions and
// MarkUnmodified are not used inside a factory operation.
//
// The property object also has LoadValue(value), which sets the value without
// tracking regardless of pause state. The framework uses it - the generated
// EntityLazyLoad setter, for example. A factory operation never needs it.
//
// COMMON MISTAKE: LoadValue inside a factory operation.
//
// WRONG:
//   [Remote, Fetch]
//   internal void Fetch(int id, [Service] IRepo repo) {
//       var data = repo.Get(id);
//       this["Name"].LoadValue(data.Name);  // Noise - the object is already paused
//   }
//
// RIGHT:
//   [Remote, Fetch]
//   internal void Fetch(int id, [Service] IRepo repo) {
//       var data = repo.Get(id);
//       Name = data.Name;  // Paused: a clean baseline load
//   }
//   // After Fetch: IsModified=false
// =============================================================================

/// <summary>
/// Demonstrates: assignment inside a factory operation (paused) and after it (tracked).
/// </summary>
[Factory]
internal partial class SetValueVsLoadValueDemo : EntityBase<SetValueVsLoadValueDemo>, ISetValueVsLoadValueDemo
{
    public partial string? Name { get; set; }
    public partial int Value { get; set; }

    public SetValueVsLoadValueDemo(IEntityBaseServices<SetValueVsLoadValueDemo> services) : base(services) { }

    [Create]
    public void Create()
    {
        // Paused by the Create operation - this is a default, not user work
        Name = "Default";
        // IsNew=true, IsSelfModified=false
    }

    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IPropertyDemoRepository repository)
    {
        // Paused by the Fetch operation - plain assignment is the load
        var data = repository.GetById(id);
        Name = data.Name;
        Value = data.Value;
        // After Fetch: IsNew=false, IsSelfModified=false
    }

    [Remote]
    [Insert]
    internal void Insert([Service] IPropertyDemoRepository repository) { }

    [Remote]
    [Update]
    internal void Update([Service] IPropertyDemoRepository repository) { }

    [Remote]
    [Delete]
    internal void Delete([Service] IPropertyDemoRepository repository) { }
}

// =============================================================================
// Indexer Access - Direct Property Manipulation
// =============================================================================
// The indexer (entity["PropertyName"]) returns the property backing field:
// - For EntityBase: returns IEntityProperty
// - For ValidateBase: returns IValidateProperty
//
// Through the indexer you can:
// - Check IsModified (EntityBase only), IsBusy, IsReadOnly
// - Access validation messages
// - MarkReadOnly (field-level authorization, see FieldLevelAuthorization.cs)
//
// DESIGN DECISION: The indexer is for a property's metadata. Values are set
// through the property accessors, including inside factory operations.
// =============================================================================

/// <summary>
/// Demonstrates: Property indexer and IEntityProperty/IValidateProperty access.
/// </summary>
[Factory]
internal partial class IndexerAccessDemo : EntityBase<IndexerAccessDemo>, IIndexerAccessDemo
{
    public partial string? Name { get; set; }
    public partial int Amount { get; set; }

    public IndexerAccessDemo(IEntityBaseServices<IndexerAccessDemo> services) : base(services) { }

    [Create]
    public void Create() { }

    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IPropertyDemoRepository repository)
    {
        var data = repository.GetById(id);

        // The object is paused by its own factory operation, so plain
        // assignment is a clean baseline load.
        Name = data.Name;
        Amount = data.Value;

        // The indexer gives the property's metadata, not a way to load it
        IEntityProperty nameProperty = this["Name"];

        // Check property state
        bool isNameModified = nameProperty.IsModified; // false: assigned while paused

        // Access validation messages for this property
        var nameMessages = nameProperty.PropertyMessages;
    }

    [Remote]
    [Insert]
    internal void Insert([Service] IPropertyDemoRepository repository) { }

    [Remote]
    [Update]
    internal void Update([Service] IPropertyDemoRepository repository) { }

    [Remote]
    [Delete]
    internal void Delete([Service] IPropertyDemoRepository repository) { }
}

// =============================================================================
// IValidateProperty vs IEntityProperty
// =============================================================================
// The property interfaces form a hierarchy:
//
// IValidateProperty (base):
// - Value: Get/set the property value
// - SetValue(value): Set with events and rules
// - LoadValue(value): Set without tracking, regardless of pause state
//   (framework use; a factory operation assigns the property instead)
// - Messages: Validation messages for this property
// - IsBusy: Async operations pending on this property
//
// IEntityProperty (extends IValidateProperty):
// - IsModified: True if value changed since last MarkUnmodified
// - MarkSelfUnmodified(): Clear modification state
//
// DESIGN DECISION: IEntityProperty extends IValidateProperty because
// entities need ALL validation capabilities PLUS modification tracking.
// ValidateBase objects don't track modification (no persistence).
// =============================================================================

// =============================================================================
// Deprecated: Getter<T>/Setter Pattern
// =============================================================================
// The old pattern used Getter<T>() and Setter() methods:
//
// DID NOT DO THIS ANYMORE: Use Getter<T>/Setter methods.
//
// DEPRECATED PATTERN:
//   public string? Name {
//       get => Getter<string?>();
//       set => Setter(value);
//   }
//
// CURRENT PATTERN:
//   public partial string? Name { get; set; }
//
// WHY DEPRECATED:
// 1. Requires [CallerMemberName] magic - error prone
// 2. Not compatible with all IDE features
// 3. Partial properties are cleaner, more C# idiomatic
// 4. Generator produces optimized code
//
// The Getter/Setter methods are marked [Obsolete] and will be removed.
// =============================================================================

// =============================================================================
// Support Interfaces
// =============================================================================

public interface IPropertyDemoRepository
{
    (string Name, int Value) GetById(int id);
}
