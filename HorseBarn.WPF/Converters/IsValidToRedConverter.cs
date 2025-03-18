using System.Windows.Data;
using System.Windows.Media;

namespace HorseBarn.WPF.Converters;

internal class IsValidToRedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool && !(bool)value)
        {
            return Brushes.LightPink;
        }
        return null;
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
