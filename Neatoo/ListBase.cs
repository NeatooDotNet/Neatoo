using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Neatoo;

public interface IListBase : INeatooObject, INotifyCollectionChanged, INotifyPropertyChanged, IList, INotifyNeatooPropertyChanged, IBaseMetaProperties
{
    IBase? Parent { get; }
}

public interface IListBase<I> : IList<I>, INeatooObject, INotifyCollectionChanged, INotifyPropertyChanged, INotifyNeatooPropertyChanged, IBaseMetaProperties
    where I : IBase
{
    IBase? Parent { get; }

}

[Factory]
public abstract class ListBase<I> : ObservableCollection<I>, INeatooObject, IListBase<I>, IListBase, ISetParent, IBaseMetaProperties,
    IJsonOnDeserialized, IJsonOnDeserializing, IJsonOnSerialized, IJsonOnSerializing
    where I : IBase
{

    public ListBase()
    {
    }

    public IBase? Parent { get; protected set; }

    public bool IsBusy => this.Any(c => c.IsBusy);
    public bool IsSelfBusy => false;
    public event NeatooPropertyChanged? NeatooPropertyChanged;

    void ISetParent.SetParent(IBase parent)
    {
        // The list is not the Parent

        Parent = parent;

        foreach (var item in this)
        {
            if (item is ISetParent setParent)
            {
                setParent.SetParent(parent);
            }
        }
    }

    protected override void InsertItem(int index, I item)
    {
        ((ISetParent)item).SetParent(this.Parent);

        base.InsertItem(index, item);

        item.PropertyChanged += _PropertyChanged;
        item.NeatooPropertyChanged += _NeatooPropertyChanged;

        RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(Count), this));

        CheckIfMetaPropertiesChanged();
    }

    protected override void RemoveItem(int index)
    {

        this[index].PropertyChanged -= _PropertyChanged;
        this[index].NeatooPropertyChanged -= _NeatooPropertyChanged;

        base.RemoveItem(index);

        RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(Count), this));

        CheckIfMetaPropertiesChanged();
    }

    protected virtual Task PostPortalConstruct()
    {
        return Task.CompletedTask;
    }

    public virtual void OnDeserializing()
    {

    }

    public virtual void OnDeserialized()
    {
        foreach (var item in this)
        {
            item.PropertyChanged += _PropertyChanged;
            item.NeatooPropertyChanged += _NeatooPropertyChanged;
            if (item is ISetParent setParent)
            {
                setParent.SetParent(this.Parent);
            }
        }
    }

    public virtual void OnSerialized()
    {
        
    }

    public virtual void OnSerializing()
    {
        
    }

    protected virtual Task RaiseNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        return NeatooPropertyChanged?.Invoke(eventArgs) ?? Task.CompletedTask;
    }

    protected virtual Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        CheckIfMetaPropertiesChanged();
        // Lists don't add to the eventArgs
        return RaiseNeatooPropertyChanged(eventArgs);
    }

    private Task _NeatooPropertyChanged(NeatooPropertyChangedEventArgs propertyNameBreadCrumbs)
    {
        return HandleNeatooPropertyChanged(propertyNameBreadCrumbs);
    }

    protected virtual void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        CheckIfMetaPropertiesChanged(true);
    }

    private void _PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        HandlePropertyChanged(sender, e);
    }

    protected virtual void CheckIfMetaPropertiesChanged(bool raiseBusy = false)
    {

    }

    public async Task WaitForTasks()
    {
        var busyTask = this.FirstOrDefault(x => x.IsBusy)?.WaitForTasks();

        while (busyTask != null)
        {
            await busyTask;
            busyTask = this.FirstOrDefault(x => x.IsBusy)?.WaitForTasks();
        }
    }

}
