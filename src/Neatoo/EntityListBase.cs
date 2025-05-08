using Neatoo.RemoteFactory;
using System.Collections;
using System.ComponentModel;

namespace Neatoo;


public interface IEntityListBase : IValidateListBase, IEntityMetaProperties
{
    internal IEnumerable DeletedList { get; }
}

public interface IEntityListBase<I> : IValidateListBase<I>, IEntityMetaProperties
    where I : IEntityBase
{
    new void RemoveAt(int index);
}

[Factory]
public abstract class EntityListBase<I> : ValidateListBase<I>, INeatooObject, IEntityListBase<I>, IEntityListBase
    where I : IEntityBase
{

    public EntityListBase() : base()
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

    protected (bool IsModified, bool IsSelfModified, bool IsSavable) EntityMetaState { get; private set; }

    IEnumerable IEntityListBase.DeletedList => DeletedList;

    protected override void CheckIfMetaPropertiesChanged()
    {
        base.CheckIfMetaPropertiesChanged();

        if (EntityMetaState.IsModified != IsModified)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsModified)));
            RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsModified), this));
        }
        if (EntityMetaState.IsSelfModified != IsSelfModified)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsSelfModified)));
            RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsSelfModified), this));
        }
        if (EntityMetaState.IsSavable != IsSavable)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsSavable)));
            RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsSavable), this));
        }

        ResetMetaState();
    }

    protected override void ResetMetaState()
    {
        base.ResetMetaState();
        EntityMetaState = (IsModified, IsSelfModified, IsSavable);
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
                //((IDataMapperEntityTarget)item).MarkModified(); // TODO Add back
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
