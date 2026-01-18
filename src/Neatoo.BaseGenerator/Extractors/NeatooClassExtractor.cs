using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Neatoo.BaseGenerator.Models;

namespace Neatoo.BaseGenerator.Extractors;

/// <summary>
/// Main extractor that orchestrates data extraction from SemanticModel.
/// Called during the transform phase of the incremental generator.
/// Returns immutable NeatooClassInfo with NO SemanticModel reference.
/// </summary>
internal static class NeatooClassExtractor
{
    /// <summary>
    /// Extract all data needed for code generation from the class symbol and syntax.
    /// This is the ONLY place SemanticModel should be accessed.
    /// </summary>
    public static NeatooClassInfo Extract(
        ClassDeclarationSyntax classDeclaration,
        INamedTypeSymbol classSymbol,
        SemanticModel semanticModel,
        bool isMinimalGeneration)
    {
        try
        {
            var className = classSymbol.Name;
            var namespaceName = GetNamespace(classDeclaration) ?? "MissingNamespace";
            var classDeclarationText = GetClassDeclarationText(classDeclaration);

            // Extract type information
            var typeParameters = ExtractTypeParameters(classSymbol);
            var neatooBaseTypeArg = GetNeatooBaseTypeParameter(classSymbol);
            var isDirectlyInheriting = IsDirectlyInheritingNeatooBase(classSymbol);
            var needsCast = NeedsCastToTypeParameter(classSymbol, neatooBaseTypeArg);

            // Extract interface info
            var hasPartialInterface = PropertyExtractor.HasPartialInterface(classSymbol);

            // Extract properties
            var properties = PropertyExtractor.ExtractProperties(classDeclaration, classSymbol);

            // Extract mapper methods (only for full generation)
            var mapperMethods = isMinimalGeneration
                ? EquatableArray<MapperMethodInfo>.Empty
                : MapperMethodExtractor.ExtractMapperMethods(classDeclaration, classSymbol, semanticModel);

            // Extract rule expressions (only for full generation)
            var ruleExpressions = isMinimalGeneration
                ? default
                : RuleExpressionExtractor.ExtractRuleExpressions(classDeclaration);

            // Extract using directives
            var usingDirectives = isMinimalGeneration
                ? UsingDirectivesExtractor.ExtractMinimalUsings()
                : UsingDirectivesExtractor.ExtractUsings(classDeclaration, classSymbol, semanticModel, namespaceName);

            return new NeatooClassInfo(
                ClassName: className,
                Namespace: namespaceName,
                ClassDeclarationText: classDeclarationText,
                TypeParameters: typeParameters,
                NeatooBaseTypeArgument: neatooBaseTypeArg,
                IsDirectlyInheritingNeatooBase: isDirectlyInheriting,
                NeedsCastToTypeParameter: needsCast,
                HasPartialInterface: hasPartialInterface,
                Properties: properties,
                RuleExpressions: ruleExpressions,
                MapperMethods: mapperMethods,
                UsingDirectives: usingDirectives,
                IsMinimalGeneration: isMinimalGeneration,
                ErrorMessage: null,
                StackTrace: null
            );
        }
        catch (Exception ex)
        {
            return NeatooClassInfo.Error(
                classDeclaration.Identifier.Text,
                ex.Message,
#if DEBUG
                ex.StackTrace
#else
                null
#endif
            );
        }
    }

    /// <summary>
    /// Finds the namespace for a class declaration.
    /// </summary>
    private static string? GetNamespace(SyntaxNode syntaxNode)
    {
        if (syntaxNode.Parent is NamespaceDeclarationSyntax namespaceDeclaration)
            return namespaceDeclaration.Name.ToString();

        if (syntaxNode.Parent is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
            return fileScopedNamespace.Name.ToString();

        if (syntaxNode.Parent != null)
            return GetNamespace(syntaxNode.Parent);

        return null;
    }

    /// <summary>
    /// Gets the class declaration text for the generated partial class.
    /// </summary>
    private static string GetClassDeclarationText(ClassDeclarationSyntax classDeclaration)
    {
        var fullText = classDeclaration.ToFullString();
        var start = classDeclaration.Modifiers.FullSpan.Start - classDeclaration.FullSpan.Start;

        if (classDeclaration.TypeParameterList != null)
        {
            var end = classDeclaration.TypeParameterList.FullSpan.End - classDeclaration.FullSpan.Start;
            return fullText.Substring(start, end - start);
        }

        var identifierEnd = classDeclaration.Identifier.FullSpan.End - classDeclaration.FullSpan.Start;
        return fullText.Substring(start, identifierEnd - start);
    }

    /// <summary>
    /// Extracts type parameters from the class symbol.
    /// </summary>
    private static EquatableArray<string> ExtractTypeParameters(INamedTypeSymbol classSymbol)
    {
        if (classSymbol.TypeParameters.Length == 0)
            return EquatableArray<string>.Empty;

        var parameters = classSymbol.TypeParameters.Select(tp => tp.Name).ToList();
        return new EquatableArray<string>(parameters);
    }

    /// <summary>
    /// Gets the type parameter passed to the Neatoo base class (ValidateBase&lt;T&gt; or EntityBase&lt;T&gt;).
    /// </summary>
    private static string? GetNeatooBaseTypeParameter(INamedTypeSymbol classSymbol)
    {
        var currentType = classSymbol;
        while (currentType != null)
        {
            if (currentType.BaseType != null)
            {
                var baseTypeName = currentType.BaseType.Name;
                if ((baseTypeName == "ValidateBase" || baseTypeName == "EntityBase") &&
                    currentType.BaseType.ContainingNamespace?.Name == "Neatoo")
                {
                    if (currentType.BaseType.TypeArguments.Length > 0)
                    {
                        return currentType.BaseType.TypeArguments[0].ToDisplayString();
                    }
                }
            }
            currentType = currentType.BaseType;
        }
        return null;
    }

    /// <summary>
    /// Checks if the class directly inherits from ValidateBase&lt;T&gt; or EntityBase&lt;T&gt;.
    /// </summary>
    private static bool IsDirectlyInheritingNeatooBase(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;
        if (baseType == null)
            return false;

        var baseTypeName = baseType.Name;
        var baseNamespace = baseType.ContainingNamespace?.ToDisplayString();

        return (baseTypeName == "ValidateBase" || baseTypeName == "EntityBase") &&
               baseNamespace == "Neatoo";
    }

    /// <summary>
    /// Determines if the generated code needs to cast 'this' to the type parameter.
    /// </summary>
    private static bool NeedsCastToTypeParameter(INamedTypeSymbol classSymbol, string? typeParameter)
    {
        if (typeParameter == null)
            return false;

        if (classSymbol.TypeParameters.Length > 0)
        {
            return classSymbol.TypeParameters.Any(tp => tp.Name == typeParameter);
        }
        return false;
    }

    /// <summary>
    /// Checks if the class or any base class is a Neatoo base class.
    /// </summary>
    public static bool ClassOrBaseClassIsNeatooBaseClass(INamedTypeSymbol namedTypeSymbol)
    {
        if (namedTypeSymbol.Name == "ValidateBase" && namedTypeSymbol.ContainingNamespace.Name == "Neatoo")
            return true;

        if (namedTypeSymbol.BaseType != null)
            return ClassOrBaseClassIsNeatooBaseClass(namedTypeSymbol.BaseType);

        return false;
    }

    /// <summary>
    /// Checks if the class has the [Factory] attribute.
    /// </summary>
    public static bool HasFactoryAttribute(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == "Neatoo.RemoteFactory.FactoryAttribute");
    }
}
