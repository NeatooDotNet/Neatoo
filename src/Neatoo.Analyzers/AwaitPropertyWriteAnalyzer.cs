using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Neatoo.Analyzers;

/// <summary>
/// Reports a write to a Neatoo property inside a rule or a verb. The generated setter cannot
/// await, so the write forks the property's rule cascade and returns before the write's
/// consequences land. Domain code awaits the generated <c>Set{Name}</c> instead. Factory operations
/// assign: the object is paused there, so assignment is the baseline and nothing forks.
/// </summary>
/// <remarks>
/// A write is a simple or compound assignment, or an increment or decrement. Its context is a rule
/// (a lambda registered with the RuleManager, or a RuleBase/AsyncRuleBase Execute override), a verb
/// (a public instance method of the entity that is not a factory operation or a framework override),
/// or a non-public helper method that a rule or verb in the same file calls.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AwaitPropertyWriteAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NEATOO011";

    /// <summary>Diagnostic property: "true" when the receiver has a matching Set{Name}.</summary>
    public const string HasSetMethodKey = "HasSetMethod";

    private const string RuleContext = "rule";
    private const string VerbContext = "verb";
    private const string HelperContext = "method a rule or verb calls";

    private static readonly LocalizableString Title = "Await the write in a rule or verb";
    private static readonly LocalizableString MessageFormat =
        "'{0}' is written inside a {1} and the write returns before its consequences land: await Set{0}(...) instead{2}";
    private static readonly LocalizableString Description =
        "A property's setter cannot await its rule cascade, so a rule or verb that assigns runs on before the rules that write triggered have finished. Await the generated Set{Name}(value), which returns the Task of the whole cascade. Factory operations are exempt: the object is paused there and assignment is the baseline.";
    private const string Category = "Usage";

    public static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    private static readonly ImmutableHashSet<string> FactoryOperationAttributes = ImmutableHashSet.Create(
        "CreateAttribute", "FetchAttribute", "InsertAttribute", "UpdateAttribute", "DeleteAttribute", "ExecuteAttribute");

    private static readonly SyntaxKind[] AssignmentKinds =
    [
        SyntaxKind.SimpleAssignmentExpression,
        SyntaxKind.AddAssignmentExpression,
        SyntaxKind.SubtractAssignmentExpression,
        SyntaxKind.MultiplyAssignmentExpression,
        SyntaxKind.DivideAssignmentExpression,
        SyntaxKind.ModuloAssignmentExpression,
        SyntaxKind.AndAssignmentExpression,
        SyntaxKind.OrAssignmentExpression,
        SyntaxKind.ExclusiveOrAssignmentExpression,
        SyntaxKind.LeftShiftAssignmentExpression,
        SyntaxKind.RightShiftAssignmentExpression,
        SyntaxKind.CoalesceAssignmentExpression,
    ];

    private static readonly SyntaxKind[] IncrementKinds =
    [
        SyntaxKind.PreIncrementExpression,
        SyntaxKind.PreDecrementExpression,
        SyntaxKind.PostIncrementExpression,
        SyntaxKind.PostDecrementExpression,
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(c => AnalyzeWrite(c, ((AssignmentExpressionSyntax)c.Node).Left), AssignmentKinds);
        context.RegisterSyntaxNodeAction(c => AnalyzeWrite(c, IncrementOperand(c.Node)), IncrementKinds);
    }

    private static ExpressionSyntax IncrementOperand(SyntaxNode node) => node switch
    {
        PrefixUnaryExpressionSyntax prefix => prefix.Operand,
        PostfixUnaryExpressionSyntax postfix => postfix.Operand,
        _ => throw new InvalidOperationException("Not an increment or decrement"),
    };

    private static void AnalyzeWrite(SyntaxNodeAnalysisContext context, ExpressionSyntax target)
    {
        var write = context.Node;

        // An object initializer's member assignment is construction, not a write to a live object.
        if (write.Parent is InitializerExpressionSyntax)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(target, context.CancellationToken).Symbol is not IPropertySymbol property
            || !IsNeatooProperty(property))
        {
            return;
        }

        var where = WriteContext(write, context.SemanticModel, new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default), context.CancellationToken);
        if (where == null)
        {
            return;
        }

        var receiverType = ReceiverType(target, context.SemanticModel, context.CancellationToken);
        var hasSetMethod = receiverType != null && HasMatchingSetMethod(receiverType, property);
        var reason = hasSetMethod ? string.Empty
            : receiverType?.TypeKind == TypeKind.Interface
                ? $"; declare {receiverType.Name} partial so the generator adds Set{property.Name}"
                : $"; no Set{property.Name} was generated because a member with that name already exists";

        var properties = ImmutableDictionary<string, string?>.Empty.Add(HasSetMethodKey, hasSetMethod ? "true" : "false");
        context.ReportDiagnostic(Diagnostic.Create(Rule, write.GetLocation(), properties, property.Name, where, reason));
    }

    /// <summary>
    /// The rule, verb or helper context the node is in; null when it is in none.
    /// </summary>
    private static string? WriteContext(SyntaxNode node, SemanticModel model, HashSet<IMethodSymbol> visited, CancellationToken token)
    {
        foreach (var ancestor in node.Ancestors())
        {
            switch (ancestor)
            {
                case AnonymousFunctionExpressionSyntax lambda when IsRuleRegistrationArgument(lambda, model, token):
                    // A rule triggered on IsBusy must be synchronous (NEATOO012), so its write cannot be awaited.
                    return TriggersOnIsBusy(lambda, model, token) ? null : RuleContext;

                case MethodDeclarationSyntax method:
                    return model.GetDeclaredSymbol(method, token) is IMethodSymbol symbol
                        ? MethodContext(symbol, method, model, visited, token)
                        : null;

                case ConstructorDeclarationSyntax:
                case AccessorDeclarationSyntax:
                case ArrowExpressionClauseSyntax arrow when arrow.Parent is PropertyDeclarationSyntax:
                case TypeDeclarationSyntax:
                    return null;
            }
        }

        return null;
    }

    private static string? MethodContext(IMethodSymbol method, MethodDeclarationSyntax declaration, SemanticModel model, HashSet<IMethodSymbol> visited, CancellationToken token)
    {
        if (method.IsStatic)
        {
            return null;
        }

        if (method.Name == "Execute" && method.IsOverride && DerivesFromNeatooRule(method.ContainingType))
        {
            // A rule class triggered on IsBusy must be synchronous (NEATOO012), so its write cannot be awaited.
            var ruleClass = declaration.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            return ruleClass != null && AsyncRuleOnIsBusyAnalyzer.RuleClassTriggersOnIsBusy(ruleClass, model, token) ? null : RuleContext;
        }

        if (!IsNeatooClass(method.ContainingType) || IsFactoryOperation(method) || OverridesNeatooMember(method))
        {
            return null;
        }

        if (IsPublicSurface(method))
        {
            return VerbContext;
        }

        // A non-public helper is a rule or verb when one calls it, directly or through another helper.
        if (!visited.Add(method))
        {
            return null;
        }

        var type = declaration.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (type == null)
        {
            return null;
        }

        foreach (var reference in type.DescendantNodes().OfType<SimpleNameSyntax>())
        {
            if (reference.Identifier.Text != method.Name
                || !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(reference, token).Symbol?.OriginalDefinition, method.OriginalDefinition))
            {
                continue;
            }

            // A method group handed to a rule registration is a rule.
            var expression = reference.Parent is MemberAccessExpressionSyntax access && access.Name == reference ? (ExpressionSyntax)access : reference;
            if (expression.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax registration } arguments }
                && IsRuleRegistration(registration, model, token))
            {
                // A method group registered with an IsBusy trigger is a sync IsBusy rule (NEATOO012): exempt.
                if (arguments.DescendantNodes().OfType<LambdaExpressionSyntax>().Any(l => AsyncRuleOnIsBusyAnalyzer.IsIsBusyTrigger(l, model, token)))
                {
                    continue;
                }

                return HelperContext;
            }

            if (WriteContext(reference, model, visited, token) != null)
            {
                return HelperContext;
            }
        }

        return null;
    }

    /// <summary>
    /// The lambda is an argument to a RuleManager registration: AddAction, AddActionAsync, AddValidation...
    /// </summary>
    private static bool IsRuleRegistrationArgument(AnonymousFunctionExpressionSyntax lambda, SemanticModel model, CancellationToken token)
        => lambda.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } }
            && IsRuleRegistration(invocation, model, token);

    /// <summary>
    /// The registration this rule lambda is passed to also passes an IsBusy trigger.
    /// </summary>
    private static bool TriggersOnIsBusy(AnonymousFunctionExpressionSyntax lambda, SemanticModel model, CancellationToken token)
        => lambda.Parent?.Parent is ArgumentListSyntax arguments
            && arguments.DescendantNodes().OfType<LambdaExpressionSyntax>()
                .Any(trigger => trigger != lambda && AsyncRuleOnIsBusyAnalyzer.IsIsBusyTrigger(trigger, model, token));

    private static bool IsRuleRegistration(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken token)
    {
        if (model.GetSymbolInfo(invocation, token).Symbol is not IMethodSymbol method)
        {
            return false;
        }

        return method.Name.StartsWith("Add", StringComparison.Ordinal)
            && method.ContainingType?.ContainingNamespace?.ToDisplayString() == "Neatoo.Rules"
            && method.ContainingType.Name.Contains("RuleManager");
    }

    private static bool IsPublicSurface(IMethodSymbol method)
        => method.MethodKind == MethodKind.ExplicitInterfaceImplementation
            || (method.MethodKind == MethodKind.Ordinary && method.DeclaredAccessibility == Accessibility.Public);

    private static bool IsFactoryOperation(IMethodSymbol method)
        => method.GetAttributes().Any(a =>
            a.AttributeClass != null
            && FactoryOperationAttributes.Contains(a.AttributeClass.Name)
            && a.AttributeClass.ContainingNamespace?.ToDisplayString() == "Neatoo.RemoteFactory");

    /// <summary>
    /// The method overrides a member declared in Neatoo itself, a lifecycle hook such as
    /// FactoryComplete or OnDeserialized: framework plumbing, not a verb.
    /// </summary>
    private static bool OverridesNeatooMember(IMethodSymbol method)
    {
        for (var overridden = method.OverriddenMethod; overridden != null; overridden = overridden.OverriddenMethod)
        {
            if (overridden.ContainingNamespace?.ToDisplayString() == "Neatoo")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A partial property of a Neatoo class, or a settable property of an interface that extends
    /// Neatoo's IValidateBase. Lazy-load properties are exempt: their setter is a load, not a write.
    /// </summary>
    private static bool IsNeatooProperty(IPropertySymbol property)
    {
        if (property.SetMethod == null || IsLazyLoad(property.Type))
        {
            return false;
        }

        var owner = property.ContainingType;
        if (owner.TypeKind == TypeKind.Interface)
        {
            return owner.AllInterfaces.Any(IsNeatooValidateBaseInterface) || IsNeatooValidateBaseInterface(owner);
        }

        var isPartial = property.IsPartialDefinition || property.PartialDefinitionPart != null || property.PartialImplementationPart != null;
        return isPartial && IsNeatooClass(owner);
    }

    private static bool IsLazyLoad(ITypeSymbol type)
        => type is INamedTypeSymbol named
            && named.OriginalDefinition.Name == "EntityLazyLoad"
            && named.ContainingNamespace?.ToDisplayString() == "Neatoo";

    private static bool IsNeatooValidateBaseInterface(INamedTypeSymbol type)
        => type.Name == "IValidateBase" && type.ContainingNamespace?.ToDisplayString() == "Neatoo";

    /// <summary>
    /// The class or a base class is Neatoo's ValidateBase (EntityBase derives from it).
    /// </summary>
    private static bool IsNeatooClass(INamedTypeSymbol? type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (current.Name == "ValidateBase" && current.ContainingNamespace?.ToDisplayString() == "Neatoo")
            {
                return true;
            }
        }

        return false;
    }

    private static bool DerivesFromNeatooRule(INamedTypeSymbol? type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if ((current.Name == "RuleBase" || current.Name == "AsyncRuleBase")
                && current.ContainingNamespace?.ToDisplayString() == "Neatoo.Rules")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The type the property is written through: the member access's receiver, or the containing type.
    /// </summary>
    private static ITypeSymbol? ReceiverType(ExpressionSyntax target, SemanticModel model, CancellationToken token)
    {
        if (target is MemberAccessExpressionSyntax memberAccess)
        {
            return model.GetTypeInfo(memberAccess.Expression, token).Type;
        }

        return model.GetEnclosingSymbol(target.SpanStart, token)?.ContainingType;
    }

    /// <summary>
    /// The receiver (or a type it inherits) has Set{Name} taking one parameter of the property's type.
    /// </summary>
    private static bool HasMatchingSetMethod(ITypeSymbol receiver, IPropertySymbol property)
    {
        var name = $"Set{property.Name}";
        IEnumerable<ITypeSymbol> types = receiver.TypeKind == TypeKind.Interface
            ? new[] { receiver }.Concat(receiver.AllInterfaces)
            : BaseTypes(receiver);

        return types.SelectMany(t => t.GetMembers(name)).OfType<IMethodSymbol>().Any(m =>
            m.Parameters.Length == 1
            && SymbolEqualityComparer.Default.Equals(
                m.Parameters[0].Type.WithNullableAnnotation(NullableAnnotation.None),
                property.Type.WithNullableAnnotation(NullableAnnotation.None)));
    }

    private static IEnumerable<ITypeSymbol> BaseTypes(ITypeSymbol type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            yield return current;
        }
    }
}
