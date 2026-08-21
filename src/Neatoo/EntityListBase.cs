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

    // IsSavable removed — lists are never savable (saved through parent entity)

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
    /// Stores the previous values of <see cref="IsModified"/> and <see cref="IsSelfModified"/>.
    /// </summary>
    protected (bool IsModified, bool IsSelfModified) EntityMetaState { get; private set; }

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
    /// <remarks>
    /// IMPORTANT: Entity-specific comparisons must happen BEFORE calling base.CheckIfMetaPropertiesChanged(),
    /// because base calls ResetMetaState() which updates EntityMetaState via virtual dispatch.
    /// </remarks>
    protected override void CheckIfMetaPropertiesChanged()
    {
        // Compare entity meta properties BEFORE calling base (which resets the cache)
        RaiseIfChanged(this.EntityMetaState.IsModified, this.IsModified, nameof(this.IsModified));
        RaiseIfChanged(this.EntityMetaState.IsSelfModified, this.IsSelfModified, nameof(this.IsSelfModified));

        base.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Resets the cached meta state to the current property values.
    /// Includes both validation and entity meta state reset.
    /// </summary>
    protected override void ResetMetaState()
    {
        base.ResetMetaState();
        this.EntityMetaState = (this.IsModified, this.IsSelfModified);
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
                // Both aggregates are usually the same TYPE, so naming types alone
                // produces "belongs to 'Order' but this list belongs to 'Order'",
                // which reads as a framework bug rather than a boundary violation.
                var itemRootName = item.Root.GetType().Name;
                var thisRootName = this.Root?.GetType().Name;

                var boundaryDescription = this.Root == null
                    ? $"item belongs to a '{itemRootName}' aggregate, but this list is not part of any aggregate yet"
                    : itemRootName == thisRootName
                        ? $"item belongs to a different '{itemRootName}' instance than this list"
                        : $"item belongs to a '{itemRootName}' aggregate, but this list belongs to a '{thisRootName}' aggregate";

                throw new InvalidOperationException(
                    $"Cannot add {item.GetType().Name} to list: {boundaryDescription}. " +
                    "Aggregate boundaries cannot be crossed - create a new child in the " +
                    "target aggregate and remove the original instead.");
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

            // Attaching an item to a LIVE list is a change to this graph, whatever
            // the item's own persistence state: a new child is work the user will
            // lose if they walk away, exactly as an existing child moved in is.
            //
            // This mark is what carries child state upward now that IsNew is no
            // longer welded into IsModified. IsNew itself never aggregates - it is
            // per-object routing state - so without this a user-added new child
            // would leave its parent clean and unsavable, and modified-guarded save
            // cascades would skip its insert. Paused adds are baseline population
            // and are deliberately excluded (see the paused branch below).
            itemInternal.MarkModified();

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
            // Paused adds are baseline population: a factory operation loading
            // its children, or deserialization restoring them. No dirt-producing
            // step runs here (notably MarkModified), but the child IDENTITY
            // steps do: being a child of this list is what the object IS, not a
            // change to it, and both marks are baseline-neutral.
            //
            // Without this, children loaded by the canonical list [Fetch] would
            // have IsChild=false and no ContainingList, so Delete() would
            // silently bypass list routing. ContainingList also cannot be
            // serialized, so this is what restores it after deserialization.
            var pausedItemInternal = (IEntityBaseInternal)item;
            pausedItemInternal.MarkAsChild();
            pausedItemInternal.SetContainingList(this);

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

            // Announce it. base.RemoveItem already ran its meta-property check
            // ABOVE, while this cache was still stale, so without this the list's
            // IsModified true->false transition is silent - and the parent's own
            // cached IsModified (refreshed only by a property-changed event) stays
            // true forever. Symptom: add a child, remove it again, and the
            // aggregate still claims unsaved changes with nothing to save.
            this.CheckIfMetaPropertiesChanged();
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

            // SetItem deliberately applies NO attach-marking and NO child identity.
            // Unlike Add, replacement has never dirtied the graph here, and an
            // existing test pins that (replacing the only modified item with an
            // unmodified one leaves the list unmodified) - so marking here would
            // change a separate, deliberate behavior rather than replace the weld's
            // job, which is what the IsNew/IsModified split is scoped to.
            //
            // SetItem does have real defects: the DISPLACED item is dropped without
            // MarkDeleted and without entering DeletedList (silently orphaning its
            // row), the incoming item gets no IsChild/ContainingList, and none of
            // Add's guards (duplicate / busy / aggregate boundary) run. All of that
            // is tracked as ISNEW-009, which changes save-side behavior and needs
            // its own review and release note.
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
