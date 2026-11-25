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
    /// <remarks>
    /// Read from the items, each of which reads its own running tasks. Nothing is cached, so
    /// nothing can go stale.
    /// </remarks>
    public bool IsBusy => this.Any(item => item.IsBusy);

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

        if (item.IsBusy)
        {
            this._busyMayHaveChanged = true;
        }

        // Update cached meta properties based on new item's state
        if (!this.IsPaused)
        {
            if (!item.IsValid)
            {
                _cachedIsValid = false;
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
        this._busyMayHaveChanged = true;
        var item = this[index];
        bool wasItemInvalid = !item.IsValid;

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
        this._busyMayHaveChanged = true;
        var oldItem = this[index];
        bool oldWasInvalid = !oldItem.IsValid;

        // Unsubscribe from old item
        oldItem.PropertyChanged -= this._PropertyChanged;
        oldItem.NeatooPropertyChanged -= this._NeatooPropertyChanged;

        // Set parent on new item
        ((ISetParent)item).SetParent(this.Parent);

        base.SetItem(index, item);

        // Subscribe to new item
        item.PropertyChanged += this._PropertyChanged;
        item.NeatooPropertyChanged += this._NeatooPropertyChanged;

        if (item.IsBusy)
        {
            this._busyMayHaveChanged = true;
        }

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
        }

        this.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Removes all items from the collection.
    /// Unsubscribes from all items' events and resets cached meta properties.
    /// </summary>
    protected override void ClearItems()
    {
        this._busyMayHaveChanged = true;
        // Unsubscribe from all items
        foreach (var item in this)
        {
            item.PropertyChanged -= this._PropertyChanged;
            item.NeatooPropertyChanged -= this._NeatooPropertyChanged;
        }

        base.ClearItems();

        // Reset cache to empty list state
        _cachedIsValid = true;

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
    /// Sets parent references for all items and resumes.
    /// </summary>
    /// <remarks>
    /// It does not subscribe to the items. <see cref="InsertItem"/> did when the serializer added
    /// each one: a second subscription here made the list handle every item change twice.
    /// </remarks>
    public virtual void OnDeserialized()
    {
        foreach (var item in this)
        {
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
        if (e.PropertyName == nameof(IValidateMetaProperties.IsBusy))
        {
            this._busyMayHaveChanged = true;
        }

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
        RaiseIfChanged(this.MetaState.IsBusy, this.BusyForChangeDetection(), nameof(this.IsBusy));

        this.ResetMetaState();
    }

    // True when something has happened that can change whether an item is busy: an item said
    // so, or the items themselves changed. Starts true so the first check reads the items.
    private bool _busyMayHaveChanged = true;

    /// <summary>
    /// Busy, for deciding whether to raise the list's IsBusy notification. It reads the items
    /// only when one of them may have changed; otherwise it is the last reading.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: <see cref="IsBusy"/> itself always reads the items, so its value is never
    /// stale. Only this check, which runs on every notification from every item, skips the read
    /// when nothing that could change busy has happened. Reading the items there each time made
    /// one edit in a list of 20,000 cost a hundred times more.
    /// DID NOT DO THIS: answer IsBusy from a flag kept up to date by notifications.
    /// </remarks>
    private bool BusyForChangeDetection()
    {
        return this._busyMayHaveChanged ? this.IsBusy : this.MetaState.IsBusy;
    }

    /// <summary>
    /// Resets the cached meta state to the current property values.
    /// Called after property change notifications to prepare for the next change detection cycle.
    /// </summary>
    protected virtual void ResetMetaState()
    {
        this.MetaState = (this.IsValid, this.IsSelfValid, this.BusyForChangeDetection());
        this._busyMayHaveChanged = false;
    }

    /// <summary>
    /// Waits for the items' work in flight to finish.
    /// </summary>
    /// <returns>A task that completes when no item has work in flight.</returns>
    /// <remarks>
    /// DESIGN DECISION: await the items' own waits, and go round again only while one of them
    /// had something left to await, because finishing work can start more. A list holds no
    /// running tasks of its own: its items' work is registered with the object that owns the
    /// list, and that object's wait is the one that counts.
    /// DID NOT DO THIS: loop while an item reports busy. A busy flag gives nothing to await.
    /// </remarks>
    public Task WaitForTasks()
    {
        return this.WaitForItems(static item => item.WaitForTasks());
    }

    /// <summary>
    /// Waits for the items' work in flight to finish, with cancellation of the wait.
    /// </summary>
    /// <param name="token">Cancels the wait. The work runs to completion.</param>
    /// <returns>A task that completes when no item has work in flight or cancellation is requested.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the cancellation token is triggered.</exception>
    public Task WaitForTasks(CancellationToken token)
    {
        return this.WaitForItems(item => item.WaitForTasks(token));
    }

    private async Task WaitForItems(Func<I, Task> waitFor)
    {
        while (true)
        {
            // Only busy items are asked: asking an idle item for its wait would take the fault
            // its tracker holds for its own first wait.
            var waits = this.Where(item => item.IsBusy).Select(waitFor).ToList();

            if (waits.All(wait => wait.IsCompletedSuccessfully))
            {
                return;
            }

            await Task.WhenAll(waits);
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
            this._busyMayHaveChanged = true;

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
    /// <remarks>
    /// Resumes through <see cref="ResumeAllActions"/> rather than clearing
    /// <see cref="IsPaused"/> directly: items added while paused do not update the cached
    /// meta state (see InsertItem), so the caches must be recalculated on the way out or a
    /// list populated by its own factory operation reports the state of an empty list.
    /// </remarks>
    public virtual void FactoryComplete(FactoryOperation factoryOperation)
    {
        this.ResumeAllActions();
    }
}
