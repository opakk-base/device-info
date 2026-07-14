using System.Windows.Input;
using GetDevice.Services;

namespace GetDevice.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IPasswordService _passwordService;
    private readonly IConfigService _configService;
    private readonly IHttpServerService _httpServerService;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isFirstLaunch;
    private bool _isHttpRunning;
    private string _httpStatusText = string.Empty;
    private readonly EventHandler<bool> _runningChangedHandler;

    public event Action? LoginSucceeded;
    public event Action<string>? LoginFailed;

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsFirstLaunch
    {
        get => _isFirstLaunch;
        set => SetProperty(ref _isFirstLaunch, value);
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

    public ICommand LoginCommand { get; }
    public ICommand SkipChangePasswordCommand { get; }
    public ICommand ToggleApiCommand { get; }
    public string HttpToggleText => IsHttpRunning ? "Stop" : "Start";

    public LoginViewModel(IPasswordService passwordService, IConfigService configService, IHttpServerService httpServerService)
    {
        _passwordService = passwordService;
        _httpServerService = httpServerService;
        _configService = configService;

        LoginCommand = new RelayCommand(ExecuteLogin);
        SkipChangePasswordCommand = new RelayCommand(_ =>
        {
            Unsubscribe();
            LoginSucceeded?.Invoke();
        });
        ToggleApiCommand = new RelayCommand(_ =>
        {
            if (_httpServerService.IsRunning)
                _httpServerService.Stop();
            else
                _httpServerService.Start(_configService.Load().HttpPort);
        });
        IsFirstLaunch = _passwordService.IsDefaultPassword();

        IsHttpRunning = _httpServerService.IsRunning;
        HttpStatusText = _httpServerService.IsRunning
            ? $"localhost:{_configService.Load().HttpPort}"
            : "Stopped";

        _runningChangedHandler = (_, running) =>
        {
            IsHttpRunning = running;
            HttpStatusText = running
                ? $"localhost:{_configService.Load().HttpPort}"
                : "Stopped";
            OnPropertyChanged(nameof(HttpToggleText));
            if (System.Windows.Application.Current is not null)
                CommandManager.InvalidateRequerySuggested();
        };
        _httpServerService.RunningChanged += _runningChangedHandler;
    }

    public void Unsubscribe()
    {
        _httpServerService.RunningChanged -= _runningChangedHandler;
    }

    private void ExecuteLogin(object? parameter)
    {
        if (string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Please enter a password.";
            return;
        }

        if (_passwordService.Verify(Password))
        {
            ErrorMessage = string.Empty;
            Unsubscribe();
            LoginSucceeded?.Invoke();
        }
        else
        {
            ErrorMessage = "Incorrect password.";
            LoginFailed?.Invoke("Incorrect password.");
        }
    }
}
