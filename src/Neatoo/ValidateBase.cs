using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Neatoo;

/// <summary>
/// Defines the interface for Neatoo objects that support validation and business rules.
/// </summary>
/// <remarks>
/// <see cref="IValidateBase"/> extends <see cref="IBase"/> with validation capabilities,
/// including property-level validation messages, rule execution, and validity state tracking.
/// Objects implementing this interface can participate in the Neatoo validation pipeline.
/// </remarks>
public interface IValidateBase : IBase, IValidateMetaProperties, INotifyPropertyChanged, INotifyNeatooPropertyChanged
{
	/// <summary>
	/// Gets a value indicating whether the object is in a paused state.
	/// </summary>
	/// <remarks>
	/// When paused, events, rules, and modification tracking are suspended.
	/// This only affects the Setter method, not SetProperty or LoadProperty.
	/// </remarks>
	/// <value><c>true</c> if the object is paused; otherwise, <c>false</c>.</value>
	bool IsPaused { get; }

	/// <summary>
	/// Gets an object-level validation error message, if any.
	/// </summary>
	/// <value>The validation error message, or <c>null</c> if the object is valid.</value>
	internal string? ObjectInvalid { get; }

	/// <summary>
	/// Gets the validation property with the specified name.
	/// </summary>
	/// <param name="propertyName">The name of the property to retrieve.</param>
	/// <returns>The <see cref="IValidateProperty"/> instance for the specified property.</returns>
	new IValidateProperty GetProperty(string propertyName);

	/// <summary>
	/// Gets the validation property with the specified name using indexer syntax.
	/// </summary>
	/// <param name="propertyName">The name of the property to retrieve.</param>
	/// <returns>The <see cref="IValidateProperty"/> instance for the specified property.</returns>
	new IValidateProperty this[string propertyName] { get => GetProperty(propertyName); }

	/// <summary>
	/// Attempts to get the validation property with the specified name.
	/// </summary>
	/// <param name="propertyName">The name of the property to retrieve.</param>
	/// <param name="validateProperty">When this method returns, contains the validation property if found; otherwise, <c>null</c>.</param>
	/// <returns><c>true</c> if the property was found; otherwise, <c>false</c>.</returns>
	bool TryGetProperty(string propertyName, out IValidateProperty validateProperty);
}

/// <summary>
/// Abstract base class for Neatoo domain objects that support validation and business rules.
/// </summary>
/// <typeparam name="T">The concrete type deriving from this base class, used for the curiously recurring template pattern (CRTP).</typeparam>
/// <remarks>
/// <para>
/// <see cref="ValidateBase{T}"/> extends <see cref="Base{T}"/> with comprehensive validation capabilities:
/// </para>
/// <list type="bullet">
/// <item><description>Property-level validation messages via <see cref="IValidatePropertyManager{T}"/></description></item>
/// <item><description>Business rule execution through <see cref="IRuleManager{T}"/></description></item>
/// <item><description>Validity state tracking (IsValid, IsSelfValid)</description></item>
/// <item><description>Pause/resume functionality for batch operations</description></item>
/// </list>
/// <para>
/// Validation rules are automatically triggered when properties change, unless the object is paused.
/// Use <see cref="PauseAllActions"/> during batch updates to improve performance and prevent
/// intermediate validation states.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Customer : ValidateBase&lt;Customer&gt;
/// {
///     public string Name { get =&gt; Getter&lt;string&gt;(); set =&gt; Setter(value); }
///
///     public Customer(IValidateBaseServices&lt;Customer&gt; services) : base(services)
///     {
///         RuleManager.AddRule(c =&gt; !string.IsNullOrEmpty(c.Name), "Name is required", c =&gt; c.Name);
///     }
/// }
/// </code>
/// </example>
[Factory]
public abstract class ValidateBase<T> : Base<T>, IValidateBase, INotifyPropertyChanged, IJsonOnDeserializing, IJsonOnDeserialized, IFactoryOnStart, IFactoryOnComplete
	where T : ValidateBase<T>
{
	/// <summary>
	/// Gets the property manager with validation capabilities.
	/// </summary>
	/// <remarks>
	/// This property shadows the base class PropertyManager to provide access to validation-specific features.
	/// </remarks>
	protected new IValidatePropertyManager<IValidateProperty> PropertyManager => (IValidatePropertyManager<IValidateProperty>)base.PropertyManager;

	/// <summary>
	/// Gets the rule manager responsible for executing business rules on this object.
	/// </summary>
	/// <remarks>
	/// Use the RuleManager in constructors or initialization methods to register validation rules
	/// that will be automatically executed when relevant properties change.
	/// </remarks>
	protected IRuleManager<T> RuleManager { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ValidateBase{T}"/> class.
	/// </summary>
	/// <param name="services">The validation services containing the property manager and rule manager factory.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
	public ValidateBase(IValidateBaseServices<T> services) : base(services)
	{
		ArgumentNullException.ThrowIfNull(services, nameof(services));

		this.RuleManager = services.CreateRuleManager((T)(IValidateBase)this);

		this.RuleManager.AddValidation(static (t) =>
		{
			if (!string.IsNullOrEmpty(t.ObjectInvalid))
			{
				return t.ObjectInvalid;
			}
			return string.Empty;
		}, (t) => t.ObjectInvalid);

		this.ResetMetaState();
	}

	/// <summary>
	/// Gets a value indicating whether the object and all its child objects are valid.
	/// </summary>
	/// <value><c>true</c> if all properties and child objects pass validation; otherwise, <c>false</c>.</value>
	public bool IsValid => this.PropertyManager.IsValid;

	/// <summary>
	/// Gets a value indicating whether this object's own properties are valid, excluding child objects.
	/// </summary>
	/// <value><c>true</c> if all direct properties pass validation; otherwise, <c>false</c>.</value>
	public bool IsSelfValid => this.PropertyManager.IsSelfValid;

	/// <summary>
	/// Gets the collection of validation messages for all properties.
	/// </summary>
	/// <value>A read-only collection of <see cref="IPropertyMessage"/> instances.</value>
	public IReadOnlyCollection<IPropertyMessage> PropertyMessages => this.PropertyManager.PropertyMessages;

	/// <summary>
	/// Gets or sets the cached meta property state for change detection.
	/// </summary>
	/// <remarks>
	/// Used internally to track changes to meta properties (IsValid, IsSelfValid, IsBusy)
	/// and raise appropriate property changed notifications.
	/// </remarks>
	protected (bool IsValid, bool IsSelfValid, bool IsBusy) MetaState { get; private set; }

	/// <summary>
	/// Checks if validation-related meta properties have changed and raises notifications.
	/// </summary>
	/// <remarks>
	/// This method extends the base implementation to track changes to IsValid, IsSelfValid, and IsBusy.
	/// It automatically raises PropertyChanged events when these values change.
	/// </remarks>
	protected override void CheckIfMetaPropertiesChanged()
	{
		base.CheckIfMetaPropertiesChanged();

		if (this.MetaState.IsValid != this.IsValid)
		{
			this.RaisePropertyChanged(nameof(this.IsValid));
			this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsValid), this));
		}
		if (this.MetaState.IsSelfValid != this.IsSelfValid)
		{
			this.RaisePropertyChanged(nameof(this.IsSelfValid));
			this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsSelfValid), this));
		}
		if (this.MetaState.IsBusy != this.IsBusy)
		{
			this.RaisePropertyChanged(nameof(this.IsBusy));
			this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(this.IsBusy), this));
		}

		this.ResetMetaState();
	}

	/// <summary>
	/// Resets the cached meta property state to current values.
	/// </summary>
	/// <remarks>
	/// Called after meta property notifications are raised to prepare for the next change detection cycle.
	/// </remarks>
	protected virtual void ResetMetaState()
	{
		this.MetaState = (this.IsValid, this.IsSelfValid, this.IsBusy);
	}

	/// <summary>
	/// Handles property change notifications from child objects, triggering validation rules.
	/// </summary>
	/// <param name="eventArgs">The event arguments containing the child property change details.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	/// When not paused, this method runs rules for the changed property and propagates the notification.
	/// When paused, only the meta state is reset without running rules or propagating events.
	/// </remarks>
	protected override async Task ChildNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
	{
		if (!this.IsPaused)
		{
			await this.RunRules(eventArgs.FullPropertyName);

			await base.ChildNeatooPropertyChanged(eventArgs);

			this.CheckIfMetaPropertiesChanged();
		}
		else
		{
			this.ResetMetaState();
		}
	}

	/// <summary>
	/// Raises the PropertyChanged event if the object is not paused.
	/// </summary>
	/// <param name="propertyName">The name of the property that changed.</param>
	/// <remarks>
	/// When the object is paused via <see cref="PauseAllActions"/>, no PropertyChanged events are raised.
	/// </remarks>
	protected override void RaisePropertyChanged(string propertyName)
	{
		if (!this.IsPaused)
		{
			base.RaisePropertyChanged(propertyName);
		}
	}

	/// <summary>
	/// Permanently marks the object as invalid with the specified message.
	/// </summary>
	/// <param name="message">The validation error message explaining why the object is invalid.</param>
	/// <remarks>
	/// <para>
	/// The invalid state persists until <see cref="RunRules(RunRulesFlag, CancellationToken?)"/> is called with
	/// <see cref="RunRulesFlag.All"/>, which clears all messages including this object-level error.
	/// </para>
	/// <para>
	/// Use this method for object-level validation that cannot be expressed as a property rule,
	/// such as cross-property validation or external state validation.
	/// </para>
	/// </remarks>
	protected virtual void MarkInvalid(string message)
	{
		this.ObjectInvalid = message;
		this.CheckIfMetaPropertiesChanged();
	}

	/// <summary>
	/// Gets or sets the object-level validation error message.
	/// </summary>
	/// <value>The error message, or <c>null</c> if the object is valid at the object level.</value>
	public string? ObjectInvalid { get => this.Getter<string>(); protected set => this.Setter(value); }

	/// <summary>
	/// Gets the validation property with the specified name.
	/// </summary>
	/// <param name="propertyName">The name of the property to retrieve.</param>
	/// <returns>The <see cref="IValidateProperty"/> instance for the specified property.</returns>
	new public IValidateProperty GetProperty(string propertyName)
	{
		return this.PropertyManager[propertyName];
	}

	/// <summary>
	/// Attempts to get the validation property with the specified name.
	/// </summary>
	/// <param name="propertyName">The name of the property to retrieve.</param>
	/// <param name="validateProperty">When this method returns, contains the property if found; otherwise, <c>null</c>.</param>
	/// <returns><c>true</c> if the property exists; otherwise, <c>false</c>.</returns>
	public bool TryGetProperty(string propertyName, out IValidateProperty validateProperty)
	{
		if (this.PropertyManager.HasProperty(propertyName))
		{
			validateProperty = this.PropertyManager[propertyName];
			return true;
		}
		validateProperty = null!;
		return false;
	}

	/// <summary>
	/// Gets the validation property with the specified name using indexer syntax.
	/// </summary>
	/// <param name="propertyName">The name of the property to retrieve.</param>
	/// <returns>The <see cref="IValidateProperty"/> instance for the specified property.</returns>
	new public IValidateProperty this[string propertyName] { get => this.GetProperty(propertyName); }

	/// <summary>
	/// Gets a value indicating whether the object is in a paused state.
	/// </summary>
	/// <value><c>true</c> if property change events, rules, and notifications are suppressed; otherwise, <c>false</c>.</value>
	/// <remarks>
	/// Use <see cref="PauseAllActions"/> to pause and <see cref="ResumeAllActions"/> to resume.
	/// </remarks>
	public bool IsPaused { get; protected set; }

	/// <summary>
	/// Private helper class that resumes actions when disposed.
	/// </summary>
	private class Paused : IDisposable
	{
		private readonly ValidateBase<T> _validateBase;
		public Paused(ValidateBase<T> validateBase)
		{
			_validateBase = validateBase;
		}
		public void Dispose()
		{
			_validateBase.ResumeAllActions();
		}
	}

	/// <summary>
	/// Pauses all property change events, rule execution, and notifications.
	/// </summary>
	/// <returns>An <see cref="IDisposable"/> that will resume actions when disposed.</returns>
	/// <remarks>
	/// <para>
	/// Use this method to perform batch updates without triggering intermediate validation states.
	/// The returned disposable ensures actions are resumed even if an exception occurs.
	/// </para>
	/// <para>
	/// While paused, the <see cref="Setter{P}"/> method will not trigger rules or raise events.
	/// This only affects the Setter method; SetProperty and LoadProperty are unaffected.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// using (customer.PauseAllActions())
	/// {
	///     customer.Name = "John";
	///     customer.Email = "john@example.com";
	///     customer.Phone = "555-1234";
	/// } // Rules run automatically when disposed
	/// </code>
	/// </example>
	public virtual IDisposable PauseAllActions()
	{
		if (!this.IsPaused)
		{
			this.IsPaused = true;
			this.PropertyManager.PauseAllActions();
		}

		return new Paused(this);
	}

	/// <summary>
	/// Resumes property change events, rule execution, and notifications after being paused.
	/// </summary>
	/// <remarks>
	/// This method is called automatically when the disposable returned by <see cref="PauseAllActions"/> is disposed.
	/// It can also be called directly to resume operations.
	/// </remarks>
	public virtual void ResumeAllActions()
	{
		if (this.IsPaused)
		{
			this.IsPaused = false;
			this.PropertyManager.ResumeAllActions();
			this.ResetMetaState();
		}
	}

	/// <summary>
	/// Called after portal construction is complete to perform post-initialization tasks.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	/// Override this method to add custom logic that should run after the object is fully constructed
	/// through a factory portal operation.
	/// </remarks>
	public virtual Task PostPortalConstruct()
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// Runs validation rules associated with the specified property.
	/// </summary>
	/// <param name="propertyName">The name of the property whose rules should be executed.</param>
	/// <param name="token">Optional cancellation token for the async operation.</param>
	/// <returns>A task representing the asynchronous rule execution.</returns>
	/// <remarks>
	/// This method is typically called automatically when a property value changes.
	/// It can also be called manually to re-validate a specific property.
	/// </remarks>
	public virtual Task RunRules(string propertyName, CancellationToken? token = null)
	{
		var task = this.RuleManager.RunRules(propertyName, token);

		this.CheckIfMetaPropertiesChanged();

		return task;
	}

	/// <summary>
	/// Runs validation rules based on the specified flags.
	/// </summary>
	/// <param name="runRules">Flags indicating which rules to run (All, Self, or Children).</param>
	/// <param name="token">Optional cancellation token for the async operation.</param>
	/// <returns>A task representing the asynchronous rule execution.</returns>
	/// <remarks>
	/// <para>
	/// When <paramref name="runRules"/> is <see cref="RunRulesFlag.All"/>, all validation messages
	/// are cleared before running rules, providing a clean validation state.
	/// </para>
	/// <para>
	/// This method waits for all async rules to complete before returning.
	/// </para>
	/// </remarks>
	public virtual async Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null)
	{
		if (runRules == Neatoo.RunRulesFlag.All)
		{
			this.ClearAllMessages();
		}

		if ((runRules | Neatoo.RunRulesFlag.Self) != Neatoo.RunRulesFlag.Self)
		{
			await this.PropertyManager.RunRules(runRules, token);
		}

		await this.RuleManager.RunRules(runRules, token);
		await this.RunningTasks.AllDone;
	}

	/// <summary>
	/// Clears all validation messages from this object's direct properties.
	/// </summary>
	/// <remarks>
	/// This method does not clear messages from child objects. Use <see cref="ClearAllMessages"/>
	/// to clear messages from the entire object graph.
	/// </remarks>
	public virtual void ClearSelfMessages()
	{
		this[nameof(this.ObjectInvalid)].ClearAllMessages();
		this.PropertyManager.ClearSelfMessages();
	}

	/// <summary>
	/// Clears all validation messages from this object and all child objects.
	/// </summary>
	/// <remarks>
	/// This method recursively clears messages throughout the object graph.
	/// </remarks>
	public virtual void ClearAllMessages()
	{
		this[nameof(this.ObjectInvalid)].ClearAllMessages();
		this.PropertyManager.ClearAllMessages();
	}

	/// <summary>
	/// Explicit interface implementation for getting a validation property.
	/// </summary>
	/// <param name="propertyName">The name of the property to retrieve.</param>
	/// <returns>The validation property instance.</returns>
	IValidateProperty IValidateBase.GetProperty(string propertyName)
	{
		return this.GetProperty(propertyName);
	}

	/// <summary>
	/// Called before JSON deserialization begins to pause all actions.
	/// </summary>
	/// <remarks>
	/// Pausing during deserialization prevents unnecessary rule execution and event raising
	/// while property values are being restored.
	/// </remarks>
	public void OnDeserializing()
	{
		this.PauseAllActions();
	}

	/// <summary>
	/// Called after JSON deserialization completes to resume all actions.
	/// </summary>
	/// <remarks>
	/// Resumes actions that were paused in <see cref="OnDeserializing"/> and restores
	/// event subscriptions through the base class implementation.
	/// </remarks>
	override public void OnDeserialized()
	{
		base.OnDeserialized();
		this.ResumeAllActions();
	}

	/// <summary>
	/// Called when a factory operation begins to pause all actions.
	/// </summary>
	/// <param name="factoryOperation">The type of factory operation being performed.</param>
	/// <remarks>
	/// This ensures that Create, Fetch, Insert, Update, and Delete operations
	/// do not trigger validation rules or events during execution.
	/// </remarks>
	public virtual void FactoryStart(FactoryOperation factoryOperation)
	{
		this.PauseAllActions();
	}

	/// <summary>
	/// Called when a factory operation completes to resume all actions.
	/// </summary>
	/// <param name="factoryOperation">The type of factory operation that completed.</param>
	/// <remarks>
	/// This resumes normal event and rule processing after a factory operation.
	/// </remarks>
	public virtual void FactoryComplete(FactoryOperation factoryOperation)
	{
		this.ResumeAllActions();
	}
}

/// <summary>
/// Exception thrown when the AddRules method is not defined for a ValidateBase-derived type.
/// </summary>
/// <typeparam name="T">The type that is missing the AddRules definition.</typeparam>
[Serializable]
public class AddRulesNotDefinedException<T> : ConfigurationException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AddRulesNotDefinedException{T}"/> class
	/// with a default message indicating the type missing the AddRules definition.
	/// </summary>
	public AddRulesNotDefinedException() : base($"AddRules not defined for {typeof(T).Name}") { }

	/// <summary>
	/// Initializes a new instance of the <see cref="AddRulesNotDefinedException{T}"/> class
	/// with a specified error message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public AddRulesNotDefinedException(string message) : base(message) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="AddRulesNotDefinedException{T}"/> class
	/// with a specified error message and inner exception.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="inner">The exception that is the cause of the current exception.</param>
	public AddRulesNotDefinedException(string message, Exception inner) : base(message, inner) { }
}
