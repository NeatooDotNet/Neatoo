using Neatoo;
using System.ComponentModel;
using System.Diagnostics;

#if !CLIENT 
using HorseBarn.Dal.Ef;
#endif

namespace HorseBarn.lib;

internal interface ICustomEditBase<T>
{
    int? Id { get; }
}

internal abstract class CustomEditBase<T> : EditBase<T>
    where T : CustomEditBase<T>
{
    public CustomEditBase(IEditBaseServices<T> services) : base(services)
    {
    }

    public int? Id { get => Getter<int?>(); set => Setter(value); }

#if !CLIENT

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

#endif

}
