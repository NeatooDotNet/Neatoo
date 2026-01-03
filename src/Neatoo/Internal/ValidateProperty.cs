using Neatoo.Rules;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Neatoo.Internal;



[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2119:SealMethodsThatSatisfyPrivateInterfaces", Justification = "Class intentionally non-sealed for inheritance by EntityProperty")]
public class ValidateProperty<T> : Property<T>, IValidateProperty<T>, IValidatePropertyInternal
{
    [JsonIgnore]
    public virtual IValidateMetaProperties? ValueIsValidateBase => this.Value as IValidateMetaProperties;

    public ValidateProperty(IPropertyInfo propertyInfo) : base(propertyInfo) { }

    [JsonConstructor]
    public ValidateProperty(string name, T value, IRuleMessage[] serializedRuleMessages, bool isReadOnly) : base(name, value, isReadOnly)
    {
        this.RuleMessages = serializedRuleMessages.ToList();
    }

    public bool IsSelfValid => this.ValueIsValidateBase != null ? true : this.RuleMessages.Count == 0;
    public bool IsValid => this.ValueIsValidateBase != null ? this.ValueIsValidateBase.IsValid : this.RuleMessages.Count == 0;

    public Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null) { return this.ValueIsValidateBase?.RunRules(runRules, token) ?? Task.CompletedTask; }

    [JsonIgnore]
    public IReadOnlyCollection<IPropertyMessage> PropertyMessages =>
                            this.ValueIsValidateBase != null ? this.ValueIsValidateBase.PropertyMessages :
                                                                this.RuleMessages.Select(rm => new PropertyMessage(this, rm.Message)).ToList().AsReadOnly();

    [JsonIgnore]
    public List<IRuleMessage> RuleMessages { get; set; } = new List<IRuleMessage>();

    public IRuleMessage[] SerializedRuleMessages => this.RuleMessages.ToArray();

    [JsonIgnore]
    private object RuleMessagesLock { get; } = new object();

    protected void SetMessagesForRule(IReadOnlyList<IRuleMessage> ruleMessages)
    {
        Debug.Assert(this.ValueIsValidateBase == null, "If the Child is IValidateBase then it should be handling the errors");
        lock (this.RuleMessagesLock)
        {
            this.RuleMessages.RemoveAll(rm => ruleMessages.Any(rm2 => rm2.RuleIndex == rm.RuleIndex));
            this.RuleMessages.AddRange(ruleMessages.Where(rm => rm.Message != null));
        }
        this.OnPropertyChanged(nameof(IsValid));
        this.OnPropertyChanged(nameof(IsSelfValid));
        this.OnPropertyChanged(nameof(RuleMessages));
    }

    void IValidatePropertyInternal.SetMessagesForRule(IReadOnlyList<IRuleMessage> ruleMessages)
    {
        this.SetMessagesForRule(ruleMessages);
    }

    void IValidatePropertyInternal.ClearMessagesForRule(uint ruleIndex)
    {
        this.RuleMessages.RemoveAll(rm => rm.RuleIndex == ruleIndex);
        this.OnPropertyChanged(nameof(IsValid));
        this.OnPropertyChanged(nameof(IsSelfValid));
        this.OnPropertyChanged(nameof(RuleMessages));
    }

    void IValidatePropertyInternal.ClearSelfMessages()
    {
        this.RuleMessages.Clear();
        this.OnPropertyChanged(nameof(IsValid));
        this.OnPropertyChanged(nameof(IsSelfValid));
        this.OnPropertyChanged(nameof(RuleMessages));
    }

    void IValidatePropertyInternal.ClearAllMessages()
    {
        this.RuleMessages.Clear();
        this.ValueIsValidateBase?.ClearAllMessages();

        this.OnPropertyChanged(nameof(IsValid));
        this.OnPropertyChanged(nameof(IsSelfValid));
        this.OnPropertyChanged(nameof(RuleMessages));
    }
}
