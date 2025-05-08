using Neatoo;
using Person.Ef;
using System.ComponentModel;
using System.Diagnostics;

namespace DomainModel;

public interface IIdEntityBase : IEntityBase
{
    int? Id { get; }
}

internal partial class IdEntityBase<T> : EntityBase<T>
    where T : EntityBase<T>
{
    public IdEntityBase(IEntityBaseServices<T> services) : base(services)
    {
    }

    public partial int? Id { get; set; }

    /// <summary>
    /// Get the Id from the EF model entity once it is saved
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void HandleIdPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Debug.Assert(sender as IdPropertyChangedBase != null, "Unexpected null");

        if (sender is IdPropertyChangedBase id && e.PropertyName == nameof(IdPropertyChangedBase.Id))
        {
            // If the normal setting is used sets to IsModified = true
            // TODO: Anyway to not have to define <int?> ??
            this[nameof(Id)].LoadValue(((IdPropertyChangedBase)sender).Id);
            id.PropertyChanged -= HandleIdPropertyChanged;
        }
    }
}
