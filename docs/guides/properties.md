# Properties

[← Parent-Child](parent-child.md) | [↑ Guides](index.md) | [Remote Factory →](remote-factory.md)

Neatoo's property system provides managed properties with source-generated backing fields, automatic change notifications, validation integration, and parent-child relationship tracking. Properties declared as partial are implemented by the BaseGenerator source generator, which creates property backing fields and wires up event handling, rule execution, and task tracking.

## Partial Property Declaration

Properties in ValidateBase and EntityBase are declared as partial properties. The source generator completes the implementation by creating property wrappers and backing field access.

Declare a partial property:

<!-- snippet: properties-partial-declaration -->
```cs
[Factory]
public partial class PropEmployee : ValidateBase<PropEmployee>
{
    public PropEmployee(IValidateBaseServices<PropEmployee> services) : base(services) { }

    // Partial property - source generator completes the implementation
    public partial string Name { get; set; }

    public partial string Email { get; set; }

    public partial DateTime HireDate { get; set; }
}
```
<!-- endSnippet -->

The source generator creates:
- A protected `NameProperty` field of type `IValidateProperty<string>`
- Full getter/setter implementation
- Automatic property change notifications
- Task tracking for async operations
- Parent cascade for child tasks

Partial properties must be public and non-static. The generator analyzes the class during compilation and generates the implementation in a separate file.

## Source-Generated Implementation

The BaseGenerator creates the property implementation with backing field wrappers that integrate with the property management system.

Generated property implementation:

<!-- snippet: properties-generated-implementation -->
```cs
[Fact]
public void GeneratedImplementation_PropertyBackingField()
{
    var employee = new PropEmployee(new ValidateBaseServices<PropEmployee>());

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
<!-- endSnippet -->

The generated code:
- Accesses the property through PropertyManager
- Provides a strongly-typed `NameProperty` wrapper
- Sets values through `IValidateProperty<T>.Value`
- Tracks async tasks when property setters run async rules
- Propagates tasks up to Parent for aggregate-level task coordination
- Maintains type safety through generic IValidateProperty<T>

The property wrapper handles all validation, events, and state management behind the scenes.

## Property Backing Fields

Each partial property gets a generated backing field that wraps the underlying IValidateProperty instance from PropertyManager.

Access the property wrapper:

<!-- snippet: properties-backing-field-access -->
```cs
[Fact]
public void BackingFieldAccess_PropertyWrapper()
{
    var employee = new PropEmployee(new ValidateBaseServices<PropEmployee>());
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
<!-- endSnippet -->

Property wrappers provide:
- **Value**: Get or set the property value
- **Task**: Access the pending async task (if any)
- **IsBusy**: Check if async operations are running
- **IsValid**: Check if the property passes validation
- **PropertyMessages**: Access validation error messages
- **RunRules**: Manually trigger validation rules

The wrapper is strongly typed (`IValidateProperty<string>`) for compile-time safety while accessing underlying property metadata.

## PropertyChanged Events

Properties raise two types of change events: standard INotifyPropertyChanged for UI binding and NeatooPropertyChanged for framework-internal coordination.

### INotifyPropertyChanged

The standard PropertyChanged event fires when property values change. This event is consumed by UI frameworks like WPF and Blazor for two-way binding.

Subscribe to PropertyChanged:

<!-- snippet: properties-property-changed -->
```cs
[Fact]
public void PropertyChanged_StandardNotification()
{
    var employee = new PropEmployee(new ValidateBaseServices<PropEmployee>());
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
<!-- endSnippet -->

PropertyChanged behavior:
- Fires after value changes
- Includes property name in event args
- Fires even during Load operations (for UI binding)
- Does not include old/new value comparison
- Fires for meta-properties (IsValid, IsDirty, IsBusy)

UI binding relies on this event to update when properties change.

### NeatooPropertyChanged

NeatooPropertyChanged is Neatoo's internal event for coordinating validation, dirty state, and parent-child relationships. This async event enables complex cascading behaviors.

Subscribe to NeatooPropertyChanged:

<!-- snippet: properties-neatoo-property-changed -->
```cs
[Fact]
public async Task NeatooPropertyChanged_ExtendedNotification()
{
    var order = new PropOrder(new EntityBaseServices<PropOrder>());
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
```cs
[Fact]
public void ChangeReasonUserEdit_NormalPropertyAssignment()
{
    var invoice = new PropInvoice(new ValidateBaseServices<PropInvoice>());
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

    // Validation rules execute with UserEdit
    Assert.True(invoice.IsValid);
}
```
<!-- endSnippet -->

### ChangeReason.Load

Load indicates data loading from persistence or deserialization. LoadValue sets properties without triggering validation or modification tracking.

Use LoadValue during data loading:

<!-- snippet: properties-load-value -->
```cs
[Fact]
public void LoadValue_DataLoadingWithoutRules()
{
    var invoice = new PropInvoice(new ValidateBaseServices<PropInvoice>());

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
<!-- endSnippet -->

LoadValue behavior:
- Validation rules do NOT execute
- Dirty state does NOT cascade to parent
- PropertyChanged fires (for UI binding)
- NeatooPropertyChanged fires with Reason = Load
- Parent-child relationships ARE established (SetParent called on child objects)
- IsModified remains false

LoadValue is essential during factory Fetch operations to load data without marking the entity as modified. It still establishes parent-child structure for validation cascade.

## Meta-Properties

Properties expose meta-properties for querying their state beyond the current value.

Access property metadata:

<!-- snippet: properties-meta-properties -->
```cs
[Fact]
public async Task MetaProperties_QueryPropertyState()
{
    var invoice = new PropInvoice(new ValidateBaseServices<PropInvoice>());

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
<!-- endSnippet -->

Available meta-properties:
- **IsBusy**: True if async operations are pending on this property
- **IsValid**: True if the property and all child objects are valid
- **IsSelfValid**: True if the property itself is valid (ignores child validation)
- **PropertyMessages**: Collection of validation messages
- **Task**: The pending async task (completed task if not busy)
- **IsReadOnly**: True if the property cannot be modified

Meta-properties enable conditional UI rendering, save-enablement logic, and validation feedback.

## Custom Getter Logic

Properties can override the getter to compute values from other properties. Setter behavior remains source-generated.

Implement a computed property:

<!-- snippet: properties-custom-getter -->
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
<!-- endSnippet -->

Custom getter patterns:
- Getter computes value from other properties
- Setter stores the value normally (source-generated)
- Computed properties typically have no setter (read-only)
- Use CallerMemberName to access the correct backing field
- PropertyManager indexer returns the property wrapper

Read-only properties only declare the getter. The source generator respects the partial property signature.

## Read-Only Properties

Properties can be declared read-only by omitting the setter. The source generator creates only the getter implementation.

Declare a read-only property:

<!-- snippet: properties-read-only -->
```cs
[Factory]
public partial class PropContact : ValidateBase<PropContact>
{
    public PropContact(IValidateBaseServices<PropContact> services) : base(services) { }

    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    // Read-only property - only getter implementation generated
    public partial string FullName { get; }
}
```
<!-- endSnippet -->

Read-only properties:
- Do not have setter implementation
- Can still be set via LoadValue during deserialization
- Throw PropertyReadOnlyException if Value setter is called directly
- Are typically computed from other properties
- Have IsReadOnly == true

Read-only properties are common for identity fields and computed values.

## Suppressing Property Events

During batch operations, suppress property change events to improve performance and avoid intermediate validation states.

Pause property events during batch updates:

<!-- snippet: properties-suppress-events -->
```cs
[Fact]
public void SuppressEvents_PauseAllActions()
{
    var invoice = new PropInvoice(new ValidateBaseServices<PropInvoice>());
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
<!-- endSnippet -->

PauseAllActions behavior:
- PropertyChanged events are deferred
- NeatooPropertyChanged events are deferred
- Validation rules do NOT execute
- Dirty state tracking is deferred
- Parent cascade is suppressed

After Resume:
- All deferred events fire
- Validation rules execute for changed properties
- Dirty state recalculates
- Parent cascade resumes

Use PauseAllActions when setting multiple properties during initialization, deserialization, or bulk updates.

## Property Access via Indexer

Properties can be accessed dynamically by name using the indexer syntax.

Access properties by name:

<!-- snippet: properties-indexer-access -->
```cs
[Fact]
public void IndexerAccess_DynamicPropertyAccess()
{
    var employee = new PropEmployee(new ValidateBaseServices<PropEmployee>());
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
```cs
[Fact]
public async Task TaskTracking_AsyncOperations()
{
    var pricingService = new MockPricingService();
    var product = new PropAsyncProduct(
        new ValidateBaseServices<PropAsyncProduct>(),
        pricingService);

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
```cs
[Fact]
public async Task ValidationIntegration_PropertyValidation()
{
    var invoice = new PropInvoice(new ValidateBaseServices<PropInvoice>());

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
```cs
[Fact]
public async Task ChangePropagation_ChildToParent()
{
    var order = new PropOrder(new EntityBaseServices<PropOrder>());
    order.OrderNumber = "ORD-001";

    var receivedEvents = new List<NeatooPropertyChangedEventArgs>();

    order.NeatooPropertyChanged += (args) =>
    {
        receivedEvents.Add(args);
        return Task.CompletedTask;
    };

    // Add child item
    var item = new PropOrderItem(new EntityBaseServices<PropOrderItem>());
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
<!-- endSnippet -->

Cascade behavior:
- Child property changes fire NeatooPropertyChanged
- Parent subscribes to child's NeatooPropertyChanged
- Parent re-fires the event with updated breadcrumb (FullPropertyName)
- Event bubbles to aggregate root
- Root can react to any property change in the entire graph

The FullPropertyName property builds the breadcrumb: "Order.LineItems[2].Quantity".

## Constructor Property Assignment

Properties set in constructors outside of factory methods are tracked as modifications because constructors run before the factory pause mechanism activates.

Avoid constructor property assignment:

<!-- snippet: properties-constructor-assignment -->
```cs
[Fact]
public void ConstructorAssignment_UseLoadValueInstead()
{
    // Avoid setting properties directly in constructors
    // outside of factory methods, as they will be tracked
    // as modifications.

    // Instead, use LoadValue for initial values:
    var employee = new PropEmployee(new ValidateBaseServices<PropEmployee>());

    // LoadValue sets value without triggering modification tracking
    employee["Name"].LoadValue("Default Employee");
    employee["Email"].LoadValue("default@example.com");

    // Properties are set but not marked as modified
    // (In a full EntityBase scenario with MarkUnmodified)
    Assert.Equal("Default Employee", employee.Name);
    Assert.Equal("default@example.com", employee.Email);
}
```
<!-- endSnippet -->

The analyzer warns about constructor assignments and offers a code fix to convert to LoadValue. This ensures new entities start in an unmodified state.

Use LoadValue in constructors when initial values must be set outside of factory Create methods.

---

**UPDATED:** 2026-01-24
