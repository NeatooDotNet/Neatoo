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
<sup><a href='/src/samples/PropertiesSamples.cs#L234-L254' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-generated-implementation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Object-Per-Property Architecture

Each partial property declared on a Neatoo class is backed by its own `IValidateProperty<T>` object. This is not just a backing field — it is a full object that owns:

| Member | Interface | Purpose |
|--------|-----------|---------|
| `Value` | `IValidateProperty` | The current property value |
| `IsValid` | `IValidateProperty` | Whether this property passes its validation rules |
| `PropertyMessages` | `IValidateProperty` | Validation error messages for this property |
| `IsBusy` | `IValidateProperty` | Whether an async rule is currently running for this property |
| `IsReadOnly` | `IValidateProperty` | Whether this property is read-only |
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
<sup><a href='/src/samples/PropertiesSamples.cs#L280-L302' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-property-changed' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/PropertiesSamples.cs#L304-L332' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-neatoo-property-changed' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/PropertiesSamples.cs#L363-L384' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-load-value' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/PropertiesSamples.cs#L454-L485' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-suppress-events' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/PropertiesSamples.cs#L386-L417' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-meta-properties' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/PropertiesSamples.cs#L256-L278' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-backing-field-access' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/PropertiesSamples.cs#L334-L361' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-change-reason-useredit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Related

- [Validation](validation.md) - How property changes trigger validation
- [Change Tracking](entities.md#change-tracking) - IsModified and modification state
- [Base Classes](base-classes.md) - Which base classes support properties
