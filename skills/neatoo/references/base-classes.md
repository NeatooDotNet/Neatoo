# Base Classes

Neatoo provides base classes that map to Domain-Driven Design concepts. Choose the appropriate base class based on the DDD concept you're modeling.

## Base Class to DDD Mapping

| Neatoo Base Class | DDD Concept | Persistence | Validation | Change Tracking |
|-------------------|-------------|-------------|------------|-----------------|
| `ValidateBase<T>` | Value Object | No | Yes | Yes |
| `EntityBase<T>` | Entity / Aggregate Root | Yes | Yes | Yes |
| `EntityListBase<I>` | Collection of Entities | Yes (via parent) | Yes | Yes |
| Static class with `[Execute]` | Command | Execute only | No | No |
| `ValidateBase<T>` with `[Fetch]` only | Read Model | No | Yes | Yes |
| `ValidateListBase<I>` | Collection of Read Models | No | Yes | Yes |

## ValidateBase<T>

Use for objects that need validation and change tracking but no persistence lifecycle.

**DDD Concept:** Value Object - Immutable data identified by its attributes, not by identity.

**When to use:**
- Composite values (Address, Money, DateRange)
- Form data that needs validation before submission
- DTOs with business rules

<!-- snippet: validate-base-sample -->
<a id='snippet-validate-base-sample'></a>
```cs
/// <summary>
/// Address value object demonstrating ValidateBase usage.
/// ValidateBase provides validation and change tracking without persistence lifecycle.
/// </summary>
[Factory]
public partial class SkillAddress : ValidateBase<SkillAddress>
{
    public SkillAddress(IValidateBaseServices<SkillAddress> services) : base(services)
    {
        // Add validation rule for zip code format
        RuleManager.AddValidation(
            addr => string.IsNullOrEmpty(addr.ZipCode) || addr.ZipCode.Length == 5
                ? ""
                : "Zip code must be 5 digits",
            a => a.ZipCode);
    }

    [Required(ErrorMessage = "Street is required")]
    public partial string Street { get; set; }

    [Required(ErrorMessage = "City is required")]
    public partial string City { get; set; }

    [Required(ErrorMessage = "State is required")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "State must be 2 characters")]
    public partial string State { get; set; }

    public partial string ZipCode { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/BaseClassSamples.cs#L16-L49' title='Snippet source file'>snippet source</a> | <a href='#snippet-validate-base-sample' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## EntityBase<T>

Use for persistent entities with full CRUD lifecycle.

**DDD Concept:** Entity or Aggregate Root - Objects with identity that persist across time.

**When to use:**
- Domain entities that are saved to a database
- Aggregate roots that coordinate child entities
- Any object that needs Create/Fetch/Update/Delete operations

<!-- snippet: edit-base-sample -->
<a id='snippet-edit-base-sample'></a>
```cs
/// <summary>
/// Employee entity demonstrating EntityBase usage.
/// EntityBase provides full persistence lifecycle with Create/Fetch/Update/Delete.
/// </summary>
[Factory]
public partial class SkillEmployee : EntityBase<SkillEmployee>
{
    public SkillEmployee(IEntityBaseServices<SkillEmployee> services) : base(services)
    {
        // Initialize child collection
        AddressesProperty.LoadValue(new SkillEmployeeAddressList());

        // Validation rule for salary
        RuleManager.AddValidation(
            emp => emp.Salary >= 0 ? "" : "Salary cannot be negative",
            e => e.Salary);
    }

    public partial int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public partial string Name { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public partial string Email { get; set; }

    [Range(0, 1000000, ErrorMessage = "Salary must be between 0 and 1,000,000")]
    public partial decimal Salary { get; set; }

    public partial DateTime HireDate { get; set; }

    public partial string Department { get; set; }

    // Child collection - part of the aggregate
    public partial ISkillEmployeeAddressList Addresses { get; set; }

    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
        Email = "";
        Salary = 0;
        HireDate = DateTime.Today;
    }

    [Fetch]
    public void Fetch(int id, string name, string email, decimal salary)
    {
        Id = id;
        Name = name;
        Email = email;
        Salary = salary;
    }

    [Insert]
    public async Task InsertAsync([Service] ISkillEmployeeRepository repository)
    {
        await repository.InsertAsync(this);
    }

    [Update]
    public async Task UpdateAsync([Service] ISkillEmployeeRepository repository)
    {
        await repository.UpdateAsync(this);
    }

    [Delete]
    public async Task DeleteAsync([Service] ISkillEmployeeRepository repository)
    {
        await repository.DeleteAsync(this);
    }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/BaseClassSamples.cs#L55-L129' title='Snippet source file'>snippet source</a> | <a href='#snippet-edit-base-sample' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## EntityListBase<I>

Use for collections of child entities within an aggregate.

**DDD Concept:** Collection of Entities - Managed collection within an aggregate boundary.

**When to use:**
- Order lines in an Order aggregate
- Addresses on a Customer entity
- Any collection of child entities

<!-- snippet: editable-list-base-sample -->
<a id='snippet-editable-list-base-sample'></a>
```cs
/// <summary>
/// EntityListBase for employee addresses.
/// Manages collection of child entities with deletion tracking.
/// </summary>
public class SkillEmployeeAddressList : EntityListBase<ISkillEmployeeAddress>, ISkillEmployeeAddressList
{
    // DeletedList tracks removed items for persistence
    public int DeletedCount => DeletedList.Count;
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/BaseClassSamples.cs#L188-L198' title='Snippet source file'>snippet source</a> | <a href='#snippet-editable-list-base-sample' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Commands (Static Classes with [Execute])

Use for operations that execute on the server without persistence state.

**DDD Concept:** Command - Request to perform an action.

**When to use:**
- Operations that don't return entity state
- Batch operations
- Side-effect operations (send email, generate report)


<!-- snippet: command-base-sample -->
<a id='snippet-command-base-sample'></a>
```cs
/// <summary>
/// Send email command demonstrating the static command pattern.
/// Commands are static classes with [Execute] methods that generate delegates.
/// The delegate is injected via DI and always executes on the server.
/// </summary>
[Factory]
public static partial class SkillSendEmailCommand
{
    // [Execute] generates a delegate: SendEmailCommand.SendEmail
    // The delegate is resolved from DI and called like a function
    [Execute]
    internal static async Task<bool> _SendEmail(
        string to,
        string subject,
        string body,
        [Service] ISkillEmailService emailService)
    {
        try
        {
            await emailService.SendAsync(to, subject, body);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/BaseClassSamples.cs#L204-L233' title='Snippet source file'>snippet source</a> | <a href='#snippet-command-base-sample' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Read Models (ValidateBase with [Fetch] Only)

Use for query results that don't need editing.

**DDD Concept:** Read Model - Optimized view of data for queries.

**When to use:**
- Dashboard data
- Dropdown lists
- Search results


<!-- snippet: readonly-base-sample -->
<a id='snippet-readonly-base-sample'></a>
```cs
/// <summary>
/// Employee summary read model using ValidateBase.
/// For read-only data, use ValidateBase without [Insert]/[Update]/[Delete] methods.
/// ValidateBase provides validation and property tracking without persistence lifecycle.
/// </summary>
[Factory]
public partial class SkillEmployeeSummary : ValidateBase<SkillEmployeeSummary>
{
    public SkillEmployeeSummary(IValidateBaseServices<SkillEmployeeSummary> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }
    public partial string Department { get; set; }
    public partial decimal Salary { get; set; }

    // Fetch-only pattern - no Create/Insert/Update/Delete
    // This makes the object effectively read-only after loading
    [Fetch]
    public void Fetch(int id, string name, string department, decimal salary)
    {
        Id = id;
        Name = name;
        Department = department;
        Salary = salary;
    }
}

/// <summary>
/// Employee summary list using ValidateListBase.
/// </summary>
public class SkillEmployeeSummaryList : ValidateListBase<SkillEmployeeSummary>
{
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/BaseClassSamples.cs#L239-L273' title='Snippet source file'>snippet source</a> | <a href='#snippet-readonly-base-sample' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Inheritance Guidelines

1. **Always inherit from the appropriate base class** - Don't implement interfaces directly
2. **Use `partial` keyword** - Source generators extend your class
3. **Follow DDD aggregate boundaries** - EntityBase for roots, children within the aggregate

## Related

- [Properties](properties.md) - Getter/Setter patterns
- [Entities](entities.md) - EntityBase lifecycle and persistence
- [Collections](collections.md) - EntityListBase patterns
