using Neatoo.Rules;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Neatoo.Internal;



public class ValidateProperty<T> : Property<T>, IValidateProperty<T>
{
    [JsonIgnore]
    public virtual IValidateMetaProperties? ValueIsValidateBase => Value as IValidateMetaProperties;

    public ValidateProperty(IPropertyInfo propertyInfo) : base(propertyInfo) { }

    [JsonConstructor]
    public ValidateProperty(string name, T value, IRuleMessage[] serializedRuleMessages, bool isReadOnly) : base(name, value, isReadOnly)
    {
        RuleMessages = serializedRuleMessages.ToList();
    }

    public bool IsSelfValid => ValueIsValidateBase != null ? true : RuleMessages.Count == 0;
    public bool IsValid => ValueIsValidateBase != null ? ValueIsValidateBase.IsValid : RuleMessages.Count == 0;

    public Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null) { return ValueIsValidateBase?.RunRules(runRules, token) ?? Task.CompletedTask; }

    [JsonIgnore]
    public IReadOnlyCollection<IPropertyMessage> PropertyMessages => 
                            ValueIsValidateBase != null ? ValueIsValidateBase.PropertyMessages :                                                   
                                                                RuleMessages.Select(rm => new PropertyMessage(this, rm.Message)).ToList().AsReadOnly();

    [JsonIgnore]
    public List<IRuleMessage> RuleMessages { get; set; } = new List<IRuleMessage>();

    public IRuleMessage[] SerializedRuleMessages => RuleMessages.ToArray();

    [JsonIgnore]
    private object RuleMessagesLock { get; } = new object();

    protected void SetMessagesForRule(IReadOnlyList<IRuleMessage> ruleMessages)
    {
        Debug.Assert(ValueIsValidateBase == null, "If the Child is IValidateBase then it should be handling the errors");
        lock (RuleMessagesLock)
        {
            RuleMessages.RemoveAll(rm => ruleMessages.Any(rm2 => rm2.RuleIndex == rm.RuleIndex));
            RuleMessages.AddRange(ruleMessages);
        }
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(IsSelfValid));
        OnPropertyChanged(nameof(RuleMessages));
    }

    void IValidateProperty.SetMessagesForRule(IReadOnlyList<IRuleMessage> ruleMessages)
    {
        SetMessagesForRule(ruleMessages);
    }

    void IValidateProperty.ClearMessagesForRule(uint ruleIndex)
    {
        RuleMessages.RemoveAll(rm => rm.RuleIndex == ruleIndex);
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(IsSelfValid));
        OnPropertyChanged(nameof(RuleMessages));
    }

    public virtual void ClearSelfMessages()
    {
        RuleMessages.Clear();
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(IsSelfValid));
        OnPropertyChanged(nameof(RuleMessages));
    }

    public virtual void ClearAllMessages()
    {
        RuleMessages.Clear();
        ValueIsValidateBase?.ClearAllMessages();

        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(IsSelfValid));
        OnPropertyChanged(nameof(RuleMessages));
    }
}
