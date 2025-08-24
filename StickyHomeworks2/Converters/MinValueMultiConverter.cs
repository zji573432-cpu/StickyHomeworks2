using System.Globalization;
using System.Windows.Data;

namespace StickyHomeworks.Converters;

public class MinValueMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var a = (double)values[0];
        var b = (double)values[1];
        return Math.Min(a, b);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}