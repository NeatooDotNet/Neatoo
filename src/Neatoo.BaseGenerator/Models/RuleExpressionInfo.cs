namespace Neatoo.BaseGenerator.Models;

/// <summary>
/// Collection of rule expressions for GetRuleId generation.
/// Expressions are already normalized and sorted alphabetically.
/// </summary>
internal readonly record struct RuleExpressionInfo(
    EquatableArray<string> SortedExpressions
) : IEquatable<RuleExpressionInfo>;
