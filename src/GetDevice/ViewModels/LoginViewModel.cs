using System.Windows.Input;
using GetDevice.Services;

namespace GetDevice.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IPasswordService _passwordService;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isFirstLaunch;

    public event Action? LoginSucceeded;

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

    public ICommand LoginCommand { get; }
    public ICommand SkipChangePasswordCommand { get; }

    public LoginViewModel(IPasswordService passwordService)
    {
        _passwordService = passwordService;
        LoginCommand = new RelayCommand(ExecuteLogin);
        SkipChangePasswordCommand = new RelayCommand(_ => LoginSucceeded?.Invoke());
        IsFirstLaunch = _passwordService.IsDefaultPassword();
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
            LoginSucceeded?.Invoke();
        }
        else
        {
            ErrorMessage = "Incorrect password.";
        }
    }
}
