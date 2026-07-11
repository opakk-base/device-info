using System.Windows;
using GetDevice.Services;
using GetDevice.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GetDevice.Views;

public partial class MainWindow : Window
{
    private readonly IConfigService _configService;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        _configService = App.ServiceProvider.GetRequiredService<IConfigService>();

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

        Closing += (_, e) =>
        {
            var config = _configService.Load();
            if (config.MinimizeToTrayOnClose)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            var config = _configService.Load();
            if (config.MinimizeToTrayOnClose)
                Hide();
        }
        base.OnStateChanged(e);
    }
}
