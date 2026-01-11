/// <summary>
/// Code samples for docs/validation-and-rules.md - Rule usage patterns
///
/// Full snippets (for complete examples):
/// - docs:validation-and-rules:rule-registration
/// - docs:validation-and-rules:parent-child-validation
///
/// Micro-snippets (for focused inline examples):
/// - docs:validation-and-rules:rule-interface-definition
/// - docs:validation-and-rules:entity-rule-injection
/// - docs:validation-and-rules:rule-manager-addrule
/// - docs:validation-and-rules:parent-child-rule-class
/// - docs:validation-and-rules:parent-access-in-rule
///
/// Compile-time validation only (docs use short inline examples):
/// - docs:validation-and-rules:async-action-rule
/// - docs:validation-and-rules:pause-all-actions
/// - docs:validation-and-rules:manual-execution
/// - docs:validation-and-rules:load-property
/// - docs:validation-and-rules:validation-messages
/// - docs:validation-and-rules:ismodified-check
/// </summary>

using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Samples.DomainModel.ValidationAndRules.RuleUsage;

#region rule-registration
/// <summary>
/// Demonstrates rule registration in entity constructor and DI setup.
/// </summary>
public partial interface IRuleRegistrationPerson : IValidateBase
{
    string? FirstName { get; set; }
    string? LastName { get; set; }
    int Age { get; set; }
}

#region rule-interface-definition
// Rule interfaces
public interface IAgeValidationRule : IRule<IRuleRegistrationPerson> { }
public interface IUniqueNameValidationRule : IRule<IRuleRegistrationPerson> { }
#endregion

// Entity with rule registration
[Factory]
internal partial class RuleRegistrationPerson : ValidateBase<RuleRegistrationPerson>, IRuleRegistrationPerson
{
    #region entity-rule-injection
    public RuleRegistrationPerson(
        IValidateBaseServices<RuleRegistrationPerson> services,
        IUniqueNameValidationRule uniqueNameRule,
        IAgeValidationRule ageRule) : base(services)
    {
        #region rule-manager-addrule
        RuleManager.AddRule(uniqueNameRule);
        RuleManager.AddRule(ageRule);
        #endregion
    }
    #endregion

    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
    public partial int Age { get; set; }

    [Create]
    public void Create() { }
}

// Rule implementations
public class AgeValidationRuleImpl : RuleBase<IRuleRegistrationPerson>, IAgeValidationRule
{
    public AgeValidationRuleImpl() : base(p => p.Age) { }

    protected override IRuleMessages Execute(IRuleRegistrationPerson target)
    {
        if (target.Age < 0)
            return (nameof(target.Age), "Age cannot be negative").AsRuleMessages();
        return None;
    }
}

public class UniqueNameValidationRuleImpl : RuleBase<IRuleRegistrationPerson>, IUniqueNameValidationRule
{
    public UniqueNameValidationRuleImpl() : base(p => p.FirstName, p => p.LastName) { }

    protected override IRuleMessages Execute(IRuleRegistrationPerson target)
    {
        // Simplified - real implementation would check database
        return None;
    }
}

// DI Registration example (shown in comments - actual registration in test setup)
// builder.Services.AddScoped<IUniqueNameValidationRule, UniqueNameValidationRuleImpl>();
// builder.Services.AddScoped<IAgeValidationRule, AgeValidationRuleImpl>();
#endregion

#region async-action-rule
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
#endregion

#region pause-all-actions
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
#endregion

#region manual-execution
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
#endregion

#region parent-child-validation
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

#region parent-child-rule-class
public class UniquePhoneTypeRule : RuleBase<IContactPhone>, IUniquePhoneTypeRule
{
    public UniquePhoneTypeRule() : base(p => p.PhoneType) { }

    protected override IRuleMessages Execute(IContactPhone target)
    {
        if (target.ParentContact == null)
            return None;

        #region parent-access-in-rule
        var hasDuplicate = target.ParentContact.PhoneList
            .Where(p => p != target)
            .Any(p => p.PhoneType == target.PhoneType);
        #endregion

        if (hasDuplicate)
        {
            return (nameof(target.PhoneType), "Phone type must be unique").AsRuleMessages();
        }
        return None;
    }
}
#endregion

[Factory]
internal partial class ContactPhone : EntityBase<ContactPhone>, IContactPhone
{
    public ContactPhone(
        IEntityBaseServices<ContactPhone> services,
        IUniquePhoneTypeRule uniquePhoneTypeRule) : base(services)
    {
        RuleManager.AddRule(uniquePhoneTypeRule);
    }

    public partial PhoneType? PhoneType { get; set; }
    public partial string? Number { get; set; }

    public IParentContact? ParentContact => Parent as IParentContact;

    [Create]
    public void Create() { }
}

[Factory]
internal class ContactPhoneList : EntityListBase<IContactPhone>, IContactPhoneList
{
    [Create]
    public void Create() { }
}

[Factory]
internal partial class ParentContact : EntityBase<ParentContact>, IParentContact
{
    public ParentContact(IEntityBaseServices<ParentContact> services) : base(services) { }

    public partial IContactPhoneList PhoneList { get; set; }

    [Create]
    public void Create([Service] IContactPhoneListFactory phoneListFactory)
    {
        PhoneList = phoneListFactory.Create();
    }
}
#endregion

#region load-property
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
#endregion

#region validation-messages
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
#endregion

#region ismodified-check
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
#endregion
