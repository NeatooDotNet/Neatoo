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
