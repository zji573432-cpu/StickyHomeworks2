using System.Globalization;
using System.Windows.Data;

namespace StickyHomeworks.Converter;

public class AndExpressionConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        foreach (var i in values)
        {
            if (i is bool b)
            {
                if (b != true)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}