using System.Windows;
using GetDevice.ViewModels;

namespace GetDevice.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.OpenSettingsRequested += () =>
        {
            var settingsVm = App.ServiceProvider.GetService<SettingsViewModel>();
            if (settingsVm != null)
            {
                var settingsWindow = new SettingsWindow(settingsVm);
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();
            }
        };
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
            Hide();

        base.OnStateChanged(e);
    }
}
