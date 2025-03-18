using Neatoo.Internal;
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
    IReadOnlyList<string> ErrorMessages { get; }
    internal void SetErrorsForRule(uint ruleIndex, IReadOnlyList<string> errorMessages);
    internal void ClearErrorsForRule(uint ruleIndex);
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
    public ValidateProperty(string name, T value, string[] serializedErrorMessages, bool isReadOnly) : base(name, value, isReadOnly)
    {
        for (int i = 0; i < serializedErrorMessages.Length; i++)
        {
            SetError((uint)i, new List<string> { serializedErrorMessages[i].ToString() });
        }
    }

    public bool IsSelfValid => ValueIsValidateBase != null ? true : !RuleErrorMessages.Any();
    public bool IsValid => ValueIsValidateBase != null ? ValueIsValidateBase.IsValid : !RuleErrorMessages.Any();

    public Task RunAllRules(CancellationToken? token = null) { return ValueIsValidateBase?.RunAllRules(token) ?? Task.CompletedTask; }

    [JsonIgnore]
    public IReadOnlyList<string> ErrorMessages => RuleErrorMessages.SelectMany(r => r.Value).ToList().AsReadOnly();

    // [PortalDataMember] Ummm...ising the RuleIndex going to be different...
    protected IDictionary<uint, List<string>> RuleErrorMessages { get; } = new ConcurrentDictionary<uint, List<string>>();

    public string[] SerializedErrorMessages => RuleErrorMessages.SelectMany(r => r.Value).ToArray();

    protected void SetError(uint ruleIndex, IReadOnlyList<string> errorMessages)
    {
        Debug.Assert(ValueIsValidateBase == null, "If the Child is IValidateBase then it should be handling the errors");
        RuleErrorMessages[ruleIndex] = errorMessages.ToList();
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(ErrorMessages));
    }


    void IValidateProperty.SetErrorsForRule(uint ruleIndex, IReadOnlyList<string> errorMessages)
    {
        SetError(ruleIndex, errorMessages);
    }

    void IValidateProperty.ClearErrorsForRule(uint ruleIndex)
    {
        RuleErrorMessages.Remove(ruleIndex);
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(ErrorMessages));
    }

    public virtual void ClearSelfErrors()
    {
        RuleErrorMessages.Clear();
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(ErrorMessages));
    }

    public virtual void ClearAllErrors()
    {
        RuleErrorMessages.Clear();
        ValueIsValidateBase?.ClearAllErrors();

        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(ErrorMessages));
    }
}
