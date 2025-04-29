using Neatoo.Rules;
using System.ComponentModel;

namespace Neatoo;

public interface IValidateProperty : IProperty, INotifyPropertyChanged
{
    bool IsSelfValid { get; }
    bool IsValid { get; }
    Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null);
    IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }
    internal void SetMessagesForRule(IReadOnlyList<IRuleMessage> ruleMessages);
    internal void ClearMessagesForRule(uint ruleIndex);
    internal void ClearAllMessages();
    internal void ClearSelfMessages();
}

public interface IValidateProperty<T> : IValidateProperty, IProperty<T>
{
}