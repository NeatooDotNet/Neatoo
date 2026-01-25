# Validation

[← Remote Factory](remote-factory.md) | [↑ Guides](index.md)

ValidateBase provides the core validation infrastructure for Neatoo domain objects. Validation rules execute automatically when properties change, collecting error messages and tracking validity state across the object graph. The validation system integrates with DataAnnotations attributes, supports custom synchronous and asynchronous rules, and coordinates validation across parent-child relationships.

## ValidateBase Inheritance

Inherit from ValidateBase<T> to enable validation on a domain object. The type parameter uses the curiously recurring template pattern (CRTP) to provide strongly-typed rule registration and property access.

Declare a ValidateBase class:

<!-- snippet: validation-basic -->
```cs
[Factory]
public partial class ValidationCustomer : ValidateBase<ValidationCustomer>
{
    public ValidationCustomer(IValidateBaseServices<ValidationCustomer> services) : base(services) { }

    public partial string Name { get; set; }

    public partial string Email { get; set; }
}
```
<!-- endSnippet -->

ValidateBase provides:
- **IsValid**: True if all properties and child objects pass validation
- **IsSelfValid**: True if this object's properties pass validation (ignores children)
- **PropertyMessages**: Collection of validation error messages
- **RuleManager**: Registers validation rules in the constructor
- **PropertyManager**: Manages property state, change notifications, and validation
- **PauseAllActions**: Suspends validation during batch updates

ValidateBase classes must have a constructor accepting IValidateBaseServices<T> and pass it to the base constructor.

## Property Declarations

Properties in ValidateBase are declared as partial properties. The BaseGenerator source generator completes the implementation by creating property backing fields and wiring validation integration.

Declare partial properties with validation attributes:

<!-- snippet: validation-properties -->
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
}
```
<!-- endSnippet -->

Property characteristics:
- **Partial keyword**: Required for source generation
- **Public and non-static**: Source generator only implements public instance properties
- **Getter and setter**: Both required unless property is read-only
- **Attributes**: DataAnnotations attributes apply validation rules automatically

See [Properties](properties.md) for details on property implementation and source generation.

## Built-In Validation Attributes

Neatoo integrates with System.ComponentModel.DataAnnotations. Attributes like [Required], [MaxLength], [EmailAddress], and [Range] automatically generate validation rules that execute when properties change.

Apply DataAnnotations attributes to properties:

<!-- snippet: validation-attributes -->
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
}
```
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

Register custom validation rules in the constructor using RuleManager.AddValidation. Validation rules are lambda expressions that return an error message (non-empty string indicates failure) or empty string for success.

Add a custom validation rule:

<!-- snippet: validation-custom-rule -->
```cs
public ValidationInvoice(IValidateBaseServices<ValidationInvoice> services) : base(services)
{
    // Custom validation rule: Amount must be positive
    RuleManager.AddValidation(
        invoice => invoice.Amount > 0 ? "" : "Amount must be greater than zero",
        i => i.Amount);
}
```
<!-- endSnippet -->

Validation rule patterns:
- **Lambda expression**: Receives the entity instance, returns error message or empty string
- **Dependencies**: Second parameter specifies which properties trigger the rule
- **Error message**: Non-empty string indicates validation failure
- **Multiple dependencies**: List all properties the rule depends on

Rules execute when dependent properties change. The rule manager tracks dependencies and ensures rules run in the correct order.

## Cross-Property Validation

Validation rules can depend on multiple properties. The rule executes when any dependent property changes, enabling cross-property constraints like "end date must be after start date".

Register cross-property validation:

<!-- snippet: validation-cross-property -->
```cs
public ValidationDateRange(IValidateBaseServices<ValidationDateRange> services) : base(services)
{
    // Cross-property rule: EndDate must be after StartDate
    // Triggers when either StartDate OR EndDate changes
    RuleManager.AddRule(new ValidationDateRangeRule());
}
```
<!-- endSnippet -->

Cross-property patterns:
- **Multiple dependencies**: Pass multiple property expressions to the rule
- **Dependency tracking**: Rule executes when ANY dependent property changes
- **Execution order**: Rules execute in registration order
- **Cascading validation**: Changes to one property can trigger validation of other properties

Cross-property rules ensure aggregate-level invariants hold as properties change.

## Async Validation Rules

Validation rules can execute asynchronously using RuleManager.AddValidationAsync. Async rules enable scenarios like database uniqueness checks, external service calls, and I/O-bound validation.

Add async validation rule:

<!-- snippet: validation-async-rule -->
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
<!-- endSnippet -->

Async validation behavior:
- **Task-returning lambda**: Validation executes asynchronously
- **IsBusy tracking**: Property and entity are busy until validation completes
- **Property.Task**: Access the pending task for the property
- **WaitForTasks()**: Await all pending validation tasks
- **Parent cascade**: Async tasks propagate up to parent for aggregate-level coordination

Async rules return immediately when triggered. The entity tracks running tasks through IsBusy and Task properties.

## RunRulesAsync

Manually trigger validation using RunRules methods. This is useful after batch updates, during save operations, or to re-validate after external state changes.

Run validation manually:

<!-- snippet: validation-run-rules -->
```cs
[Fact]
public async Task RunRulesManually_RevalidateEntity()
{
    var order = new ValidationOrder(new ValidateBaseServices<ValidationOrder>());

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
```cs
[Fact]
public void AccessValidationMessages_PropertyAndObject()
{
    var product = new ValidationProduct(new ValidateBaseServices<ValidationProduct>());

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
<!-- endSnippet -->

Message metadata:
- **PropertyName**: Name of the property that failed validation
- **Message**: The error message text
- **Severity**: Information, Warning, or Error (extensible)
- **PropertyMessages**: Collection of all messages across the object
- **Property.PropertyMessages**: Messages specific to one property

UI can bind to PropertyMessages to display validation feedback. Individual property messages enable field-level error display.

## Property-Level Validation State

Each property tracks its own validation state independently. Access property validation metadata through the property wrapper returned by the indexer.

Check property validation state:

<!-- snippet: validation-property-state -->
```cs
[Fact]
public async Task PropertyValidationState_IndividualPropertyTracking()
{
    var uniquenessService = new MockUniquenessService();
    var account = new ValidationAccount(
        new ValidateBaseServices<ValidationAccount>(),
        uniquenessService);

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
<!-- endSnippet -->

Property validation metadata:
- **IsValid**: True if the property and all child objects are valid
- **IsSelfValid**: True if the property itself is valid (ignores child validation)
- **PropertyMessages**: Collection of messages for this property
- **IsBusy**: True if async validation is running on this property
- **Task**: The pending validation task (CompletedTask if not busy)

Property-level state enables granular validation feedback and selective validation execution.

## Object-Level Validation

Mark the entire object as invalid using MarkInvalid when validation errors exist at the aggregate level rather than individual properties. Object-level errors appear in PropertyMessages with propertyName equal to "ObjectInvalid".

Mark object as invalid:

<!-- snippet: validation-object-invalid -->
```cs
[Fact]
public void MarkObjectInvalid_ObjectLevelValidation()
{
    var transaction = new ValidationTransaction(new ValidateBaseServices<ValidationTransaction>());
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
```cs
[Fact]
public void PauseAllActions_BatchUpdatesWithoutValidation()
{
    var order = new ValidationOrder(new ValidateBaseServices<ValidationOrder>());

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
```cs
[Fact]
public void LoadValue_DataLoadingWithoutValidation()
{
    var invoice = new ValidationInvoice(new ValidateBaseServices<ValidationInvoice>());

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
```cs
[Fact]
public void ValidationCascade_ChildToParent()
{
    var invoice = new ValidationInvoiceWithItems(new ValidateBaseServices<ValidationInvoiceWithItems>());
    invoice.InvoiceNumber = "INV-001";

    // Parent starts valid
    Assert.True(invoice.IsValid);

    // Add invalid child (empty description)
    var lineItem = new ValidationLineItem(new ValidateBaseServices<ValidationLineItem>());
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
```cs
[Fact]
public async Task MetaProperties_TrackValidationState()
{
    var uniquenessService = new MockUniquenessService();
    var account = new ValidationAccount(
        new ValidateBaseServices<ValidationAccount>(),
        uniquenessService);

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
<!-- endSnippet -->

Meta-property definitions:
- **IsValid**: True if this object and ALL child objects pass validation
- **IsSelfValid**: True if this object's properties pass validation (ignores children)
- **IsBusy**: True if any async validation tasks are running
- **PropertyMessages**: Collection of ALL validation messages (this object + children)
- **IsSavable**: (EntityBase only) True if IsValid && !IsBusy

Meta-properties fire PropertyChanged events when their values change, enabling UI binding for save button enablement and validation indicators.

## Validation During Save

Validate entities before persisting changes. The IsSavable property (EntityBase only) combines IsValid and IsBusy to determine if the entity can be saved.

Validate before save:

<!-- snippet: validation-before-save -->
```cs
[Fact]
public async Task ValidateBeforeSave_IsSavableCheck()
{
    var order = new ValidationSaveableOrder(new EntityBaseServices<ValidationSaveableOrder>());
    order.DoMarkNew(); // Mark as new entity needing insert

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
<!-- endSnippet -->

Save validation patterns:
- **Check IsSavable**: Ensures entity is valid and not busy
- **RunRules(RunRulesFlag.All)**: Clear messages and re-validate before save
- **Await WaitForTasks()**: Ensure async validation completes before save
- **Return null on failure**: Factory Save methods return null if validation fails
- **UI save button**: Bind IsEnabled to IsSavable

Validation prevents invalid state from being persisted. Async validation must complete before save operations execute.

## Cancellation Token Support

Validation rules support cancellation through CancellationToken. Canceled validation marks the object invalid with "Validation cancelled" message. Re-validate with RunRules(RunRulesFlag.All) to clear the cancellation state.

Use cancellation tokens with validation:

<!-- snippet: validation-cancellation -->
```cs
[Fact]
public async Task CancellationToken_CancelAsyncValidation()
{
    var inventoryService = new MockInventoryService();
    var order = new ValidationAsyncOrder(
        new ValidateBaseServices<ValidationAsyncOrder>(),
        inventoryService);

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
<!-- endSnippet -->

Cancellation behavior:
- **AddValidationAsync with token**: Pass CancellationToken to async rule lambda
- **RunRules(flag, token)**: Cancels validation if token is triggered
- **OperationCanceledException**: Thrown when cancellation occurs
- **MarkInvalid("Validation cancelled")**: Object marked invalid on cancellation
- **Re-validation required**: Must call RunRules(RunRulesFlag.All) to clear cancellation state

Cancellation is useful for long-running validation operations like database queries or external service calls.

## Validation Rule Execution Order

Validation rules execute in registration order. Rules registered first run before rules registered later. Cross-property rules execute when any dependent property changes.

Rule execution sequence:
1. Property value changes (via setter)
2. DataAnnotations attribute rules execute for that property
3. Custom rules dependent on that property execute (in registration order)
4. Cross-property rules dependent on that property execute (in registration order)
5. Parent's rules that depend on child state execute (if applicable)
6. IsValid and IsSelfValid recalculate
7. PropertyChanged events fire for meta-properties
8. Validation state cascades to parent

Rule execution is synchronous unless AddValidationAsync is used. Async rules return immediately and track completion via Task properties.

## Validation Messages Collection

PropertyMessages contains all validation errors across the object graph. Filter messages by property name or severity to provide targeted feedback.

Work with validation messages:

<!-- snippet: validation-messages-collection -->
```cs
[Fact]
public async Task WorkWithValidationMessages_FilterAndAccess()
{
    var product = new ValidationProduct(new ValidateBaseServices<ValidationProduct>());

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
<!-- endSnippet -->

Message collection operations:
- **PropertyMessages**: Read-only collection of all messages
- **Filter by property**: `messages.Where(m => m.PropertyName == "Name")`
- **Filter by severity**: `messages.Where(m => m.Severity == MessageSeverity.Error)`
- **Clear messages**: Use ClearAllMessages() or ClearSelfMessages()
- **Object-level messages**: `messages.Where(m => m.PropertyName == "ObjectInvalid")`

PropertyMessages updates automatically as validation rules execute and properties change.

## Combining Attributes and Custom Rules

DataAnnotations attributes and custom rules work together. Attributes provide standard validation (required, length, format), while custom rules implement business logic and cross-property constraints.

Combine attributes and custom rules:

<!-- snippet: validation-combined -->
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
}
```
<!-- endSnippet -->

Combined validation patterns:
- **Attributes for standard constraints**: Required, MaxLength, EmailAddress, Range
- **Custom rules for business logic**: Cross-property validation, business invariants
- **Async rules for external validation**: Database lookups, service calls
- **All rules execute**: Both attribute and custom rules run on property changes
- **Multiple error messages**: One property can have multiple validation failures

Layer validation from simple (attributes) to complex (custom rules) to build comprehensive validation.

---

**UPDATED:** 2026-01-24
