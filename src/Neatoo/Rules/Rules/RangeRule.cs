using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Neatoo.Rules.Rules;

public interface IRangeRule : IRule
{
    string ErrorMessage { get; }
    object Minimum { get; }
    object Maximum { get; }
}

internal class RangeRule<T> : RuleBase<T>, IRangeRule
    where T : class, IValidateBase
{
    public string ErrorMessage { get; }
    public object Minimum { get; }
    public object Maximum { get; }

    private readonly Type _operandType;
    private readonly IComparable _min;
    private readonly IComparable _max;

    public RangeRule(ITriggerProperty triggerProperty, RangeAttribute attribute) : base()
    {
        this.TriggerProperties.Add(triggerProperty);

        _operandType = attribute.OperandType;
        Minimum = attribute.Minimum;
        Maximum = attribute.Maximum;

        // Convert min/max to IComparable for comparison
        _min = ConvertToComparable(attribute.Minimum, _operandType);
        _max = ConvertToComparable(attribute.Maximum, _operandType);

        this.ErrorMessage = attribute.ErrorMessage
            ?? $"{triggerProperty.PropertyName} must be between {Minimum} and {Maximum}.";
    }

    protected override IRuleMessages Execute(T target)
    {
        var value = ((ITriggerProperty<T>)this.TriggerProperties[0]).GetValue(target);

        // Null values pass - use Required for null check
        if (value == null)
        {
            return RuleMessages.None;
        }

        // Convert value to the operand type for comparison
        IComparable? comparableValue;
        try
        {
            comparableValue = ConvertToComparable(value, _operandType);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            // If conversion fails, treat as validation failure
            return (this.TriggerProperties.Single().PropertyName, this.ErrorMessage).AsRuleMessages();
        }

        if (comparableValue == null)
        {
            return RuleMessages.None;
        }

        // Check if value is within range
        var belowMin = comparableValue.CompareTo(_min) < 0;
        var aboveMax = comparableValue.CompareTo(_max) > 0;

        if (belowMin || aboveMax)
        {
            return (this.TriggerProperties.Single().PropertyName, this.ErrorMessage).AsRuleMessages();
        }

        return RuleMessages.None;
    }

    private static IComparable ConvertToComparable(object value, Type targetType)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        // If already the target type, just cast
        if (value.GetType() == targetType)
        {
            return (IComparable)value;
        }

        // Handle string conversion for types like decimal, DateTime
        if (value is string stringValue)
        {
            if (targetType == typeof(DateTime))
            {
                return DateTime.Parse(stringValue, CultureInfo.InvariantCulture);
            }
            if (targetType == typeof(DateTimeOffset))
            {
                return DateTimeOffset.Parse(stringValue, CultureInfo.InvariantCulture);
            }
            if (targetType == typeof(decimal))
            {
                return decimal.Parse(stringValue, CultureInfo.InvariantCulture);
            }
            if (targetType == typeof(double))
            {
                return double.Parse(stringValue, CultureInfo.InvariantCulture);
            }
            if (targetType == typeof(int))
            {
                return int.Parse(stringValue, CultureInfo.InvariantCulture);
            }
            if (targetType == typeof(long))
            {
                return long.Parse(stringValue, CultureInfo.InvariantCulture);
            }
        }

        // Use Convert.ChangeType for numeric conversions
        return (IComparable)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }
}
