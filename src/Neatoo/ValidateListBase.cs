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
    /// Cached value for IsValid property. Updated incrementally when child state changes.
    /// </summary>
    private bool _cachedIsValid = true;

    /// <summary>
    /// Cached value for IsBusy property. Updated incrementally when child state changes.
    /// </summary>
    private bool _cachedIsBusy = false;

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
    public bool IsBusy => _cachedIsBusy;

    /// <summary>
    /// Gets a value indicating whether all items in the list are valid.
    /// Returns <c>true</c> if no items report validation errors.
    /// </summary>
    public bool IsValid => _cachedIsValid;

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
    /// Updates cached meta properties based on the new item's state.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the item.</param>
    /// <param name="item">The item to insert into the collection.</param>
    protected override void InsertItem(int index, I item)
    {
        ((ISetParent)item).SetParent(this.Parent);

        base.InsertItem(index, item);

        item.PropertyChanged += this._PropertyChanged;
        item.NeatooPropertyChanged += this._NeatooPropertyChanged;

        // Update cached meta properties based on new item's state
        if (!this.IsPaused)
        {
            if (!item.IsValid)
            {
                _cachedIsValid = false;
            }
            if (item.IsBusy)
            {
                _cachedIsBusy = true;
            }
        }

        this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.Count), this));

        this.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Removes the item at the specified index from the collection.
    /// Unsubscribes from the item's property change events before removal.
    /// Updates cached meta properties based on the removed item's state.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    protected override void RemoveItem(int index)
    {
        var item = this[index];
        bool wasItemInvalid = !item.IsValid;
        bool wasItemBusy = item.IsBusy;

        item.PropertyChanged -= this._PropertyChanged;
        item.NeatooPropertyChanged -= this._NeatooPropertyChanged;

        base.RemoveItem(index);

        // Update cached meta properties based on removed item's state
        if (!this.IsPaused)
        {
            // If removed item was invalid and we were invalid, check if still invalid
            if (wasItemInvalid && !_cachedIsValid)
            {
                _cachedIsValid = !this.Any(c => !c.IsValid);
            }
            // If removed item was busy and we were busy, check if still busy
            if (wasItemBusy && _cachedIsBusy)
            {
                _cachedIsBusy = this.Any(c => c.IsBusy);
            }
        }

        this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.Count), this));

        this.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Replaces the element at the specified index.
    /// Unsubscribes from the old item's events and subscribes to the new item's events.
    /// Updates cached meta properties based on the state transition.
    /// </summary>
    /// <param name="index">The zero-based index of the element to replace.</param>
    /// <param name="item">The new item to set at the specified index.</param>
    protected override void SetItem(int index, I item)
    {
        var oldItem = this[index];
        bool oldWasInvalid = !oldItem.IsValid;
        bool oldWasBusy = oldItem.IsBusy;

        // Unsubscribe from old item
        oldItem.PropertyChanged -= this._PropertyChanged;
        oldItem.NeatooPropertyChanged -= this._NeatooPropertyChanged;

        // Set parent on new item
        ((ISetParent)item).SetParent(this.Parent);

        base.SetItem(index, item);

        // Subscribe to new item
        item.PropertyChanged += this._PropertyChanged;
        item.NeatooPropertyChanged += this._NeatooPropertyChanged;

        // Update cached meta properties based on state transition
        if (!this.IsPaused)
        {
            // Handle IsValid transition
            if (!item.IsValid)
            {
                // New item is invalid → we're definitely invalid
                _cachedIsValid = false;
            }
            else if (oldWasInvalid && !_cachedIsValid)
            {
                // Old was invalid, new is valid → may need to recalculate
                _cachedIsValid = !this.Any(c => !c.IsValid);
            }

            // Handle IsBusy transition
            if (item.IsBusy)
            {
                // New item is busy → we're definitely busy
                _cachedIsBusy = true;
            }
            else if (oldWasBusy && _cachedIsBusy)
            {
                // Old was busy, new is not busy → may need to recalculate
                _cachedIsBusy = this.Any(c => c.IsBusy);
            }
        }

        this.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Removes all items from the collection.
    /// Unsubscribes from all items' events and resets cached meta properties.
    /// </summary>
    protected override void ClearItems()
    {
        // Unsubscribe from all items
        foreach (var item in this)
        {
            item.PropertyChanged -= this._PropertyChanged;
            item.NeatooPropertyChanged -= this._NeatooPropertyChanged;
        }

        base.ClearItems();

        // Reset cache to empty list state
        _cachedIsValid = true;
        _cachedIsBusy = false;

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
    /// Updates cached meta properties based on the child's state transition.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments containing the property name.</param>
    protected virtual void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is IValidateBase child)
        {
            // Handle IsValid changes
            if (e.PropertyName == nameof(IValidateMetaProperties.IsValid))
            {
                if (!child.IsValid)
                {
                    // Child BECAME invalid → we're definitely invalid now (O(1))
                    _cachedIsValid = false;
                }
                else if (!_cachedIsValid)
                {
                    // Child BECAME valid, and we were invalid
                    // Check if any other child is still invalid (O(k) where k = first invalid)
                    _cachedIsValid = !this.Any(c => !c.IsValid);
                }
                // else: child became valid, we were already valid → no-op
            }

            // Handle IsBusy changes
            if (e.PropertyName == nameof(IValidateMetaProperties.IsBusy))
            {
                if (child.IsBusy)
                {
                    // Child BECAME busy → we're definitely busy now (O(1))
                    _cachedIsBusy = true;
                }
                else if (_cachedIsBusy)
                {
                    // Child BECAME not busy, and we were busy
                    // Check if any other child is still busy (O(k) where k = first busy)
                    _cachedIsBusy = this.Any(c => c.IsBusy);
                }
                // else: child became not busy, we were already not busy → no-op
            }
        }

        this.CheckIfMetaPropertiesChanged();
    }

    private void _PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        this.HandlePropertyChanged(sender, e);
    }

    /// <summary>
    /// Raises property changed events if the value has changed from the cached state.
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="cachedValue">The previously cached value.</param>
    /// <param name="currentValue">The current value.</param>
    /// <param name="propertyName">The name of the property.</param>
    protected void RaiseIfChanged<TValue>(TValue cachedValue, TValue currentValue, string propertyName)
    {
        if (!EqualityComparer<TValue>.Default.Equals(cachedValue, currentValue))
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
            this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(propertyName, this));
        }
    }

    /// <summary>
    /// Checks if any validation meta properties have changed and raises appropriate property change notifications.
    /// Compares current values against cached <see cref="MetaState"/> and notifies on differences.
    /// </summary>
    protected virtual void CheckIfMetaPropertiesChanged()
    {
        RaiseIfChanged(this.MetaState.IsValid, this.IsValid, nameof(this.IsValid));
        RaiseIfChanged(this.MetaState.IsSelfValid, this.IsSelfValid, nameof(this.IsSelfValid));
        RaiseIfChanged(this.MetaState.IsBusy, this.IsBusy, nameof(this.IsBusy));

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
    /// Waits for all pending asynchronous tasks in the collection to complete, with cancellation support.
    /// Continues waiting until no items report being busy or cancellation is requested.
    /// </summary>
    /// <param name="token">Cancellation token to cancel the wait operation.</param>
    /// <returns>A task that completes when all items have finished their pending operations or cancellation is requested.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the cancellation token is triggered.</exception>
    public async Task WaitForTasks(CancellationToken token)
    {
        var busyTask = this.FirstOrDefault(x => x.IsBusy)?.WaitForTasks(token);

        while (busyTask != null)
        {
            token.ThrowIfCancellationRequested();
            await busyTask;
            busyTask = this.FirstOrDefault(x => x.IsBusy)?.WaitForTasks(token);
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
    /// Recalculates cached meta properties and resets the meta state after resuming.
    /// </summary>
    public virtual void ResumeAllActions()
    {
        if (this.IsPaused)
        {
            this.IsPaused = false;

            // Recalculate cached values since items may have changed while paused
            _cachedIsValid = !this.Any(c => !c.IsValid);
            _cachedIsBusy = this.Any(c => c.IsBusy);

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
