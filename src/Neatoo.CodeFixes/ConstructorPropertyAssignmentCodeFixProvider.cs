using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Neatoo.Analyzers;
using System.Collections.Immutable;
using System.Composition;

namespace Neatoo.CodeFixes;

/// <summary>
/// Code fix provider that transforms property assignments in constructors
/// to use LoadValue() instead of direct assignment.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstructorPropertyAssignmentCodeFixProvider))]
[Shared]
public class ConstructorPropertyAssignmentCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(ConstructorPropertyAssignmentAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the assignment expression
        var assignment = root.FindNode(diagnosticSpan)
            .AncestorsAndSelf()
            .OfType<AssignmentExpressionSyntax>()
            .FirstOrDefault();

        if (assignment == null)
        {
            return;
        }

        // Extract property name
        var propertyName = GetPropertyName(assignment);
        if (propertyName == null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Use {propertyName}Property.LoadValue()",
                createChangedDocument: c => UseLoadValueAsync(context.Document, assignment, propertyName, c),
                equivalenceKey: nameof(ConstructorPropertyAssignmentCodeFixProvider)),
            diagnostic);
    }

    private static string? GetPropertyName(AssignmentExpressionSyntax assignment)
    {
        // Handle: Name = value
        if (assignment.Left is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.Text;
        }

        // Handle: this.Name = value
        if (assignment.Left is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression is ThisExpressionSyntax &&
            memberAccess.Name is IdentifierNameSyntax memberIdentifier)
        {
            return memberIdentifier.Identifier.Text;
        }

        return null;
    }

    private static async Task<Document> UseLoadValueAsync(
        Document document,
        AssignmentExpressionSyntax assignment,
        string propertyName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        // Get the value expression (right side of assignment)
        var valueExpression = assignment.Right;

        // Create: PropertyNameProperty.LoadValue(value)
        var loadValueInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName($"{propertyName}Property"),
                SyntaxFactory.IdentifierName("LoadValue")),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(valueExpression))));

        // Find the containing statement (the expression statement)
        var containingStatement = assignment.Parent;

        SyntaxNode newRoot;

        if (containingStatement is ExpressionStatementSyntax expressionStatement)
        {
            // Replace the entire expression statement
            var newStatement = SyntaxFactory.ExpressionStatement(loadValueInvocation)
                .WithLeadingTrivia(expressionStatement.GetLeadingTrivia())
                .WithTrailingTrivia(expressionStatement.GetTrailingTrivia());

            newRoot = root.ReplaceNode(expressionStatement, newStatement);
        }
        else
        {
            // Fallback: just replace the assignment expression
            newRoot = root.ReplaceNode(assignment, loadValueInvocation);
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
