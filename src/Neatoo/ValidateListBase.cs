using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Neatoo;

public interface IValidateListBase : IListBase
{

}

public interface IValidateListBase<I> : IListBase<I>, IValidateMetaProperties
    where I : IValidateBase
{

}

public abstract class ValidateListBase<I> : ListBase<I>, IValidateListBase<I>, IValidateListBase,
                                                        INotifyPropertyChanged,
                                                        IFactoryOnStart, IFactoryOnComplete
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

    protected (bool IsValid, bool IsSelfValid, bool IsBusy) MetaState { get; private set; }

    public IReadOnlyCollection<IPropertyMessage> PropertyMessages => this.SelectMany(_ => _.PropertyMessages).ToList().AsReadOnly();

    protected override void CheckIfMetaPropertiesChanged()
    {
        if (MetaState.IsValid != IsValid)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsValid)));
            RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsValid), this));
        }
        if (MetaState.IsSelfValid != IsSelfValid)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsSelfValid)));
            RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsSelfValid), this));
        }
        if (MetaState.IsBusy != IsBusy)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsBusy)));
            RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsBusy), this));
        }

        ResetMetaState();
        base.CheckIfMetaPropertiesChanged();
    }

    protected virtual void ResetMetaState()
    {
        MetaState = (IsValid, IsSelfValid, IsBusy);
    }

    public async Task RunRules(string propertyName, CancellationToken? token = default)
    {
        foreach (var item in this)
        {
            await item.RunRules(propertyName, token);
        }
        CheckIfMetaPropertiesChanged();
    }

    public async Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = default)
    {
        foreach (var item in this)
        {
            await item.RunRules(runRules, token);
        }
        CheckIfMetaPropertiesChanged();
    }

    public void ClearAllMessages()
    {
        foreach (var item in this)
        {
            item.ClearAllMessages();
        }
    }

    public void ClearSelfMessages()
    {
        foreach (var item in this)
        {
            item.ClearSelfMessages();
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

    public override void OnDeserializing()
    {
        base.OnDeserializing();
        IsPaused = true;
    }

    public override void OnDeserialized()
    {
        base.OnDeserialized();
        ResumeAllActions();
    }

    public virtual void FactoryStart(FactoryOperation factoryOperation)
    {
        IsPaused = true;
    }

    public virtual void FactoryComplete(FactoryOperation factoryOperation)
    {
        IsPaused = false;
    }
}
