using Neatoo.Internal;
using Neatoo.Rules;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Neatoo.Core;

public interface IValidateProperty : IProperty, INotifyPropertyChanged
{
    bool IsSelfValid { get; }
    bool IsValid { get; }
    Task RunAllRules(CancellationToken? token = null);
    IReadOnlyCollection<IRuleMessage> RuleMessages { get; }
    internal void SetMessagesForRule(IReadOnlyList<IRuleMessage> ruleMessages);
    internal void ClearMessagesForRule(uint ruleIndex);
    internal void ClearAllErrors();
    internal void ClearSelfErrors();
}

public interface IValidateProperty<T> : IValidateProperty, IProperty<T>
{
}

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

    public bool IsSelfValid => ValueIsValidateBase != null ? true : !RuleMessages.Any();
    public bool IsValid => ValueIsValidateBase != null ? ValueIsValidateBase.IsValid : !RuleMessages.Any();

    public Task RunAllRules(CancellationToken? token = null) { return ValueIsValidateBase?.RunAllRules(token) ?? Task.CompletedTask; }

    [JsonIgnore]
    IReadOnlyCollection<IRuleMessage> IValidateProperty.RuleMessages => 
                            ValueIsValidateBase != null ? ValueIsValidateBase.RuleMessages :                                                   
                                                                RuleMessages.AsReadOnly();

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
        OnPropertyChanged(nameof(RuleMessages));
    }

    public virtual void ClearSelfErrors()
    {
        RuleMessages.Clear();
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(RuleMessages));
    }

    public virtual void ClearAllErrors()
    {
        RuleMessages.Clear();
        ValueIsValidateBase?.ClearAllErrors();

        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(RuleMessages));
    }
}
