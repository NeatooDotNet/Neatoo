using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Neatoo.BaseGenerator.Models;

namespace Neatoo.BaseGenerator.Extractors;

/// <summary>
/// Extracts MapModifiedTo partial method information and property mappings.
/// </summary>
internal static class MapperMethodExtractor
{
    /// <summary>
    /// Extracts all partial MapModifiedTo methods and their property mappings.
    /// </summary>
    public static EquatableArray<MapperMethodInfo> ExtractMapperMethods(
        ClassDeclarationSyntax classDeclaration,
        INamedTypeSymbol classSymbol,
        SemanticModel semanticModel)
    {
        var classProperties = GetPropertiesRecursive(classSymbol);
        var classMethods = classSymbol.GetMembers().OfType<IMethodSymbol>().ToList();
        var mapperMethods = new List<MapperMethodInfo>();

        foreach (var classMethod in classMethods)
        {
            if (classMethod.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()
                is not MethodDeclarationSyntax classSyntax)
                continue;

            // Check for partial MapModifiedTo method
            if (classSyntax.Identifier.Text != "MapModifiedTo")
                continue;
            if (!classSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                continue;

            // Get parameter
            var parameterSymbol = classMethod.Parameters.SingleOrDefault();
            if (parameterSymbol == null)
                continue;

            var parameterSyntax = classSyntax.ParameterList.Parameters.First();
            var parameterIdentifier = parameterSyntax.Identifier.Text;
            var methodSignature = classSyntax.ToFullString().Trim().TrimEnd(';');

            // Get parameter type properties
            var parameterProperties = GetPropertiesRecursive(parameterSymbol.Type as INamedTypeSymbol);

            // Match properties
            var mappings = new List<PropertyMapping>();
            foreach (var parameterProperty in parameterProperties)
            {
                var classProperty = classProperties.FirstOrDefault(p => p.Name == parameterProperty.Name);
                if (classProperty != null)
                {
                    var needsNullCheck = classProperty.NullableAnnotation == NullableAnnotation.Annotated
                        && parameterProperty.NullableAnnotation != NullableAnnotation.Annotated;

                    var typesMatch = classProperty.Type.ToDisplayString().Trim('?') ==
                        parameterProperty.Type.ToDisplayString().Trim('?');

                    mappings.Add(new PropertyMapping(
                        ClassPropertyName: classProperty.Name,
                        ParameterPropertyName: parameterProperty.Name,
                        ClassPropertyType: classProperty.Type.ToDisplayString(),
                        ParameterPropertyType: parameterProperty.Type.ToDisplayString(),
                        NeedsNullCheck: needsNullCheck,
                        TypesMatch: typesMatch
                    ));
                }
            }

            if (mappings.Count > 0)
            {
                mapperMethods.Add(new MapperMethodInfo(
                    MethodSignature: methodSignature,
                    ParameterName: parameterIdentifier,
                    ClassDisplayString: classSymbol.ToDisplayString(),
                    Mappings: new EquatableArray<PropertyMapping>(mappings)
                ));
            }
        }

        return new EquatableArray<MapperMethodInfo>(mapperMethods);
    }

    /// <summary>
    /// Gets all properties including inherited ones.
    /// </summary>
    private static List<IPropertySymbol> GetPropertiesRecursive(INamedTypeSymbol? classSymbol)
    {
        var properties = classSymbol?.GetMembers().OfType<IPropertySymbol>().ToList() ?? new List<IPropertySymbol>();
        if (classSymbol?.BaseType != null)
        {
            properties.AddRange(GetPropertiesRecursive(classSymbol.BaseType));
        }
        return properties;
    }
}
