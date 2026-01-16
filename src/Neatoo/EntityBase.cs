using Neatoo.RemoteFactory;

namespace Neatoo;

/// <summary>
/// Defines the interface for Neatoo entity objects that support persistence, modification tracking, and validation.
/// </summary>
/// <remarks>
/// <see cref="IEntityBase"/> extends <see cref="IValidateBase"/> with entity-specific capabilities
/// for Domain-Driven Design (DDD) scenarios, including modification tracking, deletion handling,
/// and save operations. Entity objects can be persisted through a factory pattern.
/// </remarks>
public interface IEntityBase : IValidateBase, IEntityMetaProperties, IFactorySaveMeta
{
    /// <summary>
    /// Gets the aggregate root of the object graph this entity belongs to.
    /// </summary>
    /// <value>
    /// The aggregate root, or <c>null</c> if this entity is the root or not yet part of an aggregate.
    /// </value>
    IValidateBase? Root { get; }

    /// <summary>
    /// Gets the collection of property names that have been modified since the last save.
    /// </summary>
    /// <value>An enumerable collection of modified property names.</value>
    IEnumerable<string> ModifiedProperties { get; }

    /// <summary>
    /// Marks the entity for deletion. The entity will be deleted when <see cref="Save()"/> is called.
    /// </summary>
    void Delete();

    /// <summary>
    /// Reverses a previous call to <see cref="Delete"/>, removing the deletion mark from the entity.
    /// </summary>
    void UnDelete();

    /// <summary>
    /// Persists the entity asynchronously using the configured factory.
    /// </summary>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the saved entity.</returns>
    Task<IEntityBase> Save();

    /// <summary>
    /// Persists the entity asynchronously with cancellation support.
    /// </summary>
    /// <param name="token">Cancellation token to cancel the operation before persistence begins.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the saved entity.</returns>
    /// <remarks>
    /// Cancellation is checked before persistence operations begin. Once Insert, Update, or Delete
    /// starts executing, cancellation is not checked to avoid leaving the database in an inconsistent state.
    /// </remarks>
    Task<IEntityBase> Save(CancellationToken token);

    /// <summary>
    /// Gets the entity property with the specified name using indexer syntax.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The <see cref="IEntityProperty"/> instance for the specified property.</returns>
    new IEntityProperty this[string propertyName] { get; }
}

/// <summary>
/// Abstract base class for Neatoo entity objects that support persistence, modification tracking, and validation.
/// </summary>
/// <typeparam name="T">The concrete type deriving from this base class, used for the curiously recurring template pattern (CRTP).</typeparam>
/// <remarks>
/// <para>
/// <see cref="EntityBase{T}"/> extends <see cref="ValidateBase{T}"/> with entity-specific capabilities
/// for Domain-Driven Design (DDD) scenarios:
/// </para>
/// <list type="bullet">
/// <item><description>Modification tracking via <see cref="IsModified"/> and <see cref="ModifiedProperties"/></description></item>
/// <item><description>New/existing state tracking via <see cref="IsNew"/></description></item>
/// <item><description>Soft delete support via <see cref="Delete"/> and <see cref="IsDeleted"/></description></item>
/// <item><description>Savability determination via <see cref="IsSavable"/></description></item>
/// <item><description>Child entity support for aggregate patterns via <see cref="IsChild"/></description></item>
/// </list>
/// <para>
/// Entity objects can be persisted through the factory pattern. The <see cref="Save()"/> method
/// delegates to the appropriate Insert, Update, or Delete factory method based on the entity state.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public partial class Customer : EntityBase&lt;Customer&gt;
/// {
///     public partial string Name { get; set; }
///
///     public Customer(IEntityBaseServices&lt;Customer&gt; services) : base(services) { }
///
///     [Insert]
///     public async Task Insert([Service] ICustomerRepository repo)
///     {
///         await repo.InsertAsync(this);
///     }
/// }
/// </code>
/// </example>
[Factory]
public abstract class EntityBase<T> : ValidateBase<T>, INeatooObject, IEntityBase, IEntityBaseInternal, IEntityMetaProperties
    where T : EntityBase<T>
{
    /// <summary>
    /// Gets the property manager with entity-specific capabilities including modification tracking.
    /// </summary>
    /// <remarks>
    /// This property shadows the base class PropertyManager to provide access to entity-specific features.
    /// </remarks>
    protected new IEntityPropertyManager PropertyManager => (IEntityPropertyManager)base.PropertyManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityBase{T}"/> class.
    /// </summary>
    /// <param name="services">The entity services containing the property manager, rule manager factory, and save factory.</param>
    public EntityBase(IEntityBaseServices<T> services) : base(services)
    {
        this.Factory = services.Factory;
    }

    /// <summary>
    /// Gets or sets the factory used to save this entity.
    /// </summary>
    /// <value>The save factory, or <c>null</c> if no default save operation is configured.</value>
    public IFactorySave<T>? Factory { get; protected set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entity has been explicitly marked as modified.
    /// </summary>
    /// <value><c>true</c> if explicitly marked modified; otherwise, <c>false</c>.</value>
    public virtual bool IsMarkedModified { get; protected set; } = false;

    /// <summary>
    /// Gets a value indicating whether this entity or any child entities have been modified.
    /// </summary>
    /// <value><c>true</c> if any property has changed, the entity is new, deleted, or explicitly marked modified; otherwise, <c>false</c>.</value>
    public virtual bool IsModified => this.PropertyManager.IsModified || this.IsDeleted || this.IsNew || this.IsSelfModified;

    /// <summary>
    /// Gets or sets a value indicating whether this entity's own properties have been modified.
    /// </summary>
    /// <value><c>true</c> if any direct property has changed, the entity is deleted, or explicitly marked modified; otherwise, <c>false</c>.</value>
    public virtual bool IsSelfModified { get => this.PropertyManager.IsSelfModified || this.IsDeleted || this.IsMarkedModified; protected set => this.IsMarkedModified = value; }

    /// <summary>
    /// Gets a value indicating whether this entity can be saved.
    /// </summary>
    /// <value><c>true</c> if the entity is modified, valid, not busy, and not a child entity; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// Child entities cannot be saved independently; they must be saved through their parent aggregate root.
    /// </remarks>
    public virtual bool IsSavable => this.IsModified && this.IsValid && !this.IsBusy && !this.IsChild;

    /// <summary>
    /// Gets or sets a value indicating whether this is a new entity that has not been persisted.
    /// </summary>
    /// <value><c>true</c> if the entity is new; otherwise, <c>false</c>.</value>
    public virtual bool IsNew { get; protected set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entity has been marked for deletion.
    /// </summary>
    /// <value><c>true</c> if the entity is marked for deletion; otherwise, <c>false</c>.</value>
    public virtual bool IsDeleted { get; protected set; }

    /// <summary>
    /// Gets the collection of property names that have been modified since the last save.
    /// </summary>
    /// <value>An enumerable collection of modified property names.</value>
    public virtual IEnumerable<string> ModifiedProperties => this.PropertyManager.ModifiedProperties;

    /// <summary>
    /// Gets or sets a value indicating whether this entity is a child within an aggregate.
    /// </summary>
    /// <value><c>true</c> if this is a child entity; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// Child entities are saved as part of their parent aggregate and cannot call <see cref="Save()"/> directly.
    /// </remarks>
    public virtual bool IsChild { get; protected set; }

    /// <summary>
    /// Gets or sets the list that contains this entity.
    /// </summary>
    /// <remarks>
    /// Used internally for Delete/Remove consistency and intra-aggregate moves.
    /// Stays set when the entity is removed from the list (pending deletion) and
    /// is cleared only after the save operation completes.
    /// </remarks>
    protected IEntityListBase? ContainingList { get; set; }

    /// <summary>
    /// Gets the aggregate root of the object graph this entity belongs to.
    /// </summary>
    /// <value>
    /// The aggregate root, or <c>null</c> if this entity is the root or not yet part of an aggregate.
    /// </value>
    /// <remarks>
    /// <para>
    /// The Root property walks up the Parent chain to find the aggregate root:
    /// </para>
    /// <list type="bullet">
    /// <item><description>If Parent is null, this entity is the root (or standalone), so Root is null</description></item>
    /// <item><description>If Parent has a Root, return that Root</description></item>
    /// <item><description>If Parent has no Root, Parent itself is the root, so return Parent</description></item>
    /// </list>
    /// </remarks>
    public IValidateBase? Root => Parent == null ? null : ((Parent as IEntityBase)?.Root ?? Parent);

    /// <summary>
    /// Gets or sets the cached entity meta property state for change detection.
    /// </summary>
    /// <remarks>
    /// Used internally to track changes to entity meta properties (IsModified, IsSelfModified, IsSavable, IsDeleted)
    /// and raise appropriate property changed notifications.
    /// </remarks>
    protected (bool IsModified, bool IsSelfModified, bool IsSavable, bool IsDeleted) EntityMetaState { get; private set; }

    /// <summary>
    /// Checks if entity-specific meta properties have changed and raises notifications.
    /// </summary>
    /// <remarks>
    /// This method extends the base implementation to track changes to IsModified, IsSelfModified,
    /// IsSavable, and IsDeleted. It automatically raises PropertyChanged events when these values change.
    /// </remarks>
    protected override void CheckIfMetaPropertiesChanged()
    {
        if (!this.IsPaused)
        {
            RaiseIfChanged(this.EntityMetaState.IsModified, this.IsModified, nameof(this.IsModified));
            RaiseIfChanged(this.EntityMetaState.IsSelfModified, this.IsSelfModified, nameof(this.IsSelfModified));
            RaiseIfChanged(this.EntityMetaState.IsSavable, this.IsSavable, nameof(this.IsSavable));
            RaiseIfChanged(this.EntityMetaState.IsDeleted, this.IsDeleted, nameof(this.IsDeleted));
        }

        base.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Resets the cached entity meta property state to current values.
    /// </summary>
    /// <remarks>
    /// Called after meta property notifications are raised to prepare for the next change detection cycle.
    /// </remarks>
    protected override void ResetMetaState()
    {
        base.ResetMetaState();
        this.EntityMetaState = (this.IsModified, this.IsSelfModified, this.IsSavable, this.IsDeleted);
    }

    /// <summary>
    /// Gets a value indicating whether this entity has been explicitly marked as modified for interface implementation.
    /// </summary>
    bool IEntityMetaProperties.IsMarkedModified => this.IsMarkedModified;

    /// <summary>
    /// Marks this entity as a child entity within an aggregate.
    /// </summary>
    /// <remarks>
    /// Child entities cannot be saved independently; they are persisted through their parent aggregate root.
    /// This method is typically called by list containers when an entity is added.
    /// </remarks>
    protected virtual void MarkAsChild()
    {
        this.IsChild = true;
    }

    /// <summary>
    /// Marks this entity as unmodified, clearing all modification tracking.
    /// </summary>
    /// <remarks>
    /// This method clears the IsSelfModified state for this entity's direct properties
    /// and resets the IsMarkedModified flag. Typically called after a successful save operation.
    /// </remarks>
    protected virtual void MarkUnmodified()
    {
        if (this.IsBusy)
        {
            throw new InvalidOperationException(
                "Cannot mark entity as unmodified while async operations are in progress. " +
                "Call await WaitForTasks() before marking unmodified.");
        }

        this.PropertyManager.MarkSelfUnmodified();
        this.IsMarkedModified = false;
        this.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Explicitly marks this entity as modified.
    /// </summary>
    /// <remarks>
    /// Use this method to force the entity to be considered modified even if no properties have changed.
    /// This is useful for scenarios where external state changes require the entity to be re-saved.
    /// </remarks>
    protected virtual void MarkModified()
    {
        this.IsMarkedModified = true;
        this.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Marks this entity as new (not yet persisted).
    /// </summary>
    /// <remarks>
    /// New entities will trigger an Insert operation when saved.
    /// This is typically called automatically after a Create factory operation.
    /// </remarks>
    protected virtual void MarkNew()
    {
        this.IsNew = true;
    }

    /// <summary>
    /// Marks this entity as existing (already persisted).
    /// </summary>
    /// <remarks>
    /// Existing entities will trigger an Update operation when saved.
    /// This is typically called automatically after Insert or Fetch factory operations.
    /// </remarks>
    protected virtual void MarkOld()
    {
        this.IsNew = false;
    }

    /// <summary>
    /// Marks this entity for deletion.
    /// </summary>
    /// <remarks>
    /// Deleted entities will trigger a Delete operation when saved.
    /// Use <see cref="UnDelete"/> to reverse this operation.
    /// </remarks>
    protected virtual void MarkDeleted()
    {
        this.IsDeleted = true;
        this.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Marks this entity for deletion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the entity is in a list, this method delegates to the list's Remove method,
    /// ensuring consistent behavior between <c>entity.Delete()</c> and <c>list.Remove(entity)</c>.
    /// </para>
    /// <para>
    /// The entity will be deleted from persistent storage when <see cref="Save()"/> is called.
    /// Use <see cref="UnDelete"/> to reverse this operation before saving.
    /// </para>
    /// </remarks>
    public void Delete()
    {
        if (this.ContainingList != null)
        {
            // Delegate to the list's Remove method for consistency
            this.ContainingList.Remove(this);
            return;
        }

        this.MarkDeleted();
    }

    /// <summary>
    /// Removes the deletion mark from this entity.
    /// </summary>
    /// <remarks>
    /// This method reverses a previous call to <see cref="Delete"/>.
    /// If the entity was not marked for deletion, this method has no effect.
    /// </remarks>
    public void UnDelete()
    {
        if (this.IsDeleted)
        {
            this.IsDeleted = false;
            this.CheckIfMetaPropertiesChanged();
        }
    }

    /// <summary>
    /// Handles property change notifications from child objects.
    /// </summary>
    /// <param name="eventArgs">The event arguments containing the child property change details.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// When a child entity property is set (not a nested change), this method automatically
    /// un-deletes the child if it was previously marked for deletion.
    /// </remarks>
    protected override Task ChildNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.InnerEventArgs == null && eventArgs.Property.Value is IEntityBase child)
        {
            child.UnDelete();
        }

        return base.ChildNeatooPropertyChanged(eventArgs);
    }

    /// <summary>
    /// Persists this entity asynchronously using the configured factory.
    /// </summary>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the saved entity.</returns>
    /// <exception cref="Exception">Thrown when the entity is not savable, is a child entity, is invalid, not modified, or is busy.</exception>
    /// <remarks>
    /// <para>
    /// The save operation delegates to the appropriate factory method based on the entity state:
    /// </para>
    /// <list type="bullet">
    /// <item><description>If <see cref="IsNew"/> is <c>true</c>, calls the Insert method</description></item>
    /// <item><description>If <see cref="IsDeleted"/> is <c>true</c>, calls the Delete method</description></item>
    /// <item><description>Otherwise, calls the Update method</description></item>
    /// </list>
    /// <para>
    /// Child entities cannot be saved directly; they must be saved through their parent aggregate root.
    /// </para>
    /// </remarks>
    public virtual async Task<IEntityBase> Save()
    {
        if (!this.IsSavable)
        {
            if (this.IsChild)
            {
                throw new SaveOperationException(SaveFailureReason.IsChildObject);
            }
            if (!this.IsValid)
            {
                throw new SaveOperationException(SaveFailureReason.IsInvalid);
            }
            if (!(this.IsModified || this.IsSelfModified))
            {
                throw new SaveOperationException(SaveFailureReason.NotModified);
            }
            if (this.IsBusy)
            {
                throw new SaveOperationException(SaveFailureReason.IsBusy);
            }
        }

        if (this.Factory == null)
        {
            throw new SaveOperationException(SaveFailureReason.NoFactoryMethod);
        }

        return (IEntityBase)await this.Factory.Save((T)this);
    }

    /// <summary>
    /// Saves the entity to persistence with cancellation support.
    /// </summary>
    /// <param name="token">Cancellation token to cancel the operation before persistence begins.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result is the saved entity.</returns>
    /// <remarks>
    /// <para>
    /// Cancellation is only checked before persistence operations begin. Once Insert, Update, or Delete
    /// starts executing, cancellation is not checked to avoid leaving the database in an inconsistent state.
    /// </para>
    /// <para>
    /// If the entity has pending async operations (IsBusy), this method will wait for them with
    /// cancellation support before proceeding.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested before persistence begins.</exception>
    /// <exception cref="SaveOperationException">Thrown when the entity cannot be saved due to validation or state issues.</exception>
    public virtual async Task<IEntityBase> Save(CancellationToken token)
    {
        // Wait for any pending async operations with cancellation support
        await this.WaitForTasks(token);

        // Check cancellation before persistence
        token.ThrowIfCancellationRequested();

        // Delegate to the standard Save method
        // NO cancellation checks during persistence to avoid data corruption
        return await this.Save();
    }

    /// <summary>
    /// Gets the entity property with the specified name.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The <see cref="IEntityProperty"/> instance for the specified property.</returns>
    new protected IEntityProperty GetProperty(string propertyName)
    {
        return this.PropertyManager[propertyName];
    }

    /// <summary>
    /// Gets the entity property with the specified name using indexer syntax.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The <see cref="IEntityProperty"/> instance for the specified property.</returns>
    new public IEntityProperty this[string propertyName] { get => this.GetProperty(propertyName); }

    /// <summary>
    /// Pauses all property change events, rule execution, and modification tracking.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that will resume actions when disposed.</returns>
    /// <remarks>
    /// This method extends the base implementation to also pause modification tracking
    /// on the entity property manager.
    /// </remarks>
    public override IDisposable PauseAllActions()
    {
        var d = base.PauseAllActions();
        this.PropertyManager.PauseAllActions();
        return d;
    }

    /// <summary>
    /// Resumes property change events, rule execution, and modification tracking.
    /// </summary>
    /// <remarks>
    /// This method extends the base implementation to also resume modification tracking
    /// on the entity property manager.
    /// </remarks>
    public override void ResumeAllActions()
    {
        base.ResumeAllActions();
        this.PropertyManager.ResumeAllActions();
    }

    /// <summary>
    /// Called when a factory operation completes to update entity state based on the operation type.
    /// </summary>
    /// <param name="factoryOperation">The type of factory operation that completed.</param>
    /// <remarks>
    /// <para>
    /// This method updates the entity state based on the completed operation:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="FactoryOperation.Create"/>: Marks the entity as new</description></item>
    /// <item><description><see cref="FactoryOperation.Insert"/> or <see cref="FactoryOperation.Update"/>: Marks the entity as unmodified and old (existing)</description></item>
    /// <item><description><see cref="FactoryOperation.Fetch"/> and <see cref="FactoryOperation.Delete"/>: No state changes</description></item>
    /// </list>
    /// </remarks>
    public override void FactoryComplete(FactoryOperation factoryOperation)
    {
        base.FactoryComplete(factoryOperation);

        switch (factoryOperation)
        {
            case FactoryOperation.Create:
                this.MarkNew();
                break;
            case FactoryOperation.Fetch:
                break;
            case FactoryOperation.Delete:
                break;
            case FactoryOperation.Insert:
            case FactoryOperation.Update:
                this.MarkUnmodified();
                this.MarkOld();
                break;
            default:
                break;
        }

        this.ResumeAllActions();
    }

    /// <summary>
    /// Explicit interface implementation for marking the entity as modified.
    /// </summary>
    void IEntityBaseInternal.MarkModified()
    {
        this.MarkModified();
    }

    /// <summary>
    /// Explicit interface implementation for marking the entity as a child.
    /// </summary>
    void IEntityBaseInternal.MarkAsChild()
    {
        this.MarkAsChild();
    }

    /// <summary>
    /// Explicit interface implementation for marking the entity as deleted.
    /// Used by EntityListBase.RemoveItem to avoid recursion with Delete().
    /// </summary>
    void IEntityBaseInternal.MarkDeleted()
    {
        this.MarkDeleted();
    }

    /// <summary>
    /// Explicit interface implementation for getting the containing list.
    /// </summary>
    IEntityListBase? IEntityBaseInternal.ContainingList => this.ContainingList;

    /// <summary>
    /// Sets the containing list for this entity.
    /// </summary>
    /// <param name="list">The list that contains this entity, or null to clear.</param>
    void IEntityBaseInternal.SetContainingList(IEntityListBase? list)
    {
        this.ContainingList = list;
    }
}
