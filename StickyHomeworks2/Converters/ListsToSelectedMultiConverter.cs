using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace StickyHomeworks.Converters;

public class ListsToSelectedMultiConverter : IMultiValueConverter
{
    private ObservableCollection<string>? AllCollection { get; set; }

    private string? CurrentValue { get; set; }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var a = CurrentValue = (string)values[0];
        var b = AllCollection = (ObservableCollection<string>)values[1];
        return b.Contains(a);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        if (CurrentValue == null || AllCollection == null)
        {
            return null;
        }
        if ((bool)value && !AllCollection.Contains(CurrentValue))
        {
            AllCollection.Add(CurrentValue);
        }

        if ((!(bool)value) && AllCollection.Contains(CurrentValue))
        {
            AllCollection.Remove(CurrentValue);
        }
        return new object[]
        {
            CurrentValue,
            AllCollection
        };
    }
}