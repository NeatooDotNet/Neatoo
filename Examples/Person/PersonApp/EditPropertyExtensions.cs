using Neatoo;
using System.ComponentModel;

namespace PersonApp
{
    public static class EditPropertyExtensions
    {
        public static Task SetStringValue(this IEditProperty property, string? value)
        {
            if (value == null)
            {
                return property.SetValue(default);
            }
            else
            {
                var converter = TypeDescriptor.GetConverter(property.Type);
                if (converter != null && converter.IsValid(value))
                {
                    return property.SetValue(converter.ConvertFromString(value));
                }
                return property.SetValue(default);
            }
        }
    }
}
