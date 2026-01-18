using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Neatoo.BaseGenerator.Diagnostics;
using Neatoo.BaseGenerator.Extractors;
using Neatoo.BaseGenerator.Generators;
using Neatoo.BaseGenerator.Models;

namespace Neatoo.BaseGenerator;

/// <summary>
/// Incremental source generator for Neatoo domain model classes.
/// Generates property backing fields, property implementations, MapModifiedTo methods,
/// GetRuleId overrides, and InitializePropertyBackingFields overrides.
/// </summary>
/// <remarks>
/// This generator follows incremental generator best practices:
/// - Transform phase extracts all data into immutable, equatable records
/// - Execute phase uses only cached data (no SemanticModel access)
/// - Proper caching prevents unnecessary regeneration
/// </remarks>
[Generator(LanguageNames.CSharp)]
public class PartialBaseGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Pipeline 1: Classes with [Factory] attribute - full generation
        var factoryClasses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Neatoo.RemoteFactory.FactoryAttribute",
                predicate: static (s, _) => IsPartialClass(s),
                transform: static (ctx, _) => TransformFactoryClass(ctx))
            .Where(static info => info.IsSuccess);

        context.RegisterSourceOutput(factoryClasses,
            static (ctx, source) => Execute(ctx, source));
    }

    /// <summary>
    /// Checks if a syntax node is a partial class declaration.
    /// </summary>
    private static bool IsPartialClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDeclaration &&
               classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }

    /// <summary>
    /// Transform phase for classes with [Factory] attribute.
    /// Extracts all data needed for full code generation.
    /// </summary>
    private static NeatooClassInfo TransformFactoryClass(GeneratorAttributeSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;

        try
        {
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
            if (classSymbol == null)
                return NeatooClassInfo.Empty;

            if (!NeatooClassExtractor.ClassOrBaseClassIsNeatooBaseClass(classSymbol))
                return NeatooClassInfo.Empty;

            return NeatooClassExtractor.Extract(
                classDeclaration,
                classSymbol,
                context.SemanticModel,
                isMinimalGeneration: false);
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
    /// Execute phase - generates source code from cached NeatooClassInfo.
    /// No SemanticModel access in this phase.
    /// </summary>
    private static void Execute(SourceProductionContext context, NeatooClassInfo classInfo)
    {
        // Handle error results from the transform phase
        if (classInfo.IsError)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                GeneratorDiagnostics.SemanticTargetException,
                Location.None,
                classInfo.ClassName,
                classInfo.ErrorMessage));

#if DEBUG
            if (!string.IsNullOrWhiteSpace(classInfo.StackTrace))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    GeneratorDiagnostics.GeneratorStackTrace,
                    Location.None,
                    classInfo.StackTrace));
            }
#endif
            return;
        }

        // Skip empty/invalid results
        if (!classInfo.IsSuccess)
            return;

        try
        {
            // Generate source code using only cached data
            var source = SourceGenerator.GenerateSource(classInfo);

            // Determine filename based on generation mode
            var fileName = classInfo.IsMinimalGeneration
                ? $"{classInfo.Namespace}.{classInfo.ClassName}.Minimal.g.cs"
                : $"{classInfo.Namespace}.{classInfo.ClassName}.g.cs";

            context.AddSource(fileName, source);
        }
        catch (Exception ex)
        {
            GeneratorDiagnostics.ReportExceptionWithStackTrace(
                context,
                GeneratorDiagnostics.GeneratorException,
                ex,
                Location.None,
                $"{classInfo.Namespace}.{classInfo.ClassName}");
        }
    }
}
