using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Neatoo;

/// <summary>
/// Non-generic interface for a Neatoo validation list base.
/// </summary>
public interface IValidateListBase : IListBase
{

}

/// <summary>
/// Generic interface for a Neatoo validation list that contains validatable items of type <typeparamref name="I"/>.
/// Provides access to validation meta properties aggregated from all items in the list.
/// </summary>
/// <typeparam name="I">The type of items in the list, must implement <see cref="IValidateBase"/>.</typeparam>
public interface IValidateListBase<I> : IListBase<I>, IValidateMetaProperties
    where I : IValidateBase
{

}

/// <summary>
/// Base class for Neatoo collections that support validation.
/// Aggregates validation state from all child items and provides rule execution across the collection.
/// </summary>
/// <typeparam name="I">The type of items in the list, must implement <see cref="IValidateBase"/>.</typeparam>
public abstract class ValidateListBase<I> : ListBase<I>, IValidateListBase<I>, IValidateListBase,
                                                        INotifyPropertyChanged,
                                                        IFactoryOnStart, IFactoryOnComplete
    where I : IValidateBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateListBase{I}"/> class.
    /// </summary>
    public ValidateListBase() : base()
    {
        this.ResetMetaState();
    }

    /// <summary>
    /// Gets a value indicating whether all items in the list are valid.
    /// Returns <c>true</c> if no items report validation errors.
    /// </summary>
    public bool IsValid => !this.Any(c => !c.IsValid);

    /// <summary>
    /// Gets a value indicating whether the list itself (not its items) is valid.
    /// Always returns <c>true</c> as lists do not have their own validation rules.
    /// </summary>
    public bool IsSelfValid => true;

    /// <summary>
    /// Gets a value indicating whether rule execution and property change events are paused.
    /// Used during deserialization and factory operations to prevent premature validation.
    /// </summary>
    [JsonIgnore]
    public bool IsPaused { get; protected set; } = false;

    /// <summary>
    /// Gets the cached meta state for change detection.
    /// Stores the previous values of <see cref="IsValid"/>, <see cref="IsSelfValid"/>, and <see cref="IsBusy"/>.
    /// </summary>
    protected (bool IsValid, bool IsSelfValid, bool IsBusy) MetaState { get; private set; }

    /// <summary>
    /// Gets all property validation messages from all items in the collection.
    /// </summary>
    public IReadOnlyCollection<IPropertyMessage> PropertyMessages => this.SelectMany(_ => _.PropertyMessages).ToList().AsReadOnly();

    /// <summary>
    /// Checks if any validation meta properties have changed and raises appropriate property change notifications.
    /// Compares current values against cached <see cref="MetaState"/> and notifies on differences.
    /// </summary>
    protected override void CheckIfMetaPropertiesChanged()
    {
        if (this.MetaState.IsValid != this.IsValid)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.IsValid)));
            this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsValid), this));
        }
        if (this.MetaState.IsSelfValid != this.IsSelfValid)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.IsSelfValid)));
            this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsSelfValid), this));
        }
        if (this.MetaState.IsBusy != this.IsBusy)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.IsBusy)));
            this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsBusy), this));
        }

        this.ResetMetaState();
        base.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Resets the cached meta state to the current property values.
    /// Called after property change notifications to prepare for the next change detection cycle.
    /// </summary>
    protected virtual void ResetMetaState()
    {
        this.MetaState = (this.IsValid, this.IsSelfValid, this.IsBusy);
    }

    /// <summary>
    /// Runs validation rules for the specified property on all items in the collection.
    /// </summary>
    /// <param name="propertyName">The name of the property whose rules should be executed.</param>
    /// <param name="token">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous rule execution.</returns>
    public async Task RunRules(string propertyName, CancellationToken? token = default)
    {
        foreach (var item in this)
        {
            await item.RunRules(propertyName, token);
        }
        this.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Runs validation rules on all items in the collection according to the specified flags.
    /// </summary>
    /// <param name="runRules">Flags indicating which rules to run. Defaults to <see cref="RunRulesFlag.All"/>.</param>
    /// <param name="token">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous rule execution.</returns>
    public async Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = default)
    {
        foreach (var item in this)
        {
            await item.RunRules(runRules, token);
        }
        this.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Clears all validation messages from all items in the collection, including messages from child objects.
    /// </summary>
    public void ClearAllMessages()
    {
        foreach (var item in this)
        {
            item.ClearAllMessages();
        }
    }

    /// <summary>
    /// Clears validation messages that belong directly to each item (not from child objects).
    /// </summary>
    public void ClearSelfMessages()
    {
        foreach (var item in this)
        {
            item.ClearSelfMessages();
        }
    }

    /// <summary>
    /// Resumes all paused actions, including rule execution and property change notifications.
    /// Resets the meta state after resuming to ensure proper change detection.
    /// </summary>
    public virtual void ResumeAllActions()
    {
        if (this.IsPaused)
        {
            this.IsPaused = false;
            this.ResetMetaState();
        }
    }

    /// <summary>
    /// Called when JSON deserialization is starting.
    /// Pauses rule execution to prevent validation during deserialization.
    /// </summary>
    public override void OnDeserializing()
    {
        base.OnDeserializing();
        this.IsPaused = true;
    }

    /// <summary>
    /// Called when JSON deserialization is complete.
    /// Resumes all paused actions after deserialization is finished.
    /// </summary>
    public override void OnDeserialized()
    {
        base.OnDeserialized();
        this.ResumeAllActions();
    }

    /// <summary>
    /// Called when a factory operation is starting.
    /// Pauses rule execution during the factory operation.
    /// </summary>
    /// <param name="factoryOperation">The type of factory operation being performed.</param>
    public virtual void FactoryStart(FactoryOperation factoryOperation)
    {
        this.IsPaused = true;
    }

    /// <summary>
    /// Called when a factory operation is complete.
    /// Resumes rule execution after the factory operation finishes.
    /// </summary>
    /// <param name="factoryOperation">The type of factory operation that was performed.</param>
    public virtual void FactoryComplete(FactoryOperation factoryOperation)
    {
        this.IsPaused = false;
    }
}
