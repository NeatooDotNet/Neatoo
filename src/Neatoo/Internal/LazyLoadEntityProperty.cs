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

    // --- Look-through overrides ---

    /// <summary>
    /// Looks through the LazyLoad wrapper to the inner entity.
    /// Uses BoxedValue to avoid triggering auto-load.
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
    /// Throws InvalidOperationException. LazyLoad values are set by the load process,
    /// not through PropertyManager.
    /// </summary>
    public override Task SetValue(object? newValue)
    {
        throw new InvalidOperationException(
            "Cannot set a LazyLoad property value through PropertyManager. " +
            "LazyLoad values are populated by the load process.");
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
}
