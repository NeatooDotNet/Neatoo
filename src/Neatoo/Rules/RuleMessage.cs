using System.Text.Json.Serialization;

namespace Neatoo.Rules;

/// <summary>
/// Represents a validation message produced by a rule, associated with a specific property.
/// </summary>
/// <remarks>
/// Rule messages are the primary way rules communicate validation errors to the UI.
/// Each message is associated with a property name and tracked by the rule's unique index.
/// </remarks>
public interface IRuleMessage
{
    /// <summary>
    /// Gets or sets the unique index of the rule that produced this message.
    /// Used internally to track which rule owns this message for proper cleanup.
    /// </summary>
    uint RuleIndex { get; internal set; }

    /// <summary>
    /// Gets the name of the property this message is associated with.
    /// </summary>
    string PropertyName { get; }

    /// <summary>
    /// Gets the validation error message text.
    /// </summary>
    string? Message { get; }
}

/// <summary>
/// Default implementation of <see cref="IRuleMessage"/> as an immutable record.
/// Represents a validation message associated with a property.
/// </summary>
/// <remarks>
/// <para>
/// RuleMessage is a record type, providing value-based equality and immutability.
/// It supports implicit conversion from a tuple for convenient creation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create using constructor
/// var message = new RuleMessage("Name", "Name is required");
///
/// // Create using tuple implicit conversion
/// RuleMessage message = ("Name", "Name is required");
///
/// // Use in rule return
/// return (nameof(Person.Name), "Name is required").AsRuleMessages();
/// </code>
/// </example>
public record RuleMessage : IRuleMessage
{
    /// <inheritdoc />
    public uint RuleIndex { get; set; }

    /// <inheritdoc />
    public string PropertyName { get; }

    /// <inheritdoc />
    public string? Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleMessage"/> record with no message (for clearing).
    /// </summary>
    /// <param name="propertyName">The name of the property this message is associated with.</param>
    public RuleMessage(string propertyName)
    {
        this.PropertyName = propertyName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleMessage"/> record with a property name and message.
    /// </summary>
    /// <param name="propertyName">The name of the property this message is associated with.</param>
    /// <param name="message">The validation error message.</param>
    [JsonConstructor]
    public RuleMessage(string propertyName, string message)
    {
        this.PropertyName = propertyName;
        this.Message = message;
    }

    /// <summary>
    /// Implicitly converts a tuple of (property name, error message) to a <see cref="RuleMessage"/>.
    /// </summary>
    /// <param name="ruleMessage">The tuple containing the property name and error message.</param>
    public static implicit operator RuleMessage((string name, string errorMessage) ruleMessage)
    {
        return new RuleMessage(ruleMessage.name, ruleMessage.errorMessage);
    }
}

/// <summary>
/// Extension methods for creating rule messages from property names and strings.
/// Provides a fluent API for building validation messages.
/// </summary>
public static class PropertyRuleMessageExtension
{
    /// <summary>
    /// Creates a rule messages collection that clears any existing messages for the specified property.
    /// </summary>
    /// <param name="propertyName">The property name to clear messages for.</param>
    /// <returns>A rule messages collection with a message-less entry for the property.</returns>
    public static IRuleMessages ClearRuleMessageForProperty(this string propertyName)
    {
        return new RuleMessages(new RuleMessage(propertyName));
    }

    /// <summary>
    /// Creates a single rule message for the specified property.
    /// </summary>
    /// <param name="propertyName">The property name to associate the message with.</param>
    /// <param name="message">The validation error message.</param>
    /// <returns>A rule message for the property.</returns>
    public static IRuleMessage RuleMessage(this string propertyName, string message)
    {
        return new RuleMessage(propertyName, message);
    }

    /// <summary>
    /// Creates a rule messages collection containing a single message for the specified property.
    /// </summary>
    /// <param name="propertyName">The property name to associate the message with.</param>
    /// <param name="message">The validation error message.</param>
    /// <returns>A rule messages collection containing the message.</returns>
    public static IRuleMessages RuleMessages(this string propertyName, string message)
    {
        return (propertyName, message).AsRuleMessages();
    }

    /// <summary>
    /// Converts a tuple of (property name, message) to a rule messages collection.
    /// </summary>
    /// <param name="ruleMessage">The tuple containing property name and message.</param>
    /// <returns>A rule messages collection containing the message.</returns>
    public static IRuleMessages AsRuleMessages(this (string propertyName, string message) ruleMessage)
    {
        return new RuleMessages() { new RuleMessage(ruleMessage.propertyName, ruleMessage.message) };
    }

    /// <summary>
    /// Converts an array of tuples to a rule messages collection.
    /// </summary>
    /// <param name="ruleMessage">An array of tuples containing property names and messages.</param>
    /// <returns>A rule messages collection containing all the messages.</returns>
    public static IRuleMessages AsRuleMessages(this (string propertyName, string errorMessage)[] ruleMessage)
    {
        var ruleMessages = new RuleMessages();
        foreach (var rm in ruleMessage)
        {
            ruleMessages.Add(rm.propertyName, rm.errorMessage);
        }
        return ruleMessages;
    }
}

/// <summary>
/// Represents a collection of <see cref="IRuleMessage"/> instances returned by a rule.
/// Extends <see cref="IList{T}"/> to provide collection semantics with additional convenience methods.
/// </summary>
public interface IRuleMessages : IList<IRuleMessage>
{
    /// <summary>
    /// Adds a new rule message with the specified property name and message.
    /// </summary>
    /// <param name="propertyName">The property name to associate the message with.</param>
    /// <param name="message">The validation error message.</param>
    void Add(string propertyName, string message);

    /// <summary>
    /// Gets an empty rule messages collection, indicating validation passed.
    /// </summary>
    public static IRuleMessages None = new RuleMessages();
}

/// <summary>
/// Default implementation of <see cref="IRuleMessages"/> that extends <see cref="List{T}"/>.
/// Provides a mutable collection of rule messages with convenience methods for building validation results.
/// </summary>
/// <remarks>
/// Use <see cref="None"/> to return an empty collection when validation passes.
/// Use the <see cref="If"/> method for conditional message creation.
/// </remarks>
/// <example>
/// <code>
/// protected override IRuleMessages Execute(Person target)
/// {
///     return RuleMessages.If(
///         target.Age &lt; 0,
///         nameof(Person.Age),
///         "Age cannot be negative");
/// }
/// </code>
/// </example>
public class RuleMessages : List<IRuleMessage>, IRuleMessages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuleMessages"/> class with optional initial messages.
    /// </summary>
    /// <param name="ruleMessages">Initial messages to add to the collection.</param>
    public RuleMessages(params RuleMessage[] ruleMessages)
    {
        this.AddRange(ruleMessages);
    }

    /// <summary>
    /// Gets an empty rule messages collection, indicating validation passed.
    /// Use this as a return value when no validation errors occurred.
    /// </summary>
    public static RuleMessages None = new RuleMessages();

    /// <inheritdoc />
    public void Add(string propertyName, string message)
    {
        this.Add(new RuleMessage(propertyName, message));
    }

    /// <summary>
    /// Conditionally creates a rule messages collection with a message if the expression is true.
    /// </summary>
    /// <param name="expr">The condition to evaluate.</param>
    /// <param name="propertyName">The property name to associate the message with.</param>
    /// <param name="message">The validation error message.</param>
    /// <returns>A rule messages collection containing the message if the condition is true, otherwise an empty collection.</returns>
    public static RuleMessages If(bool expr, string propertyName, string message)
    {
        var result = new RuleMessages();

        if (expr)
        {
            result.Add(propertyName, message);
        }
        return result;
    }
}

/// <summary>
/// Provides fluent builder extension methods for constructing <see cref="IRuleMessages"/> collections.
/// Enables chaining of conditional message additions.
/// </summary>
/// <example>
/// <code>
/// protected override IRuleMessages Execute(Person target)
/// {
///     return new RuleMessages()
///         .If(string.IsNullOrEmpty(target.Name), nameof(Person.Name), "Name is required")
///         .If(target.Age &lt; 0, nameof(Person.Age), "Age cannot be negative");
/// }
/// </code>
/// </example>
public static class RuleMessagesBuilder
{
    /// <summary>
    /// Conditionally adds a message to the collection if the expression is true.
    /// </summary>
    /// <param name="ruleMessages">The rule messages collection to add to.</param>
    /// <param name="expr">The condition to evaluate.</param>
    /// <param name="propertyName">The property name to associate the message with.</param>
    /// <param name="message">The validation error message.</param>
    /// <returns>The rule messages collection for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ruleMessages"/> is null.</exception>
    public static IRuleMessages If(this IRuleMessages ruleMessages, bool expr, string propertyName, string message)
    {
        ArgumentNullException.ThrowIfNull(ruleMessages, nameof(ruleMessages));

        if (expr)
        {
            ruleMessages.Add(propertyName, message);
        }

        return ruleMessages;
    }

    /// <summary>
    /// Conditionally adds a message only if no messages have been added yet and the expression is true.
    /// The expression is evaluated lazily, only if the collection is empty.
    /// </summary>
    /// <param name="ruleMessages">The rule messages collection to add to.</param>
    /// <param name="expr">A function that returns the condition to evaluate.</param>
    /// <param name="propertyName">The property name to associate the message with.</param>
    /// <param name="message">The validation error message.</param>
    /// <returns>The rule messages collection for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ruleMessages"/> is null.</exception>
    public static IRuleMessages ElseIf(this IRuleMessages ruleMessages, Func<bool> expr, string propertyName, string message)
    {
        if (ruleMessages.Any())
        {
            return ruleMessages;
        }

        ArgumentNullException.ThrowIfNull(ruleMessages, nameof(ruleMessages));

        if (expr())
        {
            ruleMessages.Add(propertyName, message);
        }

        return ruleMessages;
    }
}


