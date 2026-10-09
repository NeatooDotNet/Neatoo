# Properties

Neatoo uses C# partial properties with source generation. Declare the property signature; the generator provides change tracking, validation triggering, and property change notifications. (The old `Getter<T>()`/`Setter()` methods are `[Obsolete]`.)

## Basic Property Declaration

Declare properties as `partial` -- the source generator fills in the implementation:

<!-- snippet: skill-partial-property-class -->
<a id='snippet-skill-partial-property-class'></a>
```cs
[Factory]
internal partial class ValidationChildDemo : ValidateBase<ValidationChildDemo>, IValidationChildDemo
{
    public partial string? RequiredField { get; set; }

    public ValidationChildDemo(IValidateBaseServices<ValidationChildDemo> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.RequiredField) ? "Child field is required" : string.Empty,
            t => t.RequiredField);
    }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/StateProperties.cs#L96-L112' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-partial-property-class' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Generated Implementation

For each partial property the generator emits a protected accessor over the property object (`NameProperty`), a getter and setter over `NameProperty.Value`, and the registration in `InitializePropertyBackingFields`:

<!-- snippet: skill-generated-property-shape -->
<a id='snippet-skill-generated-property-shape'></a>
```cs
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
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/PropertyBasics.cs#L24-L56' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-generated-property-shape' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Object-Per-Property Architecture

Each partial property declared on a Neatoo class is backed by its own `IValidateProperty<T>` object. This is not just a backing field — it is a full object that owns:

| Member | Interface | Purpose |
|--------|-----------|---------|
| `Value` | `IValidateProperty` | The current property value |
| `IsValid` | `IValidateProperty` | Whether this property passes its validation rules |
| `PropertyMessages` | `IValidateProperty` | Validation error messages for this property |
| `IsBusy` | `IValidateProperty` | Whether an async rule is currently running for this property |
| `IsReadOnly` | `IValidateProperty` | Whether this property is read-only (true for `private set` properties, or after `MarkReadOnly()` is called at runtime) |
| `IsModified` | `IEntityProperty` only | Whether this property has been changed (EntityBase properties only, not ValidateBase) |

Each property object fires its own `PropertyChanged` event independently. This enables fine-grained UI updates — a validation error on `Email` triggers a re-render only for the Email field's error display, not the entire form.

Access the property object via the indexer; cast to `IValidateProperty<T>` for the typed `Value`:

<!-- snippet: skill-property-metadata -->
<a id='snippet-skill-property-metadata'></a>
```cs
[TestMethod]
public void Indexer_ExposesPropertyMetadata()
{
    var entity = _factory.Create();

    // Each partial property is backed by its own property object
    var nameProperty = entity["Name"];

    entity.Name = "";  // Name is required
    Assert.IsFalse(nameProperty.IsValid);
    Assert.IsTrue(nameProperty.PropertyMessages.Count > 0);
    Assert.IsFalse(nameProperty.IsBusy);
    Assert.IsFalse(nameProperty.IsReadOnly);

    // The object aggregates every property's messages
    Assert.IsTrue(entity.PropertyMessages.Any(m => m.Property.Name == "Name"));

    entity.Name = "Set";
    Assert.IsTrue(nameProperty.IsValid);
    Assert.AreEqual(0, nameProperty.PropertyMessages.Count);

    // Strongly typed access by casting
    var typed = (Neatoo.IValidateProperty<string?>)nameProperty;
    Assert.AreEqual("Set", typed.Value);
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs#L83-L109' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-property-metadata' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`SetValue` is the awaitable way to set a property. The generated setter runs the same rules but returns no `Task`; a component that must wait for the rules calls `SetValue`:

<!-- snippet: skill-set-value -->
<a id='snippet-skill-set-value'></a>
```cs
[TestMethod]
public async Task SetValue_IsTheAwaitablePath()
{
    var entity = _factory.Create();

    // The property setter runs the same rules but returns no Task.
    // A component that needs to await the rules calls SetValue.
    await entity["Name"].SetValue("Manual Value");

    Assert.AreEqual("Manual Value", entity.Name);
    Assert.IsTrue(entity["Name"].IsValid);
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs#L111-L124' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-set-value' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

See [blazor.md](blazor.md) — Two Binding Modes for how this architecture enables per-field validation display and busy indicators in Blazor.

## Computed Properties: `private set` Plus a Rule

A derived value is a partial property with a `private set`, computed by an `AddAction` rule. The generator emits `private set` on the implementation and `get;` only on the interface, so consumers read it and cannot write it; the rule sets it through the private setter. `IsReadOnly` is `true`, so MudNeatoo renders it read-only without configuration.

<!-- snippet: skill-private-set-property -->
<a id='snippet-skill-private-set-property'></a>
```cs
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
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/PropertyBasics.cs#L173-L202' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-private-set-property' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Generated Behavior

For `public partial decimal ComputedTotal { get; private set; }`, the generator emits:

- **Property implementation:** `private set` accessor calling `SetPrivateValue(value)` (bypasses `IsReadOnly` check)
- **Interface declaration:** `decimal ComputedTotal { get; }` (no setter exposed)
- **Backing field:** `IsReadOnly = true`

### Indexer Behavior with Private-Set Properties

| Operation | Behavior |
|-----------|----------|
| `entity["Prop"].SetValue(x)` | Throws `PropertyReadOnlyException` (IsReadOnly is true) |
| `entity["Prop"].LoadValue(x)` | Sets value, bypasses IsReadOnly (framework and deserialization use) |
| `entity["Prop"].SetPrivateValue(x)` | Sets value bypassing IsReadOnly check; rules and `PropertyChanged` run normally |
| `entity["Prop"].IsReadOnly` | Returns `true` |

### Protected and Internal Setters

`protected set` and `internal set` preserve their accessor visibility in generated code but do NOT set `IsReadOnly = true`. The runtime's `PropertyInfoWrapper` reports read-only for `!CanWrite || SetMethod?.IsPrivate == true`: a get-only property or a private setter. Protected and internal setters use the standard `.Value = value` path.

### Serialization

`IsReadOnly` survives client-server round-trips. The property value itself is serialized through the property manager, which bypasses setters, so private setters do not affect serialization.

### A Plain Getter Is Not a Neatoo Property

A regular (non-partial) computed getter is allowed but is not tracked: it raises no `PropertyChanged`, so a bound UI does not refresh it when its inputs change, and it is not serialized. Use it only for a value nothing binds to:

<!-- snippet: skill-plain-computed-getter -->
<a id='snippet-skill-plain-computed-getter'></a>
```cs
// =========================================================================
// Computed Property (not persisted)
// =========================================================================
// This is a regular property, not partial - not tracked by Neatoo and it
// raises no PropertyChanged. A bound UI does not refresh it when
// FirstName or LastName changes; for that, use a partial property set by
// an AddAction rule triggered on both.
// =========================================================================
public string FullName => $"{FirstName} {LastName}";
```
<sup><a href='/src/Design/Design.Domain/Entities/Employee.cs#L70-L80' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-plain-computed-getter' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Field-Level Authorization via MarkReadOnly

`IValidateProperty` exposes `void MarkReadOnly()` for locking a single property on a specific instance at runtime. Unlike `private set` (compile-time, every instance), `MarkReadOnly()` is decided per instance — during `[Fetch]`, from a server-side service that knows the current user. The permission is never a parameter the client passes: a parameter of a `[Remote]` operation is client input.

<!-- snippet: skill-mark-read-only -->
<a id='snippet-skill-mark-read-only'></a>
```cs
[Remote]
[Fetch]
internal void Fetch(int id, [Service] IFieldLevelAuthRepository repository, [Service] ISalaryPermission permission)
{
    var data = repository.GetById(id);
    Name = data.Name;
    Salary = data.Salary;
    Department = data.Department;

    // Field-level authorization: lock down Salary if user lacks permission
    if (!permission.CanEditSalary)
    {
        this["Salary"].MarkReadOnly();
    }
}
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/FieldLevelAuthorization.cs#L58-L74' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mark-read-only' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Semantics

- **One-and-done.** Once called, `IsReadOnly` stays `true` permanently on that property object. There is no `MarkWritable()` counterpart.
- **Per-instance, not per-class.** Targets `this["PropertyName"]`, so one instance can have `Salary` locked while another does not.
- **No generator involvement.** `MarkReadOnly()` is a runtime API on `IValidateProperty`.
- **The client's view of the permission.** The flag travels to the client with the entity. A modified client can ignore it, so the server's `[Update]` enforces the permission again before writing the field.

### Interaction with SetValue / SetPrivateValue / LoadValue

After `MarkReadOnly()`:

| Operation | Behavior |
|-----------|----------|
| `entity["Prop"].SetValue(x)` | Throws `PropertyReadOnlyException` |
| Partial property setter (`entity.Prop = x`) | Throws `PropertyReadOnlyException` (setter calls `SetValue`) |
| `entity["Prop"].SetPrivateValue(x)` | Succeeds — rules and computed properties continue to work |
| `entity["Prop"].LoadValue(x)` | Succeeds — deserialization is unaffected |

### When to Use MarkReadOnly vs. private set

| Requirement | Use |
|-------------|-----|
| Property is always computed / never writable by users | `private set` on the partial property |
| Property is writable for some users but not others | `MarkReadOnly()` in `[Fetch]` from a server-side permission service |
| Property becomes read-only after a state transition on this instance | `MarkReadOnly()` during the state transition (factory or business method) |

Do not call `MarkReadOnly()` outside factory methods or controlled state transitions. It is permanent, so calling it in a setter or rule will silently lock the instance for the rest of its lifetime.

## Property Change Notifications

Properties raise `PropertyChanged`:

<!-- snippet: skill-property-changed -->
<a id='snippet-skill-property-changed'></a>
```cs
[TestMethod]
public void Property_SetTriggersPropertyChanged()
{
    // Arrange
    var entity = _factory.Create();
    var changedProperties = new List<string>();
    entity.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

    // Act
    entity.Name = "Test";

    // Assert
    Assert.IsTrue(changedProperties.Contains("Name"));
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs#L45-L60' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-property-changed' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Neatoo also raises `NeatooPropertyChanged`, an async event that carries the `ChangeReason` (`UserEdit` or `Load`), the property object, and a dotted `FullPropertyName` for changes that bubble up from descendants (`"Items.LineTotal"`):

<!-- snippet: skill-neatoo-property-changed -->
<a id='snippet-skill-neatoo-property-changed'></a>
```cs
[TestMethod]
public async Task NeatooPropertyChanged_CarriesFullNameAndReason()
{
    var entity = _factory.Create();
    var received = new List<Neatoo.NeatooPropertyChangedEventArgs>();
    entity.NeatooPropertyChanged += args =>
    {
        received.Add(args);
        return Task.CompletedTask;
    };

    entity.Name = "Test";
    await entity.WaitForTasks();

    var nameEvent = received.Single(e => e.PropertyName == "Name");
    Assert.AreEqual("Name", nameEvent.FullPropertyName, "A dotted path for descendants; the bare name here");
    Assert.AreEqual(Neatoo.ChangeReason.UserEdit, nameEvent.Reason, "A setter outside a factory operation is a user edit");
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs#L62-L81' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-neatoo-property-changed' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Assignment Inside and Outside a Factory Operation

Application code sets a value one way: the property setter. What it does depends on whether the object is paused.

- Inside a factory operation (`[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`) the object is paused, so plain assignment is a clean baseline load: nothing is marked modified, no rules run, no `PropertyChanged`. Do not use `LoadValue`, `PauseAllActions` or `MarkUnmodified` there.
- After the operation returns, a setter is a user edit: the property and the entity are marked modified, rules run, `PropertyChanged` fires.

`LoadValue()` on the property object sets a value without tracking regardless of pause state. The framework uses it (the generated `EntityLazyLoad` setter, deserialization). It is not needed inside a factory operation:

<!-- snippet: skill-load-value-outside-operation -->
<a id='snippet-skill-load-value-outside-operation'></a>
```cs
[TestMethod]
public void LoadValue_DoesNotMarkPropertyModified()
{
    // Arrange
    var entity = _factory.Create();

    // Act
    entity["Name"].LoadValue("Loaded");

    // Assert
    Assert.IsFalse(entity["Name"].IsModified, "Property should not be marked modified via LoadValue");
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/StatePropertyTests.cs#L46-L59' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-load-value-outside-operation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Suppressing Rules and Events

`PauseAllActions()` pauses an object outside a factory operation. While paused, setters run no rules and raise no `PropertyChanged`; `ResumeAllActions` recalculates cached validity but does not run the skipped rules:

<!-- snippet: skill-pause-all-actions -->
<a id='snippet-skill-pause-all-actions'></a>
```cs
[TestMethod]
public void Gotcha4_PausedPropertyChanges_DoNotTriggerRules()
{
    // Arrange
    var factory = _scope.GetRequiredService<IGotcha4DemoFactory>();
    var entity = factory.Create();

    // Act - Modify properties while paused
    using (entity.PauseAllActions())
    {
        entity.Quantity = 10;
        entity.Price = 5.00m;
    }
    // ResumeAllActions() is called, but rules don't automatically run

    // Assert - Total is NOT calculated
    Assert.AreEqual(0m, entity.Total, "Total should be 0 - rules did not run while paused");
}
```
<sup><a href='/src/Design/Design.Tests/GotchaTests/CommonGotchaTests.cs#L177-L196' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-pause-all-actions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

On an entity, a property set while paused is not marked modified, so edits made under `PauseAllActions()` on a fetched entity leave `IsModified` false and are not saved. Never use it inside a factory operation: the operation is already paused, and disposing the `using` resumes the object early.

## Custom Property Types

An `EntityProperty<T>` subclass can carry extra per-property metadata (for example a plausible range from an attribute) and expose it beside `IsValid`, `IsBusy`, and `IsReadOnly`, so UI binds it through `entity["Name"]` like any other property metadata. Requires 0.35.0+ (earlier versions dropped `IsSelfModified` for any subclass on deserialization).

### The property type

The subclass declares the five-argument `[JsonConstructor]` the deserializer calls, re-reads its attribute in `ApplyPropertyInfo` (called after deserialization), and marks its own members `[JsonIgnore]`:

<!-- snippet: skill-custom-property-type -->
<a id='snippet-skill-custom-property-type'></a>
```cs
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
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/CustomPropertyType.cs#L70-L105' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-custom-property-type' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Construction: substitute the property factory

The generated `InitializePropertyBackingFields` creates every backing field through `services.PropertyFactory`. For `EntityBase`, `EntityBaseServices<T>` always constructs its own `EntityPropertyFactory<T>` and **never resolves `IPropertyFactory<T>` from DI** -- registering one does nothing. Wrap the injected services instead:

<!-- snippet: skill-custom-property-services -->
<a id='snippet-skill-custom-property-services'></a>
```cs
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
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/CustomPropertyType.cs#L135-L178' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-custom-property-services' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The custom `IPropertyFactory<T>.Create<TProperty>` returns the subclass for the properties it cares about and defers the rest to the wrapped factory. (`ValidateBaseServices<T>` does resolve `IPropertyFactory<T>` from DI, so a closed registration works for `ValidateBase`.)

### Serialization contract

The converter writes each property's `$type` as its open generic definition and on read calls `MakeGenericType(valueType)`, then `Activator.CreateInstance` with the `[JsonConstructor]` arguments. A custom property type must therefore:

- Be an **open generic with exactly one type parameter** (`PlausibleProperty<T>`, not `PlausibleDoubleProperty`).
- Declare the **five-argument `[JsonConstructor]`** with exactly the shape shown above.
- Live in an **assembly passed to `AddNeatooServices`** on both tiers. `$type` is resolved by full name through `IServiceAssemblies.FindType`.
- On a **trimmed WASM client**, have its `[JsonConstructor]` **rooted** (for example a `[DynamicDependency]` on the constructor from code that is kept, or a linker descriptor). Only `Activator.CreateInstance` reaches it, so the trimmer cannot see it is used.

Only the standard fields are read back: `Name`, `Value`, `IsReadOnly`, `IsSelfModified`, `SerializedRuleMessages`. Any other public member of the subclass is written and then skipped on read, so mark subclass members `[JsonIgnore]` and restore them in `ApplyPropertyInfo`. Read the attribute in **both** the `IPropertyInfo` constructor and `ApplyPropertyInfo`: `ApplyPropertyInfo` is not called from the base constructor (a virtual call from a constructor would run before the subclass is initialized), and the `[JsonConstructor]` path has no `IPropertyInfo`.

## Related

- [Validation](validation.md) - How property changes trigger validation
- [Change Tracking](entities.md#change-tracking) - IsModified and modification state
- [Base Classes](base-classes.md) - Which base classes support properties
