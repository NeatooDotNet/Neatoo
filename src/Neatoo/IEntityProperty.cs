namespace Neatoo;

public interface IEntityProperty : IValidateProperty
{
    bool IsPaused { get; set; }
    bool IsModified { get; }
    bool IsSelfModified { get; }
    void MarkSelfUnmodified();
    string DisplayName { get; }
}

public interface IEntityProperty<T> : IEntityProperty, IValidateProperty<T>
{

}