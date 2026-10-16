using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Neatoo.Analyzers;
using System.Collections.Immutable;
using System.Composition;

namespace Neatoo.CodeFixes;

/// <summary>
/// Rewrites a write to a Neatoo property inside a rule or verb as <c>await x.Set{Name}(value)</c>:
/// <c>X = v</c> becomes <c>Set{Name}(v)</c>, <c>X += v</c> becomes <c>Set{Name}(X + v)</c>, and
/// <c>X++</c> becomes <c>Set{Name}(X + 1)</c>. Offered only where the result compiles: the receiver
/// has the matching Set{Name}, the write is a statement or a lambda's body, and the enclosing function
/// is async already or is an AddAction lambda, which becomes AddActionAsync. A sync method is left to
/// its author, because making it async changes its signature.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AwaitPropertyWriteCodeFixProvider))]
[Shared]
public class AwaitPropertyWriteCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(AwaitPropertyWriteAnalyzer.DiagnosticId);

    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        if (!diagnostic.Properties.TryGetValue(AwaitPropertyWriteAnalyzer.HasSetMethodKey, out var hasSetMethod) || hasSetMethod != "true")
        {
            return;
        }

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var write = root?.FindNode(diagnostic.Location.SourceSpan)
            .AncestorsAndSelf()
            .FirstOrDefault(n => n is AssignmentExpressionSyntax || IsIncrement(n)) as ExpressionSyntax;
        if (write == null)
        {
            return;
        }

        var target = Target(write);
        var propertyName = PropertyName(target);
        if (propertyName == null || !IsStatementOrLambdaBody(write))
        {
            return;
        }

        var function = write.Ancestors().FirstOrDefault(n =>
            n is AnonymousFunctionExpressionSyntax or MethodDeclarationSyntax or LocalFunctionStatementSyntax);

        var addActionName = function is AnonymousFunctionExpressionSyntax lambda ? AddActionName(lambda) : null;
        if (!IsAsync(function) && addActionName == null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Await Set{propertyName}(...)",
                createChangedDocument: c => AwaitWriteAsync(context.Document, write, target, propertyName, function, addActionName, c),
                equivalenceKey: nameof(AwaitPropertyWriteCodeFixProvider)),
            diagnostic);
    }

    private static bool IsIncrement(SyntaxNode node)
        => node.IsKind(SyntaxKind.PreIncrementExpression) || node.IsKind(SyntaxKind.PreDecrementExpression)
            || node.IsKind(SyntaxKind.PostIncrementExpression) || node.IsKind(SyntaxKind.PostDecrementExpression);

    private static ExpressionSyntax Target(ExpressionSyntax write) => write switch
    {
        AssignmentExpressionSyntax assignment => assignment.Left,
        PrefixUnaryExpressionSyntax prefix => prefix.Operand,
        PostfixUnaryExpressionSyntax postfix => postfix.Operand,
        _ => write,
    };

    private static string? PropertyName(ExpressionSyntax target) => target switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        MemberAccessExpressionSyntax { Name: IdentifierNameSyntax name } => name.Identifier.Text,
        _ => null,
    };

    private static bool IsStatementOrLambdaBody(ExpressionSyntax write)
        => write.Parent is ExpressionStatementSyntax
            || (write.Parent is AnonymousFunctionExpressionSyntax lambda && lambda.ExpressionBody == write);

    private static bool IsAsync(SyntaxNode? function) => function switch
    {
        AnonymousFunctionExpressionSyntax lambda => lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword),
        MethodDeclarationSyntax method => method.Modifiers.Any(SyntaxKind.AsyncKeyword),
        LocalFunctionStatementSyntax local => local.Modifiers.Any(SyntaxKind.AsyncKeyword),
        _ => false,
    };

    /// <summary>
    /// The simple name node of the AddAction call the lambda is an argument to, or null.
    /// </summary>
    private static SimpleNameSyntax? AddActionName(AnonymousFunctionExpressionSyntax lambda)
    {
        if (lambda.Parent is not ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } })
        {
            return null;
        }

        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            SimpleNameSyntax simple => simple,
            _ => null,
        };

        return name?.Identifier.Text == "AddAction" ? name : null;
    }

    /// <summary>
    /// The value the write leaves in the property: v for X = v, X op v for a compound write, X ± 1 for ++/--.
    /// </summary>
    private static ExpressionSyntax NewValue(ExpressionSyntax write, ExpressionSyntax target)
    {
        var current = target.WithoutTrivia();
        switch (write)
        {
            case AssignmentExpressionSyntax assignment when assignment.IsKind(SyntaxKind.SimpleAssignmentExpression):
                return assignment.Right.WithoutTrivia();

            case AssignmentExpressionSyntax assignment:
                return SyntaxFactory.BinaryExpression(BinaryKind(assignment.Kind()), current, Operand(assignment.Right.WithoutTrivia()));

            default:
                var increment = write.IsKind(SyntaxKind.PreIncrementExpression) || write.IsKind(SyntaxKind.PostIncrementExpression);
                return SyntaxFactory.BinaryExpression(
                    increment ? SyntaxKind.AddExpression : SyntaxKind.SubtractExpression,
                    current,
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
        }
    }

    private static SyntaxKind BinaryKind(SyntaxKind assignmentKind) => assignmentKind switch
    {
        SyntaxKind.AddAssignmentExpression => SyntaxKind.AddExpression,
        SyntaxKind.SubtractAssignmentExpression => SyntaxKind.SubtractExpression,
        SyntaxKind.MultiplyAssignmentExpression => SyntaxKind.MultiplyExpression,
        SyntaxKind.DivideAssignmentExpression => SyntaxKind.DivideExpression,
        SyntaxKind.ModuloAssignmentExpression => SyntaxKind.ModuloExpression,
        SyntaxKind.AndAssignmentExpression => SyntaxKind.BitwiseAndExpression,
        SyntaxKind.OrAssignmentExpression => SyntaxKind.BitwiseOrExpression,
        SyntaxKind.ExclusiveOrAssignmentExpression => SyntaxKind.ExclusiveOrExpression,
        SyntaxKind.LeftShiftAssignmentExpression => SyntaxKind.LeftShiftExpression,
        SyntaxKind.RightShiftAssignmentExpression => SyntaxKind.RightShiftExpression,
        SyntaxKind.CoalesceAssignmentExpression => SyntaxKind.CoalesceExpression,
        _ => throw new InvalidOperationException($"Unexpected assignment kind {assignmentKind}"),
    };

    /// <summary>
    /// The right-hand side as a binary operand: parenthesized unless it is already atomic.
    /// </summary>
    private static ExpressionSyntax Operand(ExpressionSyntax right)
        => right is IdentifierNameSyntax or LiteralExpressionSyntax or MemberAccessExpressionSyntax
            or InvocationExpressionSyntax or ParenthesizedExpressionSyntax or InterpolatedStringExpressionSyntax
            or ElementAccessExpressionSyntax
            ? right
            : SyntaxFactory.ParenthesizedExpression(right);

    private static async Task<Document> AwaitWriteAsync(
        Document document,
        ExpressionSyntax write,
        ExpressionSyntax target,
        string propertyName,
        SyntaxNode? function,
        SimpleNameSyntax? addActionName,
        CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        ExpressionSyntax setter = target switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.WithoutTrivia().WithName(SyntaxFactory.IdentifierName($"Set{propertyName}")),
            _ => SyntaxFactory.IdentifierName($"Set{propertyName}"),
        };

        var awaitWrite = SyntaxFactory.AwaitExpression(
            SyntaxFactory.InvocationExpression(
                setter,
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(NewValue(write, target))))))
            .WithTriviaFrom(write);

        editor.ReplaceNode(write, awaitWrite);

        if (function is AnonymousFunctionExpressionSyntax lambda && !IsAsync(lambda))
        {
            editor.ReplaceNode(lambda, (current, _) =>
            {
                var currentLambda = (AnonymousFunctionExpressionSyntax)current;
                var leading = currentLambda.GetLeadingTrivia();
                var asyncToken = SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space);
                return currentLambda.WithoutLeadingTrivia().WithAsyncKeyword(asyncToken).WithLeadingTrivia(leading);
            });
        }

        if (addActionName != null)
        {
            editor.ReplaceNode(addActionName, SyntaxFactory.IdentifierName("AddActionAsync").WithTriviaFrom(addActionName));
        }

        return editor.GetChangedDocument();
    }
}
