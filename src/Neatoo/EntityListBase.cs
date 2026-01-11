using Neatoo.RemoteFactory;
using System.Collections;
using System.ComponentModel;

namespace Neatoo;


/// <summary>
/// Non-generic interface for a Neatoo entity list that supports persistence tracking.
/// Provides access to entity meta properties.
/// </summary>
public interface IEntityListBase : IValidateListBase, IEntityMetaProperties
{
    /// <summary>
    /// Gets the aggregate root of the object graph this list belongs to.
    /// </summary>
    /// <value>
    /// The aggregate root, or <c>null</c> if this list is not yet part of an aggregate.
    /// </value>
    IValidateBase? Root { get; }
}

/// <summary>
/// Generic interface for a Neatoo entity list that contains entity items of type <typeparamref name="I"/>.
/// Supports persistence tracking with deleted item management and entity meta properties.
/// </summary>
/// <typeparam name="I">The type of items in the list, must implement <see cref="IEntityBase"/>.</typeparam>
public interface IEntityListBase<I> : IValidateListBase<I>, IEntityMetaProperties
    where I : IEntityBase
{
    /// <summary>
    /// Removes the item at the specified index, marking it as deleted if it has been persisted.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    new void RemoveAt(int index);
}

/// <summary>
/// Base class for Neatoo entity collections that support persistence tracking.
/// Manages deleted items, modification state, and provides entity lifecycle management across the collection.
/// </summary>
/// <typeparam name="I">The type of items in the list, must implement <see cref="IEntityBase"/>.</typeparam>
[Factory]
public abstract class EntityListBase<I> : ValidateListBase<I>, INeatooObject, IEntityListBase<I>, IEntityListBase, IEntityListBaseInternal
    where I : IEntityBase
{
    /// <summary>
    /// Cached value for IsModified property (children only, not DeletedList).
    /// Updated incrementally when child state changes.
    /// </summary>
    private bool _cachedChildrenModified = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityListBase{I}"/> class.
    /// </summary>
    public EntityListBase() : base()
    {

    }

    /// <summary>
    /// Gets a value indicating whether any item in the list has been modified or any items are pending deletion.
    /// </summary>
    public bool IsModified => _cachedChildrenModified || this.DeletedList.Any();

    /// <summary>
    /// Gets a value indicating whether the list itself has been modified.
    /// Always returns <c>false</c> as lists do not have their own modifiable properties.
    /// </summary>
    public bool IsSelfModified => false;

    /// <summary>
    /// Gets a value indicating whether the list has been explicitly marked as modified.
    /// Always returns <c>false</c> as lists cannot be explicitly marked as modified.
    /// </summary>
    public bool IsMarkedModified => false;

    /// <summary>
    /// Gets a value indicating whether the list can be saved.
    /// Always returns <c>false</c> as lists are saved through their parent entity.
    /// </summary>
    public bool IsSavable => false;

    /// <summary>
    /// Gets a value indicating whether the list is new (not yet persisted).
    /// Always returns <c>false</c> as lists do not have their own persistence state.
    /// </summary>
    public bool IsNew => false;

    /// <summary>
    /// Gets a value indicating whether the list is marked for deletion.
    /// Always returns <c>false</c> as lists do not have their own deletion state.
    /// </summary>
    public bool IsDeleted => false;

    /// <summary>
    /// Gets a value indicating whether the list is a child of another entity.
    /// Always returns <c>false</c> as lists manage child relationships through their items.
    /// </summary>
    public bool IsChild => false;

    /// <summary>
    /// Gets the aggregate root of the object graph this list belongs to.
    /// </summary>
    /// <value>
    /// The aggregate root, or <c>null</c> if this list is not yet part of an aggregate.
    /// </value>
    /// <remarks>
    /// For entity lists, the Root is computed by checking if the Parent implements <see cref="IEntityBase"/>
    /// and returning its Root. If the Parent has no Root (meaning Parent is the aggregate root),
    /// then Parent itself is returned.
    /// </remarks>
    public IValidateBase? Root => (Parent as IEntityBase)?.Root ?? Parent;

    /// <summary>
    /// Gets the collection of items that have been removed from the list but need to be deleted during persistence.
    /// </summary>
    protected List<I> DeletedList { get; } = new List<I>();

    /// <summary>
    /// Gets the cached entity meta state for change detection.
    /// Stores the previous values of <see cref="IsModified"/>, <see cref="IsSelfModified"/>, and <see cref="IsSavable"/>.
    /// </summary>
    protected (bool IsModified, bool IsSelfModified, bool IsSavable) EntityMetaState { get; private set; }

    /// <summary>
    /// Gets the deleted list for internal access by the framework.
    /// </summary>
    IEnumerable IEntityListBaseInternal.DeletedList => this.DeletedList;

    /// <summary>
    /// Removes an item from the deleted list.
    /// Called during intra-aggregate moves.
    /// </summary>
    /// <param name="item">The entity to remove from the deleted list.</param>
    void IEntityListBaseInternal.RemoveFromDeletedList(IEntityBase item)
    {
        this.DeletedList.Remove((I)item);
    }

    /// <summary>
    /// Handles the <see cref="INotifyPropertyChanged.PropertyChanged"/> event from child items.
    /// Updates cached IsModified property based on the child's state transition.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments containing the property name.</param>
    protected override void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is IEntityBase child && e.PropertyName == nameof(IEntityMetaProperties.IsModified))
        {
            if (child.IsModified)
            {
                // Child BECAME modified → we're definitely modified now (O(1))
                _cachedChildrenModified = true;
            }
            else if (_cachedChildrenModified)
            {
                // Child BECAME unmodified, and we were modified
                // Check if any other child is still modified (O(k) where k = first modified)
                _cachedChildrenModified = this.Any(c => c.IsModified);
            }
            // else: child became unmodified, we were already unmodified → no-op
        }

        base.HandlePropertyChanged(sender, e);
    }

    /// <summary>
    /// Checks if any entity meta properties have changed and raises appropriate property change notifications.
    /// Compares current values against cached <see cref="EntityMetaState"/> and notifies on differences.
    /// </summary>
    protected override void CheckIfMetaPropertiesChanged()
    {
        base.CheckIfMetaPropertiesChanged();

        RaiseIfChanged(this.EntityMetaState.IsModified, this.IsModified, nameof(this.IsModified));
        RaiseIfChanged(this.EntityMetaState.IsSelfModified, this.IsSelfModified, nameof(this.IsSelfModified));
        RaiseIfChanged(this.EntityMetaState.IsSavable, this.IsSavable, nameof(this.IsSavable));

        this.ResetMetaState();
    }

    /// <summary>
    /// Resets the cached meta state to the current property values.
    /// Includes both validation and entity meta state reset.
    /// </summary>
    protected override void ResetMetaState()
    {
        base.ResetMetaState();
        this.EntityMetaState = (this.IsModified, this.IsSelfModified, this.IsSavable);
    }

    /// <summary>
    /// Inserts an item into the collection at the specified index.
    /// When not paused, handles entity state management including undeleting previously deleted items,
    /// marking existing items as modified, setting child status, and managing ContainingList.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the item.</param>
    /// <param name="item">The entity item to insert into the collection.</param>
    protected override void InsertItem(int index, I item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!this.IsPaused)
        {
            // Prevent adding the same item twice
            if (this.Contains(item))
            {
                throw new InvalidOperationException(
                    $"Cannot add {item.GetType().Name} to list: item is already in this list.");
            }

            // Prevent adding busy items (async rules running)
            if (item.IsBusy)
            {
                throw new InvalidOperationException(
                    $"Cannot add {item.GetType().Name} to list: item is busy (async rules running).");
            }

            // Prevent adding items from a different aggregate
            if (item.Root != null && item.Root != this.Root)
            {
                throw new InvalidOperationException(
                    $"Cannot add {item.GetType().Name} to list: " +
                    $"item belongs to aggregate '{item.Root.GetType().Name}', " +
                    $"but this list belongs to aggregate '{this.Root?.GetType().Name ?? "none"}'.");
            }

            // Cast to internal interface for ContainingList access
            var itemInternal = (IEntityBaseInternal)item;

            // Handle re-add or intra-aggregate move
            if (itemInternal.ContainingList != null)
            {
                var oldList = (IEntityListBaseInternal)itemInternal.ContainingList;
                oldList.RemoveFromDeletedList(item);
            }

            if (item.IsDeleted)
            {
                item.UnDelete();
            }

            if (!item.IsNew)
            {
                itemInternal.MarkModified();
            }

            itemInternal.MarkAsChild();

            // Set ContainingList to this list
            itemInternal.SetContainingList(this);

            // Update cached modified state - item will be modified after MarkAsChild/MarkModified
            if (item.IsModified)
            {
                _cachedChildrenModified = true;
            }
        }
        else
        {
            if (item.IsDeleted)
            {
                this.DeletedList.Add(item);
                return;
            }
        }

        base.InsertItem(index, item);
    }

    /// <summary>
    /// Removes the item at the specified index from the collection.
    /// When not paused, marks non-new items as deleted and adds them to the <see cref="DeletedList"/>
    /// for persistence during save operations. ContainingList stays set to track ownership.
    /// Updates cached modified state based on the removed item's state.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    protected override void RemoveItem(int index)
    {
        bool wasItemModified = false;

        if (!this.IsPaused)
        {
            var item = this[index];
            wasItemModified = item.IsModified;

            if (!item.IsNew)
            {
                // Use MarkDeleted via internal interface to avoid recursion with Delete()
                ((IEntityBaseInternal)item).MarkDeleted();
                this.DeletedList.Add(item);
            }

            // NOTE: ContainingList stays set - item is still "owned" by this list for persistence.
            // ContainingList is cleared only after save in FactoryComplete.
        }

        base.RemoveItem(index);

        // Update cached modified state if needed
        // Note: We don't need to recalculate here for deleted items going to DeletedList
        // because IsModified already checks DeletedList.Any()
        if (!this.IsPaused && wasItemModified && _cachedChildrenModified)
        {
            // Removed a modified item, check if any others are still modified
            _cachedChildrenModified = this.Any(c => c.IsModified);
        }
    }

    /// <summary>
    /// Replaces the element at the specified index.
    /// Updates cached modified state based on the state transition.
    /// </summary>
    /// <param name="index">The zero-based index of the element to replace.</param>
    /// <param name="item">The new item to set at the specified index.</param>
    protected override void SetItem(int index, I item)
    {
        bool oldWasModified = false;

        if (!this.IsPaused)
        {
            oldWasModified = this[index].IsModified;
        }

        base.SetItem(index, item);

        // Update cached modified state
        if (!this.IsPaused)
        {
            if (item.IsModified)
            {
                // New item is modified → we're definitely modified
                _cachedChildrenModified = true;
            }
            else if (oldWasModified && _cachedChildrenModified)
            {
                // Old was modified, new is not → may need to recalculate
                _cachedChildrenModified = this.Any(c => c.IsModified);
            }
        }
    }

    /// <summary>
    /// Removes all items from the collection.
    /// Resets cached modified state.
    /// </summary>
    protected override void ClearItems()
    {
        base.ClearItems();

        // Reset cache to empty list state (no children modified)
        // Note: DeletedList is NOT cleared here - that happens in FactoryComplete
        _cachedChildrenModified = false;
    }

    /// <summary>
    /// Called when JSON deserialization is starting.
    /// Pauses entity tracking to prevent modification during deserialization.
    /// </summary>
    public override void OnDeserializing()
    {
        base.OnDeserializing();
        this.IsPaused = true;
    }

    /// <summary>
    /// Called when JSON serialization is starting.
    /// Override to prepare the entity list for serialization.
    /// </summary>
    public override void OnSerializing()
    {
        base.OnSerializing();
    }

    /// <summary>
    /// Called when a factory operation is complete.
    /// Clears the <see cref="DeletedList"/> after an update operation since deleted items have been persisted.
    /// Also clears ContainingList on deleted items since deletion is now persisted.
    /// Recalculates cached modified state after DeletedList is cleared.
    /// </summary>
    /// <param name="factoryOperation">The type of factory operation that was performed.</param>
    public override void FactoryComplete(FactoryOperation factoryOperation)
    {
        base.FactoryComplete(factoryOperation);
        if (factoryOperation == FactoryOperation.Update)
        {
            // Clear ContainingList on deleted items - deletion is now persisted
            foreach (var item in this.DeletedList)
            {
                ((IEntityBaseInternal)item).SetContainingList(null);
            }

            this.DeletedList.Clear();

            // Recalculate cached modified state since DeletedList was cleared
            // and items may have been marked unmodified during the save
            _cachedChildrenModified = this.Any(c => c.IsModified);
        }
    }

    /// <summary>
    /// Resumes all paused actions, including rule execution and property change notifications.
    /// Recalculates cached modified state after resuming.
    /// </summary>
    public override void ResumeAllActions()
    {
        if (this.IsPaused)
        {
            // Recalculate cached modified value since items may have changed while paused
            _cachedChildrenModified = this.Any(c => c.IsModified);
        }

        base.ResumeAllActions();
    }
}
