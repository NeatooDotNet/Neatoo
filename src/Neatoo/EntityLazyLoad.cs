using System.Text.Json.Serialization;
using Neatoo.Rules;

namespace Neatoo;

/// <summary>
/// Wrapper for async lazy loading of child entities or related data.
/// Inherits core loading logic from <see cref="Neatoo.RemoteFactory.LazyLoad{T}"/>
/// and adds Neatoo-specific meta-property interfaces (<see cref="IValidateMetaProperties"/>,
/// <see cref="IEntityMetaProperties"/>).
/// </summary>
/// <typeparam name="T">The type of value to lazy load. Must be a reference type.</typeparam>
/// <remarks>
/// <para>
/// Key principle: <see cref="Neatoo.RemoteFactory.LazyLoad{T}.Value"/> never triggers a load.
/// It returns the current state (<c>null</c> if not loaded, the loaded value otherwise).
/// Call <see cref="Neatoo.RemoteFactory.LazyLoad{T}.LoadAsync"/> explicitly to invoke the
/// loader delegate.
/// </para>
/// <para>
/// Always use <see cref="IEntityLazyLoadFactory"/> to create instances. Do not instantiate directly.
/// </para>
/// </remarks>
public class EntityLazyLoad<T> : Neatoo.RemoteFactory.LazyLoad<T>, IValidateMetaProperties, IEntityMetaProperties
    where T : class?
{
    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    [JsonConstructor]
    public EntityLazyLoad() : base() { }

    /// <summary>
    /// Creates a new lazy load wrapper with the specified loader delegate.
    /// </summary>
    /// <param name="loader">Async function that loads the value when invoked.</param>
    public EntityLazyLoad(Func<Task<T?>> loader) : base(loader) { }

    /// <summary>
    /// Creates a new lazy load wrapper with a pre-loaded value.
    /// </summary>
    /// <param name="value">The pre-loaded value.</param>
    public EntityLazyLoad(T? value) : base(value) { }

    #region IValidateMetaProperties

    /// <inheritdoc />
    public bool IsBusy => IsLoading || ((Value as IValidateMetaProperties)?.IsBusy ?? false);

    /// <inheritdoc />
    bool IValidateMetaProperties.IsValid => !HasLoadError && ((Value as IValidateMetaProperties)?.IsValid ?? true);

    /// <inheritdoc />
    public bool IsSelfValid => !HasLoadError;

    /// <inheritdoc />
    public IReadOnlyCollection<IPropertyMessage> PropertyMessages
    {
        get
        {
            // Delegate to value's messages if loaded
            return (Value as IValidateMetaProperties)?.PropertyMessages ?? Array.Empty<IPropertyMessage>();
        }
    }

    /// <inheritdoc />
    public Task WaitForTasks()
    {
        if (LoadTask != null && !LoadTask.IsCompleted)
            return LoadTask;
        return (Value as IValidateMetaProperties)?.WaitForTasks() ?? Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task WaitForTasks(CancellationToken token)
    {
        if (LoadTask != null && !LoadTask.IsCompleted)
            return LoadTask.WaitAsync(token);
        return (Value as IValidateMetaProperties)?.WaitForTasks(token) ?? Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RunRules(string propertyName, CancellationToken? token = null)
        => (Value as IValidateMetaProperties)?.RunRules(propertyName, token) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null)
        => (Value as IValidateMetaProperties)?.RunRules(runRules, token) ?? Task.CompletedTask;

    /// <inheritdoc />
    public void ClearAllMessages()
    {
        ClearLoadError();
        (Value as IValidateMetaProperties)?.ClearAllMessages();
    }

    /// <inheritdoc />
    public void ClearSelfMessages()
    {
        ClearLoadError();
    }

    #endregion

    #region IEntityMetaProperties

    /// <inheritdoc />
    public bool IsChild => (Value as IEntityMetaProperties)?.IsChild ?? false;

    /// <inheritdoc />
    public bool IsModified => (Value as IEntityMetaProperties)?.IsModified ?? false;

    /// <inheritdoc />
    public bool IsSelfModified => false;  // EntityLazyLoad itself is never modified

    /// <inheritdoc />
    public bool IsMarkedModified => (Value as IEntityMetaProperties)?.IsMarkedModified ?? false;

    // IsSavable removed — moved to IEntityRoot

    /// <inheritdoc />
    public bool IsNew => (Value as IEntityMetaProperties)?.IsNew ?? false;

    /// <inheritdoc />
    public bool IsDeleted => (Value as IEntityMetaProperties)?.IsDeleted ?? false;

    #endregion
}
