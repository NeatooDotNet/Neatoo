namespace Neatoo.Rules;

public interface IRuleMessage
{
    uint RuleIndex { get; internal set; }
    string PropertyName { get; }
    string Message { get; }
}

public record RuleMessage : IRuleMessage
{
    public uint RuleIndex { get; set; }
    public string PropertyName { get; }
    public string Message { get; }
    public RuleMessage(string propertyName, string message)
    {
        PropertyName = propertyName;
        Message = message;
    }

    public static implicit operator RuleMessage((string name, string errorMessage) ruleMessage)
    {
        return new RuleMessage(ruleMessage.name, ruleMessage.errorMessage);
    }

}

public static class PropertyRuleMessageExtension
{
    public static IRuleMessage RuleMessage(this string propertyName, string message)
    {
        return new RuleMessage(propertyName, message);
    }

    public static IRuleMessages RuleMessages(this string propertyName, string message)
    {
        return (propertyName, message).AsRuleMessages();
    }

    public static IRuleMessages AsRuleMessages(this (string propertyName, string message) ruleMessage)
    {
        return new RuleMessages() { new RuleMessage(ruleMessage.propertyName, ruleMessage.message) };
    }

    public static IRuleMessages AsRuleMessages(this (string propertyName, string errorMessage)[] ruleMessage)
    {
        var ruleMessages = new RuleMessages();
        foreach (var rm in ruleMessage)
        {
            ruleMessages.Add(rm.propertyName, rm.errorMessage);
        }
        return ruleMessages;
    }
}

public interface IRuleMessages : IList<IRuleMessage>
{
    void Add(string propertyName, string message);
    public static IRuleMessages None = new RuleMessages();
}

public class RuleMessages : List<IRuleMessage>, IRuleMessages
{

    public static RuleMessages None = new RuleMessages();

    public void Add(string propertyName, string message)
    {
        Add(new RuleMessage(propertyName, message));
    }
}
