using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Neatoo.Analyzers;

/// <summary>
/// Analyzer that detects simple assignments to partial properties inside constructors
/// of Neatoo classes. These assignments should use LoadValue() instead to avoid
/// unintended modification tracking.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConstructorPropertyAssignmentAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NEATOO010";

    private static readonly LocalizableString Title = "Use LoadValue() in constructors";
    private static readonly LocalizableString MessageFormat = "Property '{0}' should be set using {0}Property.LoadValue() in constructors to avoid unintended modification tracking";
    private static readonly LocalizableString Description = "Assignments to partial properties in constructors are tracked as modifications because constructors run outside of the factory pause mechanism. Use LoadValue() instead.";
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

        // Register for constructor declarations
        context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);
    }

    private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
    {
        var constructor = (ConstructorDeclarationSyntax)context.Node;

        // Get containing class
        if (constructor.Parent is not ClassDeclarationSyntax classDeclaration)
        {
            return;
        }

        // Check if class inherits from Neatoo base class
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
        if (classSymbol == null || !ClassOrBaseClassIsNeatooBaseClass(classSymbol))
        {
            return;
        }

        // Get all partial properties in this class
        var partialPropertyNames = GetPartialPropertyNames(classDeclaration);
        if (partialPropertyNames.Count == 0)
        {
            return;
        }

        // Walk constructor body looking for simple assignments
        if (constructor.Body == null)
        {
            return;
        }

        var assignments = constructor.Body.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Where(a => a.IsKind(SyntaxKind.SimpleAssignmentExpression));

        foreach (var assignment in assignments)
        {
            // Check if left side is a simple identifier (property name)
            if (assignment.Left is IdentifierNameSyntax identifier)
            {
                var propertyName = identifier.Identifier.Text;
                if (partialPropertyNames.Contains(propertyName))
                {
                    // Report diagnostic
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        assignment.GetLocation(),
                        propertyName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
            // Also check for this.Property = value
            else if (assignment.Left is MemberAccessExpressionSyntax memberAccess &&
                     memberAccess.Expression is ThisExpressionSyntax &&
                     memberAccess.Name is IdentifierNameSyntax memberIdentifier)
            {
                var propertyName = memberIdentifier.Identifier.Text;
                if (partialPropertyNames.Contains(propertyName))
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        assignment.GetLocation(),
                        propertyName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    /// <summary>
    /// Gets the names of all partial properties declared in the class.
    /// </summary>
    private static HashSet<string> GetPartialPropertyNames(ClassDeclarationSyntax classDeclaration)
    {
        var names = new HashSet<string>();

        var properties = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(p => p.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));

        foreach (var property in properties)
        {
            names.Add(property.Identifier.Text);
        }

        return names;
    }

    /// <summary>
    /// Checks if a class or any of its base classes is a Neatoo base class
    /// (ValidateBase or EntityBase in the Neatoo namespace).
    /// This is the same logic used by BaseGenerator.
    /// </summary>
    private static bool ClassOrBaseClassIsNeatooBaseClass(INamedTypeSymbol namedTypeSymbol)
    {
        if (namedTypeSymbol.Name == "ValidateBase" && namedTypeSymbol.ContainingNamespace?.Name == "Neatoo")
        {
            return true;
        }
        if (namedTypeSymbol.BaseType != null)
        {
            return ClassOrBaseClassIsNeatooBaseClass(namedTypeSymbol.BaseType);
        }
        return false;
    }
}
