using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Neatoo.BaseGenerator.Models;

namespace Neatoo.BaseGenerator.Extractors;

/// <summary>
/// Extracts using directives from the class and its base classes.
/// </summary>
internal static class UsingDirectivesExtractor
{
    /// <summary>
    /// Extracts all using directives needed for the generated code.
    /// </summary>
    public static EquatableArray<string> ExtractUsings(
        ClassDeclarationSyntax classDeclaration,
        INamedTypeSymbol classSymbol,
        SemanticModel semanticModel,
        string namespaceName)
    {
        var usingDirectives = new List<string>
        {
            "using Neatoo;",
            "using Microsoft.Extensions.DependencyInjection;"
        };

        // Add parent class static using if nested
        var parentClassUsing = GetParentClassStaticUsing(classDeclaration, namespaceName);
        if (!string.IsNullOrEmpty(parentClassUsing))
        {
            usingDirectives.Add(parentClassUsing);
        }

        // Extract usings from this class and base classes
        var recurseClassDeclaration = classDeclaration;
        while (recurseClassDeclaration != null)
        {
            var compilationUnit = (CompilationUnitSyntax)recurseClassDeclaration.SyntaxTree.GetRoot();
            foreach (var usingDirective in compilationUnit.Usings)
            {
                var usingText = usingDirective.ToString();
                if (!usingDirectives.Contains(usingText))
                {
                    usingDirectives.Add(usingText);
                }
            }

            recurseClassDeclaration = GetBaseClassDeclarationSyntax(semanticModel, recurseClassDeclaration);
        }

        return new EquatableArray<string>(usingDirectives);
    }

    /// <summary>
    /// Extracts minimal using directives for minimal generation mode.
    /// </summary>
    public static EquatableArray<string> ExtractMinimalUsings()
    {
        return new EquatableArray<string>(new[] { "using Neatoo;" });
    }

    /// <summary>
    /// Gets the static using directive for nested classes.
    /// </summary>
    private static string? GetParentClassStaticUsing(ClassDeclarationSyntax classDeclaration, string namespaceName)
    {
        var parentClass = classDeclaration.Parent as ClassDeclarationSyntax;
        if (parentClass == null)
            return null;

        var parentPath = "";
        while (parentClass != null)
        {
            parentPath = $"{parentClass.Identifier.Text}.{parentPath}";
            parentClass = parentClass.Parent as ClassDeclarationSyntax;
        }

        return $"using static {namespaceName}.{parentPath.TrimEnd('.')};";
    }

    /// <summary>
    /// Gets the base class declaration syntax for walking up the inheritance chain.
    /// </summary>
    private static ClassDeclarationSyntax? GetBaseClassDeclarationSyntax(
        SemanticModel semanticModel,
        ClassDeclarationSyntax classDeclaration)
    {
        try
        {
            var correctSemanticModel = semanticModel.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = correctSemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

            if (classSymbol?.BaseType == null)
                return null;

            var baseTypeSyntaxReference = classSymbol.BaseType.DeclaringSyntaxReferences.FirstOrDefault();
            return baseTypeSyntaxReference?.GetSyntax() as ClassDeclarationSyntax;
        }
        catch
        {
            return null;
        }
    }
}
