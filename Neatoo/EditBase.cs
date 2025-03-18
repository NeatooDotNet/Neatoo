using Neatoo.Core;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System.Text.Json.Serialization;

namespace Neatoo;

public interface IEditBase : IValidateBase, IEditMetaProperties, IFactorySaveMeta
{
    IEnumerable<string> ModifiedProperties { get; }

    void Delete();
    void UnDelete();
    Task<IEditBase> Save();
    new IEditProperty this[string propertyName] { get; }
}

[Factory]
public abstract class EditBase<T> : ValidateBase<T>, INeatooObject, IEditBase, IEditMetaProperties
    where T : EditBase<T>
{
    protected new IEditPropertyManager PropertyManager => (IEditPropertyManager)base.PropertyManager;

    public EditBase(IEditBaseServices<T> services) : base(services)
    {
        this.Factory = services.Factory;
    }

    public IFactorySave<T>? Factory { get; protected set; }
    public bool IsMarkedModified { get; protected set; } = false;
    public bool IsModified => PropertyManager.IsModified || IsDeleted || IsNew || IsSelfModified;
    public bool IsSelfModified { get => PropertyManager.IsSelfModified || IsDeleted || IsMarkedModified; protected set => IsMarkedModified = value; }
    public bool IsSavable => IsModified && IsValid && !IsBusy && !IsChild;
    public bool IsNew { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public IEnumerable<string> ModifiedProperties => PropertyManager.ModifiedProperties;
    public bool IsChild { get; protected set; }

    protected (bool IsModified, bool IsSelfModified, bool IsSavable, bool IsDeleted) EditMetaState { get; private set; }

    protected override void CheckIfMetaPropertiesChanged(bool raiseBusy = true)
    {
        if (!IsPaused)
        {
            if (EditMetaState.IsModified != IsModified)
            {
                RaisePropertyChanged(nameof(IsModified));
            }
            if (EditMetaState.IsSelfModified != IsSelfModified)
            {
                RaisePropertyChanged(nameof(IsSelfModified));
            }
            if (EditMetaState.IsSavable != IsSavable)
            {
                RaisePropertyChanged(nameof(IsSavable));
            }
            if (EditMetaState.IsDeleted != IsDeleted)
            {
                RaisePropertyChanged(nameof(IsDeleted));
            }
        }

        base.CheckIfMetaPropertiesChanged(raiseBusy);
    }

    protected override void ResetMetaState()
    {
        base.ResetMetaState();
        EditMetaState = (IsModified, IsSelfModified, IsSavable, IsDeleted);
    }

    bool IEditMetaProperties.IsMarkedModified => IsMarkedModified;

    protected virtual void MarkAsChild()
    {
        if (!IsPaused)
        {
            IsChild = true;
        }
    }

    // TODO - Recursive set clean for all children
    protected virtual void MarkUnmodified()
    {
        if (!IsPaused)
        {
            // TODO : What if busy??
            PropertyManager.MarkSelfUnmodified();
            IsMarkedModified = false;
            CheckIfMetaPropertiesChanged(); // Really shouldn't be anything listening to this
        }
    }

    protected virtual void MarkModified()
    {
        if (!IsPaused)
        {
            IsMarkedModified = true;
            CheckIfMetaPropertiesChanged();
        }
    }

    protected virtual void MarkNew()
    {
        if (!IsPaused)
        {
            IsNew = true;
        }
    }

    protected virtual void MarkOld()
    {
        if (!IsPaused)
        {
            IsNew = false;
        }
    }

    protected virtual void MarkDeleted()
    {
        if (!IsPaused)
        {
            IsDeleted = true;
            CheckIfMetaPropertiesChanged();
        }
    }

    public void Delete()
    {
        MarkDeleted();
    }

    public void UnDelete()
    {
        if (!IsPaused)
        {

            if (IsDeleted)
            {
                IsDeleted = false;
                CheckIfMetaPropertiesChanged();
            }
        }
    }

    protected override Task ChildNeatooPropertyChanged(PropertyChangedBreadCrumbs breadCrumbs)
    {
        
        // TODO - if an object isn't assigned to another IBase
        // it will still consider us to be the Parent

        if (breadCrumbs.PropertyName == nameof(IEditProperty.Value) && breadCrumbs.Source is IEditBase child)
        {
            child.UnDelete();
        }

        return base.ChildNeatooPropertyChanged(breadCrumbs);
    }

    public virtual async Task<IEditBase> Save()
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

        return (IEditBase) await Factory.Save((T)this);
    }

    new protected IEditProperty GetProperty(string propertyName)
    {
        return PropertyManager[propertyName];
    }
    new public IEditProperty this[string propertyName] { get => GetProperty(propertyName); }

    public override void PauseAllActions()
    {
        base.PauseAllActions();
        PropertyManager.PauseAllActions();
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
    }
}
