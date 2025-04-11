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
    void ClearAllMessages();
    void ClearSelfMessages();
}
