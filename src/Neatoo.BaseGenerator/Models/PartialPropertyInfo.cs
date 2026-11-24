namespace Neatoo.BaseGenerator.Models;

/// <summary>
/// Extracted metadata for a partial property.
/// All data needed to generate property implementation.
/// </summary>
/// <param name="IsInit">The property's setter is an <c>init</c> accessor: no awaitable setter is generated.</param>
/// <param name="HasSetMethodConflict">
/// The class or one of its base classes already declares a member named <c>Set{Name}</c>, so no awaitable
/// setter is generated for this property.
/// </param>
/// <param name="InterfaceDeclaresSetMethod">The partial interface already declares <c>Set{Name}</c>.</param>
/// <param name="InterfaceDeclaresReadOnly">
/// The partial interface declares the property without a setter: its author left the write out, so no
/// awaitable setter goes on the interface either.
/// </param>
internal readonly record struct PartialPropertyInfo(
    string Name,
    string Type,
    string Accessibility,
    bool HasSetter,
    string? SetterAccessibility,
    bool NeedsInterfaceDeclaration,
    bool IsLazyLoad,
    string? LazyLoadInnerType,
    bool IsInit,
    bool HasSetMethodConflict,
    bool InterfaceDeclaresSetMethod,
    bool InterfaceDeclaresReadOnly
) : IEquatable<PartialPropertyInfo>;
