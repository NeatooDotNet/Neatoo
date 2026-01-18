using System.Text;
using Neatoo.BaseGenerator.Models;

namespace Neatoo.BaseGenerator.Generators;

/// <summary>
/// Generates MapModifiedTo method implementations.
/// </summary>
internal static class MapperGenerator
{
    /// <summary>
    /// Generates all MapModifiedTo methods.
    /// </summary>
    public static void GenerateMapperMethods(StringBuilder sb, EquatableArray<MapperMethodInfo> mapperMethods)
    {
        foreach (var method in mapperMethods)
        {
            GenerateMapperMethod(sb, method);
        }
    }

    /// <summary>
    /// Generates a single MapModifiedTo method.
    /// </summary>
    private static void GenerateMapperMethod(StringBuilder sb, MapperMethodInfo method)
    {
        sb.AppendLine($"{method.MethodSignature}");
        sb.AppendLine("{");

        foreach (var mapping in method.Mappings)
        {
            var nullException = "";
            var typeCast = "";

            if (mapping.NeedsNullCheck)
            {
                nullException = $"?? throw new NullReferenceException(\"{method.ClassDisplayString}.{mapping.ClassPropertyName}\")";
            }

            if (!mapping.TypesMatch)
            {
                var targetTypeWithNullable = mapping.ParameterPropertyType +
                    (nullException.Length > 0 ? "?" : "");
                typeCast = $"({targetTypeWithNullable}) ";
            }

            sb.AppendLine($"if (this[nameof({mapping.ClassPropertyName})].IsModified){{");
            sb.AppendLine($"{method.ParameterName}.{mapping.ParameterPropertyName} = {typeCast}this.{mapping.ClassPropertyName}{nullException};");
            sb.AppendLine("}");
        }

        sb.AppendLine("}");
    }
}
