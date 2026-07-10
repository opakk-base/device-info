using System.Windows;
using GetDevice.Services;
using GetDevice.ViewModels;
using GetDevice.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GetDevice;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private LoginWindow? _loginWindow;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private ServiceProvider? _serviceProvider;

    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        ServiceProvider = _serviceProvider;

        ShowLogin();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<IDeviceInfoService, DeviceInfoService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IHttpServerService, HttpServerService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    private void ShowLogin()
    {
        var loginVm = ServiceProvider.GetRequiredService<LoginViewModel>();
        _loginWindow = new LoginWindow(loginVm);
        loginVm.LoginSucceeded += ShowMainWindow;
        _loginWindow.ShowDialog();
    }

    private void ShowMainWindow()
    {
        _loginWindow?.Close();
        _loginWindow = null;

        var mainVm = ServiceProvider.GetRequiredService<MainViewModel>();
        _mainWindow = new MainWindow(mainVm);
        _mainWindow.Show();

        var passwordService = ServiceProvider.GetRequiredService<IPasswordService>();
        if (passwordService.IsDefaultPassword())
        {
            MessageBox.Show(
                "You are using the default password '12345678'. Please change it in Settings.",
                "Security Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        SetupTrayIcon();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                System.Reflection.Assembly.GetExecutingAssembly().Location),
            Text = "GetDevice",
            Visible = true
        };

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (_, _) =>
        {
            _mainWindow?.Show();
            _mainWindow?.WindowState = WindowState.Normal;
            _mainWindow?.Activate();
        });
        contextMenu.Items.Add("Exit", null, (_, _) =>
        {
            _trayIcon?.Dispose();
            Shutdown();
        });
        _trayIcon.ContextMenuStrip = contextMenu;

        _trayIcon.DoubleClick += (_, _) =>
        {
            _mainWindow?.Show();
            _mainWindow?.WindowState = WindowState.Normal;
            _mainWindow?.Activate();
        };

        var httpService = ServiceProvider.GetRequiredService<IHttpServerService>();
        httpService.RunningChanged += (_, running) =>
        {
            _trayIcon.Text = running
                ? "GetDevice — HTTP: Running"
                : "GetDevice — HTTP: Stopped";
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        var httpService = ServiceProvider.GetService<IHttpServerService>();
        httpService?.Stop();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
