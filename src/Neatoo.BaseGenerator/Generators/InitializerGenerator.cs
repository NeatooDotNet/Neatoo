using System.Text;
using Neatoo.BaseGenerator.Models;

namespace Neatoo.BaseGenerator.Generators;

/// <summary>
/// Generates InitializePropertyBackingFields method.
/// </summary>
internal static class InitializerGenerator
{
    /// <summary>
    /// Generates the InitializePropertyBackingFields override method.
    /// </summary>
    public static void GenerateInitializeMethod(StringBuilder sb, NeatooClassInfo classInfo)
    {
        var typeParameter = classInfo.NeatooBaseTypeArgument ?? classInfo.ClassName;
        var shouldCallBase = !classInfo.IsDirectlyInheritingNeatooBase;
        var thisRef = classInfo.NeedsCastToTypeParameter
            ? $"({typeParameter})this"
            : "this";

        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Generated override to initialize property backing fields.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        protected override void InitializePropertyBackingFields(IPropertyFactory<{typeParameter}> factory)");
        sb.AppendLine("        {");

        if (shouldCallBase)
        {
            sb.AppendLine("            // Initialize inherited properties first");
            sb.AppendLine("            base.InitializePropertyBackingFields(factory);");
            sb.AppendLine();
        }

        if (classInfo.Properties.Count > 0)
        {
            sb.AppendLine("            // Initialize and register this class's properties");
            sb.AppendLine("            // The backing field properties are computed and fetch from PropertyManager");
            foreach (var property in classInfo.Properties)
            {
                sb.AppendLine($"            PropertyManager.Register(factory.Create<{property.Type}>({thisRef}, nameof({property.Name})));");
            }
        }

        sb.AppendLine("        }");
    }
}
