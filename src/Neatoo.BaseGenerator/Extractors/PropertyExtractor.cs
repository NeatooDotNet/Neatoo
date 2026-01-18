using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Neatoo.BaseGenerator.Models;

namespace Neatoo.BaseGenerator.Extractors;

/// <summary>
/// Extracts partial property metadata from class declaration.
/// </summary>
internal static class PropertyExtractor
{
    /// <summary>
    /// Extracts all partial properties from a class declaration.
    /// </summary>
    public static EquatableArray<PartialPropertyInfo> ExtractProperties(
        ClassDeclarationSyntax classDeclaration,
        INamedTypeSymbol classSymbol)
    {
        // Get existing interface properties (to avoid duplicate declarations)
        var existingInterfaceProperties = GetExistingInterfaceProperties(classSymbol);
        var hasPartialInterface = HasPartialInterface(classSymbol);

        var properties = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(p => p.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            .Select(property =>
            {
                var accessibility = property.Modifiers.FirstOrDefault().ToString();
                var propertyType = property.Type.ToString();
                var propertyName = property.Identifier.Text;

                var hasSetter = property.AccessorList?.Accessors
                    .Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) ||
                              a.IsKind(SyntaxKind.InitAccessorDeclaration)) ?? false;

                var needsInterfaceDeclaration = hasPartialInterface &&
                    !existingInterfaceProperties.Contains(propertyName);

                return new PartialPropertyInfo(
                    Name: propertyName,
                    Type: propertyType,
                    Accessibility: accessibility,
                    HasSetter: hasSetter,
                    NeedsInterfaceDeclaration: needsInterfaceDeclaration
                );
            })
            .ToList();

        return new EquatableArray<PartialPropertyInfo>(properties);
    }

    /// <summary>
    /// Checks if the class has a partial interface I{ClassName}.
    /// </summary>
    public static bool HasPartialInterface(INamedTypeSymbol classSymbol)
    {
        var interfaceSyntax = classSymbol.Interfaces
            .FirstOrDefault(i => i.Name == $"I{classSymbol.Name}");

        if (interfaceSyntax == null)
            return false;

        var interfaceDeclaration = interfaceSyntax.DeclaringSyntaxReferences
            .FirstOrDefault()?.GetSyntax() as InterfaceDeclarationSyntax;

        return interfaceDeclaration?.Modifiers
            .Any(m => m.IsKind(SyntaxKind.PartialKeyword)) ?? false;
    }

    /// <summary>
    /// Gets the names of properties already declared in the partial interface.
    /// </summary>
    private static HashSet<string> GetExistingInterfaceProperties(INamedTypeSymbol classSymbol)
    {
        var interfaceSymbol = classSymbol.Interfaces
            .FirstOrDefault(i => i.Name == $"I{classSymbol.Name}");

        if (interfaceSymbol == null)
            return new HashSet<string>();

        var interfaceDeclaration = interfaceSymbol.DeclaringSyntaxReferences
            .FirstOrDefault()?.GetSyntax() as InterfaceDeclarationSyntax;

        if (interfaceDeclaration == null)
            return new HashSet<string>();

        return new HashSet<string>(
            interfaceDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Select(p => p.Identifier.Text));
    }
}
