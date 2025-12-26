using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Neatoo;

/// <summary>
/// Non-generic interface for a Neatoo list base, providing collection change notifications,
/// property change notifications, and access to the parent object.
/// </summary>
public interface IListBase : INeatooObject, INotifyCollectionChanged, INotifyPropertyChanged, IList, INotifyNeatooPropertyChanged, IBaseMetaProperties
{
    /// <summary>
    /// Gets the parent object that owns this list.
    /// </summary>
    IBase? Parent { get; }
}

/// <summary>
/// Generic interface for a Neatoo list base that contains items of type <typeparamref name="I"/>.
/// Provides collection and property change notifications along with access to the parent object.
/// </summary>
/// <typeparam name="I">The type of items in the list, must implement <see cref="IBase"/>.</typeparam>
public interface IListBase<I> : IList<I>, INeatooObject, INotifyCollectionChanged, INotifyPropertyChanged, INotifyNeatooPropertyChanged, IBaseMetaProperties
    where I : IBase
{
    /// <summary>
    /// Gets the parent object that owns this list.
    /// </summary>
    IBase? Parent { get; }

}

/// <summary>
/// Base class for Neatoo collections that provides observable collection functionality,
/// parent-child relationship management, and property change notifications.
/// This class serves as the foundation for all list-based Neatoo objects.
/// </summary>
/// <typeparam name="I">The type of items in the list, must implement <see cref="IBase"/>.</typeparam>
[Factory]
public abstract class ListBase<I> : ObservableCollection<I>, INeatooObject, IListBase<I>, IListBase, ISetParent, IBaseMetaProperties,
    IJsonOnDeserialized, IJsonOnDeserializing, IJsonOnSerialized, IJsonOnSerializing
    where I : IBase
{

    /// <summary>
    /// Initializes a new instance of the <see cref="ListBase{I}"/> class.
    /// </summary>
    public ListBase()
    {
    }

    /// <summary>
    /// Gets the parent object that owns this list.
    /// </summary>
    public IBase? Parent { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether any item in the list is currently busy executing asynchronous operations.
    /// </summary>
    public bool IsBusy => this.Any(c => c.IsBusy);

    /// <summary>
    /// Occurs when a Neatoo property value changes, providing detailed change information.
    /// </summary>
    public event NeatooPropertyChanged? NeatooPropertyChanged;

    void ISetParent.SetParent(IBase parent)
    {
        // The list is not the Parent

        this.Parent = parent;

        foreach (var item in this)
        {
            if (item is ISetParent setParent)
            {
                setParent.SetParent(parent);
            }
        }
    }

    /// <summary>
    /// Inserts an item into the collection at the specified index.
    /// Sets the item's parent and subscribes to property change events.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the item.</param>
    /// <param name="item">The item to insert into the collection.</param>
    protected override void InsertItem(int index, I item)
    {
        ((ISetParent)item).SetParent(this.Parent);

        base.InsertItem(index, item);

        item.PropertyChanged += this._PropertyChanged;
        item.NeatooPropertyChanged += this._NeatooPropertyChanged;

        this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.Count), this));

        this.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Removes the item at the specified index from the collection.
    /// Unsubscribes from the item's property change events before removal.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    protected override void RemoveItem(int index)
    {

        this[index].PropertyChanged -= this._PropertyChanged;
        this[index].NeatooPropertyChanged -= this._NeatooPropertyChanged;

        base.RemoveItem(index);

        this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.Count), this));

        this.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Called after the object is constructed by the factory portal.
    /// Override to perform additional initialization after construction.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task PostPortalConstruct()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when JSON deserialization is starting.
    /// Override to perform initialization before deserialization.
    /// </summary>
    public virtual void OnDeserializing()
    {

    }

    /// <summary>
    /// Called when JSON deserialization is complete.
    /// Resubscribes to property change events for all items and sets parent references.
    /// </summary>
    public virtual void OnDeserialized()
    {
        foreach (var item in this)
        {
            item.PropertyChanged += this._PropertyChanged;
            item.NeatooPropertyChanged += this._NeatooPropertyChanged;
            if (item is ISetParent setParent)
            {
                setParent.SetParent(this.Parent);
            }
        }
    }

    /// <summary>
    /// Called when JSON serialization is complete.
    /// Override to perform cleanup after serialization.
    /// </summary>
    public virtual void OnSerialized()
    {

    }

    /// <summary>
    /// Called when JSON serialization is starting.
    /// Override to prepare the object for serialization.
    /// </summary>
    public virtual void OnSerializing()
    {

    }

    /// <summary>
    /// Raises the <see cref="NeatooPropertyChanged"/> event with the specified event arguments.
    /// </summary>
    /// <param name="eventArgs">The event arguments containing property change information.</param>
    /// <returns>A task representing the asynchronous event invocation.</returns>
    protected virtual Task RaiseNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        return this.NeatooPropertyChanged?.Invoke(eventArgs) ?? Task.CompletedTask;
    }

    /// <summary>
    /// Handles the <see cref="NeatooPropertyChanged"/> event from child items.
    /// Checks for meta property changes and propagates the event to parent listeners.
    /// </summary>
    /// <param name="eventArgs">The event arguments containing property change information.</param>
    /// <returns>A task representing the asynchronous event handling.</returns>
    protected virtual Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        this.CheckIfMetaPropertiesChanged();
        // Lists don't add to the eventArgs
        return this.RaiseNeatooPropertyChanged(eventArgs);
    }

    private Task _NeatooPropertyChanged(NeatooPropertyChangedEventArgs propertyNameBreadCrumbs)
    {
        return this.HandleNeatooPropertyChanged(propertyNameBreadCrumbs);
    }

    /// <summary>
    /// Handles the <see cref="INotifyPropertyChanged.PropertyChanged"/> event from child items.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments containing the property name.</param>
    protected virtual void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        this.CheckIfMetaPropertiesChanged();
    }

    private void _PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        this.HandlePropertyChanged(sender, e);
    }

    /// <summary>
    /// Checks if any meta properties (such as <see cref="IsBusy"/>) have changed
    /// and raises appropriate property change notifications.
    /// </summary>
    protected virtual void CheckIfMetaPropertiesChanged()
    {

    }

    /// <summary>
    /// Waits for all pending asynchronous tasks in the collection to complete.
    /// Continues waiting until no items report being busy.
    /// </summary>
    /// <returns>A task that completes when all items have finished their pending operations.</returns>
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
