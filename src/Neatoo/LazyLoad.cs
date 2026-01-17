using System.ComponentModel;
using System.Runtime.CompilerServices;
using Neatoo.Rules;

namespace Neatoo;

/// <summary>
/// Wrapper for explicit async lazy loading of child entities or related data.
/// Separates UI binding concerns (state properties) from async loading (explicit await).
/// </summary>
/// <typeparam name="T">The type of value to lazy load. Must be a reference type.</typeparam>
/// <remarks>
/// <para>
/// Key principle: Accessing <see cref="Value"/> never triggers a load - it returns current state only.
/// Loading is always explicit via <c>await</c> or <see cref="LoadAsync"/>.
/// </para>
/// <para>
/// Always use <see cref="ILazyLoadFactory"/> to create instances. Do not instantiate directly.
/// </para>
/// </remarks>
public class LazyLoad<T> : INotifyPropertyChanged, IValidateMetaProperties where T : class
{
    private readonly Func<Task<T?>> _loader;
    private readonly object _loadLock = new();

    private T? _value;
    private bool _isLoaded;
    private bool _isLoading;
    private Task<T?>? _loadTask;
    private string? _loadError;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Creates a new lazy load wrapper with the specified loader delegate.
    /// </summary>
    /// <param name="loader">Async function that loads the value when invoked.</param>
    public LazyLoad(Func<Task<T?>> loader)
    {
        _loader = loader;
    }

    /// <summary>
    /// Gets the current value. Returns <c>null</c> if not yet loaded.
    /// Never triggers a load - use <c>await</c> or <see cref="LoadAsync"/> to load.
    /// </summary>
    public T? Value => _value;

    /// <summary>
    /// Gets whether the value has been loaded.
    /// </summary>
    public bool IsLoaded => _isLoaded;

    /// <summary>
    /// Gets whether a load operation is currently in progress.
    /// </summary>
    public bool IsLoading => _isLoading;

    /// <summary>
    /// Gets whether a load error occurred.
    /// </summary>
    public bool HasLoadError => _loadError != null;

    /// <summary>
    /// Gets the error message from the last failed load attempt, or <c>null</c> if no error.
    /// </summary>
    public string? LoadError => _loadError;

    /// <summary>
    /// Loads the value asynchronously by invoking the loader delegate.
    /// Sets <see cref="IsLoaded"/> to <c>true</c> and updates <see cref="Value"/>.
    /// </summary>
    /// <returns>The loaded value, or <c>null</c> if the loader returns null.</returns>
    /// <remarks>
    /// Thread-safe: Multiple concurrent calls share a single load operation.
    /// If a load is already in progress, subsequent calls return the same task.
    /// </remarks>
    public Task<T?> LoadAsync()
    {
        if (_isLoaded)
            return Task.FromResult(_value);

        lock (_loadLock)
        {
            if (_loadTask != null)
                return _loadTask;

            _loadTask = LoadAsyncCore();
            return _loadTask;
        }
    }

    private async Task<T?> LoadAsyncCore()
    {
        _isLoading = true;
        OnPropertyChanged(nameof(IsLoading));
        try
        {
            _value = await _loader();
            _isLoaded = true;
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(IsLoaded));
            return _value;
        }
        catch (Exception ex)
        {
            _loadError = ex.Message;
            OnPropertyChanged(nameof(HasLoadError));
            OnPropertyChanged(nameof(LoadError));
            throw;
        }
        finally
        {
            _isLoading = false;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    /// <summary>
    /// Gets an awaiter for this lazy load, enabling <c>await lazyLoad</c> syntax.
    /// </summary>
    /// <returns>A task awaiter that loads the value when awaited.</returns>
    public TaskAwaiter<T?> GetAwaiter() => LoadAsync().GetAwaiter();

    #region IValidateMetaProperties

    /// <inheritdoc />
    public bool IsBusy => IsLoading || ((_value as IValidateMetaProperties)?.IsBusy ?? false);

    /// <inheritdoc />
    bool IValidateMetaProperties.IsValid => !HasLoadError && ((_value as IValidateMetaProperties)?.IsValid ?? true);

    /// <inheritdoc />
    public bool IsSelfValid => !HasLoadError;

    /// <inheritdoc />
    public IReadOnlyCollection<IPropertyMessage> PropertyMessages
    {
        get
        {
            // Delegate to value's messages if loaded
            return (_value as IValidateMetaProperties)?.PropertyMessages ?? Array.Empty<IPropertyMessage>();
        }
    }

    /// <inheritdoc />
    public Task WaitForTasks()
    {
        if (_loadTask != null && !_loadTask.IsCompleted)
            return _loadTask;
        return (_value as IValidateMetaProperties)?.WaitForTasks() ?? Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task WaitForTasks(CancellationToken token)
    {
        if (_loadTask != null && !_loadTask.IsCompleted)
            return _loadTask.WaitAsync(token);
        return (_value as IValidateMetaProperties)?.WaitForTasks(token) ?? Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RunRules(string propertyName, CancellationToken? token = null)
        => (_value as IValidateMetaProperties)?.RunRules(propertyName, token) ?? Task.CompletedTask;

    /// <inheritdoc />
    public Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null)
        => (_value as IValidateMetaProperties)?.RunRules(runRules, token) ?? Task.CompletedTask;

    /// <inheritdoc />
    public void ClearAllMessages()
    {
        _loadError = null;
        (_value as IValidateMetaProperties)?.ClearAllMessages();
    }

    /// <inheritdoc />
    public void ClearSelfMessages()
    {
        _loadError = null;
    }

    #endregion
}
