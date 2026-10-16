using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Neatoo.Analyzers;

/// <summary>
/// Refuses an async rule triggered on IsBusy. Rules may trigger on meta state, but running an async
/// rule makes the object busy and finishing it makes the object idle, so a rule triggered on IsBusy
/// re-triggers itself on every run. It is the one meta-state loop that exists by construction; every
/// other loop is the rule author's to avoid and test.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncRuleOnIsBusyAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NEATOO012";

    private static readonly LocalizableString Title = "An async rule cannot trigger on IsBusy or IsSavable";
    private static readonly LocalizableString MessageFormat =
        "This async rule is triggered on {0}, and running it makes the object busy and finishing it makes it idle, so it re-triggers itself without end: make the rule synchronous or trigger it on a property";
    private static readonly LocalizableString Description =
        "A rule may trigger on IsValid, IsModified or IsBusy. An async rule's own run flips IsBusy, and IsSavable with it, so an async rule triggered on either runs forever. A synchronous rule triggered on them does not flip IsBusy and settles.";
    private const string Category = "Usage";

    public static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeLambda, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression);
    }

    private static void AnalyzeLambda(SyntaxNodeAnalysisContext context)
    {
        var lambda = (LambdaExpressionSyntax)context.Node;
        if (!IsIsBusyTrigger(lambda, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        switch (TriggerOwner(lambda))
        {
            case InvocationExpressionSyntax invocation when IsAsyncRegistration(invocation, context.SemanticModel, context.CancellationToken):
            case InvocationExpressionSyntax addTriggers when IsAddTriggerPropertiesOnAsyncRule(addTriggers, context.SemanticModel, context.CancellationToken):
            case ConstructorInitializerSyntax initializer when IsAsyncRuleConstructor(initializer, context.SemanticModel, context.CancellationToken):
                context.ReportDiagnostic(Diagnostic.Create(Rule, lambda.GetLocation(), TriggerName(lambda)));
                break;
        }
    }

    private static string TriggerName(LambdaExpressionSyntax lambda)
        => (lambda.ExpressionBody as MemberAccessExpressionSyntax)?.Name.Identifier.Text ?? "IsBusy";

    /// <summary>
    /// The lambda is <c>t => t.IsBusy</c> or <c>t => t.IsSavable</c>: Neatoo meta properties that an
    /// async rule's own run flips.
    /// </summary>
    public static bool IsIsBusyTrigger(LambdaExpressionSyntax lambda, SemanticModel model, CancellationToken token)
    {
        var body = lambda.ExpressionBody;
        while (body is CastExpressionSyntax cast)
        {
            body = cast.Expression;
        }

        if (body is not MemberAccessExpressionSyntax { Name.Identifier.Text: "IsBusy" or "IsSavable" } access)
        {
            return false;
        }

        return model.GetSymbolInfo(access, token).Symbol is IPropertySymbol property
            && property.ContainingType?.ContainingNamespace?.ToDisplayString() == "Neatoo";
    }

    /// <summary>
    /// The invocation or base-constructor call the trigger lambda is passed to, directly or inside an array.
    /// </summary>
    public static SyntaxNode? TriggerOwner(LambdaExpressionSyntax lambda)
    {
        for (SyntaxNode? node = lambda.Parent; node != null; node = node.Parent)
        {
            switch (node)
            {
                case ArgumentSyntax:
                case ArgumentListSyntax:
                case InitializerExpressionSyntax:
                case ArrayCreationExpressionSyntax:
                case ImplicitArrayCreationExpressionSyntax:
                case CollectionExpressionSyntax:
                case ExpressionElementSyntax:
                    continue;
                case InvocationExpressionSyntax:
                case ConstructorInitializerSyntax:
                    return node;
                default:
                    return null;
            }
        }

        return null;
    }

    private static bool IsAsyncRegistration(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken token)
        => model.GetSymbolInfo(invocation, token).Symbol is IMethodSymbol method
            && method.Name.StartsWith("Add", StringComparison.Ordinal)
            && method.Name.EndsWith("Async", StringComparison.Ordinal)
            && method.ContainingType?.ContainingNamespace?.ToDisplayString() == "Neatoo.Rules";

    private static bool IsAddTriggerPropertiesOnAsyncRule(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken token)
        => model.GetSymbolInfo(invocation, token).Symbol is IMethodSymbol { Name: "AddTriggerProperties" }
            && model.GetEnclosingSymbol(invocation.SpanStart, token)?.ContainingType is INamedTypeSymbol type
            && DerivesFromAsyncRule(type);

    private static bool IsAsyncRuleConstructor(ConstructorInitializerSyntax initializer, SemanticModel model, CancellationToken token)
        => initializer.IsKind(SyntaxKind.BaseConstructorInitializer)
            && model.GetEnclosingSymbol(initializer.SpanStart, token)?.ContainingType is INamedTypeSymbol type
            && DerivesFromAsyncRule(type);

    /// <summary>
    /// The class is an async rule: it derives from AsyncRuleBase without passing through RuleBase,
    /// which derives from AsyncRuleBase but runs its Execute synchronously.
    /// </summary>
    private static bool DerivesFromAsyncRule(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (current.ContainingNamespace?.ToDisplayString() != "Neatoo.Rules")
            {
                continue;
            }

            if (current.Name == "RuleBase")
            {
                return false;
            }

            if (current.Name == "AsyncRuleBase")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The rule class names IsBusy among its triggers, in its base-constructor call or through AddTriggerProperties.
    /// </summary>
    public static bool RuleClassTriggersOnIsBusy(TypeDeclarationSyntax ruleClass, SemanticModel model, CancellationToken token)
        => ruleClass.DescendantNodes().OfType<LambdaExpressionSyntax>().Any(lambda =>
            IsIsBusyTrigger(lambda, model, token)
            && TriggerOwner(lambda) switch
            {
                ConstructorInitializerSyntax initializer => initializer.IsKind(SyntaxKind.BaseConstructorInitializer),
                InvocationExpressionSyntax invocation => model.GetSymbolInfo(invocation, token).Symbol is IMethodSymbol { Name: "AddTriggerProperties" },
                _ => false,
            });
}
