/// <summary>
/// Code samples for docs/validation-and-rules.md
///
/// Snippets in this file:
/// - docs:validation-and-rules:age-validation-rule
/// - docs:validation-and-rules:unique-email-rule
/// - docs:validation-and-rules:trigger-properties
/// - docs:validation-and-rules:returning-messages-single
/// - docs:validation-and-rules:returning-messages-multiple
/// - docs:validation-and-rules:returning-messages-conditional
/// - docs:validation-and-rules:returning-messages-chained
/// - docs:validation-and-rules:date-range-rule
/// - docs:validation-and-rules:complete-rule-example
///
/// Corresponding tests: RuleBaseSamplesTests.cs
/// </summary>

using Neatoo.Samples.DomainModel.SampleDomain;
using Neatoo.Rules;

namespace Neatoo.Samples.DomainModel.ValidationAndRules;

#region age-validation-rule
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
#endregion

#region unique-email-rule
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
#endregion

#region trigger-properties
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
#endregion

#region returning-messages-single
public class SingleMessageExample : RuleBase<IPerson>
{
    public SingleMessageExample() : base(p => p.Email) { }

    protected override IRuleMessages Execute(IPerson target)
    {
        // Return a single validation message
        return (nameof(target.Email), "Invalid email format").AsRuleMessages();
    }
}
#endregion

#region returning-messages-multiple
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
#endregion

#region returning-messages-conditional
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
#endregion

#region returning-messages-chained
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
#endregion

#region date-range-rule
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
#endregion

#region complete-rule-example
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
#endregion
