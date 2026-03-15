using System.ComponentModel;
using System.Text.Json.Serialization;
using Neatoo.Rules;

namespace Neatoo;

/// <summary>
/// Non-generic internal interface for applying deserialized state to a LazyLoad instance
/// without using reflection. Used by NeatooBaseJsonTypeConverter during deserialization
/// to merge server-side state into constructor-created instances, preserving loader delegates.
/// </summary>
internal interface ILazyLoadDeserializable
{
    bool IsLoaded { get; }
    object? BoxedValue { get; }
    void ApplyDeserializedState(object? value, bool isLoaded);
}

/// <summary>
/// Wrapper for async lazy loading of child entities or related data.
/// <see cref="Value"/> is a passive read that returns the current value or <c>null</c>
/// if not yet loaded. Use <see cref="LoadAsync"/> to trigger loading.
/// </summary>
/// <typeparam name="T">The type of value to lazy load. Must be a reference type.</typeparam>
/// <remarks>
/// <para>
/// Key principle: <see cref="Value"/> never triggers a load. It returns the current state
/// (<c>null</c> if not loaded, the loaded value otherwise). Call <see cref="LoadAsync"/>
/// explicitly to invoke the loader delegate. When the load completes,
/// <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/> fires for
/// <see cref="Value"/>, <see cref="IsLoaded"/>, and <see cref="IsLoading"/>.
/// </para>
/// <para>
/// Always use <see cref="ILazyLoadFactory"/> to create instances. Do not instantiate directly.
/// </para>
/// </remarks>
public class LazyLoad<T> : INotifyPropertyChanged, IValidateMetaProperties, IEntityMetaProperties, ILazyLoadDeserializable where T : class?
{
    [JsonIgnore]
    private readonly Func<Task<T?>>? _loader;

    [JsonIgnore]
    private readonly object _loadLock = new();

    private T? _value;
    private bool _isLoaded;
    private bool _isLoading;

    [JsonIgnore]
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

    private void SubscribeToValuePropertyChanged(T? value)
    {
        if (value is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged += OnValuePropertyChanged;
        }
    }

    private void UnsubscribeFromValuePropertyChanged(T? value)
    {
        if (value is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged -= OnValuePropertyChanged;
        }
    }

    private void OnValuePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Forward child meta-state changes as our own
        OnPropertyChanged(e.PropertyName!);
    }

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    [JsonConstructor]
    public LazyLoad()
    {
        _loader = null;
        _isLoaded = false;
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
    /// Creates a new lazy load wrapper with a pre-loaded value.
    /// </summary>
    /// <param name="value">The pre-loaded value.</param>
    public LazyLoad(T? value)
    {
        _loader = () => Task.FromResult(value);
        _value = value;
        _isLoaded = true;
        SubscribeToValuePropertyChanged(_value);
    }

    /// <inheritdoc />
    bool ILazyLoadDeserializable.IsLoaded => _isLoaded;

    /// <inheritdoc />
    object? ILazyLoadDeserializable.BoxedValue => _value;

    /// <summary>
    /// Applies deserialized state (Value and IsLoaded) to this instance,
    /// preserving the loader delegate. Used by NeatooBaseJsonTypeConverter
    /// during deserialization to merge server-side state without replacing
    /// the constructor-created instance.
    /// </summary>
    void ILazyLoadDeserializable.ApplyDeserializedState(object? value, bool isLoaded)
    {
        if (isLoaded)
        {
            _value = (T?)value;
            _isLoaded = true;
            SubscribeToValuePropertyChanged(_value);
        }
        // If not loaded, leave the instance untouched -- the constructor's
        // loader delegate is intact for on-demand loading.
    }

    /// <summary>
    /// Directly sets the inner value, bypassing the loader delegate.
    /// Marks the LazyLoad as loaded, clears any load error, and fires PropertyChanged events.
    /// Null is a valid loaded state.
    /// </summary>
    /// <param name="value">The value to set. May be null.</param>
    /// <remarks>
    /// <para>
    /// This method is used by LazyLoad property subclasses to implement the
    /// <see cref="IValidateProperty.Value"/> setter, allowing framework code to set
    /// the inner entity without going through the loader delegate.
    /// </para>
    /// <para>
    /// The loader delegate is preserved (not cleared) in case a future "reload" scenario is needed.
    /// If a load is in progress, the setter wins -- <c>_isLoaded = true</c> and <c>_loadTask = null</c>.
    /// The in-flight async operation cannot be cancelled but its result will not overwrite this value
    /// because <see cref="LoadAsync"/> checks <c>_isLoaded</c> before starting a new load.
    /// </para>
    /// </remarks>
    public void SetValue(T? value)
    {
        UnsubscribeFromValuePropertyChanged(_value);
        _value = value;
        _isLoaded = true;
        _loadError = null;
        _loadTask = null;
        SubscribeToValuePropertyChanged(_value);
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(IsLoaded));
        OnPropertyChanged(nameof(HasLoadError));
    }

    /// <summary>
    /// Gets the current value. Returns <c>null</c> if not yet loaded.
    /// This is a passive read with no side effects. Call <see cref="LoadAsync"/>
    /// to trigger loading.
    /// </summary>
    [JsonInclude]
    public T? Value
    {
        get => _value;
        private set => _value = value;
    }

    /// <summary>
    /// Gets whether the value has been loaded.
    /// </summary>
    [JsonInclude]
    public bool IsLoaded
    {
        get => _isLoaded;
        private set => _isLoaded = value;
    }

    /// <summary>
    /// Gets whether a load operation is currently in progress.
    /// </summary>
    [JsonIgnore]
    public bool IsLoading => _isLoading;

    /// <summary>
    /// Gets whether a load error occurred.
    /// </summary>
    [JsonIgnore]
    public bool HasLoadError => _loadError != null;

    /// <summary>
    /// Gets the error message from the last failed load attempt, or <c>null</c> if no error.
    /// </summary>
    [JsonIgnore]
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

        if (_loader == null)
            throw new InvalidOperationException(
                "Cannot load: no loader delegate is configured. " +
                "This LazyLoad instance was likely deserialized without a pre-loaded value.");

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
            UnsubscribeFromValuePropertyChanged(_value);
            _value = await _loader!();
            SubscribeToValuePropertyChanged(_value);
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

    #region IEntityMetaProperties

    /// <inheritdoc />
    public bool IsChild => (_value as IEntityMetaProperties)?.IsChild ?? false;

    /// <inheritdoc />
    public bool IsModified => (_value as IEntityMetaProperties)?.IsModified ?? false;

    /// <inheritdoc />
    public bool IsSelfModified => false;  // LazyLoad itself is never modified

    /// <inheritdoc />
    public bool IsMarkedModified => (_value as IEntityMetaProperties)?.IsMarkedModified ?? false;

    // IsSavable removed — moved to IEntityRoot

    /// <inheritdoc />
    public bool IsNew => (_value as IEntityMetaProperties)?.IsNew ?? false;

    /// <inheritdoc />
    public bool IsDeleted => (_value as IEntityMetaProperties)?.IsDeleted ?? false;

    #endregion
}
