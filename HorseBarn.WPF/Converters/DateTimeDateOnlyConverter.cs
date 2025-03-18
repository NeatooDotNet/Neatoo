using System.Globalization;
using System.Windows.Data;

namespace HorseBarn.WPF.Converters;

public class DateTimeDateOnlyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is DateOnly dateOnly)
        {
            return dateOnly.ToDateTime(TimeOnly.MinValue);
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is DateTime dateTime)
        {
            return DateOnly.FromDateTime(dateTime);
        }

        return null;
    }
}
