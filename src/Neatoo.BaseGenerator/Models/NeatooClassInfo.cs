namespace Neatoo.BaseGenerator.Models;

/// <summary>
/// Complete extracted metadata for a Neatoo class.
/// This is the PRIMARY result from the transform phase.
/// All data is extracted, no SemanticModel reference.
/// </summary>
internal readonly record struct NeatooClassInfo(
    // Identity
    string ClassName,
    string Namespace,
    string ClassDeclarationText,

    // Type information
    EquatableArray<string> TypeParameters,
    string? NeatooBaseTypeArgument,
    bool IsDirectlyInheritingNeatooBase,
    bool NeedsCastToTypeParameter,

    // Interface
    bool HasPartialInterface,

    // Properties
    EquatableArray<PartialPropertyInfo> Properties,

    // Rules
    RuleExpressionInfo RuleExpressions,

    // Mapper methods
    EquatableArray<MapperMethodInfo> MapperMethods,

    // Using directives
    EquatableArray<string> UsingDirectives,

    // Generation mode
    bool IsMinimalGeneration,

    // Error handling
    string? ErrorMessage,
    string? StackTrace
) : IEquatable<NeatooClassInfo>
{
    public bool IsSuccess => ErrorMessage == null && !string.IsNullOrEmpty(ClassName);
    public bool IsError => ErrorMessage != null;

    public static NeatooClassInfo Empty => default;

    public static NeatooClassInfo Error(string className, string errorMessage, string? stackTrace = null)
        => new(
            ClassName: className,
            Namespace: "Unknown",
            ClassDeclarationText: string.Empty,
            TypeParameters: EquatableArray<string>.Empty,
            NeatooBaseTypeArgument: null,
            IsDirectlyInheritingNeatooBase: false,
            NeedsCastToTypeParameter: false,
            HasPartialInterface: false,
            Properties: EquatableArray<PartialPropertyInfo>.Empty,
            RuleExpressions: default,
            MapperMethods: EquatableArray<MapperMethodInfo>.Empty,
            UsingDirectives: EquatableArray<string>.Empty,
            IsMinimalGeneration: false,
            ErrorMessage: errorMessage,
            StackTrace: stackTrace
        );
}
