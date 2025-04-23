using Neatoo.Rules;

namespace Neatoo;

public interface IValidatePropertyManager<out P> : IPropertyManager<P>
    where P : IProperty
{
    // Valid without looking at Children that are IValidateBase
    bool IsSelfValid { get; }
    bool IsValid { get; }
    Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null);
    IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }
    bool IsPaused { get; }
    void PauseAllActions();
    void ResumeAllActions();
    void ClearAllMessages();
    void ClearSelfMessages();
}
