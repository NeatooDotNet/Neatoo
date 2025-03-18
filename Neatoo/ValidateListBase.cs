using Neatoo.Core;
using Neatoo.Internal;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Neatoo;

public interface IValidateListBase : IListBase
{

}

public interface IValidateListBase<I> : IListBase<I>, IValidateListBase, IValidateMetaProperties
    where I : IValidateBase
{

}

public abstract class ValidateListBase<I> : ListBase<I>, IValidateListBase<I>, IValidateListBase, INotifyPropertyChanged
    where I : IValidateBase
{
    public ValidateListBase() : base()
    {
        ResetMetaState();
    }

    public bool IsValid => !this.Any(c => !c.IsValid);
    public bool IsSelfValid => true;

    [JsonIgnore]
    public bool IsPaused { get; protected set; } = false;

    protected (bool IsValid, bool IsSelfValid, bool IsBusy, bool IsSelfBusy) MetaState { get; private set; }

    protected override void CheckIfMetaPropertiesChanged(bool raiseBusy = false)
    {
        if (MetaState.IsValid != IsValid)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsValid)));
        }
        if (MetaState.IsSelfValid != IsSelfValid)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsSelfValid)));
        }
        if (raiseBusy && IsSelfBusy || MetaState.IsSelfBusy != IsSelfBusy)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsSelfBusy)));
        }
        if (raiseBusy && IsBusy || MetaState.IsBusy != IsBusy)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsBusy)));
        }

        ResetMetaState();
        base.CheckIfMetaPropertiesChanged(raiseBusy);
    }

    protected virtual void ResetMetaState()
    {
        MetaState = (IsValid, IsSelfValid, IsBusy, IsSelfBusy);
    }

    protected override Task HandleNeatooPropertyChanged(PropertyChangedBreadCrumbs breadCrumbs)
    {
        CheckIfMetaPropertiesChanged();
        return base.HandleNeatooPropertyChanged(breadCrumbs);
    }

    public async Task RunAllRules(CancellationToken? token = default)
    {
        foreach (var item in this)
        {
            await item.RunAllRules();
        }
    }

    public Task RunSelfRules(CancellationToken? token = default)
    {
        return Task.CompletedTask;
    }

    public void ClearAllErrors()
    {
        foreach (var item in this)
        {
            item.ClearAllErrors();
        }
    }

    public void ClearSelfErrors()
    {
        foreach (var item in this)
        {
            item.ClearSelfErrors();
        }
    }

    public virtual void ResumeAllActions()
    {
        if (IsPaused)
        {
            IsPaused = false;
            ResetMetaState();
        }
    }
}
