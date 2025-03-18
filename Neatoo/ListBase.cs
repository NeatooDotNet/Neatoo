using Neatoo.Core;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Neatoo;


public interface IReadOnlyListBase<I> : INeatooObject, INotifyCollectionChanged, INotifyPropertyChanged, IReadOnlyCollection<I>, IReadOnlyList<I>
    where I : IBase
{

}


public interface IListBase : INeatooObject, INotifyCollectionChanged, INotifyPropertyChanged, IEnumerable, ICollection, IList, INotifyNeatooPropertyChanged, IBaseMetaProperties
{
    IBase? Parent { get; }
}

public interface IListBase<I> : IListBase, IReadOnlyListBase<I>, IEnumerable<I>, ICollection<I>, IList<I>
    where I : IBase
{
    new int Count { get; }
}

[Factory]
public abstract class ListBase<I> : ObservableCollection<I>, INeatooObject, IListBase<I>, IListBase, IReadOnlyListBase<I>, ISetParent, IJsonOnDeserialized, IJsonOnDeserializing, IBaseMetaProperties
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

        RaiseNeatooPropertyChanged(new PropertyChangedBreadCrumbs(nameof(Count), this));
    }

    protected override void RemoveItem(int index)
    {

        this[index].PropertyChanged -= _PropertyChanged;
        this[index].NeatooPropertyChanged -= _NeatooPropertyChanged;

        base.RemoveItem(index);

        RaiseNeatooPropertyChanged(new PropertyChangedBreadCrumbs(nameof(Count), this));
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

    protected virtual Task RaiseNeatooPropertyChanged(PropertyChangedBreadCrumbs breadCrumbs)
    {
        return NeatooPropertyChanged?.Invoke(breadCrumbs) ?? Task.CompletedTask;
    }

    protected virtual Task HandleNeatooPropertyChanged(PropertyChangedBreadCrumbs breadCrumbs)
    {
        // Lists don't add to the breadcrumbs
        return RaiseNeatooPropertyChanged(breadCrumbs);
    }

    private Task _NeatooPropertyChanged(PropertyChangedBreadCrumbs propertyNameBreadCrumbs)
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
