using Neatoo.RemoteFactory;
using System.Collections;
using System.ComponentModel;

namespace Neatoo;


public interface IEditListBase : IValidateListBase, IEditMetaProperties
{
    internal IEnumerable DeletedList { get; }
}

public interface IEditListBase<I> : IValidateListBase<I>, IEditMetaProperties
    where I : IEditBase
{
    new void RemoveAt(int index);
}

[Factory]
public abstract class EditListBase<I> : ValidateListBase<I>, INeatooObject, IEditListBase<I>, IEditListBase
    where I : IEditBase
{

    public EditListBase() : base()
    {

    }

    public bool IsModified => this.Any(c => c.IsModified) || DeletedList.Any();
    public bool IsSelfModified => false;
    public bool IsMarkedModified => false;
    public bool IsSavable => false;
    public bool IsNew => false;
    public bool IsDeleted => false;
    public bool IsChild => false;
    protected List<I> DeletedList { get; } = new List<I>();

    protected (bool IsModified, bool IsSelfModified, bool IsSavable) EditMetaState { get; private set; }

    IEnumerable IEditListBase.DeletedList => DeletedList;

    protected override void CheckIfMetaPropertiesChanged()
    {
        base.CheckIfMetaPropertiesChanged();

        if (EditMetaState.IsModified != IsModified)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsModified)));
            RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsModified), this));
        }
        if (EditMetaState.IsSelfModified != IsSelfModified)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsSelfModified)));
            RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsSelfModified), this));
        }
        if (EditMetaState.IsSavable != IsSavable)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsSavable)));
            RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsSavable), this));
        }

        ResetMetaState();
    }

    protected override void ResetMetaState()
    {
        base.ResetMetaState();
        EditMetaState = (IsModified, IsSelfModified, IsSavable);
    }

    protected override void InsertItem(int index, I item)
    {
        if (!IsPaused)
        {
            if (item.IsDeleted)
            {
                item.UnDelete();
            }

            if (!item.IsNew)
            {
                //((IDataMapperEditTarget)item).MarkModified(); // TODO Add back
                item.MarkModified();
            }

            item.MarkAsChild();
        }
        else
        {
            if (item.IsDeleted)
            {
                DeletedList.Add(item);
                return;
            }
        }

        base.InsertItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        if (!IsPaused)
        {
            var item = this[index];

            if (!item.IsNew)
            {
                item.Delete();
                DeletedList.Add(item);
            }
        }

        base.RemoveItem(index);
    }

    public override void OnDeserializing()
    {
        base.OnDeserializing();
        IsPaused = true;
    }

    public override void OnSerializing()
    {
        base.OnSerializing();
    }

    public override void FactoryComplete(FactoryOperation factoryOperation)
    {
        base.FactoryComplete(factoryOperation);
        if(factoryOperation == FactoryOperation.Update)
        {
            DeletedList.Clear();
        }
    }
}
