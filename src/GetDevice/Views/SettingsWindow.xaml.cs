using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using GetDevice.ViewModels;

namespace GetDevice.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        Resources["BoolToErrorBrush"] = new PasswordMessageBrushConverter();

        CurrentPasswordBox.PasswordChanged += (_, _) => _viewModel.CurrentPassword = CurrentPasswordBox.Password;
        NewPasswordBox.PasswordChanged += (_, _) => _viewModel.NewPassword = NewPasswordBox.Password;
        ConfirmPasswordBox.PasswordChanged += (_, _) => _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;

        ShowPasswordsCheckBox.Checked += ToggleShowPasswords;
        ShowPasswordsCheckBox.Unchecked += ToggleShowPasswords;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        viewModel.CloseRequested += () =>
        {
            DialogResult = true;
            Close();
        };
    }

    private void ToggleShowPasswords(object sender, RoutedEventArgs e)
    {
        var show = ShowPasswordsCheckBox.IsChecked == true;
        SwapVisibility(CurrentPasswordBox, CurrentPasswordTextBox, show);
        SwapVisibility(NewPasswordBox, NewPasswordTextBox, show);
        SwapVisibility(ConfirmPasswordBox, ConfirmPasswordTextBox, show);

        if (show)
        {
            CurrentPasswordTextBox.Text = _viewModel.CurrentPassword;
            NewPasswordTextBox.Text = _viewModel.NewPassword;
            ConfirmPasswordTextBox.Text = _viewModel.ConfirmPassword;
        }
        else
        {
            CurrentPasswordBox.Password = _viewModel.CurrentPassword;
            NewPasswordBox.Password = _viewModel.NewPassword;
            ConfirmPasswordBox.Password = _viewModel.ConfirmPassword;
        }
    }

    private static void SwapVisibility(PasswordBox passwordBox, System.Windows.Controls.TextBox textBox, bool show)
    {
        passwordBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        textBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.CurrentPassword))
        {
            if (CurrentPasswordBox.Password != _viewModel.CurrentPassword)
                CurrentPasswordBox.Password = _viewModel.CurrentPassword;
        }
        else if (e.PropertyName == nameof(SettingsViewModel.NewPassword))
        {
            if (NewPasswordBox.Password != _viewModel.NewPassword)
                NewPasswordBox.Password = _viewModel.NewPassword;
        }
        else if (e.PropertyName == nameof(SettingsViewModel.ConfirmPassword))
        {
            if (ConfirmPasswordBox.Password != _viewModel.ConfirmPassword)
                ConfirmPasswordBox.Password = _viewModel.ConfirmPassword;
        }
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
