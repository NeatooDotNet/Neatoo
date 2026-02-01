// -----------------------------------------------------------------------------
// Design.Domain - Property System Basics
// -----------------------------------------------------------------------------
// This file demonstrates the Neatoo property system: partial properties,
// Getter<T>/Setter, IValidateProperty/IEntityProperty, and LoadValue vs SetValue.
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
// GENERATOR BEHAVIOR: Neatoo.BaseGenerator produces:
//
// For ValidateBase:
//   private IValidateProperty<string?> _nameProperty = null!;
//   public partial string? Name
//   {
//       get => _nameProperty.Value;
//       set => _nameProperty.SetValue(value);
//   }
//
// For EntityBase (adds modification tracking):
//   private IEntityProperty<string?> _nameProperty = null!;
//   public partial string? Name
//   {
//       get => _nameProperty.Value;
//       set => _nameProperty.SetValue(value);
//   }
//
// The InitializePropertyBackingFields() method creates the property instances:
//   protected override void InitializePropertyBackingFields(IPropertyFactory<T> factory)
//   {
//       base.InitializePropertyBackingFields(factory);
//       _nameProperty = factory.CreateProperty<string?>("Name", this);
//       PropertyManager.Add(_nameProperty);
//   }
// =============================================================================

/// <summary>
/// Demonstrates: Partial property patterns and property system basics.
/// </summary>
[Factory]
public partial class PropertyBasicsDemo : EntityBase<PropertyBasicsDemo>
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

    public partial PropertyChildDemo? Child { get; set; }

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
public partial class PropertyChildDemo : ValidateBase<PropertyChildDemo>
{
    public partial string? Value { get; set; }

    public PropertyChildDemo(IValidateBaseServices<PropertyChildDemo> services) : base(services) { }

    [Create]
    public void Create() { }
}

// =============================================================================
// SetValue vs LoadValue - Critical Distinction
// =============================================================================
// The property system has TWO ways to set a value:
//
// SetValue (via property setter): Marks property as modified, triggers rules
//   entity.Name = "New";  // Uses SetValue internally
//   // Result: IsSelfModified=true, rules triggered
//
// LoadValue (via indexer): Sets value WITHOUT modification tracking
//   entity["Name"].LoadValue("New");  // No modification tracking
//   // Result: IsSelfModified unchanged, no rules triggered
//
// DESIGN DECISION: LoadValue exists for persistence loading.
// When fetching from database, we don't want IsModified=true.
// The entity should reflect database state (IsModified=false).
//
// COMMON MISTAKE: Using property setter in Fetch.
//
// WRONG:
//   [Fetch]
//   public void Fetch(int id, [Service] IRepo repo) {
//       var data = repo.Get(id);
//       Name = data.Name;  // SetValue! IsModified becomes true!
//   }
//   // After Fetch: IsModified=true (unexpected!)
//
// RIGHT:
//   [Fetch]
//   public void Fetch(int id, [Service] IRepo repo) {
//       var data = repo.Get(id);
//       this["Name"].LoadValue(data.Name);  // LoadValue! No modification.
//   }
//   // After Fetch: IsModified=false (correct!)
// =============================================================================

/// <summary>
/// Demonstrates: SetValue vs LoadValue distinction.
/// </summary>
[Factory]
public partial class SetValueVsLoadValueDemo : EntityBase<SetValueVsLoadValueDemo>
{
    public partial string? Name { get; set; }
    public partial int Value { get; set; }

    public SetValueVsLoadValueDemo(IEntityBaseServices<SetValueVsLoadValueDemo> services) : base(services) { }

    [Create]
    public void Create()
    {
        // Using property setter - this marks as modified
        Name = "Default";
        // IsNew=true, IsSelfModified=true (from setter)
    }

    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IPropertyDemoRepository repository)
    {
        // Using LoadValue - does NOT mark as modified
        var data = repository.GetById(id);
        this["Name"].LoadValue(data.Name);
        this["Value"].LoadValue(data.Value);
        // After Fetch: IsNew=false, IsSelfModified=false
    }

    [Remote]
    [Insert]
    public void Insert([Service] IPropertyDemoRepository repository) { }

    [Remote]
    [Update]
    public void Update([Service] IPropertyDemoRepository repository) { }

    [Remote]
    [Delete]
    public void Delete([Service] IPropertyDemoRepository repository) { }
}

// =============================================================================
// Indexer Access - Direct Property Manipulation
// =============================================================================
// The indexer (entity["PropertyName"]) returns the property backing field:
// - For EntityBase: returns IEntityProperty
// - For ValidateBase: returns IValidateProperty
//
// Through the indexer you can:
// - LoadValue: Set without modification tracking
// - SetValue: Set with modification tracking (same as property setter)
// - Access Value directly
// - Check IsModified (EntityBase only)
// - Access validation messages
//
// DESIGN DECISION: Indexer provides escape hatch for advanced scenarios.
// Normal code uses property accessors; indexer is for framework/persistence code.
// =============================================================================

/// <summary>
/// Demonstrates: Property indexer and IEntityProperty/IValidateProperty access.
/// </summary>
[Factory]
public partial class IndexerAccessDemo : EntityBase<IndexerAccessDemo>
{
    public partial string? Name { get; set; }
    public partial int Amount { get; set; }

    public IndexerAccessDemo(IEntityBaseServices<IndexerAccessDemo> services) : base(services) { }

    [Create]
    public void Create() { }

    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IPropertyDemoRepository repository)
    {
        var data = repository.GetById(id);

        // Access properties through indexer
        IEntityProperty nameProperty = this["Name"];
        IEntityProperty amountProperty = this["Amount"];

        // LoadValue for fetch (no modification tracking)
        nameProperty.LoadValue(data.Name);
        amountProperty.LoadValue(data.Value);

        // Check property state
        bool isNameModified = nameProperty.IsModified; // false after LoadValue

        // Access validation messages for this property
        var nameMessages = nameProperty.PropertyMessages;
    }

    [Remote]
    [Insert]
    public void Insert([Service] IPropertyDemoRepository repository) { }

    [Remote]
    [Update]
    public void Update([Service] IPropertyDemoRepository repository) { }

    [Remote]
    [Delete]
    public void Delete([Service] IPropertyDemoRepository repository) { }
}

// =============================================================================
// IValidateProperty vs IEntityProperty
// =============================================================================
// The property interfaces form a hierarchy:
//
// IValidateProperty (base):
// - Value: Get/set the property value
// - SetValue(value): Set with events and rules
// - Messages: Validation messages for this property
// - IsBusy: Async operations pending on this property
//
// IEntityProperty (extends IValidateProperty):
// - LoadValue(value): Set WITHOUT modification tracking
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
