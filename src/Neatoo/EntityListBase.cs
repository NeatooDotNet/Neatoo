using Neatoo.RemoteFactory;
using System.Collections;
using System.ComponentModel;

namespace Neatoo;


/// <summary>
/// Non-generic interface for a Neatoo entity list that supports persistence tracking.
/// Provides access to deleted items and entity meta properties.
/// </summary>
public interface IEntityListBase : IValidateListBase, IEntityMetaProperties
{
    /// <summary>
    /// Gets the collection of items that have been marked as deleted but not yet persisted.
    /// </summary>
    internal IEnumerable DeletedList { get; }
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
public abstract class EntityListBase<I> : ValidateListBase<I>, INeatooObject, IEntityListBase<I>, IEntityListBase
    where I : IEntityBase
{

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityListBase{I}"/> class.
    /// </summary>
    public EntityListBase() : base()
    {

    }

    /// <summary>
    /// Gets a value indicating whether any item in the list has been modified or any items are pending deletion.
    /// </summary>
    public bool IsModified => this.Any(c => c.IsModified) || this.DeletedList.Any();

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
    IEnumerable IEntityListBase.DeletedList => this.DeletedList;

    /// <summary>
    /// Checks if any entity meta properties have changed and raises appropriate property change notifications.
    /// Compares current values against cached <see cref="EntityMetaState"/> and notifies on differences.
    /// </summary>
    protected override void CheckIfMetaPropertiesChanged()
    {
        base.CheckIfMetaPropertiesChanged();

        if (this.EntityMetaState.IsModified != this.IsModified)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.IsModified)));
            this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsModified), this));
        }
        if (this.EntityMetaState.IsSelfModified != this.IsSelfModified)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.IsSelfModified)));
            this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsSelfModified), this));
        }
        if (this.EntityMetaState.IsSavable != this.IsSavable)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.IsSavable)));
            this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsSavable), this));
        }

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
    /// marking existing items as modified, and setting child status.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the item.</param>
    /// <param name="item">The entity item to insert into the collection.</param>
    protected override void InsertItem(int index, I item)
    {
        if (!this.IsPaused)
        {
            if (item.IsDeleted)
            {
                item.UnDelete();
            }

            if (!item.IsNew)
            {
                //((IDataMapperEntityTarget)item).MarkModified(); // TODO Add back
                item.MarkModified();
            }

            item.MarkAsChild();
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
    /// for persistence during save operations.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    protected override void RemoveItem(int index)
    {
        if (!this.IsPaused)
        {
            var item = this[index];

            if (!item.IsNew)
            {
                item.Delete();
                this.DeletedList.Add(item);
            }
        }

        base.RemoveItem(index);
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
    /// </summary>
    /// <param name="factoryOperation">The type of factory operation that was performed.</param>
    public override void FactoryComplete(FactoryOperation factoryOperation)
    {
        base.FactoryComplete(factoryOperation);
        if (factoryOperation == FactoryOperation.Update)
        {
            this.DeletedList.Clear();
        }
    }
}
