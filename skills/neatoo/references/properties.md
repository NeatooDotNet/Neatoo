# Properties

All Neatoo properties use `Getter<T>()` and `Setter()` methods for automatic change tracking, validation triggering, and property change notifications.

## Basic Property Declaration

Declare properties as partial with `Getter<T>()` and `Setter()`:

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
<sup><a href='/src/docs/samples/PropertiesSamples.cs#L234-L254' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-generated-implementation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Read-Only Properties

For calculated or read-only properties, use only `Getter<T>()`:

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

## Custom Getter Logic

Add custom logic in the getter while still using the backing field:

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

## Property Change Notifications

Properties automatically raise `PropertyChanged` and Neatoo-specific events:

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
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Tests/TestingPatternsTests.cs#L473-L495' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-property-changed' title='Start of snippet'>anchor</a></sup>
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
<!-- snippet: properties-neatoo-property-changed -->
<!-- endSnippet -->

## Loading Values Without Triggering Rules

Use `LoadProperty()` to set values without triggering validation or marking dirty:

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
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Tests/TestingPatternsTests.cs#L524-L543' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-load-value' title='Start of snippet'>anchor</a></sup>
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

## Suppressing Events

Temporarily suppress property change events during bulk operations:

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
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Tests/TestingPatternsTests.cs#L599-L629' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-suppress-events' title='Start of snippet'>anchor</a></sup>
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

## Meta Properties

Access property metadata for validation state, dirty state, etc.:

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
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Tests/TestingPatternsTests.cs#L545-L574' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-meta-properties' title='Start of snippet'>anchor</a></sup>
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

## Direct Backing Field Access

Access the backing field directly when needed:

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
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Tests/TestingPatternsTests.cs#L576-L597' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-backing-field-access' title='Start of snippet'>anchor</a></sup>
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

## Change Reason Tracking

Neatoo tracks why a property changed (user edit, rule, load):

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
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Tests/TestingPatternsTests.cs#L631-L656' title='Snippet source file'>snippet source</a> | <a href='#snippet-properties-change-reason-useredit' title='Start of snippet'>anchor</a></sup>
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

## Related

- [Validation](validation.md) - How property changes trigger validation
- [Change Tracking](entities.md#change-tracking) - IsModified and modification state
- [Base Classes](base-classes.md) - Which base classes support properties
