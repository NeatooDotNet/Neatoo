# Validation

[← Remote Factory](remote-factory.md) | [↑ Guides](index.md)

A data-binding UI needs to know *right now* whether the form is valid — which fields have errors, what the messages are, and whether the Save button should be enabled. ValidateBase provides this: every property tracks its own `IsValid` and error messages, and the entity's `IsValid` aggregates the entire object graph. When a user edits a field, validation fires immediately, error messages update, and the UI reflects the new state — all through data-binding, with no manual orchestration. See [Business Rules](business-rules.md) for how to define the rules themselves; this guide covers the validation state and messaging infrastructure.

## ValidateBase Inheritance

Inherit from ValidateBase<T> to enable validation on a domain object. The type parameter uses the curiously recurring template pattern (CRTP) to provide strongly-typed rule registration and property access.

Declare a ValidateBase class:

<!-- snippet: validation-basic -->
<a id='snippet-validation-basic'></a>
```cs
[Factory]
public partial class ValidationCustomer : ValidateBase<ValidationCustomer>
{
    public ValidationCustomer(IValidateBaseServices<ValidationCustomer> services) : base(services) { }

    public partial string Name { get; set; }

    public partial string Email { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L16-L29' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

ValidateBase provides:
- **IsValid**: True if all properties and child objects pass validation
- **IsSelfValid**: True if this object's properties pass validation (ignores children)
- **PropertyMessages**: Collection of validation error messages
- **RuleManager**: Add validation and business rules using fluent API or custom rule classes
- **PropertyManager**: Manages property state, change notifications, and validation
- **PauseAllActions**: Suspends validation during batch updates

ValidateBase classes must have a constructor accepting IValidateBaseServices<T> and pass it to the base constructor.

## Property Declarations

Properties in ValidateBase are declared as partial properties. The BaseGenerator source generator completes the implementation by creating property backing fields and wiring validation integration.

Declare partial properties with validation attributes:

<!-- snippet: validation-properties -->
<a id='snippet-validation-properties'></a>
```cs
[Factory]
public partial class ValidationEmployee : ValidateBase<ValidationEmployee>
{
    public ValidationEmployee(IValidateBaseServices<ValidationEmployee> services) : base(services) { }

    // Partial properties - source generator completes implementation
    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    [Required]
    public partial string Email { get; set; }

    [Range(0, 200)]
    public partial int Age { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L34-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-properties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Property characteristics:
- **Partial keyword**: Required for source generation
- **Public and non-static**: Source generator only implements public instance properties
- **Getter and setter**: Both required unless property is read-only
- **Attributes**: DataAnnotations attributes apply validation rules automatically

See [Properties](properties.md) for details on property implementation and source generation.

## Built-In Validation Attributes

Neatoo integrates with System.ComponentModel.DataAnnotations. The RuleManager scans properties for validation attributes during construction and converts them to rules using the IAttributeToRule service. Attributes like [Required], [MaxLength], [EmailAddress], and [Range] become validation rules that execute when properties change.

Apply DataAnnotations attributes to properties:

<!-- snippet: validation-attributes -->
<a id='snippet-validation-attributes'></a>
```cs
[Factory]
public partial class ValidationContact : ValidateBase<ValidationContact>
{
    public ValidationContact(IValidateBaseServices<ValidationContact> services) : base(services) { }

    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100)]
    public partial string Name { get; set; }

    [EmailAddress]
    public partial string Email { get; set; }

    [Phone]
    public partial string PhoneNumber { get; set; }

    [Range(1, 150, ErrorMessage = "Age must be between 1 and 150")]
    public partial int Age { get; set; }

    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid ZIP code")]
    public partial string ZipCode { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L59-L84' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-attributes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Supported attributes:
- **[Required]**: Property value must be non-null and non-empty
- **[MaxLength(n)]**: String length cannot exceed n characters
- **[MinLength(n)]**: String length must be at least n characters
- **[EmailAddress]**: String must be valid email format
- **[Phone]**: String must be valid phone number format
- **[Range(min, max)]**: Numeric value must be within range
- **[RegularExpression(pattern)]**: String must match regex pattern
- **[StringLength(max, MinimumLength = min)]**: String length constraints
- **[Url]**: String must be valid URL format

Attribute validation rules execute automatically when the property changes. Error messages appear in PropertyMessages and IsValid reflects the validation state.

## Custom Validation Rules

Register custom validation rules in the constructor using RuleManager.AddValidation. The fluent API creates a ValidationFluentRule internally that executes your validation lambda and associates error messages with the trigger property.

Add a custom validation rule:

<!-- snippet: validation-custom-rule -->
<a id='snippet-validation-custom-rule'></a>
```cs
public ValidationInvoice(IValidateBaseServices<ValidationInvoice> services) : base(services)
{
    // Custom validation rule: Amount must be positive
    RuleManager.AddValidation(
        invoice => invoice.Amount > 0 ? "" : "Amount must be greater than zero",
        i => i.Amount);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L92-L100' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-custom-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Validation rule patterns:
- **Lambda expression**: Receives the entity instance, returns error message or empty string
- **Trigger property**: Second parameter specifies which property triggers the rule when it changes
- **Error message**: Non-empty string indicates validation failure, empty/null indicates success
- **Automatic association**: Error messages are automatically associated with the trigger property

Rules execute when the trigger property changes. The RuleManager assigns each rule a stable ID and tracks execution order.

## Cross-Property Validation

Custom rule classes inheriting from RuleBase<T> or AsyncRuleBase<T> can declare multiple trigger properties in their constructor. The rule executes when any trigger property changes, enabling cross-property constraints like "end date must be after start date".

Register cross-property validation:

<!-- snippet: validation-cross-property -->
<a id='snippet-validation-cross-property'></a>
```cs
public ValidationDateRange(IValidateBaseServices<ValidationDateRange> services) : base(services)
{
    // Cross-property rule: EndDate must be after StartDate
    // Triggers when either StartDate OR EndDate changes
    RuleManager.AddRule(new ValidationDateRangeRule());
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L136-L143' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-cross-property' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Cross-property patterns:
- **Multiple trigger properties**: Rule constructor specifies which properties trigger the rule
- **Dependency tracking**: Rule executes when ANY trigger property changes
- **Execution order**: Rules execute based on RuleOrder property (lower values first)
- **Custom rule classes**: Inherit from RuleBase<T> for sync or AsyncRuleBase<T> for async validation

Cross-property rules ensure aggregate-level invariants hold as properties change. See [Business Rules](business-rules.md) for custom rule implementation details.

## Async Validation Rules

Many real-world validations can't be checked locally — email uniqueness requires a database query, inventory availability needs a service call, credit checks hit an external API. These calls must be async to keep the UI responsive. Because Neatoo assumes a data-binding UI, the result naturally flows back: when the async rule completes, `IsValid` and `PropertyMessages` update, and the UI reflects the new state through data-binding.

Add async validation rule:

<!-- snippet: validation-async-rule -->
<a id='snippet-validation-async-rule'></a>
```cs
public ValidationUser(
    IValidateBaseServices<ValidationUser> services,
    IValidationUniquenessService uniquenessService) : base(services)
{
    // Async validation rule: Check email uniqueness
    RuleManager.AddValidationAsync(
        async user =>
        {
            if (string.IsNullOrEmpty(user.Email))
                return "";

            var isUnique = await uniquenessService.IsEmailUniqueAsync(user.Email);
            return isUnique ? "" : "Email is already in use";
        },
        u => u.Email);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L176-L193' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-async-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Async validation behavior:
- **Task-returning lambda**: Validation executes asynchronously
- **IsBusy tracking**: RuleManager marks trigger properties as busy until validation completes
- **Property.Task**: Access the pending task for the property (or Task.CompletedTask if not busy)
- **WaitForTasks()**: Await all pending validation tasks before saving
- **Parent cascade**: Child tasks propagate up via AddChildTask for aggregate-level coordination

When an async rule executes, RuleManager marks all trigger properties as busy using a unique execution ID. After the rule completes, the same ID is used to clear the busy state, ensuring multiple concurrent rules don't interfere with each other's tracking.

## Manual Validation Execution

Manually trigger validation using RunRules methods. This is useful after batch updates, during save operations, or to re-validate after external state changes.

Run validation manually:

<!-- snippet: validation-run-rules -->
<a id='snippet-validation-run-rules'></a>
```cs
[Fact]
public async Task RunRulesManually_RevalidateEntity()
{
    var factory = GetRequiredService<IValidationOrderFactory>();
    var order = factory.Create();

    // Set invalid values
    order.Quantity = -5;
    order.UnitPrice = -10;

    // Validation runs automatically on property set
    Assert.False(order.IsValid);

    // Fix values
    order.Quantity = 10;
    order.UnitPrice = 25.00m;

    // Manually run all rules (clears messages and re-validates)
    await order.RunRules(RunRulesFlag.All);

    Assert.True(order.IsValid);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L610-L633' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-run-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

RunRules overloads:
- **RunRules(propertyName)**: Execute rules dependent on a specific property
- **RunRules(RunRulesFlag.All)**: Clear all messages and run all rules
- **RunRules(RunRulesFlag.Self)**: Run only this object's rules (not children)
- **RunRules(RunRulesFlag.Children)**: Run only child object rules
- **RunRules(flag, token)**: Run with cancellation token support

RunRulesFlag.All clears existing validation messages before running rules, providing a clean validation state. Other flags preserve existing messages and add new ones.

## Error Messages and Metadata

Validation error messages are stored in PropertyMessages. Each message identifies the property that failed validation and the error text. Access messages through the PropertyMessages collection or individual property wrappers.

Access validation messages:

<!-- snippet: validation-error-messages -->
<a id='snippet-validation-error-messages'></a>
```cs
[Fact]
public void AccessValidationMessages_PropertyAndObject()
{
    var factory = GetRequiredService<IValidationProductFactory>();
    var product = factory.Create();

    // Trigger validation failures
    product.Name = "";
    product.Price = -50;

    // Access all messages on the object
    Assert.True(product.PropertyMessages.Any());

    // Filter messages by property
    var nameMessages = product.PropertyMessages
        .Where(m => m.Property.Name == "Name")
        .ToList();
    Assert.NotEmpty(nameMessages);

    // Access property-specific messages via indexer
    var priceProperty = product["Price"];
    Assert.NotEmpty(priceProperty.PropertyMessages);
    Assert.Contains(priceProperty.PropertyMessages, m => m.Message.Contains("negative"));
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L635-L660' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-error-messages' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Message metadata:
- **Property**: The IValidateProperty that failed validation (access name via `Property.Name`)
- **Message**: The error message text
- **PropertyMessages**: Collection of all messages across the object
- **Property.PropertyMessages**: Messages specific to one property

UI can bind to PropertyMessages to display validation feedback. Individual property messages enable field-level error display.

## Property-Level Validation State

Each property tracks its own validation state independently. Access property validation metadata through the property wrapper returned by the indexer.

Check property validation state:

<!-- snippet: validation-property-state -->
<a id='snippet-validation-property-state'></a>
```cs
[Fact]
public async Task PropertyValidationState_IndividualPropertyTracking()
{
    // Factory resolves ValidationAccount with IValidationUniquenessService injected
    var factory = GetRequiredService<IValidationAccountFactory>();
    var account = factory.Create();

    // Set valid account number
    account.AccountNumber = "ACC-001";

    // Check individual property state
    var accountNumberProperty = account["AccountNumber"];
    Assert.True(accountNumberProperty.IsValid);
    Assert.True(accountNumberProperty.IsSelfValid);
    Assert.Empty(accountNumberProperty.PropertyMessages);

    // Trigger async validation on email
    account.Email = "taken@example.com";

    // Wait for async validation
    await account.WaitForTasks();

    var emailProperty = account["Email"];
    Assert.False(emailProperty.IsValid);
    Assert.NotEmpty(emailProperty.PropertyMessages);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L662-L689' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-property-state' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Property validation metadata:
- **IsValid**: True if the property and all child objects are valid
- **IsSelfValid**: True if the property itself is valid (ignores child validation)
- **PropertyMessages**: Collection of messages for this property
- **IsBusy**: True if async validation is running on this property
- **Task**: The pending validation task (CompletedTask if not busy)

Property-level state enables granular validation feedback and selective validation execution.

## Object-Level Validation

Not every validation failure maps to a specific property. A payment gateway might reject an entire transaction. A server-side business rule in an Insert method might catch a constraint that spans multiple fields. `MarkInvalid` handles these cases — it marks the whole object as invalid with an error message that isn't tied to any one property.

Mark object as invalid:

<!-- snippet: validation-object-invalid -->
<a id='snippet-validation-object-invalid'></a>
```cs
[Fact]
public void MarkObjectInvalid_ObjectLevelValidation()
{
    var factory = GetRequiredService<IValidationTransactionFactory>();
    var transaction = factory.Create();
    transaction.TransactionId = "TXN-001";
    transaction.Amount = 100;

    // Initially valid
    Assert.True(transaction.IsValid);

    // Mark as invalid due to external validation failure
    transaction.MarkTransactionInvalid("Transaction rejected by payment gateway");

    // Object is now invalid
    Assert.False(transaction.IsValid);

    // Error message appears in PropertyMessages
    Assert.Contains(transaction.PropertyMessages,
        m => m.Message.Contains("rejected by payment gateway"));

    // ObjectInvalid property contains the message
    Assert.Equal("Transaction rejected by payment gateway", transaction.ObjectInvalid);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L691-L716' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-object-invalid' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Object-level validation patterns:
- **MarkInvalid(message)**: Permanently mark object invalid with error message
- **ObjectInvalid property**: Stores the object-level error message
- **RunRules(RunRulesFlag.All)**: Clears ObjectInvalid and re-validates
- **Aggregate-level rules**: Use AddValidation without property dependencies for object-level rules

Object-level validation captures errors that span multiple properties or depend on external state.

## PauseAllActions for Batching

Pause validation during batch property updates to avoid intermediate validation states and improve performance. PauseAllActions suspends rule execution, property change events, and dirty tracking until Resume is called.

Batch property updates without validation:

<!-- snippet: validation-pause-actions -->
<a id='snippet-validation-pause-actions'></a>
```cs
[Fact]
public void PauseAllActions_BatchUpdatesWithoutValidation()
{
    var factory = GetRequiredService<IValidationOrderFactory>();
    var order = factory.Create();

    // Pause validation during batch updates
    using (order.PauseAllActions())
    {
        // These assignments do NOT trigger validation rules
        order.ProductCode = "PROD-001";
        order.Quantity = 10;
        order.UnitPrice = 25.00m;

        // IsPaused is true during the using block
        Assert.True(order.IsPaused);
    }

    // After resume (automatic when using block ends):
    // - Validation rules execute for changed properties
    // - PropertyChanged events fire
    Assert.False(order.IsPaused);
    Assert.Equal("PROD-001", order.ProductCode);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L718-L743' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-pause-actions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

PauseAllActions behavior:
- **IsPaused = true**: Validation rules do NOT execute
- **Deferred events**: PropertyChanged and NeatooPropertyChanged are queued
- **Deferred dirty tracking**: IsDirty calculation is suspended
- **Automatic resume**: Disposing the returned IDisposable calls ResumeAllActions
- **Event catchup**: All deferred events fire when resumed
- **Validation execution**: Rules run for changed properties after resume

Use PauseAllActions during data loading (factory Fetch), deserialization, or bulk property assignment. The using pattern ensures Resume is called even if exceptions occur.

## Validation and Change Tracking Integration

Validation integrates with change tracking. Properties only trigger validation when changed via UserEdit (not Load). LoadValue assigns property values without executing validation rules, enabling data loading without false validation errors.

Load data without triggering validation:

<!-- snippet: validation-load-value -->
<a id='snippet-validation-load-value'></a>
```cs
[Fact]
public void LoadValue_DataLoadingWithoutValidation()
{
    var factory = GetRequiredService<IValidationInvoiceFactory>();
    var invoice = factory.Create();

    // LoadValue sets property without triggering validation
    // Typically used during Fetch factory operations
    invoice["Amount"].LoadValue(-100m); // Would fail validation rule
    invoice["CustomerName"].LoadValue("Test Customer");

    // Value is set but validation rule did not execute
    Assert.Equal(-100m, invoice.Amount);

    // Property is not marked as invalid until rules run
    // (In real usage, factory method would call RunRules after loading)
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L745-L763' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-load-value' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

ChangeReason integration:
- **ChangeReason.UserEdit**: Normal property assignment, triggers validation
- **ChangeReason.Load**: LoadValue assignment, skips validation
- **Factory Fetch**: Uses PauseAllActions + direct assignment (equivalent to Load)
- **Deserialization**: Uses PauseAllActions to prevent validation during JSON deserialization

See [Properties](properties.md) for details on ChangeReason and LoadValue.

## Validation Cascade

Validation state cascades from child objects to parents. When a child object becomes invalid, the parent's IsValid becomes false. This ensures aggregate roots reflect the validity of the entire object graph.

Validation cascade behavior:

<!-- snippet: validation-cascade -->
<a id='snippet-validation-cascade'></a>
```cs
[Fact]
public void ValidationCascade_ChildToParent()
{
    var invoiceFactory = GetRequiredService<IValidationInvoiceWithItemsFactory>();
    var invoice = invoiceFactory.Create();
    invoice.InvoiceNumber = "INV-001";

    // Parent starts valid
    Assert.True(invoice.IsValid);

    // Add invalid child (empty description)
    var lineItemFactory = GetRequiredService<IValidationLineItemFactory>();
    var lineItem = lineItemFactory.Create();
    lineItem.Description = ""; // Triggers validation failure
    invoice.LineItems.Add(lineItem);

    // Parent's IsValid reflects child's invalid state
    Assert.False(invoice.IsValid);

    // Parent's IsSelfValid ignores children
    Assert.True(invoice.IsSelfValid);

    // Fix child
    lineItem.Description = "Valid description";

    // Parent is valid again
    Assert.True(invoice.IsValid);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L765-L794' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-cascade' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Cascade characteristics:
- **Parent IsValid**: False if any child is invalid
- **Parent IsSelfValid**: Only reflects parent's own properties, ignores children
- **Automatic propagation**: Child validation changes update parent immediately
- **Parent-child structure**: Established through property assignment (SetParent)
- **NeatooPropertyChanged**: Bubbles up the parent chain with validation state changes

See [Parent-Child](parent-child.md) for details on parent-child relationship establishment and cascade behavior.

## Meta-Properties

ValidateBase exposes meta-properties that track validation state across the object graph.

Validation meta-properties:

<!-- snippet: validation-meta-properties -->
<a id='snippet-validation-meta-properties'></a>
```cs
[Fact]
public async Task MetaProperties_TrackValidationState()
{
    var factory = GetRequiredService<IValidationAccountFactory>();
    var account = factory.Create();

    // Set required field
    account.AccountNumber = "ACC-001";

    // IsValid: True if all properties and children pass validation
    Assert.True(account.IsValid);

    // IsSelfValid: True if this object's properties pass (ignores children)
    Assert.True(account.IsSelfValid);

    // Trigger async validation
    account.Email = "taken@example.com";

    // IsBusy: True while async validation runs
    // (may be false if validation completes very fast)

    // Wait for async completion
    await account.WaitForTasks();

    // PropertyMessages: All validation errors
    Assert.NotEmpty(account.PropertyMessages);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L796-L824' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-meta-properties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Meta-property definitions:
- **IsValid**: True if this object and ALL child objects pass validation
- **IsSelfValid**: True if this object's properties pass validation (ignores children)
- **IsBusy**: True if any async validation tasks are running
- **PropertyMessages**: Collection of ALL validation messages (this object + children)
- **IsSavable**: (EntityBase only) True if IsModified && IsValid && !IsBusy && !IsChild

Meta-properties fire PropertyChanged events when their values change, enabling UI binding for save button enablement and validation indicators.

## Validation During Save

Validate entities before persisting changes. The IsSavable property (EntityBase only) combines IsModified, IsValid, IsBusy, and IsChild to determine if the entity can be saved.

Validate before save:

<!-- snippet: validation-before-save -->
<a id='snippet-validation-before-save'></a>
```cs
[Fact]
public async Task ValidateBeforeSave_IsSavableCheck()
{
    // Use factory to create new entity with proper lifecycle
    var factory = GetRequiredService<IValidationSaveableOrderFactory>();
    var order = factory.Create();

    // Set invalid quantity (negative to trigger validation)
    order.Quantity = -5;

    // IsSavable combines IsModified, IsValid, and IsBusy
    Assert.False(order.IsSavable); // Invalid due to negative quantity

    // Fix the value
    order.Quantity = 5;

    // Re-run all rules before save
    await order.RunRules(RunRulesFlag.All);

    // Now savable
    Assert.True(order.IsValid);
    Assert.True(order.IsModified);
    Assert.False(order.IsBusy);
    Assert.True(order.IsSavable);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L826-L852' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-before-save' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Save validation patterns:
- **Check IsSavable**: Ensures entity is valid and not busy
- **RunRules(RunRulesFlag.All)**: Clear messages and re-validate before save
- **Await WaitForTasks()**: Ensure async validation completes before save
- **Return null on failure**: Factory Save methods return null if validation fails
- **UI save button**: Bind IsEnabled to IsSavable

Validation prevents invalid state from being persisted. Async validation must complete before save operations execute.

## Cancellation Token Support

If a user navigates away from a form, there's no point finishing a database uniqueness check for an abandoned page. If a user types quickly through a field, each keystroke might trigger async validation — but only the last one matters. Cancellation tokens let you abort superseded or abandoned validation work.

Use cancellation tokens with validation:

<!-- snippet: validation-cancellation -->
<a id='snippet-validation-cancellation'></a>
```cs
[Fact]
public async Task CancellationToken_CancelAsyncValidation()
{
    var factory = GetRequiredService<IValidationAsyncOrderFactory>();
    var order = factory.Create();

    // Set product code to trigger async validation
    order.ProductCode = "PROD-001";

    // Run with cancellation token
    var cts = new CancellationTokenSource();

    // In real scenario, would cancel during long-running validation
    // cts.Cancel();

    await order.RunRules(RunRulesFlag.All, cts.Token);

    // Validation completed without cancellation
    Assert.True(order.IsValid);

    // If cancellation occurred, object would be marked invalid:
    // Assert.False(order.IsValid);
    // Assert.Equal("Validation cancelled", order.ObjectInvalid);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L854-L879' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Cancellation behavior:
- **AddValidationAsync with token**: Pass CancellationToken to async rule lambda
- **RunRules(flag, token)**: Cancels validation if token is triggered
- **OperationCanceledException**: Thrown when cancellation occurs
- **MarkInvalid("Validation cancelled")**: Object marked invalid on cancellation
- **Re-validation required**: Must call RunRules(RunRulesFlag.All) to clear cancellation state

Cancellation is useful for long-running validation operations like database queries or external service calls.

## Validation Rule Execution Order

Rules execute based on their trigger property matching and RuleOrder property (lower values execute first, default is 1). When a property changes, the RuleManager identifies all rules with matching trigger properties and executes them sorted by RuleOrder.

Rule execution flow:
1. Property value changes (via setter)
2. RuleManager.RunRules(propertyName) is called
3. Rules with trigger properties matching propertyName are selected
4. Selected rules are sorted by RuleOrder (ascending)
5. Each rule executes sequentially (even async rules wait for previous rule to complete)
6. Rule messages are applied to properties via SetMessagesForRule
7. IsValid and IsSelfValid recalculate based on PropertyMessages
8. PropertyChanged events fire for meta-properties
9. Validation state cascades to parent via NeatooPropertyChanged

Synchronous rules (AddValidation, RuleBase) complete immediately. Async rules (AddValidationAsync, AsyncRuleBase) mark properties as IsBusy during execution and complete when the Task resolves.

## Validation Messages Collection

PropertyMessages contains all validation errors across the object graph. Filter messages by property name to provide targeted feedback.

Work with validation messages:

<!-- snippet: validation-messages-collection -->
<a id='snippet-validation-messages-collection'></a>
```cs
[Fact]
public async Task WorkWithValidationMessages_FilterAndAccess()
{
    var factory = GetRequiredService<IValidationProductFactory>();
    var product = factory.Create();

    // Trigger multiple validation failures
    product.Name = "";
    product.Price = -25;

    await product.WaitForTasks();

    // PropertyMessages contains all errors
    var allMessages = product.PropertyMessages.ToList();
    Assert.Equal(2, allMessages.Count);

    // Filter by property name
    var nameErrors = product.PropertyMessages
        .Where(m => m.Property.Name == "Name")
        .ToList();
    Assert.Single(nameErrors);

    // Clear messages and re-validate
    product.ClearAllMessages();
    Assert.Empty(product.PropertyMessages);

    // Run rules to repopulate messages
    await product.RunRules(RunRulesFlag.All);
    Assert.Equal(2, product.PropertyMessages.Count);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L881-L912' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-messages-collection' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Message collection operations:
- **PropertyMessages**: Read-only collection of all messages
- **Filter by property**: `messages.Where(m => m.Property.Name == "Name")`
- **Clear messages**: Use ClearAllMessages() or ClearSelfMessages()
- **Object-level messages**: `messages.Where(m => m.Property.Name == "ObjectInvalid")`

PropertyMessages updates automatically as validation rules execute and properties change.

## Combining Attributes and Custom Rules

DataAnnotations attributes and custom rules work together. Attributes provide standard validation (required, length, format), while custom rules implement business logic and cross-property constraints.

Combine attributes and custom rules:

<!-- snippet: validation-combined -->
<a id='snippet-validation-combined'></a>
```cs
[Factory]
public partial class ValidationRegistration : ValidateBase<ValidationRegistration>
{
    public ValidationRegistration(IValidateBaseServices<ValidationRegistration> services) : base(services)
    {
        // Custom cross-property rule: ConfirmPassword must match Password
        RuleManager.AddRule(new PasswordMatchRule());

        // Custom business rule: Username cannot be "admin"
        RuleManager.AddValidation(
            r => r.Username?.ToLower() != "admin" ? "" : "Username 'admin' is reserved",
            r => r.Username);
    }

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public partial string Username { get; set; }

    [Required]
    [EmailAddress]
    public partial string Email { get; set; }

    [Required]
    [MinLength(8)]
    public partial string Password { get; set; }

    [Required]
    public partial string ConfirmPassword { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L383-L416' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-combined' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Combined validation patterns:
- **Attributes for standard constraints**: Required, MaxLength, EmailAddress, Range
- **Custom rules for business logic**: Cross-property validation, business invariants
- **Async rules for external validation**: Database lookups, service calls
- **All rules execute**: Both attribute and custom rules run on property changes
- **Multiple error messages**: One property can have multiple validation failures

Layer validation from simple (attributes) to complex (custom rules) to build comprehensive validation.

---

**UPDATED:** 2026-02-28
