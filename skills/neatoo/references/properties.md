# Properties

Neatoo uses C# partial properties with source generation. Declare the property signature; the generator provides change tracking, validation triggering, and property change notifications. (The old `Getter<T>()`/`Setter()` pattern is deprecated.)

## Basic Property Declaration

Declare properties as `partial` -- the source generator fills in the implementation:

<!-- snippet: properties-partial-declaration -->
<a id='snippet-properties-partial-declaration'></a>
```cs
[Factory]
public partial class PropEmployee : ValidateBase<PropEmployee>
{
    public PropEmployee(IValidateBaseServices<PropEmployee> services) : base(services) { }

    // Partial property - source generator completes the implementation
    public partial string Name { get; set; }

    public partial string Email { get; set; }

    public partial DateTime HireDate { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/samples/PropertiesSamples.cs#L16-L32' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-partial-declaration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Generated Implementation

The source generator creates the backing field and wires up change tracking:

<!-- snippet: properties-generated-implementation -->
<a id='snippet-properties-generated-implementation'></a>
```cs
[Fact]
public void GeneratedImplementation_PropertyBackingField()
{
    var factory = GetRequiredService<IPropEmployeeFactory>();
    var employee = factory.Create();

    // The source generator creates:
    // - NameProperty backing field of type IValidateProperty<string>
    // - Getter that returns NameProperty.Value
    // - Setter that sets NameProperty.Value and tracks tasks
    employee.Name = "Bob Smith";

    // Verify property value is accessible
    Assert.Equal("Bob Smith", employee.Name);

    // Access generated backing field via indexer
    var nameProperty = employee["Name"];
    Assert.Equal("Bob Smith", nameProperty.Value);
}
```
<sup><a href='/src/samples/PropertiesSamples.cs#L264-L284' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-generated-implementation' title='Start of snippet'>anchor</a></sup>
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

Access the property object via the indexer:

<!-- snippet: skill-property-object-access -->
<a id='snippet-skill-property-object-access'></a>
```cs
[Fact]
public void PropertyObjectAccess_IndexerReturnsMetadata()
{
    var factory = GetRequiredService<ISkillGapEmployeeFactory>();
    var employee = factory.Create();
    employee.Email = "test@example.com";

    // Each property is backed by its own IValidateProperty object
    IValidateProperty emailProp = employee["Email"];
    bool valid = emailProp.IsValid;
    var errors = emailProp.PropertyMessages;

    Assert.NotNull(emailProp);
    Assert.True(valid);
    Assert.Empty(errors);
}
```
<sup><a href='/src/samples/SkillGapSamples.cs#L154-L171' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-property-object-access' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The source generator creates a strongly-typed backing field (e.g., `EmailProperty` of type `IValidateProperty<string>`) and wires the partial property's getter/setter through it. The indexer provides untyped access by property name.

See [blazor.md](blazor.md) — Two Binding Modes for how this architecture enables per-field validation display and busy indicators in Blazor.

## Private Setter Properties

Use `private set` on partial properties to create properties that are writable from within the entity (rules, factory methods) but read-only to external consumers. The source generator respects the `private set` accessor:

```csharp
public partial decimal ComputedTotal { get; private set; }
```

### Generated Behavior

For `public partial decimal ComputedTotal { get; private set; }`, the generator emits:

- **Property implementation:** `private set` accessor calling `SetPrivateValue(value)` (bypasses `IsReadOnly` check)
- **Interface declaration:** `decimal ComputedTotal { get; }` (no setter exposed)
- **Backing field:** `IsReadOnly = true` (set from `PropertyInfoWrapper.IsPrivateSetter`)

The generated code:
```csharp
// Generated property implementation
public partial decimal ComputedTotal
{
    get => ComputedTotalProperty.Value;
    private set
    {
        ComputedTotalProperty.SetPrivateValue(value);
        if (!ComputedTotalProperty.Task.IsCompleted)
        {
            Parent?.AddChildTask(ComputedTotalProperty.Task);
            RunningTasks.AddTask(ComputedTotalProperty.Task);
        }
    }
}

// Generated interface declaration
decimal ComputedTotal { get; }  // No setter exposed
```

### Setting Private-Set Properties

Private-set properties are set from within the entity, typically via `AddAction` rules:

```csharp
internal partial class OrderLine : EntityBase<OrderLine>, IOrderLine
{
    public partial int Quantity { get; set; }
    public partial decimal UnitPrice { get; set; }
    public partial decimal ComputedTotal { get; private set; }

    public OrderLine(IEntityBaseServices<OrderLine> services) : base(services)
    {
        // Rule sets private-set property; triggers change tracking and PropertyChanged
        RuleManager.AddAction(
            t => t.ComputedTotal = t.Quantity * t.UnitPrice,
            t => t.Quantity,
            t => t.UnitPrice);
    }
}
```

### Indexer Behavior with Private-Set Properties

| Operation | Behavior |
|-----------|----------|
| `entity["Prop"].SetValue(x)` | Throws `PropertyReadOnlyException` (IsReadOnly is true) |
| `entity["Prop"].LoadValue(x)` | Sets value (Fetch escape hatch, bypasses IsReadOnly) |
| `entity["Prop"].SetPrivateValue(x)` | Sets value bypassing IsReadOnly check |
| `entity["Prop"].IsReadOnly` | Returns `true` |

### MudNeatoo Integration

MudNeatoo components bind `ReadOnly="@EntityProperty.IsReadOnly"`. Private-set properties automatically render as read-only in the UI with no additional configuration.

### Protected and Internal Setters

`protected set` and `internal set` preserve their accessor visibility in generated code but do NOT set `IsReadOnly = true`. Only `private set` maps to `IsReadOnly = true`, matching the runtime's `PropertyInfoWrapper.IsPrivateSetter` check (which only tests `SetMethod?.IsPrivate`). Protected and internal setters use the standard `.Value = value` path.

### Serialization

`IsReadOnly` survives client-server round-trips. The JSON converter serializes and deserializes `IsReadOnly` state. The property value itself is serialized through `PropertyManager.SetProperties()` which bypasses setters entirely, so private setters do not affect serialization.

### IValidateProperty.SetPrivateValue

`SetPrivateValue(object? newValue, bool quietly = false)` is a public method on the `IValidateProperty` interface. It sets the property value bypassing `IsReadOnly` checks. Used by:
- Generated setters for `private set` properties
- Framework internals (deprecated `Setter<P>()`, `ObjectInvalid` property)

`SetPrivateValue` fires change tracking, PropertyChanged, and rule execution normally -- it only bypasses the `IsReadOnly` guard.

## Field-Level Authorization via MarkReadOnly

`IValidateProperty` exposes `void MarkReadOnly()` for locking a single property on a specific instance at runtime. Unlike `private set` (which is compile-time and applies to every instance of the class), `MarkReadOnly()` is decided per-instance — typically during `[Fetch]` based on the current user's permissions.

```csharp
[Remote]
[Fetch]
internal void Fetch(int id, bool canEditSalary, [Service] IEmployeeRepository repo)
{
    var data = repo.GetById(id);
    this["Name"].LoadValue(data.Name);
    this["Salary"].LoadValue(data.Salary);

    // This instance's Salary is read-only; other FieldLevelAuthDemo instances are unaffected.
    if (!canEditSalary)
    {
        this["Salary"].MarkReadOnly();
    }
}
```

### Semantics

- **One-and-done.** Once called, `IsReadOnly` stays `true` permanently on that property object. There is no `MarkWritable()` counterpart — prevents accidental re-enabling and matches the authorization model (server decides during Fetch, client respects).
- **Per-instance, not per-class.** Targets `this["PropertyName"]`, so one `FieldLevelAuthDemo` can have `Salary` locked while another does not. Compare to `private set`, which is a class-wide declaration.
- **No generator involvement.** `MarkReadOnly()` is a runtime API on `IValidateProperty`; no attributes, no source generation.

### Interaction with SetValue / SetPrivateValue / LoadValue

After `MarkReadOnly()`:

| Operation | Behavior |
|-----------|----------|
| `entity["Prop"].SetValue(x)` | Throws `PropertyReadOnlyException` |
| Partial property setter (`entity.Prop = x`) | Throws `PropertyReadOnlyException` (setter calls `SetValue`) |
| `entity["Prop"].SetPrivateValue(x)` | Succeeds — rules and computed properties continue to work |
| `entity["Prop"].LoadValue(x)` | Succeeds — Fetch and deserialization are unaffected |

This is the same contract as `private set`: the external write path is blocked, internal and load paths still function.

### Serialization and Client-Server Round-Trip

`IsReadOnly` is serialized by the Neatoo JSON converter. When the server calls `MarkReadOnly()` during `[Fetch]`, the flag travels with the entity to the client. No `[Remote]` plumbing is required beyond the normal Fetch — the property arrives already locked.

### MudNeatoo Integration

MudNeatoo components bind `ReadOnly="@EntityProperty.IsReadOnly"`. A `MarkReadOnly`'d property renders as read-only in the UI automatically, the same as a `private set` property.

### When to Use MarkReadOnly vs. private set

| Requirement | Use |
|-------------|-----|
| Property is always computed / never writable by users | `private set` on the partial property |
| Property is writable for some users but not others | `MarkReadOnly()` in `[Fetch]` based on permissions |
| Property becomes read-only after a state transition on this instance | `MarkReadOnly()` during the state transition (factory or business method) |

Do not call `MarkReadOnly()` outside factory methods or controlled state transitions. It is permanent, so calling it in a setter or rule will silently lock the instance for the rest of its lifetime.

## Read-Only Properties

For calculated or read-only properties, declare the partial property with only a getter:

<!-- snippet: properties-read-only -->
<a id='snippet-properties-read-only'></a>
```cs
[Factory]
public partial class PropContact : ValidateBase<PropContact>
{
    public PropContact(IValidateBaseServices<PropContact> services) : base(services) { }

    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    // Read-only property - only getter implementation generated
    public partial string FullName { get; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/samples/PropertiesSamples.cs#L37-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-read-only' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Custom Getter Logic

Add custom logic in the getter while still using the backing field:

<!-- snippet: properties-custom-getter -->
<a id='snippet-properties-custom-getter'></a>
```cs
// Computed property with custom getter logic
public string DisplayName
{
    get
    {
        // Compute value from other properties
        if (string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName))
        {
            return "(Unknown)";
        }
        return $"{LastName}, {FirstName}".Trim(',', ' ');
    }
}
```
<sup><a href='/src/samples/PropertiesSamples.cs#L75-L89' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-custom-getter' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Property Change Notifications

Properties automatically raise `PropertyChanged` and Neatoo-specific events:

<!-- snippet: properties-property-changed -->
<a id='snippet-properties-property-changed'></a>
```cs
[Fact]
public void PropertyChanged_StandardNotification()
{
    var factory = GetRequiredService<IPropEmployeeFactory>();
    var employee = factory.Create();
    var changedProperties = new List<string>();

    // Subscribe to PropertyChanged
    employee.PropertyChanged += (sender, args) =>
    {
        changedProperties.Add(args.PropertyName!);
    };

    // Set properties
    employee.Name = "Dave Wilson";
    employee.Email = "dave@example.com";

    // PropertyChanged fires for each property
    Assert.Contains("Name", changedProperties);
    Assert.Contains("Email", changedProperties);
}
```
<sup><a href='/src/samples/PropertiesSamples.cs#L310-L332' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-property-changed' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Neatoo also fires `NeatooPropertyChanged`, which provides richer information than standard `PropertyChanged` -- including the `ChangeReason` (UserEdit vs Load) and the property object reference:

<!-- snippet: properties-neatoo-property-changed -->
<a id='snippet-properties-neatoo-property-changed'></a>
```cs
[Fact]
public async Task NeatooPropertyChanged_ExtendedNotification()
{
    var factory = GetRequiredService<IPropOrderFactory>();
    var order = factory.Create();
    var receivedEvents = new List<NeatooPropertyChangedEventArgs>();

    // Subscribe to NeatooPropertyChanged
    order.NeatooPropertyChanged += (args) =>
    {
        receivedEvents.Add(args);
        return Task.CompletedTask;
    };

    // Set property
    order.OrderNumber = "ORD-001";

    // Wait for async event handling
    await order.WaitForTasks();

    // Event provides extended information
    var orderNumberEvent = receivedEvents.FirstOrDefault(e => e.PropertyName == "OrderNumber");
    Assert.NotNull(orderNumberEvent);
    Assert.Equal("OrderNumber", orderNumberEvent.PropertyName);
    Assert.Equal("OrderNumber", orderNumberEvent.FullPropertyName);
    Assert.Equal(ChangeReason.UserEdit, orderNumberEvent.Reason);
}
```
<sup><a href='/src/samples/PropertiesSamples.cs#L334-L362' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-neatoo-property-changed' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Loading Values Without Triggering Rules

Use `LoadValue()` to set values without triggering validation or marking dirty:

<!-- snippet: properties-load-value -->
<a id='snippet-properties-load-value'></a>
```cs
[Fact]
public void LoadValue_DataLoadingWithoutRules()
{
    var factory = GetRequiredService<IPropInvoiceFactory>();
    var invoice = factory.Create();

    // Use LoadValue during data loading (e.g., in Fetch factory method)
    // LoadValue:
    // - Does NOT trigger validation rules
    // - Does NOT mark entity as modified
    // - Does NOT fire PropertyChanged (suppressed during load)
    // - DOES fire NeatooPropertyChanged with ChangeReason.Load
    // - DOES establish parent-child relationships
    invoice["CustomerName"].LoadValue("Acme Corp");
    invoice["Amount"].LoadValue(500.00m);

    // Property values are set
    Assert.Equal("Acme Corp", invoice.CustomerName);
    Assert.Equal(500.00m, invoice.Amount);
}
```
<sup><a href='/src/samples/PropertiesSamples.cs#L393-L414' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-load-value' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Suppressing Events

Temporarily suppress property change events during bulk operations:

<!-- snippet: properties-suppress-events -->
<a id='snippet-properties-suppress-events'></a>
```cs
[Fact]
public void SuppressEvents_PauseAllActions()
{
    var factory = GetRequiredService<IPropInvoiceFactory>();
    var invoice = factory.Create();
    var changeCount = 0;

    invoice.PropertyChanged += (_, _) => changeCount++;

    // Pause property events during batch updates
    using (invoice.PauseAllActions())
    {
        invoice.CustomerName = "Gamma LLC";
        invoice.Amount = 750.00m;
        invoice.InvoiceDate = DateTime.Today;

        // Events are suppressed during pause
        // (changeCount may have some events from internal operations,
        // but rule execution is deferred)
    }

    // After Resume (automatic when using statement ends):
    // - All deferred events fire
    // - Validation rules execute
    // - Dirty state recalculates

    // Verify properties are set
    Assert.Equal("Gamma LLC", invoice.CustomerName);
    Assert.Equal(750.00m, invoice.Amount);
}
```
<sup><a href='/src/samples/PropertiesSamples.cs#L484-L515' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-suppress-events' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Meta Properties

Access property metadata for validation state, dirty state, etc.:

<!-- snippet: properties-meta-properties -->
<a id='snippet-properties-meta-properties'></a>
```cs
[Fact]
public async Task MetaProperties_QueryPropertyState()
{
    var factory = GetRequiredService<IPropInvoiceFactory>();
    var invoice = factory.Create();

    // Set valid data
    invoice.CustomerName = "Beta Inc";
    invoice.Amount = 250.00m;

    await invoice.WaitForTasks();

    // Access meta-properties on property wrapper
    var amountProperty = invoice["Amount"];

    // Available meta-properties:
    Assert.False(amountProperty.IsBusy);           // No async operations pending
    Assert.True(amountProperty.IsValid);           // Property passes validation
    Assert.True(amountProperty.IsSelfValid);       // Property itself is valid
    Assert.Empty(amountProperty.PropertyMessages); // No validation errors
    Assert.False(amountProperty.IsReadOnly);       // Can be modified

    // Set invalid data
    invoice.Amount = -100.00m;
    await invoice.WaitForTasks();

    // Meta-properties update
    Assert.False(invoice["Amount"].IsValid);
    Assert.True(invoice["Amount"].PropertyMessages.Any());
}
```
<sup><a href='/src/samples/PropertiesSamples.cs#L416-L447' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-meta-properties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Direct Backing Field Access

Access the backing field directly when needed:

<!-- snippet: properties-backing-field-access -->
<a id='snippet-properties-backing-field-access'></a>
```cs
[Fact]
public void BackingFieldAccess_PropertyWrapper()
{
    var factory = GetRequiredService<IPropEmployeeFactory>();
    var employee = factory.Create();
    employee.Name = "Carol Davis";

    // Access property wrapper via indexer
    var nameProperty = employee["Name"];

    // Property wrapper provides:
    Assert.Equal("Carol Davis", nameProperty.Value);        // Value access
    Assert.False(nameProperty.IsBusy);                      // Async status
    Assert.True(nameProperty.IsValid);                      // Validation status
    Assert.Empty(nameProperty.PropertyMessages);            // Error messages
    Assert.False(nameProperty.IsReadOnly);                  // Mutability

    // Strongly-typed access by casting
    var typedProperty = (IValidateProperty<string>)nameProperty;
    Assert.Equal("Carol Davis", typedProperty.Value);
}
```
<sup><a href='/src/samples/PropertiesSamples.cs#L286-L308' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-backing-field-access' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Change Reason Tracking

Neatoo tracks why a property changed (user edit, rule, load):

<!-- snippet: properties-change-reason-useredit -->
<a id='snippet-properties-change-reason-useredit'></a>
```cs
[Fact]
public void ChangeReasonUserEdit_NormalPropertyAssignment()
{
    var factory = GetRequiredService<IPropInvoiceFactory>();
    var invoice = factory.Create();
    ChangeReason capturedReason = ChangeReason.Load; // Initialize to opposite

    invoice.NeatooPropertyChanged += (args) =>
    {
        if (args.PropertyName == "Amount")
        {
            capturedReason = args.Reason;
        }
        return Task.CompletedTask;
    };

    // Standard assignment uses UserEdit
    invoice.Amount = 100.00m;

    // Reason is UserEdit for normal setter assignment
    Assert.Equal(ChangeReason.UserEdit, capturedReason);

    // Amount property's validation rule executes with UserEdit
    // (Amount > 0 passes, so Amount property is valid)
    Assert.True(invoice["Amount"].IsValid);
}
```
<sup><a href='/src/samples/PropertiesSamples.cs#L364-L391' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-change-reason-useredit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Related

- [Validation](validation.md) - How property changes trigger validation
- [Change Tracking](entities.md#change-tracking) - IsModified and modification state
- [Base Classes](base-classes.md) - Which base classes support properties
