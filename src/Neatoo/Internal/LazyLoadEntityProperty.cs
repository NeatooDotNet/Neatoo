using System.ComponentModel;

namespace Neatoo.Internal;

/// <summary>
/// Entity property subclass that looks through a LazyLoad wrapper to the inner entity.
/// Extends EntityProperty with the same look-through logic as LazyLoadValidateProperty,
/// plus EntityProperty-specific overrides for EntityChild and IsModified.
/// </summary>
/// <remarks>
/// Re-declares IEntityProperty to force interface re-implementation.
/// This ensures that non-virtual members (EntityChild, IsModified, IsBusy, WaitForTasks, ValueAsBase)
/// dispatch to the subclass overrides when accessed through IEntityProperty interface references,
/// which is how EntityPropertyManager accesses them.
///
/// EntityProperty.OnPropertyChanged IsSelfModified logic works correctly by accident:
/// Line 45: this.IsSelfModified = true &amp;&amp; this.EntityChild == null
/// Since our overridden EntityChild looks through to the inner entity (non-null when loaded),
/// IsSelfModified stays false. When inner entity is not yet loaded, EntityChild returns null,
/// but LazyLoad assignment should not mark the property as self-modified either.
/// We suppress this by overriding OnPropertyChanged for the "Value" case.
/// </remarks>
internal class LazyLoadEntityProperty<T> : EntityProperty<LazyLoad<T>>, IEntityProperty, ILazyLoadProperty
    where T : class?
{
    private object? _currentInnerChild;

    public LazyLoadEntityProperty(IPropertyInfo propertyInfo) : base(propertyInfo)
    {
        // LazyLoad properties are always read-only through PropertyManager
        this.IsReadOnly = true;
    }

    // --- Value override ---

    /// <summary>
    /// Returns the inner entity (or null) instead of the LazyLoad wrapper.
    /// Uses BoxedValue for direct internal access to the backing value.
    /// The setter delegates to <see cref="LazyLoad{T}.SetValue(T?)"/> which sets the inner value,
    /// marks the LazyLoad as loaded, clears errors, and fires PropertyChanged events.
    /// </summary>
    /// <remarks>
    /// Uses 'new' because the base <see cref="ValidateProperty{T}.Value"/> is virtual with return type
    /// <c>LazyLoad&lt;T&gt;?</c>, but we need <c>object?</c> for the inner entity.
    /// The LazyLoad subclass re-declares <see cref="IEntityProperty"/> (which extends <see cref="IValidateProperty"/>)
    /// to force interface re-implementation, so <c>IValidateProperty.Value</c> dispatches to this member
    /// when accessed through the interface.
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
    /// Uses BoxedValue for direct internal access to the backing value.
    /// </summary>
    protected new IValidateBase? ValueAsBase => LazyLoadPropertyHelper.GetValueAsBase(this._value);

    /// <summary>
    /// Looks through the LazyLoad wrapper to the inner entity for validation delegation.
    /// Falls back to the LazyLoad wrapper itself when inner entity is not loaded,
    /// so load error state (HasLoadError) propagates IsValid=false correctly.
    /// </summary>
    public override IValidateMetaProperties? ValueIsValidateBase
    {
        get
        {
            var inner = LazyLoadPropertyHelper.GetValueIsValidateBase(this._value);
            if (inner != null) return inner;
            return this._value as IValidateMetaProperties;
        }
    }

    /// <summary>
    /// Looks through the LazyLoad wrapper to the inner entity for entity-specific delegation.
    /// Makes IsModified (IsSelfModified || EntityChild?.IsModified) delegate correctly to the inner entity.
    /// </summary>
    public new IEntityMetaProperties? EntityChild => LazyLoadPropertyHelper.GetEntityChild(this._value);

    /// <summary>
    /// Delegates to LazyLoad.IsBusy which includes both the loading state and the inner child's busy state.
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
    /// Looks through the LazyLoad wrapper to the inner entity's IsModified.
    /// IsSelfModified is always false for LazyLoad properties (the wrapper itself is never modified).
    /// </summary>
    public new bool IsModified => this.EntityChild?.IsModified ?? false;

    /// <summary>
    /// Delegates to LazyLoad.WaitForTasks() which handles both the load task and inner child tasks.
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
    /// Extends base HandleNonNullValue to connect to the already-loaded inner child.
    /// </summary>
    protected override void HandleNonNullValue(LazyLoad<T> value, bool quietly = false)
    {
        LazyLoadPropertyHelper.DisconnectInnerChild(ref _currentInnerChild, this.PassThruValueNeatooPropertyChanged);

        base.HandleNonNullValue(value, quietly);

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
    /// Extends base LoadValue to connect to the already-loaded inner child.
    /// </summary>
    public override void LoadValue(object? value)
    {
        LazyLoadPropertyHelper.DisconnectInnerChild(ref _currentInnerChild, this.PassThruValueNeatooPropertyChanged);

        base.LoadValue(value);

        if (value is LazyLoad<T> lazyLoad)
        {
            var innerChild = ((ILazyLoadDeserializable)lazyLoad).BoxedValue;
            _currentInnerChild = LazyLoadPropertyHelper.ConnectInnerChild(innerChild, this.PassThruValueNeatooPropertyChanged);
        }
    }

    /// <summary>
    /// When LazyLoad fires PropertyChanged("Value"), the inner entity has changed.
    /// Run disconnect/reconnect on the inner entity and fire NeatooPropertyChanged.
    /// Also translates LazyLoad-specific property names to PropertyManager-compatible names.
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
            OnPropertyChanged(nameof(IsBusy));
        }
        else if (eventArgs.PropertyName == "HasLoadError" || eventArgs.PropertyName == "LoadError")
        {
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(IsSelfValid));
        }
    }

    /// <summary>
    /// Overrides EntityProperty.OnPropertyChanged to suppress IsSelfModified for LazyLoad properties.
    /// When Value changes, EntityProperty base would set IsSelfModified=true if EntityChild is null.
    /// For LazyLoad properties during the transition before inner child is connected,
    /// EntityChild could temporarily be null. We suppress this by skipping the base
    /// EntityProperty logic for Value changes and calling ValidateProperty.OnPropertyChanged directly.
    /// </summary>
    protected override void OnPropertyChanged(string propertyName)
    {
        if (propertyName == nameof(Value))
        {
            // Skip EntityProperty's IsSelfModified=true logic.
            // Call ValidateProperty.OnPropertyChanged directly via the PropertyChanged event.
            // We can't call base.base, so we invoke the property changed notification directly.
            base.OnPropertyChanged(propertyName);

            // But undo any IsSelfModified that EntityProperty.OnPropertyChanged may have set
            if (this.IsSelfModified)
            {
                this.IsSelfModified = false;
            }
        }
        else
        {
            base.OnPropertyChanged(propertyName);
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
