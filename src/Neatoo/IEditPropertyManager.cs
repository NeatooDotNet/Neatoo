namespace Neatoo;

public interface IEditPropertyManager : IValidatePropertyManager<IEditProperty>
{
    bool IsModified { get; }
    bool IsSelfModified { get; }

    IEnumerable<string> ModifiedProperties { get; }
    void MarkSelfUnmodified();

    void PauseAllActions();
    void ResumeAllActions();
}
