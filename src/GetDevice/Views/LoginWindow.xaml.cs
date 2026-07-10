using System.Windows;
using System.Windows.Controls;
using GetDevice.ViewModels;

namespace GetDevice.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.LoginSucceeded += OnLoginSucceeded;

        if (viewModel.IsFirstLaunch)
        {
            Title = "GetDevice - Change Default Password";
        }
    }

    private void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && sender is PasswordBox pb)
        {
            _viewModel.Password = pb.Password;
            if (_viewModel.LoginCommand.CanExecute(pb.Password))
                _viewModel.LoginCommand.Execute(pb.Password);
        }
    }

    private void OnLoginSucceeded()
    {
        _viewModel.LoginSucceeded -= OnLoginSucceeded;
        DialogResult = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _viewModel.LoginSucceeded -= OnLoginSucceeded;
    }
}
