using Neatoo;
using System.ComponentModel;

namespace Person.App
{
    public static class EntityPropertyExtensions
    {
        public static Task SetStringValue(this IEntityProperty property, string? value)
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
