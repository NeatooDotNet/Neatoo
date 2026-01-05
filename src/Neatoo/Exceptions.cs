namespace Neatoo;

/// <summary>
/// Base exception for all Neatoo framework exceptions.
/// Provides a common base for catching any Neatoo-specific error.
/// </summary>
[Serializable]
public abstract class NeatooException : Exception
{
	protected NeatooException() { }
	protected NeatooException(string message) : base(message) { }
	protected NeatooException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Base exception for property-related errors in the Neatoo framework.
/// </summary>
[Serializable]
public abstract class PropertyException : NeatooException
{
	protected PropertyException() { }
	protected PropertyException(string message) : base(message) { }
	protected PropertyException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Base exception for rule-related errors in the Neatoo framework.
/// </summary>
[Serializable]
public abstract class RuleException : NeatooException
{
	protected RuleException() { }
	protected RuleException(string message) : base(message) { }
	protected RuleException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Base exception for entity-related errors in the Neatoo framework.
/// </summary>
[Serializable]
public abstract class EntityException : NeatooException
{
	protected EntityException() { }
	protected EntityException(string message) : base(message) { }
	protected EntityException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Base exception for configuration and setup errors in the Neatoo framework.
/// </summary>
[Serializable]
public abstract class ConfigurationException : NeatooException
{
	protected ConfigurationException() { }
	protected ConfigurationException(string message) : base(message) { }
	protected ConfigurationException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Specifies the reason why a save operation failed.
/// </summary>
public enum SaveFailureReason
{
	/// <summary>Child objects cannot be saved directly; save the parent instead.</summary>
	IsChildObject,
	/// <summary>The object has validation errors and cannot be saved.</summary>
	IsInvalid,
	/// <summary>The object has not been modified, so there is nothing to save.</summary>
	NotModified,
	/// <summary>The object is currently busy with an async operation.</summary>
	IsBusy,
	/// <summary>No factory save method is configured for this object type.</summary>
	NoFactoryMethod
}

/// <summary>
/// Exception thrown when a save operation fails on an entity.
/// Check the <see cref="Reason"/> property to determine why the save failed.
/// </summary>
[Serializable]
public class SaveOperationException : EntityException
{
	/// <summary>
	/// Gets the reason why the save operation failed.
	/// </summary>
	public SaveFailureReason Reason { get; }

	public SaveOperationException() : base("Save operation failed.") { }

	public SaveOperationException(string message) : base(message) { }

	public SaveOperationException(string message, Exception inner) : base(message, inner) { }

	public SaveOperationException(SaveFailureReason reason) : base(GetMessage(reason))
	{
		this.Reason = reason;
	}

	public SaveOperationException(SaveFailureReason reason, string message) : base(message)
	{
		this.Reason = reason;
	}

	public SaveOperationException(SaveFailureReason reason, string message, Exception inner) : base(message, inner)
	{
		this.Reason = reason;
	}

	private static string GetMessage(SaveFailureReason reason) => reason switch
	{
		SaveFailureReason.IsChildObject => "Child objects cannot be saved directly. Save the parent object instead.",
		SaveFailureReason.IsInvalid => "Object is not valid and cannot be saved. Check validation errors before saving.",
		SaveFailureReason.NotModified => "Object has not been modified. There are no changes to save.",
		SaveFailureReason.IsBusy => "Object is busy with an async operation and cannot be saved. Call 'await entity.WaitForTasks()' before saving.",
		SaveFailureReason.NoFactoryMethod => "No factory save method is configured. Ensure [Insert], [Update], and/or [Delete] methods with no non-service parameters are defined.",
		_ => "Save operation failed."
	};
}

/// <summary>
/// Exception thrown when attempting to add or remove a child object that is currently busy with an async operation.
/// </summary>
[Serializable]
public class ChildObjectBusyException : PropertyException
{
	/// <summary>
	/// Gets whether the operation was an add (true) or remove (false).
	/// </summary>
	public bool IsAddOperation { get; }

	public ChildObjectBusyException() : base("Cannot modify a child that is busy with an async operation.") { }

	public ChildObjectBusyException(string message) : base(message) { }

	public ChildObjectBusyException(string message, Exception inner) : base(message, inner) { }

	public ChildObjectBusyException(bool isAddOperation)
		: base(isAddOperation ? "Cannot add a child that is busy with an async operation." : "Cannot remove a child that is busy with an async operation.")
	{
		this.IsAddOperation = isAddOperation;
	}

	public ChildObjectBusyException(bool isAddOperation, string message) : base(message)
	{
		this.IsAddOperation = isAddOperation;
	}

	public ChildObjectBusyException(bool isAddOperation, string message, Exception inner) : base(message, inner)
	{
		this.IsAddOperation = isAddOperation;
	}
}

/// <summary>
/// Exception thrown when a required type is not registered in the dependency injection container.
/// </summary>
[Serializable]
public class TypeNotRegisteredException : ConfigurationException
{
	/// <summary>
	/// Gets the type that was not registered.
	/// </summary>
	public Type? UnregisteredType { get; }

	public TypeNotRegisteredException() : base("A required type is not registered in the service container.") { }

	public TypeNotRegisteredException(string message) : base(message) { }

	public TypeNotRegisteredException(string message, Exception inner) : base(message, inner) { }

	public TypeNotRegisteredException(Type type)
		: base($"Type {type.FullName} is not registered in the service container. Ensure AddNeatoo() is called and the type is properly configured.")
	{
		this.UnregisteredType = type;
	}
}

/// <summary>
/// Exception thrown when attempting to run a rule that has not been added to the rule manager.
/// </summary>
[Serializable]
public class RuleNotAddedException : RuleException
{
	public RuleNotAddedException()
		: base("Rule must be added to the RuleManager before it can be executed.") { }

	public RuleNotAddedException(string message) : base(message) { }

	public RuleNotAddedException(string message, Exception inner) : base(message, inner) { }
}
