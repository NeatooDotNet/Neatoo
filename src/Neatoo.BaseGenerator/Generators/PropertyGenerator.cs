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

                GenerateAwaitableSetter(sb, property);
            }
        }
    }

    /// <summary>
    /// True when the property gets a generated <c>Set{Name}</c> on the class.
    /// </summary>
    private static bool HasAwaitableSetter(PartialPropertyInfo property)
        => property.HasSetter && !property.IsInit && !property.IsLazyLoad && !property.HasSetMethodConflict;

    /// <summary>
    /// Generates <c>Task Set{Name}(T value)</c>: the same write as the setter, returning the Task of the
    /// property's whole rule cascade so domain code can await the write's consequences. The setter cannot
    /// await, so a rule or verb that assigns returns before an async consequence lands.
    /// </summary>
    private static void GenerateAwaitableSetter(StringBuilder sb, PartialPropertyInfo property)
    {
        if (!HasAwaitableSetter(property))
        {
            return;
        }

        var accessibility = AccessModifier(property.SetterAccessibility ?? property.Accessibility);
        var write = property.SetterAccessibility == "private"
            ? $"{property.Name}Property.SetPrivateValue(value)"
            : $"{property.Name}Property.SetValue(value)";

        sb.AppendLine($"/// <summary>Sets {property.Name} and returns the Task of its whole rule cascade. Await it in rules and verbs.</summary>");
        sb.AppendLine($"{accessibility}global::System.Threading.Tasks.Task Set{property.Name}({property.Type} value) {{ var task = {write}; if (!task.IsCompleted) {{ Parent?.AddChildTask(task); RunningTasks.AddTask(task); }} return task; }}");
    }

    /// <summary>
    /// The modifier text for a generated member: an access keyword followed by a space, or nothing (private).
    /// </summary>
    private static string AccessModifier(string accessibility)
        => accessibility is "public" or "protected" or "internal" or "private" ? $"{accessibility} " : string.Empty;

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

            // The awaitable setter reaches other types only through the interface; only a public write goes there.
            if (HasAwaitableSetter(property)
                && property.Accessibility == "public"
                && property.SetterAccessibility == null
                && !property.InterfaceDeclaresSetMethod
                && !property.InterfaceDeclaresReadOnly)
            {
                sb.AppendLine($"/// <summary>Sets {property.Name} and returns the Task of its whole rule cascade. Await it in rules and verbs.</summary>");
                sb.AppendLine($"global::System.Threading.Tasks.Task Set{property.Name}({property.Type} value);");
            }
        }

        sb.AppendLine("}");
    }
}
