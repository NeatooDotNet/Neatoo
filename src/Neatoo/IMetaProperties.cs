using Neatoo.RemoteFactory;

namespace Neatoo;

public interface IBaseMetaProperties
{
    bool IsBusy { get; }
    Task WaitForTasks();
}

public interface IValidateMetaProperties : IBaseMetaProperties
{
    bool IsValid { get; }
    bool IsSelfValid { get; }
    IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }
    Task RunRules(string propertyName, CancellationToken? token = null);
    Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null);
    void ClearAllMessages();
    void ClearSelfMessages();
}

public interface IEntityMetaProperties : IFactorySaveMeta
{
    bool IsChild { get; }
    bool IsModified { get; }
    bool IsSelfModified { get; }
    bool IsMarkedModified { get; }
    bool IsSavable { get; }
}
