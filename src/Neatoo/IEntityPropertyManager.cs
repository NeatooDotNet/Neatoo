namespace Neatoo;

public interface IEntityPropertyManager : IValidatePropertyManager<IEntityProperty>
{
    bool IsModified { get; }
    bool IsSelfModified { get; }

    IEnumerable<string> ModifiedProperties { get; }
    void MarkSelfUnmodified();
}
