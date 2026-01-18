using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Neatoo.BaseGenerator.Models;
using System.Text.RegularExpressions;

namespace Neatoo.BaseGenerator.Extractors;

/// <summary>
/// Extracts rule expressions from constructors and validation attributes.
/// </summary>
internal static class RuleExpressionExtractor
{
    /// <summary>
    /// Extracts all rule expressions from a class (constructor invocations + validation attributes).
    /// Returns expressions already sorted and normalized.
    /// </summary>
    public static RuleExpressionInfo ExtractRuleExpressions(ClassDeclarationSyntax classDeclaration)
    {
        var expressions = new List<string>();

        // Collect from constructors
        CollectConstructorRuleExpressions(classDeclaration, expressions);

        // Collect from validation attributes
        CollectAttributeRuleExpressions(classDeclaration, expressions);

        if (expressions.Count == 0)
            return default;

        // Sort alphabetically and deduplicate
        var sortedExpressions = expressions
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        return new RuleExpressionInfo(new EquatableArray<string>(sortedExpressions));
    }

    /// <summary>
    /// Collects rule expressions from constructor invocations (AddRule, AddValidation, AddAction, etc.).
    /// </summary>
    private static void CollectConstructorRuleExpressions(
        ClassDeclarationSyntax classDeclaration,
        List<string> expressions)
    {
        var constructors = classDeclaration.Members.OfType<ConstructorDeclarationSyntax>();

        foreach (var constructor in constructors)
        {
            var invocations = constructor.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                if (IsRuleManagerInvocation(invocation, out var methodName))
                {
                    var sourceExpr = ExtractSourceExpression(invocation, methodName);
                    if (sourceExpr != null)
                    {
                        var normalized = NormalizeSourceExpression(sourceExpr);
                        expressions.Add(normalized);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if an invocation is a RuleManager method call and extracts the method name.
    /// </summary>
    private static bool IsRuleManagerInvocation(InvocationExpressionSyntax invocation, out string methodName)
    {
        methodName = string.Empty;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var objectName = memberAccess.Expression.ToString();
            methodName = memberAccess.Name.Identifier.Text;

            if (objectName == "RuleManager" || objectName == "this.RuleManager")
            {
                return methodName.StartsWith("Add");
            }
        }

        return false;
    }

    /// <summary>
    /// Extracts the source expression from a rule registration invocation.
    /// This mirrors what CallerArgumentExpression would capture at runtime.
    /// </summary>
    private static string? ExtractSourceExpression(InvocationExpressionSyntax invocation, string methodName)
    {
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count == 0)
            return null;

        // For AddRule, the source expression is the first argument (the rule instance)
        if (methodName == "AddRule" && arguments.Count >= 1)
            return arguments[0].Expression.ToString();

        // For AddValidation/AddValidationAsync, the source expression is the func parameter
        if ((methodName == "AddValidation" || methodName == "AddValidationAsync") && arguments.Count >= 1)
            return arguments[0].Expression.ToString();

        // For AddAction/AddActionAsync, the source expression is the func parameter
        if ((methodName == "AddAction" || methodName == "AddActionAsync") && arguments.Count >= 1)
            return arguments[0].Expression.ToString();

        return null;
    }

    /// <summary>
    /// Collects rule expressions from validation attributes on properties.
    /// </summary>
    private static void CollectAttributeRuleExpressions(
        ClassDeclarationSyntax classDeclaration,
        List<string> expressions)
    {
        var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>();

        foreach (var property in properties)
        {
            foreach (var attributeList in property.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var attributeName = attribute.Name.ToString();
                    if (IsValidationAttribute(attributeName))
                    {
                        var propertyName = property.Identifier.Text;
                        var normalizedAttributeName = NormalizeAttributeName(attributeName);
                        // This matches the runtime behavior in AddAttributeRules
                        var sourceExpression = $"{normalizedAttributeName}_{propertyName}";
                        expressions.Add(sourceExpression);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Normalizes an attribute name to match runtime GetType().Name format.
    /// </summary>
    private static string NormalizeAttributeName(string attributeName)
    {
        if (attributeName.EndsWith("Attribute"))
            return attributeName;
        return attributeName + "Attribute";
    }

    /// <summary>
    /// Checks if an attribute name is a validation attribute that generates rules.
    /// </summary>
    private static bool IsValidationAttribute(string attributeName)
    {
        return attributeName == "Required" ||
               attributeName == "RequiredAttribute" ||
               attributeName == "StringLength" ||
               attributeName == "StringLengthAttribute" ||
               attributeName == "Range" ||
               attributeName == "RangeAttribute" ||
               attributeName == "RegularExpression" ||
               attributeName == "RegularExpressionAttribute" ||
               attributeName == "EmailAddress" ||
               attributeName == "EmailAddressAttribute" ||
               attributeName == "Phone" ||
               attributeName == "PhoneAttribute" ||
               attributeName == "CreditCard" ||
               attributeName == "CreditCardAttribute" ||
               attributeName == "Url" ||
               attributeName == "UrlAttribute" ||
               attributeName == "MaxLength" ||
               attributeName == "MaxLengthAttribute" ||
               attributeName == "MinLength" ||
               attributeName == "MinLengthAttribute";
    }

    /// <summary>
    /// Normalizes a source expression by collapsing whitespace.
    /// </summary>
    private static string NormalizeSourceExpression(string sourceExpression)
    {
        return Regex.Replace(sourceExpression, @"\s+", " ").Trim();
    }
}
