using System.Text;
using Neatoo.BaseGenerator.Models;

namespace Neatoo.BaseGenerator.Generators;

/// <summary>
/// Generates property backing fields and implementations.
/// </summary>
internal static class PropertyGenerator
{
    /// <summary>
    /// Generates backing field properties.
    /// </summary>
    public static void GenerateBackingFields(StringBuilder sb, EquatableArray<PartialPropertyInfo> properties)
    {
        foreach (var property in properties)
        {
            sb.AppendLine($"protected IValidateProperty<{property.Type}> {property.Name}Property => (IValidateProperty<{property.Type}>)PropertyManager[nameof({property.Name})]!;");
        }
    }

    /// <summary>
    /// Generates property implementations.
    /// </summary>
    public static void GeneratePropertyImplementations(StringBuilder sb, EquatableArray<PartialPropertyInfo> properties)
    {
        foreach (var property in properties)
        {
            if (property.IsLazyLoad)
            {
                // LazyLoad properties use LoadValue (no rule triggering, no task tracking)
                if (property.HasSetter)
                {
                    var setterPrefix = property.SetterAccessibility != null ? $"{property.SetterAccessibility} " : "";
                    sb.AppendLine($"{property.Accessibility} partial {property.Type} {property.Name} {{ get => {property.Name}Property.Value; {setterPrefix}set {{ {property.Name}Property.LoadValue(value); }} }}");
                }
                else
                {
                    sb.AppendLine($"{property.Accessibility} partial {property.Type} {property.Name} {{ get => {property.Name}Property.Value; }}");
                }
            }
            else
            {
                // Scalar properties use .Value = (triggers rules and task tracking)
                if (property.HasSetter)
                {
                    if (property.SetterAccessibility == "private")
                    {
                        // Private setter: use SetPrivateValue to bypass IsReadOnly check
                        sb.AppendLine($"{property.Accessibility} partial {property.Type} {property.Name} {{ get => {property.Name}Property.Value; private set {{ {property.Name}Property.SetPrivateValue(value); if (!{property.Name}Property.Task.IsCompleted) {{ Parent?.AddChildTask({property.Name}Property.Task); RunningTasks.AddTask({property.Name}Property.Task); }} }} }}");
                    }
                    else if (property.SetterAccessibility != null)
                    {
                        // Protected/internal setter: use .Value = (same as public, with accessor)
                        sb.AppendLine($"{property.Accessibility} partial {property.Type} {property.Name} {{ get => {property.Name}Property.Value; {property.SetterAccessibility} set {{ {property.Name}Property.Value = value; if (!{property.Name}Property.Task.IsCompleted) {{ Parent?.AddChildTask({property.Name}Property.Task); RunningTasks.AddTask({property.Name}Property.Task); }} }} }}");
                    }
                    else
                    {
                        // Public setter: unchanged
                        sb.AppendLine($"{property.Accessibility} partial {property.Type} {property.Name} {{ get => {property.Name}Property.Value; set {{ {property.Name}Property.Value = value; if (!{property.Name}Property.Task.IsCompleted) {{ Parent?.AddChildTask({property.Name}Property.Task); RunningTasks.AddTask({property.Name}Property.Task); }} }} }}");
                    }
                }
                else
                {
                    sb.AppendLine($"{property.Accessibility} partial {property.Type} {property.Name} {{ get => {property.Name}Property.Value; }}");
                }
            }
        }
    }

    /// <summary>
    /// Generates partial interface declaration.
    /// </summary>
    public static void GenerateInterfaceDeclaration(StringBuilder sb, NeatooClassInfo classInfo)
    {
        sb.AppendLine($"public partial interface I{classInfo.ClassName} {{");

        foreach (var property in classInfo.Properties)
        {
            if (property.NeedsInterfaceDeclaration)
            {
                var accessors = property.HasSetter && property.SetterAccessibility == null ? "get; set;" : "get;";
                sb.AppendLine($"{property.Type} {property.Name} {{ {accessors} }}");
            }
        }

        sb.AppendLine("}");
    }
}
