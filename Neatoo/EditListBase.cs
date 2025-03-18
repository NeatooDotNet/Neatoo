using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System.ComponentModel;

namespace Neatoo;


public interface IEditListBase : IValidateListBase, IEditMetaProperties
{
}

public interface IEditListBase<I> : IEditListBase, IValidateListBase<I>, IEditMetaProperties
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

    protected override void CheckIfMetaPropertiesChanged(bool resetBusy = false)
    {
        base.CheckIfMetaPropertiesChanged(resetBusy);

        if (EditMetaState.IsModified != IsModified)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsModified)));
        }
        if (EditMetaState.IsSelfModified != IsSelfModified)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsSelfModified)));
        }
        if (EditMetaState.IsSavable != IsSavable)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsSavable)));
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
            }
        }

        base.InsertItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        if (!IsPaused)
        {
            var item = this[index];

            item.Delete();

            DeletedList.Add(item);

        }

        base.RemoveItem(index);
    }

    public override void OnDeserializing()
    {
        base.OnDeserializing();
        IsPaused = true;
    }

    public override void OnDeserialized()
    {
        base.OnDeserialized();
        IsPaused = false;
    }
}
