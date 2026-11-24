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

                var isInit = property.AccessorList?.Accessors
                    .Any(a => a.IsKind(SyntaxKind.InitAccessorDeclaration)) ?? false;

                string? setterAccessibility = null;
                if (hasSetter)
                {
                    var setAccessor = property.AccessorList?.Accessors
                        .FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) ||
                                             a.IsKind(SyntaxKind.InitAccessorDeclaration));
                    if (setAccessor != null)
                    {
                        var accessModifier = setAccessor.Modifiers.FirstOrDefault();
                        if (accessModifier != default)
                        {
                            setterAccessibility = accessModifier.ToString();
                        }
                    }
                }

                var needsInterfaceDeclaration = hasPartialInterface &&
                    !existingInterfaceProperties.Contains(propertyName);

                // Detect LazyLoad<T> properties
                var isLazyLoad = false;
                string? lazyLoadInnerType = null;
                var propertySymbol = classSymbol.GetMembers(propertyName)
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault();

                if (propertySymbol != null && propertySymbol.Type is INamedTypeSymbol namedType
                    && namedType.IsGenericType
                    && namedType.OriginalDefinition.Name == "EntityLazyLoad"
                    && namedType.OriginalDefinition.ContainingNamespace?.ToString() == "Neatoo"
                    && namedType.TypeArguments.Length == 1)
                {
                    isLazyLoad = true;
                    lazyLoadInnerType = namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                }

                return new PartialPropertyInfo(
                    Name: propertyName,
                    Type: propertyType,
                    Accessibility: accessibility,
                    HasSetter: hasSetter,
                    SetterAccessibility: setterAccessibility,
                    NeedsInterfaceDeclaration: needsInterfaceDeclaration,
                    IsLazyLoad: isLazyLoad,
                    LazyLoadInnerType: lazyLoadInnerType,
                    IsInit: isInit,
                    HasSetMethodConflict: HasMemberInHierarchy(classSymbol, $"Set{propertyName}"),
                    InterfaceDeclaresSetMethod: InterfaceDeclaresMember(classSymbol, $"Set{propertyName}"),
                    InterfaceDeclaresReadOnly: InterfaceDeclaresReadOnlyProperty(classSymbol, propertyName)
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
    /// True when the class or any base class declares a member with the given name.
    /// A generated awaitable setter never collides with or hides a hand-written member.
    /// </summary>
    private static bool HasMemberInHierarchy(INamedTypeSymbol classSymbol, string memberName)
    {
        for (var type = classSymbol; type != null; type = type.BaseType)
        {
            if (!type.GetMembers(memberName).IsEmpty)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True when the class's I{ClassName} interface already declares a member with the given name.
    /// </summary>
    private static bool InterfaceDeclaresMember(INamedTypeSymbol classSymbol, string memberName)
    {
        var interfaceSymbol = classSymbol.Interfaces
            .FirstOrDefault(i => i.Name == $"I{classSymbol.Name}");

        return interfaceSymbol != null && !interfaceSymbol.GetMembers(memberName).IsEmpty;
    }

    /// <summary>
    /// True when the class's I{ClassName} interface declares the property with no setter.
    /// </summary>
    private static bool InterfaceDeclaresReadOnlyProperty(INamedTypeSymbol classSymbol, string propertyName)
    {
        var interfaceSymbol = classSymbol.Interfaces
            .FirstOrDefault(i => i.Name == $"I{classSymbol.Name}");

        return interfaceSymbol?.GetMembers(propertyName)
            .OfType<IPropertySymbol>()
            .Any(p => p.SetMethod == null) ?? false;
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
