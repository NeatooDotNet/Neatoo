using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Skills.Domain;

// =============================================================================
// BASE CLASS SAMPLES - Demonstrates Neatoo base class to DDD concept mapping
// =============================================================================

// -----------------------------------------------------------------------------
// ValidateBase<T> - Value Object / DTO with validation and change tracking
// -----------------------------------------------------------------------------

#region validate-base-sample
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
#endregion

// -----------------------------------------------------------------------------
// EntityBase<T> - Entity / Aggregate Root with full CRUD lifecycle
// -----------------------------------------------------------------------------

#region edit-base-sample
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
#endregion

// -----------------------------------------------------------------------------
// EntityListBase<I> - Collection of Entities within an aggregate
// -----------------------------------------------------------------------------

/// <summary>
/// Employee address - child entity within Employee aggregate.
/// </summary>
public interface ISkillEmployeeAddress : IEntityBase
{
    int Id { get; set; }
    string Street { get; set; }
    string City { get; set; }
    string State { get; set; }
    string ZipCode { get; set; }
    string AddressType { get; set; }
}

[Factory]
public partial class SkillEmployeeAddress : EntityBase<SkillEmployeeAddress>, ISkillEmployeeAddress
{
    public SkillEmployeeAddress(IEntityBaseServices<SkillEmployeeAddress> services) : base(services)
    {
        RuleManager.AddValidation(
            addr => !string.IsNullOrEmpty(addr.Street) ? "" : "Street is required",
            a => a.Street);
    }

    public partial int Id { get; set; }
    public partial string Street { get; set; }
    public partial string City { get; set; }
    public partial string State { get; set; }
    public partial string ZipCode { get; set; }
    public partial string AddressType { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(int id, string street, string city, string state, string zipCode, string addressType)
    {
        Id = id;
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        AddressType = addressType;
    }
}

/// <summary>
/// Collection interface for employee addresses.
/// </summary>
public interface ISkillEmployeeAddressList : IEntityListBase<ISkillEmployeeAddress>
{
    int DeletedCount { get; }
}

#region editable-list-base-sample
/// <summary>
/// EntityListBase for employee addresses.
/// Manages collection of child entities with deletion tracking.
/// </summary>
public class SkillEmployeeAddressList : EntityListBase<ISkillEmployeeAddress>, ISkillEmployeeAddressList
{
    // DeletedList tracks removed items for persistence
    public int DeletedCount => DeletedList.Count;
}
#endregion

// -----------------------------------------------------------------------------
// Command Pattern - Static classes with [Execute] methods for server operations
// -----------------------------------------------------------------------------

#region command-base-sample
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
#endregion

// -----------------------------------------------------------------------------
// Read-Only Models - Use ValidateBase for read-only DTOs/queries
// -----------------------------------------------------------------------------

#region readonly-base-sample
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
#endregion

// -----------------------------------------------------------------------------
// Service interfaces for samples
// -----------------------------------------------------------------------------

public interface ISkillEmployeeRepository
{
    Task<(int Id, string Name, string Email, decimal Salary)> FetchByIdAsync(int id);
    Task InsertAsync(SkillEmployee employee);
    Task UpdateAsync(SkillEmployee employee);
    Task DeleteAsync(SkillEmployee employee);
}

public interface ISkillEmailService
{
    Task SendAsync(string to, string subject, string body);
}
