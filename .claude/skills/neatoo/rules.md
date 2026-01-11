# Neatoo Rules Engine Reference

## Overview

Neatoo's rules engine handles both **validation** (is the data correct?) and **transformation** (calculating derived values). Rules execute automatically when dependent properties change.

## Rule Types

| Type | Purpose | Example |
|------|---------|---------|
| Validation | Check data correctness | "Email must be valid format" |
| Transformation | Calculate derived values | "FullName = FirstName + LastName" |
| Async Validation | Server-side checks | "Email must be unique in database" |

## Data Annotation Validation

The simplest validation uses standard .NET data annotations.

### Complete Data Annotations Entity

<!-- snippet: data-annotations-entity -->
```csharp
/// <summary>
/// Sample entity demonstrating all supported data annotation attributes.
/// </summary>
[Factory]
internal partial class DataAnnotationsEntity : EntityBase<DataAnnotationsEntity>, IDataAnnotationsEntity
{
    public DataAnnotationsEntity(IEntityBaseServices<DataAnnotationsEntity> services) : base(services) { }

    public partial Guid? Id { get; set; }

    #region docs:validation-and-rules:required-attribute
    [Required]
    public partial string? FirstName { get; set; }

    [Required(ErrorMessage = "Customer name is required")]
    public partial string? CustomerName { get; set; }
```
<!-- /snippet -->

### Required Attribute

<!-- snippet: required-attribute -->
```csharp
[Required]
    public partial string? FirstName { get; set; }

    [Required(ErrorMessage = "Customer name is required")]
    public partial string? CustomerName { get; set; }
```
<!-- /snippet -->

### StringLength Attribute

<!-- snippet: stringlength-attribute -->
```csharp
// Maximum length only
    [StringLength(100)]
    public partial string? Description { get; set; }

    // Minimum and maximum
    [StringLength(100, MinimumLength = 2)]
    public partial string? Username { get; set; }

    // Custom message
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Name must be 5-50 characters")]
    public partial string? NameWithLength { get; set; }
```
<!-- /snippet -->

### MinLength/MaxLength Attributes

<!-- snippet: minmaxlength-attribute -->
```csharp
// String minimum length
    [MinLength(3)]
    public partial string? Code { get; set; }

    // String maximum length
    [MaxLength(500)]
    public partial string? Notes { get; set; }

    // Collection minimum count
    [MinLength(1, ErrorMessage = "At least one item required")]
    public partial List<string>? Tags { get; set; }

    // Array maximum count
    [MaxLength(10)]
    public partial string[]? Categories { get; set; }
```
<!-- /snippet -->

### Range Attribute

<!-- snippet: range-attribute -->
```csharp
// Integer range
    [Range(1, 100)]
    public partial int Quantity { get; set; }

    // Double range
    [Range(0.0, 100.0)]
    public partial double Percentage { get; set; }

    // Decimal range (use type-based constructor)
    [Range(typeof(decimal), "0.01", "999.99")]
    public partial decimal Price { get; set; }

    // Date range
    [Range(typeof(DateTime), "2020-01-01", "2030-12-31")]
    public partial DateTime AppointmentDate { get; set; }

    // Custom message
    [Range(0, 150, ErrorMessage = "Age must be between 0 and 150")]
    public partial int Age { get; set; }
```
<!-- /snippet -->

### RegularExpression Attribute

<!-- snippet: regularexpression-attribute -->
```csharp
// Code format: 2 letters + 4 digits
    [RegularExpression(@"^[A-Z]{2}\d{4}$")]
    public partial string? ProductCode { get; set; }

    // Phone format
    [RegularExpression(@"^\d{3}-\d{3}-\d{4}$", ErrorMessage = "Format: 555-123-4567")]
    public partial string? Phone { get; set; }

    // Alphanumeric only
    [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Letters and numbers only")]
    public partial string? UsernameAlphanumeric { get; set; }
```
<!-- /snippet -->

### EmailAddress Attribute

<!-- snippet: emailaddress-attribute -->
```csharp
[EmailAddress]
    public partial string? Email { get; set; }

    [EmailAddress(ErrorMessage = "Please enter a valid email")]
    public partial string? ContactEmail { get; set; }
```
<!-- /snippet -->

### Combining Attributes

<!-- snippet: combining-attributes -->
```csharp
[Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(254, ErrorMessage = "Email too long")]
    public partial string? CombinedEmail { get; set; }

    [Required]
    [Range(1, 1000)]
    public partial int CombinedQuantity { get; set; }

    [StringLength(100, MinimumLength = 2)]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Letters only")]
    public partial string? FullName { get; set; }
```
<!-- /snippet -->

## Fluent Rule Registration

For calculated properties and inline validation, use fluent methods in the constructor.

### Fluent Rules Person Example

<!-- snippet: fluent-rules-person -->
```csharp
/// <summary>
/// Sample person that demonstrates fluent rule registration.
/// </summary>
[Factory]
internal partial class PersonWithFluentRules : EntityBase<PersonWithFluentRules>, IPersonWithFluentRules
{
    public PersonWithFluentRules(IEntityBaseServices<PersonWithFluentRules> services,
                                  IEmailService emailService) : base(services)
    {
        #region docs:validation-and-rules:fluent-validation
        // Inline validation rule
        RuleManager.AddValidation(
            target => string.IsNullOrEmpty(target.Name) ? "Name is required" : "",
            t => t.Name);
```
<!-- /snippet -->

### AddValidation (Sync)

<!-- snippet: fluent-validation -->
```csharp
// Inline validation rule
        RuleManager.AddValidation(
            target => string.IsNullOrEmpty(target.Name) ? "Name is required" : "",
            t => t.Name);
```
<!-- /snippet -->

### AddValidationAsync

<!-- snippet: fluent-validation-async -->
```csharp
// Async validation rule
        RuleManager.AddValidationAsync(
            async target => await emailService.EmailExistsAsync(target.Email!) ? "Email in use" : "",
            t => t.Email);
```
<!-- /snippet -->

### AddAction (Transformation)

<!-- snippet: fluent-action -->
```csharp
// Action rule for calculated values
        RuleManager.AddAction(
            target => target.FullName = $"{target.FirstName} {target.LastName}",
            t => t.FirstName,
            t => t.LastName);
```
<!-- /snippet -->

## Custom Validation Rules (RuleBase)

For complex validation logic, create custom rule classes.

### Age Validation Rule

<!-- snippet: age-validation-rule -->
```csharp
public interface IAgeValidationRule : IRule<IPerson> { }

public class AgeValidationRule : RuleBase<IPerson>, IAgeValidationRule
{
    public AgeValidationRule() : base(p => p.Age) { }

    protected override IRuleMessages Execute(IPerson target)
    {
        if (target.Age < 0)
        {
            return (nameof(target.Age), "Age cannot be negative").AsRuleMessages();
        }
        if (target.Age > 150)
        {
            return (nameof(target.Age), "Age seems unrealistic").AsRuleMessages();
        }
        return None;
    }
}
```
<!-- /snippet -->

### Unique Email Rule (Async)

<!-- snippet: unique-email-rule -->
```csharp
public interface IUniqueEmailRule : IRule<IPerson> { }

public class UniqueEmailRule : AsyncRuleBase<IPerson>, IUniqueEmailRule
{
    private readonly IEmailService _emailService;

    public UniqueEmailRule(IEmailService emailService) : base(p => p.Email)
    {
        _emailService = emailService;
    }

    protected override async Task<IRuleMessages> Execute(IPerson target, CancellationToken? token = null)
    {
        if (string.IsNullOrEmpty(target.Email))
            return None;

        if (await _emailService.EmailExistsAsync(target.Email, target.Id))
        {
            return (nameof(target.Email), "Email already in use").AsRuleMessages();
        }
        return None;
    }
}
```
<!-- /snippet -->

### Trigger Properties

<!-- snippet: trigger-properties -->
```csharp
public class TriggerPropertiesConstructorExample : RuleBase<IPerson>
{
    // Constructor approach - pass trigger properties to base
    public TriggerPropertiesConstructorExample() : base(p => p.FirstName, p => p.LastName) { }

    protected override IRuleMessages Execute(IPerson target) => None;
}

public class TriggerPropertiesMethodExample : RuleBase<IPerson>
{
    // Or use AddTriggerProperties method
    public TriggerPropertiesMethodExample()
    {
        AddTriggerProperties(p => p.FirstName, p => p.LastName);
    }

    protected override IRuleMessages Execute(IPerson target) => None;
}
```
<!-- /snippet -->

### Date Range Rule

<!-- snippet: date-range-rule -->
```csharp
public interface IDateRangeRule : IRule<IEvent> { }

public class DateRangeRule : RuleBase<IEvent>, IDateRangeRule
{
    public DateRangeRule() : base(e => e.StartDate, e => e.EndDate) { }

    protected override IRuleMessages Execute(IEvent target)
    {
        if (target.StartDate > target.EndDate)
        {
            return (new[]
            {
                (nameof(target.StartDate), "Start date must be before end date"),
                (nameof(target.EndDate), "End date must be after start date")
            }).AsRuleMessages();
        }
        return None;
    }
}
```
<!-- /snippet -->

### Complete Rule Example

<!-- snippet: complete-rule-example -->
```csharp
public interface IUniqueNameRule : IRule<IPerson> { }

public class UniqueNameRule : AsyncRuleBase<IPerson>, IUniqueNameRule
{
    private readonly Func<Guid?, string, string, Task<bool>> _isUniqueName;

    public UniqueNameRule(Func<Guid?, string, string, Task<bool>> isUniqueName)
    {
        _isUniqueName = isUniqueName;
        AddTriggerProperties(p => p.FirstName, p => p.LastName);
    }

    protected override async Task<IRuleMessages> Execute(IPerson target, CancellationToken? token = null)
    {
        // Skip if properties haven't been modified
        if (!target[nameof(target.FirstName)].IsModified &&
            !target[nameof(target.LastName)].IsModified)
        {
            return None;
        }

        // Skip if values are empty
        if (string.IsNullOrEmpty(target.FirstName) || string.IsNullOrEmpty(target.LastName))
        {
            return None;
        }

        // Check uniqueness
        if (!await _isUniqueName(target.Id, target.FirstName, target.LastName))
        {
            return (new[]
            {
                (nameof(target.FirstName), "First and Last name combination is not unique"),
                (nameof(target.LastName), "First and Last name combination is not unique")
            }).AsRuleMessages();
        }

        return None;
    }
}
```
<!-- /snippet -->

## Returning Rule Messages

### Single Message

<!-- snippet: returning-messages-single -->
```csharp
public class SingleMessageExample : RuleBase<IPerson>
{
    public SingleMessageExample() : base(p => p.Email) { }

    protected override IRuleMessages Execute(IPerson target)
    {
        // Return a single validation message
        return (nameof(target.Email), "Invalid email format").AsRuleMessages();
    }
}
```
<!-- /snippet -->

### Multiple Messages

<!-- snippet: returning-messages-multiple -->
```csharp
public class MultipleMessagesExample : RuleBase<IPerson>
{
    public MultipleMessagesExample() : base(p => p.FirstName, p => p.LastName) { }

    protected override IRuleMessages Execute(IPerson target)
    {
        // Return multiple validation messages
        return (new[]
        {
            (nameof(target.FirstName), "First and Last name combination is not unique"),
            (nameof(target.LastName), "First and Last name combination is not unique")
        }).AsRuleMessages();
    }
}
```
<!-- /snippet -->

### Conditional Messages

<!-- snippet: returning-messages-conditional -->
```csharp
public class ConditionalMessageExample : RuleBase<IPerson>
{
    public ConditionalMessageExample() : base(p => p.Age) { }

    protected override IRuleMessages Execute(IPerson target)
    {
        // Conditional message using RuleMessages.If
        return RuleMessages.If(
            target.Age < 0,
            nameof(target.Age),
            "Age cannot be negative");
    }
}
```
<!-- /snippet -->

### Chained Messages

<!-- snippet: returning-messages-chained -->
```csharp
public class ChainedConditionsExample : RuleBase<IPerson>
{
    public ChainedConditionsExample() : base(p => p.FirstName) { }

    protected override IRuleMessages Execute(IPerson target)
    {
        // Chained conditions with ElseIf
        return RuleMessages.If(string.IsNullOrEmpty(target.FirstName), nameof(target.FirstName), "Name is required")
            .ElseIf(() => target.FirstName!.Length < 2, nameof(target.FirstName), "Name must be at least 2 characters");
    }
}
```
<!-- /snippet -->

## Rule Registration

### Rule Interface Definition

<!-- snippet: rule-interface-definition -->
```csharp
// Rule interfaces
public interface IAgeValidationRule : IRule<IRuleRegistrationPerson> { }
public interface IUniqueNameValidationRule : IRule<IRuleRegistrationPerson> { }
```
<!-- /snippet -->

### Entity Rule Injection

<!-- snippet: entity-rule-injection -->
```csharp
public RuleRegistrationPerson(
        IValidateBaseServices<RuleRegistrationPerson> services,
        IUniqueNameValidationRule uniqueNameRule,
        IAgeValidationRule ageRule) : base(services)
    {
        #region docs:validation-and-rules:rule-manager-addrule
        RuleManager.AddRule(uniqueNameRule);
        RuleManager.AddRule(ageRule);
```
<!-- /snippet -->

### RuleManager.AddRule

<!-- snippet: rule-manager-addrule -->
```csharp
RuleManager.AddRule(uniqueNameRule);
        RuleManager.AddRule(ageRule);
```
<!-- /snippet -->

## Async Rules - Server-Side Validation

For validation requiring database access, use async rules.

### Database-Dependent Async Rule

<!-- snippet: async-rule -->
```csharp
/// <summary>
/// Async rule that validates email uniqueness using the command.
/// </summary>
public interface IAsyncUniqueEmailRule : IRule<IUserWithEmail> { }

public class AsyncUniqueEmailRule : AsyncRuleBase<IUserWithEmail>, IAsyncUniqueEmailRule
{
    private readonly CheckEmailUnique.IsUnique _isUnique;

    public AsyncUniqueEmailRule(CheckEmailUnique.IsUnique isUnique)
    {
        _isUnique = isUnique;
        AddTriggerProperties(u => u.Email);
    }

    protected override async Task<IRuleMessages> Execute(
        IUserWithEmail target, CancellationToken? token = null)
    {
        if (string.IsNullOrEmpty(target.Email))
            return None;

        // Skip if property not modified (optimization)
        if (!target.IsNew && !target[nameof(target.Email)].IsModified)
            return None;

        var excludeId = target.IsNew ? null : (Guid?)target.Id;

        if (!await _isUnique(target.Email, excludeId))
        {
            return (nameof(target.Email), "Email already in use").AsRuleMessages();
        }

        return None;
    }
}
```
<!-- /snippet -->

### Async Action Rule

<!-- snippet: async-action-rule -->
```csharp
/// <summary>
/// Demonstrates AddActionAsync for async side effects.
/// </summary>
public partial interface IAsyncActionPerson : IValidateBase
{
    string? ZipCode { get; set; }
    decimal TaxRate { get; set; }
}

[Factory]
internal partial class AsyncActionPerson : ValidateBase<AsyncActionPerson>, IAsyncActionPerson
{
    public AsyncActionPerson(IValidateBaseServices<AsyncActionPerson> services) : base(services)
    {
        // Async action rule - updates TaxRate when ZipCode changes
        RuleManager.AddActionAsync(
            async target => target.TaxRate = await GetTaxRateAsync(target.ZipCode),
            t => t.ZipCode);
    }

    public partial string? ZipCode { get; set; }
    public partial decimal TaxRate { get; set; }

    private static Task<decimal> GetTaxRateAsync(string? zipCode)
    {
        // Simulated tax rate lookup
        return Task.FromResult(zipCode?.StartsWith('9') == true ? 0.0825m : 0.06m);
    }

    [Create]
    public void Create() { }
}
```
<!-- /snippet -->

### Pause All Actions

<!-- snippet: pause-all-actions -->
```csharp
/// <summary>
/// Demonstrates PauseAllActions for bulk updates without triggering rules.
/// </summary>
public partial interface IPauseActionsPerson : IValidateBase
{
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
}

[Factory]
internal partial class PauseActionsPerson : ValidateBase<PauseActionsPerson>, IPauseActionsPerson
{
    public PauseActionsPerson(IValidateBaseServices<PauseActionsPerson> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrEmpty(t.FirstName) ? "First name required" : "",
            t => t.FirstName);
    }

    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
    public partial string? Email { get; set; }

    [Create]
    public void Create() { }

    /// <summary>
    /// Example showing PauseAllActions usage.
    /// </summary>
    public void BulkUpdate(string firstName, string lastName, string email)
    {
        using (PauseAllActions())
        {
            FirstName = firstName;    // No rules yet
            LastName = lastName;      // No rules yet
            Email = email;            // No rules yet
        }
        // All rules run now when disposed
    }
}
```
<!-- /snippet -->

## Validation Messages

### Working with Messages

<!-- snippet: validation-messages -->
```csharp
/// <summary>
/// Demonstrates accessing validation messages.
/// </summary>
public partial interface IValidationMessagesPerson : IValidateBase
{
    string? Email { get; set; }
    string? Name { get; set; }
}

[Factory]
internal partial class ValidationMessagesPerson : ValidateBase<ValidationMessagesPerson>, IValidationMessagesPerson
{
    public ValidationMessagesPerson(IValidateBaseServices<ValidationMessagesPerson> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrEmpty(t.Email) ? "Email is required" : "",
            t => t.Email);
        RuleManager.AddValidation(
            t => string.IsNullOrEmpty(t.Name) ? "Name is required" : "",
            t => t.Name);
    }

    public partial string? Email { get; set; }
    public partial string? Name { get; set; }

    [Create]
    public void Create() { }

    /// <summary>
    /// Example showing how to access validation messages.
    /// </summary>
    public void ShowMessagesExample()
    {
        // Property-level messages
        var emailMessages = this[nameof(Email)].PropertyMessages;

        // All messages for entity
        var allMessages = PropertyMessages;

        // Check validity
        if (!IsValid)
        {
            foreach (var msg in PropertyMessages)
            {
                Console.WriteLine($"{msg.Property.Name}: {msg.Message}");
            }
        }
    }
}
```
<!-- /snippet -->

## Advanced Rule Patterns

### LoadProperty Usage

<!-- snippet: load-property -->
```csharp
/// <summary>
/// Demonstrates LoadProperty for setting values without triggering rules.
/// </summary>
public partial interface ILoadPropertyPerson : IValidateBase
{
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? FullName { get; set; }
}

public interface IFullNameRule : IRule<ILoadPropertyPerson> { }

public class FullNameRule : RuleBase<ILoadPropertyPerson>, IFullNameRule
{
    public FullNameRule() : base(p => p.FirstName, p => p.LastName) { }

    protected override IRuleMessages Execute(ILoadPropertyPerson target)
    {
        // Set FullName without triggering any FullName rules
        LoadProperty(target, t => t.FullName, $"{target.FirstName} {target.LastName}");
        return None;
    }
}

[Factory]
internal partial class LoadPropertyPerson : ValidateBase<LoadPropertyPerson>, ILoadPropertyPerson
{
    public LoadPropertyPerson(
        IValidateBaseServices<LoadPropertyPerson> services,
        IFullNameRule fullNameRule) : base(services)
    {
        RuleManager.AddRule(fullNameRule);
    }

    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
    public partial string? FullName { get; set; }

    [Create]
    public void Create() { }
}
```
<!-- /snippet -->

### IsModified Check

<!-- snippet: ismodified-check -->
```csharp
/// <summary>
/// Demonstrates checking IsModified before expensive async operations.
/// </summary>
public partial interface IIsModifiedCheckUser : IEntityBase
{
    Guid? Id { get; set; }
    string? Email { get; set; }
}

public interface IEmailCheckRule : IRule<IIsModifiedCheckUser> { }

public class EmailCheckRule : AsyncRuleBase<IIsModifiedCheckUser>, IEmailCheckRule
{
    public EmailCheckRule()
    {
        AddTriggerProperties(u => u.Email);
    }

    protected override async Task<IRuleMessages> Execute(IIsModifiedCheckUser target, CancellationToken? token = null)
    {
        // Skip expensive check if email hasn't changed
        if (!target[nameof(target.Email)].IsModified)
            return None;

        // ... expensive database check (simulated)
        await Task.Delay(10, token ?? CancellationToken.None);

        return None;
    }
}

[Factory]
internal partial class IsModifiedCheckUser : EntityBase<IsModifiedCheckUser>, IIsModifiedCheckUser
{
    public IsModifiedCheckUser(
        IEntityBaseServices<IsModifiedCheckUser> services,
        IEmailCheckRule emailCheckRule) : base(services)
    {
        RuleManager.AddRule(emailCheckRule);
    }

    public partial Guid? Id { get; set; }
    public partial string? Email { get; set; }

    [Create]
    public void Create() { }
}
```
<!-- /snippet -->

### Manual Rule Execution

<!-- snippet: manual-execution -->
```csharp
/// <summary>
/// Demonstrates manual rule execution in factory methods.
/// </summary>
public partial interface IManualExecutionEntity : IEntityBase
{
    Guid? Id { get; set; }
    string? Name { get; set; }
}

[Factory]
internal partial class ManualExecutionEntity : EntityBase<ManualExecutionEntity>, IManualExecutionEntity
{
    public ManualExecutionEntity(IEntityBaseServices<ManualExecutionEntity> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrEmpty(t.Name) ? "Name is required" : "",
            t => t.Name);
    }

    public partial Guid? Id { get; set; }
    public partial string? Name { get; set; }

    [Create]
    public void Create() { }

    [Insert]
    public async Task Insert()
    {
        await RunRules();  // Run all rules

        if (!IsSavable)
            return;  // Don't save if invalid

        // ... persist (simulated)
        Id = Guid.NewGuid();
    }
}
```
<!-- /snippet -->

## Parent-Child Validation

### Parent-Child Validation Pattern

<!-- snippet: parent-child-validation -->
```csharp
/// <summary>
/// Demonstrates accessing parent entity from child validation rules.
/// </summary>
public partial interface IParentContact : IEntityBase
{
    IContactPhoneList PhoneList { get; }
}

public partial interface IContactPhone : IEntityBase
{
    PhoneType? PhoneType { get; set; }
    string? Number { get; set; }
    IParentContact? ParentContact { get; }
}

public partial interface IContactPhoneList : IEntityListBase<IContactPhone> { }

public enum PhoneType { Home, Work, Mobile }

public interface IUniquePhoneTypeRule : IRule<IContactPhone> { }

#region docs:validation-and-rules:parent-child-rule-class
public class UniquePhoneTypeRule : RuleBase<IContactPhone>, IUniquePhoneTypeRule
{
    public UniquePhoneTypeRule() : base(p => p.PhoneType) { }

    protected override IRuleMessages Execute(IContactPhone target)
    {
        if (target.ParentContact == null)
            return None;

        #region docs:validation-and-rules:parent-access-in-rule
        var hasDuplicate = target.ParentContact.PhoneList
            .Where(p => p != target)
            .Any(p => p.PhoneType == target.PhoneType);
```
<!-- /snippet -->

### Parent-Child Rule Class

<!-- snippet: parent-child-rule-class -->
```csharp
public class UniquePhoneTypeRule : RuleBase<IContactPhone>, IUniquePhoneTypeRule
{
    public UniquePhoneTypeRule() : base(p => p.PhoneType) { }

    protected override IRuleMessages Execute(IContactPhone target)
    {
        if (target.ParentContact == null)
            return None;

        #region docs:validation-and-rules:parent-access-in-rule
        var hasDuplicate = target.ParentContact.PhoneList
            .Where(p => p != target)
            .Any(p => p.PhoneType == target.PhoneType);
```
<!-- /snippet -->

### Parent Access in Rule

<!-- snippet: parent-access-in-rule -->
```csharp
var hasDuplicate = target.ParentContact.PhoneList
            .Where(p => p != target)
            .Any(p => p.PhoneType == target.PhoneType);
```
<!-- /snippet -->

## Cross-Item Validation (Collections)

### Cross-Item Validation

<!-- snippet: cross-item-validation -->
```csharp
/// <summary>
/// List that re-validates siblings when properties change.
/// </summary>
public interface IContactPhoneList : IEntityListBase<IContactPhone>
{
    IContactPhone AddPhone();
}

[Factory]
internal class ContactPhoneList : EntityListBase<IContactPhone>, IContactPhoneList
{
    private readonly IContactPhoneFactory _phoneFactory;

    public ContactPhoneList([Service] IContactPhoneFactory phoneFactory)
    {
        _phoneFactory = phoneFactory;
    }

    public IContactPhone AddPhone()
    {
        var phone = _phoneFactory.Create();
        Add(phone);
        return phone;
    }

    /// <summary>
    /// Re-validate siblings when PhoneType changes to enforce uniqueness.
    /// </summary>
    protected override async Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        await base.HandleNeatooPropertyChanged(eventArgs);

        // When PhoneType changes, re-validate all other items for uniqueness
        if (eventArgs.PropertyName == nameof(IContactPhone.PhoneType))
        {
            if (eventArgs.Source is IContactPhone changedPhone)
            {
                // Re-run rules on all OTHER items
                await Task.WhenAll(
                    this.Except([changedPhone])
                        .Select(phone => phone.RunRules()));
            }
        }
    }

    [Create]
    public void Create() { }
}
```
<!-- /snippet -->

## Anti-Pattern: Validation in Factory Methods

**Never put business validation in `[Insert]` or `[Update]` factory methods.**

### Where Validation Belongs

| Validation Type | Correct Location | WRONG Location |
|-----------------|------------------|----------------|
| Required fields | `[Required]` attribute | Factory methods |
| Format validation | `[RegularExpression]`, `[EmailAddress]` | Factory methods |
| Range/length checks | `[Range]`, `[StringLength]` | Factory methods |
| Cross-property rules | `RuleBase<T>` | Factory methods |
| Database lookups (uniqueness, overlap) | `AsyncRuleBase<T>` + Command | Factory methods |

### Why Factory Validation is Wrong

<!-- snippet: anti-pattern -->
```csharp
/// <summary>
/// ANTI-PATTERN: Do NOT put validation in factory methods.
/// This example shows what NOT to do.
/// </summary>
/// <remarks>
/// Problems with this approach:
/// 1. Poor UX - Users only see errors after clicking Save
/// 2. Throws exceptions instead of validation messages
/// 3. Bypasses rule system - no IsBusy, no UI integration
/// 4. Returns HTTP 500 instead of validation error
/// </remarks>
public static class AntiPatternExample
{
    /// <summary>
    /// BAD: Validation logic inside Insert method.
    /// </summary>
    public static async Task Insert_AntiPattern(
        IUserWithEmail user,
        IUserRepository repo)
    {
        // DON'T DO THIS - validation only runs at save time!
        if (await repo.EmailExistsAsync(user.Email!, null))
            throw new InvalidOperationException("Email already in use");

        // ... persistence would go here
    }
}
```
<!-- /snippet -->

Problems with this approach:
1. **Poor UX** - Users only see errors after clicking Save
2. **Throws exceptions** - Instead of validation messages
3. **Bypasses rule system** - No IsBusy, no UI integration
4. **HTTP 500 errors** - Instead of validation error responses

### Correct Pattern: AsyncRuleBase + Command

See the Database-Dependent Async Rule section above for the correct pattern using:
1. A Command with `[Execute]` for the database check
2. An `AsyncRuleBase<T>` that calls the command
3. Clean factory methods with only persistence logic

### Decision Guide

When implementing validation that requires database access:

1. **Ask**: "Does this need to check the database?"
   - No → Use `[Required]`, `[Range]`, `RuleBase<T>`, etc.
   - Yes → Continue to step 2

2. **Create a Command** with `[Execute]` method for the database logic

3. **Create an AsyncRuleBase** that calls the command

4. **Register the rule** in DI and add to RuleManager

## Common Pitfalls

1. **Forgetting trigger properties** - Rules only run when specified properties change
2. **Missing await** - Always await async rules before checking IsValid
3. **Not using LoadProperty in Fetch** - Direct setters trigger rules
4. **Circular dependencies** - Calculated properties triggering each other infinitely
5. **Not checking for null** - Parent may be null during entity construction
