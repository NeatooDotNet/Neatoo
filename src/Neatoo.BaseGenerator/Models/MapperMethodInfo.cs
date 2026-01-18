namespace Neatoo.BaseGenerator.Models;

/// <summary>
/// Extracted metadata for a partial MapModifiedTo method.
/// Contains the method signature and all property mappings.
/// </summary>
internal readonly record struct MapperMethodInfo(
    string MethodSignature,
    string ParameterName,
    string ClassDisplayString,
    EquatableArray<PropertyMapping> Mappings
) : IEquatable<MapperMethodInfo>;
