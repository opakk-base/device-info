using System.Globalization;
using System.Windows.Data;

namespace GetDevice.Converters;

public class BoolToGreenRedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? System.Windows.Media.Brushes.LimeGreen : System.Windows.Media.Brushes.Red;
        return System.Windows.Media.Brushes.Red;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
