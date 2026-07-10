using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using GetDevice.ViewModels;

namespace GetDevice.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Resources["BoolToErrorBrush"] = new PasswordMessageBrushConverter();

        viewModel.CloseRequested += () =>
        {
            DialogResult = true;
            Close();
        };
    }
}

public class PasswordMessageBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isError && isError)
            return new SolidColorBrush(Colors.Red);
        return new SolidColorBrush(Colors.Green);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
