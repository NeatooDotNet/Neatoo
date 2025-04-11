using Neatoo.RemoteFactory;

namespace Neatoo;


[Flags]
public enum RunRulesFlag
{
    None = 0,
    NoMessages = 1,
    Messages = 2,
    NotExecuted = 4,
    Executed = 8,
    Self = 16,
    All = NoMessages | Messages | NotExecuted | Executed | RunRulesFlag.Self
}

public interface IBaseMetaProperties
{
    bool IsBusy { get; }
    bool IsSelfBusy { get; }
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

public interface IEditMetaProperties : IFactorySaveMeta
{
    bool IsChild { get; }
    bool IsModified { get; }
    bool IsSelfModified { get; }
    bool IsMarkedModified { get; }
    bool IsSavable { get; }
}
