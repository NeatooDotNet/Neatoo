namespace Neatoo.BaseGenerator.Models;

/// <summary>
/// Mapping between a class property and a parameter property for MapModifiedTo generation.
/// </summary>
internal readonly record struct PropertyMapping(
    string ClassPropertyName,
    string ParameterPropertyName,
    string ClassPropertyType,
    string ParameterPropertyType,
    bool NeedsNullCheck,
    bool TypesMatch
) : IEquatable<PropertyMapping>;
