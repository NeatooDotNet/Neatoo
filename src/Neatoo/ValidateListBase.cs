using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Neatoo;

/// <summary>
/// Non-generic interface for a Neatoo validation list base.
/// Provides collection change notifications, property change notifications, and access to the parent object.
/// </summary>
public interface IValidateListBase : INeatooObject, INotifyCollectionChanged, INotifyPropertyChanged, IList, INotifyNeatooPropertyChanged, IValidateMetaProperties
{
    /// <summary>
    /// Gets the parent object that owns this list.
    /// </summary>
    IValidateBase? Parent { get; }
}

/// <summary>
/// Generic interface for a Neatoo validation list that contains validatable items of type <typeparamref name="I"/>.
/// Provides collection and property change notifications, access to the parent object, and validation meta properties aggregated from all items in the list.
/// </summary>
/// <typeparam name="I">The type of items in the list, must implement <see cref="IValidateBase"/>.</typeparam>
public interface IValidateListBase<I> : IList<I>, INeatooObject, INotifyCollectionChanged, INotifyPropertyChanged, INotifyNeatooPropertyChanged, IValidateMetaProperties
    where I : IValidateBase
{
    /// <summary>
    /// Gets the parent object that owns this list.
    /// </summary>
    IValidateBase? Parent { get; }
}

/// <summary>
/// Base class for Neatoo collections that support validation.
/// Provides observable collection functionality, parent-child relationship management, property change notifications,
/// validation state aggregation from all child items, and rule execution across the collection.
/// This class serves as the foundation for all list-based Neatoo objects.
/// </summary>
/// <typeparam name="I">The type of items in the list, must implement <see cref="IValidateBase"/>.</typeparam>
[Factory]
public abstract class ValidateListBase<I> : ObservableCollection<I>, INeatooObject, IValidateListBase<I>, IValidateListBase,
                                            ISetParent, INotifyPropertyChanged, IValidateMetaProperties,
                                            IJsonOnDeserialized, IJsonOnDeserializing, IJsonOnSerialized, IJsonOnSerializing,
                                            IFactoryOnStart, IFactoryOnComplete
    where I : IValidateBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateListBase{I}"/> class.
    /// </summary>
    public ValidateListBase()
    {
        this.ResetMetaState();
    }

    /// <summary>
    /// Gets the parent object that owns this list.
    /// </summary>
    public IValidateBase? Parent { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether any item in the list is currently busy executing asynchronous operations.
    /// </summary>
    public bool IsBusy => this.Any(c => c.IsBusy);

    /// <summary>
    /// Gets a value indicating whether all items in the list are valid.
    /// Returns <c>true</c> if no items report validation errors.
    /// </summary>
    public bool IsValid => !this.Any(c => !c.IsValid);

    /// <summary>
    /// Gets a value indicating whether the list itself (not its items) is valid.
    /// Always returns <c>true</c> as lists do not have their own validation rules.
    /// </summary>
    public bool IsSelfValid => true;

    /// <summary>
    /// Gets a value indicating whether rule execution and property change events are paused.
    /// Used during deserialization and factory operations to prevent premature validation.
    /// </summary>
    [JsonIgnore]
    public bool IsPaused { get; protected set; } = false;

    /// <summary>
    /// Gets all property validation messages from all items in the collection.
    /// </summary>
    public IReadOnlyCollection<IPropertyMessage> PropertyMessages => this.SelectMany(_ => _.PropertyMessages).ToList().AsReadOnly();

    /// <summary>
    /// Gets the cached meta state for change detection.
    /// Stores the previous values of <see cref="IsValid"/>, <see cref="IsSelfValid"/>, and <see cref="IsBusy"/>.
    /// </summary>
    protected (bool IsValid, bool IsSelfValid, bool IsBusy) MetaState { get; private set; }

    /// <summary>
    /// Occurs when a Neatoo property value changes, providing detailed change information.
    /// </summary>
    public event NeatooPropertyChanged? NeatooPropertyChanged;

    void ISetParent.SetParent(IValidateBase? parent)
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
    /// Pauses rule execution to prevent validation during deserialization.
    /// </summary>
    public virtual void OnDeserializing()
    {
        this.IsPaused = true;
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
        this.ResumeAllActions();
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
    /// Checks if any validation meta properties have changed and raises appropriate property change notifications.
    /// Compares current values against cached <see cref="MetaState"/> and notifies on differences.
    /// </summary>
    protected virtual void CheckIfMetaPropertiesChanged()
    {
        if (this.MetaState.IsValid != this.IsValid)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.IsValid)));
            this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsValid), this));
        }
        if (this.MetaState.IsSelfValid != this.IsSelfValid)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.IsSelfValid)));
            this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsSelfValid), this));
        }
        if (this.MetaState.IsBusy != this.IsBusy)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.IsBusy)));
            this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsBusy), this));
        }

        this.ResetMetaState();
    }

    /// <summary>
    /// Resets the cached meta state to the current property values.
    /// Called after property change notifications to prepare for the next change detection cycle.
    /// </summary>
    protected virtual void ResetMetaState()
    {
        this.MetaState = (this.IsValid, this.IsSelfValid, this.IsBusy);
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

    /// <summary>
    /// Runs validation rules for the specified property on all items in the collection.
    /// </summary>
    /// <param name="propertyName">The name of the property whose rules should be executed.</param>
    /// <param name="token">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous rule execution.</returns>
    public async Task RunRules(string propertyName, CancellationToken? token = default)
    {
        foreach (var item in this)
        {
            await item.RunRules(propertyName, token);
        }
        this.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Runs validation rules on all items in the collection according to the specified flags.
    /// </summary>
    /// <param name="runRules">Flags indicating which rules to run. Defaults to <see cref="RunRulesFlag.All"/>.</param>
    /// <param name="token">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous rule execution.</returns>
    public async Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = default)
    {
        foreach (var item in this)
        {
            await item.RunRules(runRules, token);
        }
        this.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Clears all validation messages from all items in the collection, including messages from child objects.
    /// </summary>
    public void ClearAllMessages()
    {
        foreach (var item in this)
        {
            item.ClearAllMessages();
        }
    }

    /// <summary>
    /// Clears validation messages that belong directly to each item (not from child objects).
    /// </summary>
    public void ClearSelfMessages()
    {
        foreach (var item in this)
        {
            item.ClearSelfMessages();
        }
    }

    /// <summary>
    /// Resumes all paused actions, including rule execution and property change notifications.
    /// Resets the meta state after resuming to ensure proper change detection.
    /// </summary>
    public virtual void ResumeAllActions()
    {
        if (this.IsPaused)
        {
            this.IsPaused = false;
            this.ResetMetaState();
        }
    }

    /// <summary>
    /// Called when a factory operation is starting.
    /// Pauses rule execution during the factory operation.
    /// </summary>
    /// <param name="factoryOperation">The type of factory operation being performed.</param>
    public virtual void FactoryStart(FactoryOperation factoryOperation)
    {
        this.IsPaused = true;
    }

    /// <summary>
    /// Called when a factory operation is complete.
    /// Resumes rule execution after the factory operation finishes.
    /// </summary>
    /// <param name="factoryOperation">The type of factory operation that was performed.</param>
    public virtual void FactoryComplete(FactoryOperation factoryOperation)
    {
        this.IsPaused = false;
    }
}
