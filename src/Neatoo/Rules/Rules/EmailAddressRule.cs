using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace Neatoo.Rules.Rules;

public interface IEmailAddressRule : IRule
{
    string ErrorMessage { get; }
}

internal class EmailAddressRule<T> : RuleBase<T>, IEmailAddressRule
    where T : class, IValidateBase
{
    public string ErrorMessage { get; }

    public EmailAddressRule(ITriggerProperty triggerProperty, EmailAddressAttribute attribute) : base()
    {
        this.TriggerProperties.Add(triggerProperty);

        // Check if a custom error message was explicitly set (not the default template)
        var customMessage = attribute.ErrorMessage;
        if (string.IsNullOrEmpty(customMessage) || customMessage.Contains("{0}"))
        {
            this.ErrorMessage = $"{triggerProperty.PropertyName} is not a valid email address.";
        }
        else
        {
            this.ErrorMessage = customMessage;
        }
    }

    protected override IRuleMessages Execute(T target)
    {
        var value = ((ITriggerProperty<T>)this.TriggerProperties[0]).GetValue(target);

        // Null or non-string values pass - use Required for null check
        if (value is not string email || string.IsNullOrEmpty(email))
        {
            return RuleMessages.None;
        }

        // Use MailAddress.TryCreate for validation (same approach as .NET)
        if (!IsValidEmail(email))
        {
            return (this.TriggerProperties.Single().PropertyName, this.ErrorMessage).AsRuleMessages();
        }

        return RuleMessages.None;
    }

    private static bool IsValidEmail(string email)
    {
        // MailAddress.TryCreate is the .NET recommended approach
        // It handles RFC 5321/5322 compliant addresses
        if (!MailAddress.TryCreate(email, out var mailAddress))
        {
            return false;
        }

        // Additional check: the address portion should match the input
        // This catches cases like "Name <email@example.com>" which MailAddress accepts
        // but EmailAddressAttribute should reject (it expects just the address)
        return mailAddress.Address == email;
    }
}
