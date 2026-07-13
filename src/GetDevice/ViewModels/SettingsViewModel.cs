using System.Windows.Input;
using GetDevice.Models;
using GetDevice.Services;

namespace GetDevice.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly IPasswordService _passwordService;
    private readonly IConfigService _configService;
    private readonly IHttpServerService _httpServerService;

    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _passwordMessage = string.Empty;
    private bool _passwordMessageIsError;
    private bool _isHttpEnabled;
    private bool _minimizeToTrayOnClose;
    private bool _isHttpRunning;
    private string _httpStatusText = string.Empty;
    private int _httpPort;
    private string _httpPortText = string.Empty;

    public string CurrentPassword
    {
        get => _currentPassword;
        set => SetProperty(ref _currentPassword, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public string PasswordMessage
    {
        get => _passwordMessage;
        set => SetProperty(ref _passwordMessage, value);
    }

    public bool PasswordMessageIsError
    {
        get => _passwordMessageIsError;
        set => SetProperty(ref _passwordMessageIsError, value);
    }

    public bool IsHttpEnabled
    {
        get => _isHttpEnabled;
        set
        {
            if (SetProperty(ref _isHttpEnabled, value))
            {
                var config = _configService.Load();
                config.HttpEnabled = value;
                _configService.Save(config);

                if (value)
                    _httpServerService.Start(config.HttpPort);
                else
                    _httpServerService.Stop();
            }
        }
    }

    public bool IsHttpRunning
    {
        get => _isHttpRunning;
        set => SetProperty(ref _isHttpRunning, value);
    }

    public string HttpStatusText
    {
        get => _httpStatusText;
        set => SetProperty(ref _httpStatusText, value);
    }

    public bool MinimizeToTrayOnClose
    {
        get => _minimizeToTrayOnClose;
        set
        {
            if (SetProperty(ref _minimizeToTrayOnClose, value))
            {
                var config = _configService.Load();
                config.MinimizeToTrayOnClose = value;
                _configService.Save(config);
            }
        }
    }

    public int HttpPort
    {
        get => _httpPort;
        set
        {
            if (SetProperty(ref _httpPort, value))
            {
                var config = _configService.Load();
                config.HttpPort = value;
                _configService.Save(config);

                if (_httpServerService.IsRunning)
                {
                    _httpServerService.Stop();
                    _httpServerService.Start(value);
                }
            }
        }
    }

    public string HttpPortText
    {
        get => _httpPortText;
        set
        {
            if (SetProperty(ref _httpPortText, value))
            {
                if (int.TryParse(value, out var port) && port >= 1024 && port <= 65535)
                {
                    HttpPort = port;
                }
            }
        }
    }

    public string HttpToggleText => IsHttpRunning ? "Stop Server" : "Start Server";

    public ICommand ChangePasswordCommand { get; }
    public ICommand FactoryResetCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand ToggleServerCommand { get; }

    public event Action? CloseRequested;

    public SettingsViewModel(
        IPasswordService passwordService,
        IConfigService configService,
        IHttpServerService httpServerService)
    {
        _passwordService = passwordService;
        _configService = configService;
        _httpServerService = httpServerService;

        ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
        FactoryResetCommand = new RelayCommand(ExecuteFactoryReset);
        CloseCommand = new RelayCommand(_ => CloseRequested?.Invoke());
        ToggleServerCommand = new RelayCommand(_ =>
        {
            if (IsHttpRunning)
                _httpServerService.Stop();
            else
                _httpServerService.Start(HttpPort);
        });

        var config = _configService.Load();
        _isHttpEnabled = config.HttpEnabled;
        _httpPort = config.HttpPort;
        _httpPortText = config.HttpPort.ToString();
        _minimizeToTrayOnClose = config.MinimizeToTrayOnClose;
        _isHttpRunning = _httpServerService.IsRunning;
        _httpStatusText = _httpServerService.IsRunning
            ? $"localhost:{config.HttpPort}"
            : "Stopped";

        _httpServerService.RunningChanged += (_, running) =>
        {
            IsHttpRunning = running;
            var currentConfig = _configService.Load();
            HttpStatusText = running
                ? $"localhost:{currentConfig.HttpPort}"
                : "Stopped";
            OnPropertyChanged(nameof(HttpToggleText));
        };
    }

    private void ExecuteChangePassword(object? parameter)
    {
        if (string.IsNullOrEmpty(CurrentPassword))
        {
            PasswordMessage = "Enter current password.";
            PasswordMessageIsError = true;
            return;
        }

        if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 4)
        {
            PasswordMessage = "New password must be at least 4 characters.";
            PasswordMessageIsError = true;
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            PasswordMessage = "Passwords do not match.";
            PasswordMessageIsError = true;
            return;
        }

        if (_passwordService.Change(CurrentPassword, NewPassword))
        {
            PasswordMessage = "Password changed successfully.";
            PasswordMessageIsError = false;
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }
        else
        {
            PasswordMessage = "Current password is incorrect.";
            PasswordMessageIsError = true;
        }
    }

    private void ExecuteFactoryReset(object? parameter)
    {
        _passwordService.ResetToDefault();
        PasswordMessage = "Password reset to default. Restart app to use '12345678'.";
        PasswordMessageIsError = false;
    }
}
