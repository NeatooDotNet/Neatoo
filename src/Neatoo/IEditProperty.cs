namespace Neatoo;

public interface IEditProperty : IValidateProperty
{
    bool IsPaused { get; set; }
    bool IsModified { get; }
    bool IsSelfModified { get; }
    void MarkSelfUnmodified();
    string DisplayName { get; }
}

public interface IEditProperty<T> : IEditProperty, IValidateProperty<T>
{

}