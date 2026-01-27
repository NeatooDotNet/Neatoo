# Properties

[← Parent-Child](parent-child.md) | [↑ Guides](index.md) | [Remote Factory →](remote-factory.md)

Neatoo's property system provides managed properties with source-generated backing field properties, automatic change notifications, validation integration, and parent-child relationship tracking. Properties declared as partial are implemented by the BaseGenerator source generator, which creates backing field properties that access PropertyManager and wires up event handling, rule execution, and task tracking.

## Partial Property Declaration

Properties in ValidateBase and EntityBase are declared as partial properties. The source generator completes the implementation by creating backing field properties that access strongly-typed property wrappers from PropertyManager.

Declare a partial property:

<!-- snippet: properties-partial-declaration -->
<a id='snippet-properties-partial-declaration'></a>
```cs
/// <summary>
/// Customer entity demonstrating partial property declarations.
/// Partial properties let the source generator create backing fields.
/// </summary>
[Factory]
public partial class SkillPropCustomer : ValidateBase<SkillPropCustomer>
{
    public SkillPropCustomer(IValidateBaseServices<SkillPropCustomer> services) : base(services) { }

    // Partial properties - source generator completes the implementation
    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    public partial string Email { get; set; }

    public partial DateTime BirthDate { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/PropertySamples.cs#L16-L38' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-partial-declaration' title='Start of snippet'>anchor</a></sup>
<a id='snippet-properties-partial-declaration-1'></a>
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
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L16-L32' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-partial-declaration-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The source generator creates:
- A protected `NameProperty` property that retrieves `IValidateProperty<string>` from PropertyManager
- Full getter/setter implementation for the partial property
- Automatic property change notifications
- Task tracking for async operations
- Parent cascade for child tasks

Partial properties must be public and non-static. The generator analyzes the class during compilation and generates the implementation in a separate file.

The source generator also creates an override of `InitializePropertyBackingFields` that registers each property with the PropertyManager during construction.

## Source-Generated Implementation

The BaseGenerator creates the property implementation with backing field properties that retrieve strongly-typed wrappers from PropertyManager. PropertyManager stores all IValidateProperty instances and handles registration, lookup, and lifecycle management.

Generated property implementation:

<!-- snippet: properties-generated-implementation -->
<a id='snippet-properties-generated-implementation'></a>
```cs
// The source generator creates backing fields for each partial property:
//
// private IValidateProperty<string> NameProperty;
//
// public partial string Name
// {
//     get => NameProperty.Value;
//     set
//     {
//         NameProperty.Value = value;
//         TaskManager.Add(NameProperty.Task);
//     }
// }
//
// Access the generated property wrapper via indexer:
// var property = entity["Name"];
// property.Value, property.IsValid, property.PropertyMessages, etc.
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/PropertySamples.cs#L237-L255' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-generated-implementation' title='Start of snippet'>anchor</a></sup>
<a id='snippet-properties-generated-implementation-1'></a>
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
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L234-L254' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-generated-implementation-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The generated code creates:
- A strongly-typed `NameProperty` backing field property that retrieves from PropertyManager
- Getter implementation that returns `NameProperty.Value`
- Setter implementation that sets `NameProperty.Value` and tracks tasks
- Task propagation up to Parent for aggregate-level coordination
- Type safety through generic IValidateProperty<T>

The PropertyManager stores the actual property instances and handles validation, events, and state management behind the scenes.

## Property Backing Fields

Each partial property gets a generated backing field property that retrieves the underlying IValidateProperty instance from PropertyManager. The PropertyManager stores all property instances; the generated backing field properties provide strongly-typed access.

Access the property wrapper:

<!-- snippet: properties-backing-field-access -->
<a id='snippet-properties-backing-field-access'></a>
```cs
/// <summary>
/// Test accessing property via indexer.
/// </summary>
[TestMethod]
public void BackingFieldAccess_ViaIndexer()
{
    var factory = GetRequiredService<ISkillPropCustomerFactory>();
    var customer = factory.Create();

    // Set via property
    customer.FirstName = "John";

    // Access via indexer returns the property wrapper
    var property = customer["FirstName"];

    Assert.IsNotNull(property);
    Assert.AreEqual("FirstName", property.Name);
    Assert.AreEqual("John", property.Value);
    Assert.AreEqual(typeof(string), property.Type);
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Tests/TestingPatternsTests.cs#L546-L567' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-backing-field-access' title='Start of snippet'>anchor</a></sup>
<a id='snippet-properties-backing-field-access-1'></a>
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
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L256-L278' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-backing-field-access-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Property wrappers provide:
- **Value**: Get or set the property value
- **Task**: Access the pending async task (if any)
- **IsBusy**: Check if async operations are running
- **IsValid**: Check if the property passes validation
- **PropertyMessages**: Access validation error messages
- **RunRules**: Manually trigger validation rules

The backing field property is strongly typed (`IValidateProperty<string>`) for compile-time safety. The PropertyManager stores the actual IValidateProperty instances and manages their lifecycle.

## PropertyChanged Events

Properties raise two types of change events: standard INotifyPropertyChanged for UI binding and NeatooPropertyChanged for framework-internal coordination.

### INotifyPropertyChanged

The standard PropertyChanged event fires when property values change. This event is consumed by UI frameworks like WPF and Blazor for two-way binding.

Subscribe to PropertyChanged:

<!-- snippet: properties-property-changed -->
<a id='snippet-properties-property-changed'></a>
```cs
/// <summary>
/// Test standard PropertyChanged event.
/// </summary>
[TestMethod]
public void PropertyChanged_NotifiesOnChange()
{
    var factory = GetRequiredService<ISkillPropNotifyEntityFactory>();
    var entity = factory.Create();

    var changedProperties = new List<string>();
    entity.PropertyChanged += (s, e) =>
    {
        if (e.PropertyName != null)
            changedProperties.Add(e.PropertyName);
    };

    entity.Name = "Test";

    // PropertyChanged fired
    Assert.IsTrue(changedProperties.Contains("Name"));
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Tests/TestingPatternsTests.cs#L443-L465' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-property-changed' title='Start of snippet'>anchor</a></sup>
<a id='snippet-properties-property-changed-1'></a>
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
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L280-L302' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-property-changed-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

PropertyChanged behavior:
- Fires after value changes via normal property setters
- Includes property name in event args
- Does NOT fire during LoadValue operations (LoadValue only fires NeatooPropertyChanged)
- Does not include old/new value comparison
- Fires for meta-properties (IsValid, IsDirty, IsBusy)

UI binding relies on this event to update when properties change.

### NeatooPropertyChanged

NeatooPropertyChanged is Neatoo's internal event for coordinating validation, dirty state, and parent-child relationships. This async event enables complex cascading behaviors.

Subscribe to NeatooPropertyChanged:

<!-- snippet: properties-neatoo-property-changed -->
<a id='snippet-properties-neatoo-property-changed'></a>
```cs
/// <summary>
/// Test NeatooPropertyChanged with ChangeReason.
/// </summary>
[TestMethod]
public void NeatooPropertyChanged_IncludesChangeReason()
{
    var factory = GetRequiredService<ISkillPropNotifyEntityFactory>();
    var entity = factory.Create();

    var changes = new List<(string Property, ChangeReason Reason)>();
    entity.NeatooPropertyChanged += (e) =>
    {
        changes.Add((e.PropertyName, e.Reason));
        return Task.CompletedTask;
    };

    entity.Name = "Updated";

    // Property setter fires NeatooPropertyChanged with UserEdit reason
    // Filter for Name property changes (other properties may also fire)
    var nameChanges = changes.Where(c => c.Property == "Name").ToList();
    Assert.IsTrue(nameChanges.Count >= 1, "Name property should fire at least one change event");
    Assert.AreEqual(ChangeReason.UserEdit, nameChanges.Last().Reason);
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Tests/TestingPatternsTests.cs#L467-L492' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-neatoo-property-changed' title='Start of snippet'>anchor</a></sup>
<a id='snippet-properties-neatoo-property-changed-1'></a>
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
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L304-L332' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-neatoo-property-changed-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

NeatooPropertyChanged provides:
- **PropertyName**: Name of the changed property
- **FullPropertyName**: Breadcrumb path through nested objects (e.g., "Order.Customer.Name")
- **Source**: The object that raised the event
- **Reason**: ChangeReason enum (UserEdit or Load)
- **Property**: The IValidateProperty instance
- **OriginalEventArgs**: The root event that started the cascade

The Reason distinguishes user edits (which trigger rules) from data loading (which only establishes structure).

## ChangeReason: UserEdit vs Load

Property changes are tagged with ChangeReason to control whether validation rules execute and state cascades to the parent.

### ChangeReason.UserEdit

UserEdit indicates a normal property setter assignment. This is the default for all property assignments.

UserEdit behavior:
- Validation rules execute
- Dirty state cascades to parent
- NeatooPropertyChanged fires and bubbles up
- Parent-child relationships are established
- IsModified becomes true (for EntityBase)

Standard property assignment uses UserEdit:

<!-- snippet: properties-change-reason-useredit -->
<a id='snippet-properties-change-reason-useredit'></a>
```cs
/// <summary>
/// Test ChangeReason tracking.
/// </summary>
[TestMethod]
public void ChangeReason_TracksUserEdits()
{
    var factory = GetRequiredService<ISkillPropNotifyEntityFactory>();
    var entity = factory.Create();

    var reasons = new List<(string Property, ChangeReason Reason)>();
    entity.NeatooPropertyChanged += (e) =>
    {
        reasons.Add((e.PropertyName, e.Reason));
        return Task.CompletedTask;
    };

    // Normal property set via setter = UserEdit
    entity.Name = "Test";

    // Filter for Name property changes
    var nameReasons = reasons.Where(r => r.Property == "Name").ToList();
    Assert.IsTrue(nameReasons.Any(), "Name property should fire change event");
    Assert.AreEqual(ChangeReason.UserEdit, nameReasons.Last().Reason);
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Tests/TestingPatternsTests.cs#L601-L626' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-change-reason-useredit' title='Start of snippet'>anchor</a></sup>
<a id='snippet-properties-change-reason-useredit-1'></a>
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
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L334-L361' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-change-reason-useredit-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### ChangeReason.Load

Load indicates data loading from persistence or deserialization. LoadValue sets properties without triggering validation or modification tracking.

Use LoadValue during data loading:

<!-- snippet: properties-load-value -->
<a id='snippet-properties-load-value'></a>
```cs
/// <summary>
/// Test LoadValue for data loading without triggering rules.
/// </summary>
[TestMethod]
public void LoadValue_DoesNotTriggerRules()
{
    var factory = GetRequiredService<ISkillPropInvoiceFactory>();
    var invoice = factory.Create();

    // LoadValue bypasses validation
    invoice["Amount"].LoadValue(-100m);

    // Value is set even though it would fail validation
    Assert.AreEqual(-100m, invoice.Amount);

    // Property hasn't been validated yet
    // (LoadValue doesn't run rules)
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Tests/TestingPatternsTests.cs#L494-L513' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-load-value' title='Start of snippet'>anchor</a></sup>
<a id='snippet-properties-load-value-1'></a>
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
    // - DOES fire PropertyChanged (for UI binding)
    // - DOES establish parent-child relationships
    invoice["CustomerName"].LoadValue("Acme Corp");
    invoice["Amount"].LoadValue(500.00m);

    // Property values are set
    Assert.Equal("Acme Corp", invoice.CustomerName);
    Assert.Equal(500.00m, invoice.Amount);
}
```
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L363-L383' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-load-value-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

LoadValue behavior:
- Validation rules do NOT execute
- Dirty state does NOT cascade to parent
- PropertyChanged does NOT fire (intentionally suppressed to avoid UI updates during data loading)
- NeatooPropertyChanged fires with Reason = Load
- Parent-child relationships ARE established (SetParent called on child objects)
- IsModified remains false

LoadValue is essential during factory Fetch operations to load data without marking the entity as modified. It still establishes parent-child structure for validation cascade.

## Meta-Properties

Properties expose meta-properties for querying their state beyond the current value.

Access property metadata:

<!-- snippet: properties-meta-properties -->
<a id='snippet-properties-meta-properties'></a>
```cs
/// <summary>
/// Test property meta-properties.
/// </summary>
[TestMethod]
public async Task MetaProperties_AvailableOnProperty()
{
    var factory = GetRequiredService<ISkillPropAccountFactory>();
    var account = factory.Create();

    var accountNumberProp = account["AccountNumber"];

    // Set invalid value
    account.AccountNumber = "";

    await account.RunRules();

    // Meta-properties reflect validation state
    Assert.IsFalse(accountNumberProp.IsValid);
    Assert.IsFalse(accountNumberProp.IsBusy);
    Assert.IsTrue(accountNumberProp.PropertyMessages.Any());

    // Fix value
    account.AccountNumber = "ACCT-001";
    await account.RunRules();

    Assert.IsTrue(accountNumberProp.IsValid);
    Assert.IsFalse(accountNumberProp.PropertyMessages.Any());
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Tests/TestingPatternsTests.cs#L515-L544' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-meta-properties' title='Start of snippet'>anchor</a></sup>
<a id='snippet-properties-meta-properties-1'></a>
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
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L385-L416' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-meta-properties-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Available meta-properties:
- **IsBusy**: True if async operations are pending on this property
- **IsValid**: True if the property and all child objects are valid
- **IsSelfValid**: True if the property itself is valid (ignores child validation)
- **PropertyMessages**: Collection of validation messages
- **Task**: The pending async task (completed task if not busy)
- **IsReadOnly**: True if the property cannot be modified

Meta-properties enable conditional UI rendering, save-enablement logic, and validation feedback.

## Computed Properties

Standard C# properties can compute values from partial properties. These are regular properties, not partial, and provide derived read-only values.

Implement a computed property:

<!-- snippet: properties-custom-getter -->
<a id='snippet-properties-custom-getter'></a>
```cs
/// <summary>
/// Order entity demonstrating computed properties.
/// </summary>
[Factory]
public partial class SkillPropOrder : ValidateBase<SkillPropOrder>
{
    public SkillPropOrder(IValidateBaseServices<SkillPropOrder> services) : base(services) { }

    public partial int Quantity { get; set; }

    public partial decimal UnitPrice { get; set; }

    public partial decimal DiscountPercent { get; set; }

    // Computed property with custom getter logic
    public decimal TotalPrice
    {
        get
        {
            var subtotal = Quantity * UnitPrice;
            var discount = subtotal * (DiscountPercent / 100);
            return subtotal - discount;
        }
    }

    // Formatted display property
    public string DisplayName
    {
        get
        {
            if (Quantity == 0 || UnitPrice == 0)
                return "(No items)";
            return $"{Quantity} x {UnitPrice:C} = {TotalPrice:C}";
        }
    }

    [Create]
    public void Create() { }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/PropertySamples.cs#L78-L118' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-custom-getter' title='Start of snippet'>anchor</a></sup>
<a id='snippet-properties-custom-getter-1'></a>
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
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L75-L89' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-custom-getter-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Computed property patterns:
- Derive values from other properties using standard C# getters
- No setter means the property is read-only
- PropertyChanged does not fire for computed properties (bind to source properties instead)
- Computed properties do not use partial declarations

Computed properties are useful for display values derived from multiple source properties.

## Read-Only Properties

Properties can be declared read-only by omitting the setter. The source generator creates only the getter implementation.

Declare a read-only property:

<!-- snippet: properties-read-only -->
<a id='snippet-properties-read-only'></a>
```cs
/// <summary>
/// Contact entity demonstrating read-only properties.
/// </summary>
[Factory]
public partial class SkillPropContact : ValidateBase<SkillPropContact>
{
    public SkillPropContact(IValidateBaseServices<SkillPropContact> services) : base(services) { }

    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    // Read-only property - only getter, value set via LoadValue
    public partial string FullName { get; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        // Use LoadValue to set read-only properties during fetch
        this["FullName"].LoadValue($"{firstName} {lastName}");
    }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/PropertySamples.cs#L44-L72' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-read-only' title='Start of snippet'>anchor</a></sup>
<a id='snippet-properties-read-only-1'></a>
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
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L37-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-read-only-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Read-only properties:
- Do not have setter implementation in the partial property
- Can still be set via LoadValue during deserialization or data loading
- Throw PropertyReadOnlyException if Value setter is called on the IValidateProperty wrapper
- May be computed from other properties or set only during initialization
- Have IsReadOnly == true

Read-only properties are common for identity fields and computed values.

## Suppressing Property Events

During batch operations, suppress property change events to improve performance and avoid intermediate validation states.

Pause property events during batch updates:

<!-- snippet: properties-suppress-events -->
<a id='snippet-properties-suppress-events'></a>
```cs
/// <summary>
/// Test PauseAllActions for batch updates.
/// </summary>
[TestMethod]
public void SuppressEvents_WithPauseAllActions()
{
    var factory = GetRequiredService<ISkillPropCustomerFactory>();
    var customer = factory.Create();

    var changeCount = 0;
    customer.PropertyChanged += (s, e) => changeCount++;

    // Batch update with pause
    using (customer.PauseAllActions())
    {
        Assert.IsTrue(customer.IsPaused);

        customer.FirstName = "John";
        customer.LastName = "Doe";
        customer.Email = "john@example.com";
    }

    // After pause ends
    Assert.IsFalse(customer.IsPaused);

    // Values are set
    Assert.AreEqual("John", customer.FirstName);
    Assert.AreEqual("Doe", customer.LastName);
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Tests/TestingPatternsTests.cs#L569-L599' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-suppress-events' title='Start of snippet'>anchor</a></sup>
<a id='snippet-properties-suppress-events-1'></a>
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
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L453-L484' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-suppress-events-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

PauseAllActions behavior:
- PropertyChanged events are suppressed (not raised while paused)
- NeatooPropertyChanged propagation to parent is suppressed
- Validation rules do NOT execute
- Dirty state tracking is suppressed
- Parent cascade is suppressed

After Resume:
- PropertyChanged and NeatooPropertyChanged resume firing for new changes
- Validation rules execute normally for new property changes
- Dirty state tracking resumes
- Parent cascade resumes
- No catch-up events fire for changes made during pause

Use PauseAllActions when setting multiple properties during initialization, deserialization, or bulk updates.

## Property Access via Indexer

Properties can be accessed dynamically by name using the indexer syntax.

Access properties by name:

<!-- snippet: properties-indexer-access -->
<a id='snippet-properties-indexer-access'></a>
```cs
[Fact]
public void IndexerAccess_DynamicPropertyAccess()
{
    var factory = GetRequiredService<IPropEmployeeFactory>();
    var employee = factory.Create();
    employee.Name = "Eva Martinez";

    // Access property by name using indexer
    var property = employee["Name"];
    Assert.Equal("Eva Martinez", property.Value);

    // Cast to strongly-typed for type-safe access
    var typedProperty = (IValidateProperty<string>)property;
    typedProperty.Value = "Eva M. Martinez";

    // Use TryGetProperty for safe access
    if (employee.TryGetProperty("Email", out var emailProperty))
    {
        emailProperty.Value = "eva@example.com";
    }

    Assert.Equal("Eva M. Martinez", employee.Name);
    Assert.Equal("eva@example.com", employee.Email);
}
```
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L486-L511' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-indexer-access' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Indexer patterns:
- Returns IValidateProperty (non-generic)
- Cast to IValidateProperty<T> for strongly-typed access
- Throws PropertyMissingException if property doesn't exist
- Use TryGetProperty for safe access
- Useful for generic validation code and reflection-free property access

The indexer enables scenarios like generic validation messages, rule engines, and dynamic property access without reflection.

## Task Tracking and IsBusy

Properties track async operations through the Task property. When validation rules or setters execute asynchronously, the property enters a busy state.

Wait for property tasks to complete:

<!-- snippet: properties-task-tracking -->
<a id='snippet-properties-task-tracking'></a>
```cs
[Fact]
public async Task TaskTracking_AsyncOperations()
{
    var factory = GetRequiredService<IPropAsyncProductFactory>();
    var product = factory.Create();

    product.Name = "Widget";

    // Setting ZipCode triggers async rule
    product.ZipCode = "90210";

    // IsBusy is true while async operations run
    // (may be false if rule completes very fast)

    // Wait for all property tasks to complete
    await product.WaitForTasks();

    // After tasks complete:
    Assert.False(product.IsBusy);
    Assert.Equal(0.0825m, product.TaxRate);

    // Access property-level task
    var zipProperty = product["ZipCode"];
    Assert.True(zipProperty.Task.IsCompleted);
}
```
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L513-L539' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-task-tracking' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Task tracking behavior:
- Property setters that trigger async rules return immediately but track the task
- IsBusy is true while tasks are pending
- Task property contains the pending task (or CompletedTask if not busy)
- Tasks propagate to Parent.RunningTasks for aggregate-level coordination
- WaitForTasks() awaits all property tasks

UI can bind to IsBusy to show loading indicators during async validation or save operations.

## Property Validation Integration

Properties integrate with the validation system. Validation rules execute when properties change, and errors are stored in PropertyMessages.

Property validation coordination:

<!-- snippet: properties-validation-integration -->
<a id='snippet-properties-validation-integration'></a>
```cs
[Fact]
public async Task ValidationIntegration_PropertyValidation()
{
    var factory = GetRequiredService<IPropInvoiceFactory>();
    var invoice = factory.Create();

    // Set invalid value
    invoice.Amount = -50.00m;

    // Property-level validation state
    var amountProperty = invoice["Amount"];
    Assert.False(amountProperty.IsValid);
    Assert.True(amountProperty.PropertyMessages.Any());

    // Object-level validation reflects property state
    Assert.False(invoice.IsValid);

    // Fix the value
    invoice.Amount = 100.00m;
    await invoice.WaitForTasks();

    // Validation passes
    Assert.True(invoice["Amount"].IsValid);
    Assert.Empty(invoice["Amount"].PropertyMessages);

    // Set required field to trigger full validity
    invoice.CustomerName = "Test Customer";
    await invoice.WaitForTasks();

    Assert.True(invoice.IsValid);
}
```
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L541-L573' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-validation-integration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Validation flow:
1. Property value changes
2. Validation rules execute (if not paused and Reason == UserEdit)
3. PropertyMessages updated with any errors
4. IsValid recalculates based on PropertyMessages
5. Parent's IsValid recalculates (cascade)

See [Validation](validation.md) for details on rule execution and [Business Rules](business-rules.md) for custom validation logic.

## Property Change Propagation

Property changes propagate up the parent-child graph through NeatooPropertyChanged events. This enables aggregate-level coordination and validation.

Property change cascade:

<!-- snippet: properties-change-propagation -->
<a id='snippet-properties-change-propagation'></a>
```cs
[Fact]
public async Task ChangePropagation_ChildToParent()
{
    var orderFactory = GetRequiredService<IPropOrderFactory>();
    var order = orderFactory.Create();
    order.OrderNumber = "ORD-001";

    var receivedEvents = new List<NeatooPropertyChangedEventArgs>();

    order.NeatooPropertyChanged += (args) =>
    {
        receivedEvents.Add(args);
        return Task.CompletedTask;
    };

    // Add child item
    var itemFactory = GetRequiredService<IPropOrderItemFactory>();
    var item = itemFactory.Create();
    item.ProductName = "Widget";
    item.UnitPrice = 25.00m;
    item.Quantity = 2;

    order.LineItems.Add(item);

    // Change child property
    item.UnitPrice = 30.00m;

    await order.WaitForTasks();

    // Parent receives notification with full breadcrumb path
    var propagatedEvent = receivedEvents
        .FirstOrDefault(e => e.FullPropertyName.Contains("UnitPrice"));

    // FullPropertyName builds breadcrumb: "LineItems.UnitPrice"
    Assert.NotNull(propagatedEvent);
}
```
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L575-L612' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-change-propagation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Cascade behavior:
- Child property changes fire NeatooPropertyChanged
- Parent subscribes to child's NeatooPropertyChanged
- Parent re-fires the event with updated breadcrumb (FullPropertyName)
- Event bubbles to aggregate root
- Root can react to any property change in the entire graph

The FullPropertyName property builds the breadcrumb path by concatenating property names with dots (e.g., "LineItems.UnitPrice"). Collection indexes are not included in the breadcrumb.

## Constructor Property Assignment

Properties set in constructors outside of factory methods are tracked as modifications because constructors run before the factory pause mechanism activates.

Avoid constructor property assignment:

<!-- snippet: properties-constructor-assignment -->
<a id='snippet-properties-constructor-assignment'></a>
```cs
[Fact]
public void ConstructorAssignment_UseLoadValueInstead()
{
    // Avoid setting properties directly in constructors
    // outside of factory methods, as they will be tracked
    // as modifications.

    // Instead, use LoadValue for initial values:
    var factory = GetRequiredService<IPropEmployeeFactory>();
    var employee = factory.Create();

    // LoadValue sets value without triggering modification tracking
    employee["Name"].LoadValue("Default Employee");
    employee["Email"].LoadValue("default@example.com");

    // Properties are set but not marked as modified
    // (In a full EntityBase scenario with MarkUnmodified)
    Assert.Equal("Default Employee", employee.Name);
    Assert.Equal("default@example.com", employee.Email);
}
```
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L614-L635' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-constructor-assignment' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The analyzer warns about constructor assignments and offers a code fix to convert to LoadValue. This ensures new entities start in an unmodified state.

Use LoadValue in constructors when initial values must be set outside of factory Create methods.

---

**UPDATED:** 2026-01-24
