using Neatoo;

namespace Neatoo.Blazor.MudNeatoo.Extensions;

/// <summary>
/// Extension methods for working with IEntityProperty in MudBlazor components.
/// </summary>
public static class EntityPropertyExtensions
{
    /// <summary>
    /// Gets a typed validation function suitable for MudBlazor components.
    /// Returns the current PropertyMessages from the entity property.
    /// </summary>
    public static Func<T?, IEnumerable<string>> GetValidationFunc<T>(this IEntityProperty property)
    {
        return _ => property.PropertyMessages.Select(m => m.Message).Distinct();
    }

    /// <summary>
    /// Gets the concatenated error message string for display purposes.
    /// </summary>
    public static string GetErrorText(this IEntityProperty property)
    {
        var messages = property.PropertyMessages.Select(m => m.Message).Distinct().ToList();
        return messages.Count == 0 ? string.Empty : string.Join("; ", messages);
    }

    /// <summary>
    /// Determines if the property has any validation errors.
    /// </summary>
    public static bool HasErrors(this IEntityProperty property)
    {
        return property.PropertyMessages.Any();
    }
}
