using GetDevice.Services;
using GetDevice.ViewModels;
using Moq;

namespace GetDevice.Tests.ViewModels;

public class LoginViewModelTests
{
    [Fact]
    public void Login_WithCorrectPassword_FiresLoginSucceeded()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.Verify("correct")).Returns(true);
        mockPassword.Setup(p => p.IsDefaultPassword()).Returns(false);

        var vm = new LoginViewModel(mockPassword.Object);
        var succeeded = false;
        vm.LoginSucceeded += () => succeeded = true;

        vm.Password = "correct";
        vm.LoginCommand.Execute(null);

        Assert.True(succeeded);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void Login_WithWrongPassword_ShowsError()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.Verify("wrong")).Returns(false);
        mockPassword.Setup(p => p.IsDefaultPassword()).Returns(false);

        var vm = new LoginViewModel(mockPassword.Object);
        var succeeded = false;
        vm.LoginSucceeded += () => succeeded = true;

        vm.Password = "wrong";
        vm.LoginCommand.Execute(null);

        Assert.False(succeeded);
        Assert.NotEmpty(vm.ErrorMessage);
    }

    [Fact]
    public void Login_WithEmptyPassword_ShowsError()
    {
        var mockPassword = new Mock<IPasswordService>();

        var vm = new LoginViewModel(mockPassword.Object);

        vm.Password = "";
        vm.LoginCommand.Execute(null);

        Assert.NotEmpty(vm.ErrorMessage);
    }

    [Fact]
    public void IsFirstLaunch_WhenDefaultPassword_ReturnsTrue()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.IsDefaultPassword()).Returns(true);

        var vm = new LoginViewModel(mockPassword.Object);

        Assert.True(vm.IsFirstLaunch);
    }
}
