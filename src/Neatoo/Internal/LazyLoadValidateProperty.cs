using System.ComponentModel;

namespace Neatoo.Internal;

/// <summary>
/// Marker interface for LazyLoad property subclasses.
/// Used by NeatooBaseJsonTypeConverter to skip these entries in the PropertyManager
/// serialization array (LazyLoad properties are serialized as top-level JSON properties).
/// Also provides deserialization support via ReconnectAfterDeserialization.
/// </summary>
internal interface ILazyLoadProperty
{
    /// <summary>
    /// Reconnects the property subclass to its LazyLoad wrapper's current inner value.
    /// Called after deserialization when ApplyDeserializedState has modified the wrapper directly,
    /// bypassing the generated setter. This ensures inner child event subscriptions are correct.
    /// </summary>
    void ReconnectAfterDeserialization();
}

/// <summary>
/// Shared helper methods for LazyLoad property subclasses.
/// Extracts the look-through logic so both LazyLoadValidateProperty and
/// LazyLoadEntityProperty use identical inner-child resolution without duplication.
/// </summary>
internal static class LazyLoadPropertyHelper
{
    /// <summary>
    /// Gets the inner entity as IValidateBase by looking through the LazyLoad wrapper.
    /// Uses BoxedValue to avoid triggering auto-load on the LazyLoad.Value getter.
    /// </summary>
    internal static IValidateBase? GetValueAsBase<T>(LazyLoad<T>? lazyLoad) where T : class?
    {
        if (lazyLoad == null) return null;
        return ((ILazyLoadDeserializable)lazyLoad).BoxedValue as IValidateBase;
    }

    /// <summary>
    /// Gets the inner entity as IValidateMetaProperties by looking through the LazyLoad wrapper.
    /// Uses BoxedValue to avoid triggering auto-load on the LazyLoad.Value getter.
    /// </summary>
    internal static IValidateMetaProperties? GetValueIsValidateBase<T>(LazyLoad<T>? lazyLoad) where T : class?
    {
        if (lazyLoad == null) return null;
        return ((ILazyLoadDeserializable)lazyLoad).BoxedValue as IValidateMetaProperties;
    }

    /// <summary>
    /// Gets the inner entity as IEntityMetaProperties by looking through the LazyLoad wrapper.
    /// Uses BoxedValue to avoid triggering auto-load on the LazyLoad.Value getter.
    /// </summary>
    internal static IEntityMetaProperties? GetEntityChild<T>(LazyLoad<T>? lazyLoad) where T : class?
    {
        if (lazyLoad == null) return null;
        return ((ILazyLoadDeserializable)lazyLoad).BoxedValue as IEntityMetaProperties;
    }

    /// <summary>
    /// Connects event handlers to the inner child entity for NeatooPropertyChanged propagation.
    /// </summary>
    internal static object? ConnectInnerChild(object? innerChild, NeatooPropertyChanged handler)
    {
        if (innerChild == null) return null;

        if (innerChild is INotifyNeatooPropertyChanged npc)
        {
            npc.NeatooPropertyChanged += handler;
        }

        return innerChild;
    }

    /// <summary>
    /// Disconnects event handlers from the inner child entity and clears parent.
    /// </summary>
    internal static void DisconnectInnerChild(ref object? currentInnerChild, NeatooPropertyChanged handler)
    {
        if (currentInnerChild == null) return;

        if (currentInnerChild is INotifyNeatooPropertyChanged npc)
        {
            npc.NeatooPropertyChanged -= handler;
        }
        if (currentInnerChild is ISetParent sp)
        {
            sp.SetParent(null);
        }

        currentInnerChild = null;
    }
}

/// <summary>
/// Property subclass that looks through a LazyLoad wrapper to the inner entity.
/// Registered with PropertyManager so that RunRules cascading, PropertyMessages aggregation,
/// WaitForTasks, IsBusy, IsValid, and ClearAllMessages all work through the unified
/// property system instead of the old parallel LazyLoad helper methods.
/// </summary>
/// <remarks>
/// Re-declares IValidateProperty to force interface re-implementation.
/// This ensures that non-virtual members (IsBusy, WaitForTasks, ValueAsBase) dispatch
/// to the subclass overrides when accessed through IValidateProperty interface references,
/// which is how PropertyManager accesses them.
/// </remarks>
internal class LazyLoadValidateProperty<T> : ValidateProperty<LazyLoad<T>>, IValidateProperty, ILazyLoadProperty
    where T : class?
{
    private object? _currentInnerChild;

    public LazyLoadValidateProperty(IPropertyInfo propertyInfo) : base(propertyInfo)
    {
        // LazyLoad properties are always read-only through PropertyManager
        this.IsReadOnly = true;
    }

    // --- Value override ---

    /// <summary>
    /// Returns the inner entity (or null) instead of the LazyLoad wrapper.
    /// Uses BoxedValue to avoid triggering auto-load on the LazyLoad.Value getter.
    /// The setter delegates to <see cref="LazyLoad{T}.SetValue(T?)"/> which sets the inner value,
    /// marks the LazyLoad as loaded, clears errors, and fires PropertyChanged events.
    /// </summary>
    /// <remarks>
    /// Uses 'new' because the base <see cref="ValidateProperty{T}.Value"/> is virtual with return type
    /// <c>LazyLoad&lt;T&gt;?</c>, but we need <c>object?</c> for the inner entity.
    /// The LazyLoad subclass re-declares <see cref="IValidateProperty"/> to force interface re-implementation,
    /// so <c>IValidateProperty.Value</c> dispatches to this member when accessed through the interface.
    /// Note: <see cref="ValidateProperty{T}.PassThruValueNeatooPropertyChanged"/> and
    /// <see cref="ValidateProperty{T}.OnDeserialized"/> call <c>this.Value</c> through virtual dispatch
    /// which resolves to the base member (returns the LazyLoad wrapper), not this 'new' member.
    /// This is correct -- those methods need the LazyLoad wrapper for event subscription.
    /// </remarks>
    public new object? Value
    {
        get => ((ILazyLoadDeserializable?)this._value)?.BoxedValue;
        set
        {
            if (this._value == null)
                throw new InvalidOperationException("Cannot set value: no LazyLoad wrapper is assigned.");
            this._value.SetValue((T?)value);
        }
    }

    // --- Look-through overrides ---

    /// <summary>
    /// Looks through the LazyLoad wrapper to the inner entity.
    /// Uses BoxedValue to avoid triggering auto-load.
    /// </summary>
    protected new IValidateBase? ValueAsBase => LazyLoadPropertyHelper.GetValueAsBase(this._value);

    /// <summary>
    /// Looks through the LazyLoad wrapper to the inner entity for validation delegation.
    /// This makes IsValid, RunRules, PropertyMessages, and ClearAllMessages cascade
    /// to the inner entity automatically.
    /// When the inner entity is not loaded, falls back to the LazyLoad wrapper itself
    /// (which implements IValidateMetaProperties) so that load error state (HasLoadError)
    /// propagates IsValid=false correctly.
    /// </summary>
    public override IValidateMetaProperties? ValueIsValidateBase
    {
        get
        {
            // First try inner entity (loaded case)
            var inner = LazyLoadPropertyHelper.GetValueIsValidateBase(this._value);
            if (inner != null) return inner;
            // Fall back to LazyLoad wrapper (handles error state: IsValid=false when HasLoadError)
            return this._value as IValidateMetaProperties;
        }
    }

    /// <summary>
    /// Delegates to LazyLoad.IsBusy which includes both the loading state and the inner child's busy state.
    /// Must use 'new' because base IsBusy is non-virtual.
    /// </summary>
    public new bool IsBusy
    {
        get
        {
            var lazyLoad = this._value;
            return (lazyLoad?.IsBusy ?? false)
                || base.IsSelfBusy
                || this.IsMarkedBusy.Count > 0;
        }
    }

    /// <summary>
    /// Delegates to LazyLoad.WaitForTasks() which handles both the load task and inner child tasks.
    /// Must use 'new' because base WaitForTasks is non-virtual.
    /// </summary>
    public new async Task WaitForTasks()
    {
        var lazyLoad = this._value;
        if (lazyLoad != null)
        {
            await lazyLoad.WaitForTasks();
        }
    }

    // --- Lifecycle overrides ---

    /// <summary>
    /// Delegates to <see cref="LazyLoad{T}.SetValue(T?)"/> to set the inner value directly.
    /// This is the path used by <see cref="IValidateProperty.SetValue(object?)"/> and
    /// the property setter infrastructure.
    /// </summary>
    public override Task SetValue(object? newValue)
    {
        if (this._value == null)
            throw new InvalidOperationException("Cannot set value: no LazyLoad wrapper is assigned.");
        this._value.SetValue((T?)newValue);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Extends base HandleNonNullValue to connect to the already-loaded inner child
    /// after the base handles LazyLoad-level subscriptions.
    /// </summary>
    protected override void HandleNonNullValue(LazyLoad<T> value, bool quietly = false)
    {
        // Disconnect any existing inner child before base reassigns _value
        LazyLoadPropertyHelper.DisconnectInnerChild(ref _currentInnerChild, this.PassThruValueNeatooPropertyChanged);

        base.HandleNonNullValue(value, quietly);

        // Connect to already-loaded inner child (e.g., Create path with pre-loaded value)
        var innerChild = ((ILazyLoadDeserializable)value).BoxedValue;
        _currentInnerChild = LazyLoadPropertyHelper.ConnectInnerChild(innerChild, this.PassThruValueNeatooPropertyChanged);
    }

    /// <summary>
    /// Extends base HandleNullValue to disconnect the inner child.
    /// </summary>
    protected override void HandleNullValue(bool quietly = false)
    {
        LazyLoadPropertyHelper.DisconnectInnerChild(ref _currentInnerChild, this.PassThruValueNeatooPropertyChanged);
        base.HandleNullValue(quietly);
    }

    /// <summary>
    /// Extends base LoadValue to connect to the already-loaded inner child
    /// after the base handles LazyLoad-level setup.
    /// </summary>
    public override void LoadValue(object? value)
    {
        // Disconnect existing inner child before base reassigns
        LazyLoadPropertyHelper.DisconnectInnerChild(ref _currentInnerChild, this.PassThruValueNeatooPropertyChanged);

        base.LoadValue(value);

        if (value is LazyLoad<T> lazyLoad)
        {
            var innerChild = ((ILazyLoadDeserializable)lazyLoad).BoxedValue;
            _currentInnerChild = LazyLoadPropertyHelper.ConnectInnerChild(innerChild, this.PassThruValueNeatooPropertyChanged);
        }
    }

    /// <summary>
    /// When LazyLoad fires PropertyChanged("Value"), the inner entity has changed
    /// (load completed or value replaced). Run disconnect/reconnect on the inner entity
    /// and fire NeatooPropertyChanged so parent runs rule cascading and SetParent.
    /// Also translates LazyLoad-specific property names (IsLoading, HasLoadError) to
    /// PropertyManager-compatible names (IsBusy, IsValid, IsSelfValid) so the parent
    /// PropertyManager recalculates its cached state correctly.
    /// </summary>
    protected override void PassThruValuePropertyChanged(object? source, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == "Value" && source is ILazyLoadDeserializable ll)
        {
            var newInnerChild = ll.BoxedValue;
            LazyLoadPropertyHelper.DisconnectInnerChild(ref _currentInnerChild, this.PassThruValueNeatooPropertyChanged);
            _currentInnerChild = LazyLoadPropertyHelper.ConnectInnerChild(newInnerChild, this.PassThruValueNeatooPropertyChanged);

            // Fire NeatooPropertyChanged so parent runs rule cascading and SetParent
            this.Task = this.OnValueNeatooPropertyChanged(
                new NeatooPropertyChangedEventArgs(this, ChangeReason.Load));
        }

        base.PassThruValuePropertyChanged(source, eventArgs);

        // Translate LazyLoad-specific property change notifications to
        // PropertyManager-compatible names so parent recalculates cached state.
        if (eventArgs.PropertyName == "IsLoading")
        {
            // LazyLoad.IsLoading changed -> our IsBusy depends on it
            OnPropertyChanged(nameof(IsBusy));
        }
        else if (eventArgs.PropertyName == "HasLoadError" || eventArgs.PropertyName == "LoadError")
        {
            // LazyLoad load error -> our IsValid/IsSelfValid depends on it
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(IsSelfValid));
        }
    }

    // --- Deserialization support ---

    /// <summary>
    /// Reconnects inner child events after deserialization.
    /// ApplyDeserializedState modifies the LazyLoad wrapper's inner value directly,
    /// bypassing the generated setter. This method re-establishes event subscriptions.
    /// </summary>
    void ILazyLoadProperty.ReconnectAfterDeserialization()
    {
        if (this._value != null)
        {
            LazyLoadPropertyHelper.DisconnectInnerChild(ref _currentInnerChild, this.PassThruValueNeatooPropertyChanged);
            var innerChild = ((ILazyLoadDeserializable)this._value).BoxedValue;
            _currentInnerChild = LazyLoadPropertyHelper.ConnectInnerChild(innerChild, this.PassThruValueNeatooPropertyChanged);
        }
    }
}
