namespace Neatoo.BaseGenerator.Models;

/// <summary>
/// Extracted metadata for a partial property.
/// All data needed to generate property implementation.
/// </summary>
internal readonly record struct PartialPropertyInfo(
    string Name,
    string Type,
    string Accessibility,
    bool HasSetter,
    string? SetterAccessibility,
    bool NeedsInterfaceDeclaration,
    bool IsLazyLoad,
    string? LazyLoadInnerType
) : IEquatable<PartialPropertyInfo>;
