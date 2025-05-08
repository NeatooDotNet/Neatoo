using Neatoo.RemoteFactory;

namespace Neatoo;

public interface IEntityBase : IValidateBase, IEntityMetaProperties, IFactorySaveMeta
{
    IEnumerable<string> ModifiedProperties { get; }

    void Delete();
    void UnDelete();
    Task<IEntityBase> Save();
    new IEntityProperty this[string propertyName] { get; }

    internal void MarkModified();
    internal void MarkAsChild();
}

[Factory]
public abstract class EntityBase<T> : ValidateBase<T>, INeatooObject, IEntityBase, IEntityMetaProperties
    where T : EntityBase<T>
{
    protected new IEntityPropertyManager PropertyManager => (IEntityPropertyManager)base.PropertyManager;

    public EntityBase(IEntityBaseServices<T> services) : base(services)
    {
        this.Factory = services.Factory;
    }

    public IFactorySave<T>? Factory { get; protected set; }
    public virtual bool IsMarkedModified { get; protected set; } = false;
    public virtual bool IsModified => PropertyManager.IsModified || IsDeleted || IsNew || IsSelfModified;
    public virtual bool IsSelfModified { get => PropertyManager.IsSelfModified || IsDeleted || IsMarkedModified; protected set => IsMarkedModified = value; }
    public virtual bool IsSavable => IsModified && IsValid && !IsBusy && !IsChild;
    public virtual bool IsNew { get; protected set; }
    public virtual bool IsDeleted { get; protected set; }
    public virtual IEnumerable<string> ModifiedProperties => PropertyManager.ModifiedProperties;
    public virtual bool IsChild { get; protected set; }

    protected (bool IsModified, bool IsSelfModified, bool IsSavable, bool IsDeleted) EntityMetaState { get; private set; }

    protected override void CheckIfMetaPropertiesChanged()
    {
        if (!IsPaused)
        {
            if (EntityMetaState.IsModified != IsModified)
            {
                RaisePropertyChanged(nameof(IsModified));
                RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsModified), this));
            }
            if (EntityMetaState.IsSelfModified != IsSelfModified)
            {
                RaisePropertyChanged(nameof(IsSelfModified));
                RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsSelfModified), this));
            }
            if (EntityMetaState.IsSavable != IsSavable)
            {
                RaisePropertyChanged(nameof(IsSavable));
                RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsSavable), this));
            }
            if (EntityMetaState.IsDeleted != IsDeleted)
            {
                RaisePropertyChanged(nameof(IsDeleted));
                RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsDeleted), this));
            }
        }

        base.CheckIfMetaPropertiesChanged();
    }

    protected override void ResetMetaState()
    {
        base.ResetMetaState();
        EntityMetaState = (IsModified, IsSelfModified, IsSavable, IsDeleted);
    }

    bool IEntityMetaProperties.IsMarkedModified => IsMarkedModified;

    protected virtual void MarkAsChild()
    {
        IsChild = true;
    }

    // TODO - Recursive set clean for all children
    protected virtual void MarkUnmodified()
    {
        // TODO : What if busy??
        PropertyManager.MarkSelfUnmodified();
        IsMarkedModified = false;
        CheckIfMetaPropertiesChanged(); // Really shouldn't be anything listening to this
    }

    protected virtual void MarkModified()
    {
        IsMarkedModified = true;
        CheckIfMetaPropertiesChanged();
    }

    protected virtual void MarkNew()
    {
        IsNew = true;
    }

    protected virtual void MarkOld()
    {
        IsNew = false;
    }

    protected virtual void MarkDeleted()
    {
        IsDeleted = true;
        CheckIfMetaPropertiesChanged();
    }

    public void Delete()
    {
        MarkDeleted();
    }

    public void UnDelete()
    {
        if (IsDeleted)
        {
            IsDeleted = false;
            CheckIfMetaPropertiesChanged();
        }
    }

    protected override Task ChildNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {

        // TODO - if an object isn't assigned to another IBase
        // it will still consider us to be the Parent

        if (eventArgs.InnerEventArgs == null && eventArgs.Property.Value is IEntityBase child)
        {
            child.UnDelete();
        }

        return base.ChildNeatooPropertyChanged(eventArgs);
    }

    public virtual async Task<IEntityBase> Save()
    {
        if (!IsSavable)
        {
            if (IsChild)
            {
                throw new Exception("Child objects cannot be saved");
            }
            if (!IsValid)
            {
                throw new Exception("Object is not valid and cannot be saved.");
            }
            if (!(IsModified || IsSelfModified))
            {
                throw new Exception("Object has not been modified.");
            }
            if (IsBusy)
            {
                // TODO await this.WaitForTasks(); ??
                throw new Exception("Object is busy and cannot be saved.");
            }
        }

        if (Factory == null)
        {
            throw new Exception("Default Factory.Save() is not set. To use the save method [Insert], [Update] and/or [Delete] methods with no non-service parameters are required.");
        }

        return (IEntityBase)await Factory.Save((T)this);
    }

    new protected IEntityProperty GetProperty(string propertyName)
    {
        return PropertyManager[propertyName];
    }
    new public IEntityProperty this[string propertyName] { get => GetProperty(propertyName); }

    public override IDisposable PauseAllActions()
    {
        var d = base.PauseAllActions();
        PropertyManager.PauseAllActions();
        return d;
    }

    public override void ResumeAllActions()
    {
        base.ResumeAllActions();
        PropertyManager.ResumeAllActions();
    }

    public override void FactoryComplete(FactoryOperation factoryOperation)
    {
        base.FactoryComplete(factoryOperation);

        switch (factoryOperation)
        {
            case FactoryOperation.Create:
                MarkNew();
                break;
            case FactoryOperation.Fetch:
                break;
            case FactoryOperation.Delete:
                break;
            case FactoryOperation.Insert:
            case FactoryOperation.Update:
                MarkUnmodified();
                MarkOld();
                break;
            default:
                break;
        }

        ResumeAllActions();
    }

    void IEntityBase.MarkModified()
    {
        MarkModified();
    }

    void IEntityBase.MarkAsChild()
    {
        MarkAsChild();
    }
}
