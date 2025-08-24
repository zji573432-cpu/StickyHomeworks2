using System.Globalization;
using System.Windows.Data;

namespace StickyHomeworks.Converters;

public class DoubleToStringMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var a = (double)values[0];
        var b = (string)values[1];
        return a.ToString(b);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}