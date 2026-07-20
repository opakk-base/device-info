using System.Windows;
using GetDevice.Services;
using GetDevice.ViewModels;
using GetDevice.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GetDevice;

public partial class App : System.Windows.Application
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

        var singleInstance = _serviceProvider.GetRequiredService<ISingleInstanceService>();
        if (!singleInstance.TryAcquire())
        {
            Shutdown();
            return;
        }

        singleInstance.Activated += () => Dispatcher.Invoke(BringToFront);

        ShowLogin();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<IDeviceInfoService, DeviceInfoService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IHttpServerService, HttpServerService>();
        services.AddSingleton<ISingleInstanceService, SingleInstanceService>();

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
        _mainWindow.Closed += (_, _) => _mainWindow = null;
        _mainWindow.Show();

        var passwordService = ServiceProvider.GetRequiredService<IPasswordService>();
        if (passwordService.IsDefaultPassword())
        {
            System.Windows.MessageBox.Show(
                "You are using the default password '12345678'. Please change it in Settings.",
                "Security Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        if (_trayIcon == null)
            SetupTrayIcon();
    }

    private void BringToFront()
    {
        if (_loginWindow != null)
        {
            _loginWindow.Show();
            _loginWindow.WindowState = WindowState.Normal;
            _loginWindow.Activate();
            return;
        }

        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
            return;
        }

        ShowLoginForTrayRestore();
    }

    private void ShowLoginForTrayRestore()
    {
        var loginVm = ServiceProvider.GetRequiredService<LoginViewModel>();
        var loginWindow = new LoginWindow(loginVm);
        loginVm.LoginSucceeded += () =>
        {
            if (_mainWindow != null)
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
            }
            else
            {
                ShowMainWindow();
            }
            loginWindow.Close();
        };
        loginWindow.ShowDialog();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                Environment.ProcessPath!),
            Text = "GetDevice",
            Visible = true
        };

        var httpService = ServiceProvider.GetRequiredService<IHttpServerService>();
        var configService = ServiceProvider.GetRequiredService<IConfigService>();

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (_, _) => ShowLoginForTrayRestore());

        var toggleItem = new System.Windows.Forms.ToolStripMenuItem(
            httpService.IsRunning ? "Stop Server" : "Start Server",
            null,
            (_, _) =>
            {
                if (httpService.IsRunning)
                    httpService.Stop();
                else
                    httpService.Start(configService.Load().HttpPort);
            });
        contextMenu.Items.Add(toggleItem);

        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        contextMenu.Items.Add("Exit", null, (_, _) =>
        {
            _trayIcon?.Dispose();
            Shutdown();
        });
        _trayIcon.ContextMenuStrip = contextMenu;

        _trayIcon.DoubleClick += (_, _) => ShowLoginForTrayRestore();

        _trayIcon.Text = httpService.IsRunning
            ? "GetDevice — HTTP: Running"
            : "GetDevice — HTTP: Stopped";
        httpService.RunningChanged += (_, running) =>
        {
            _trayIcon.Text = running
                ? "GetDevice — HTTP: Running"
                : "GetDevice — HTTP: Stopped";
            toggleItem.Text = running ? "Stop Server" : "Start Server";
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        var httpService = ServiceProvider.GetService<IHttpServerService>();
        httpService?.Stop();
        var singleInstance = _serviceProvider?.GetService<ISingleInstanceService>();
        singleInstance?.Release();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
