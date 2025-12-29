using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Neatoo.Rules.Rules;

public interface IRegularExpressionRule : IRule
{
    string ErrorMessage { get; }
    string Pattern { get; }
}

internal class RegularExpressionRule<T> : RuleBase<T>, IRegularExpressionRule
    where T : class, IValidateBase
{
    public string ErrorMessage { get; }
    public string Pattern { get; }
    private readonly Regex _regex;

    public RegularExpressionRule(ITriggerProperty triggerProperty, RegularExpressionAttribute attribute) : base()
    {
        this.TriggerProperties.Add(triggerProperty);
        this.Pattern = attribute.Pattern;
        this.ErrorMessage = attribute.ErrorMessage ?? $"{triggerProperty.PropertyName} is not in the correct format.";

        var options = RegexOptions.Compiled;
        if (attribute.MatchTimeoutInMilliseconds > 0)
        {
            _regex = new Regex(Pattern, options, TimeSpan.FromMilliseconds(attribute.MatchTimeoutInMilliseconds));
        }
        else
        {
            _regex = new Regex(Pattern, options);
        }
    }

    protected override IRuleMessages Execute(T target)
    {
        var value = ((ITriggerProperty<T>)this.TriggerProperties[0]).GetValue(target);

        // Null or non-string values pass - use Required for null check
        if (value is not string s || string.IsNullOrEmpty(s))
        {
            return RuleMessages.None;
        }

        if (!_regex.IsMatch(s))
        {
            return (this.TriggerProperties.Single().PropertyName, this.ErrorMessage).AsRuleMessages();
        }

        return RuleMessages.None;
    }
}
