using GetDevice.Models;
using GetDevice.Services;
using GetDevice.ViewModels;
using Moq;

namespace GetDevice.Tests.ViewModels;

public class LoginViewModelTests
{
    private static (Mock<IConfigService> Config, Mock<IHttpServerService> Http) CreateMocks()
    {
        var configMock = new Mock<IConfigService>();
        configMock.Setup(c => c.Load()).Returns(new AppConfig());
        var httpMock = new Mock<IHttpServerService>();
        httpMock.Setup(h => h.IsRunning).Returns(false);
        return (configMock, httpMock);
    }

    [Fact]
    public void Login_WithCorrectPassword_FiresLoginSucceeded()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.Verify("correct")).Returns(true);
        mockPassword.Setup(p => p.IsDefaultPassword()).Returns(false);
        var (configMock, httpMock) = CreateMocks();

        var vm = new LoginViewModel(mockPassword.Object, configMock.Object, httpMock.Object);
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
        var (configMock, httpMock) = CreateMocks();

        var vm = new LoginViewModel(mockPassword.Object, configMock.Object, httpMock.Object);
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
        var (configMock, httpMock) = CreateMocks();

        var vm = new LoginViewModel(mockPassword.Object, configMock.Object, httpMock.Object);

        vm.Password = "";
        vm.LoginCommand.Execute(null);

        Assert.NotEmpty(vm.ErrorMessage);
    }

    [Fact]
    public void IsFirstLaunch_WhenDefaultPassword_ReturnsTrue()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.IsDefaultPassword()).Returns(true);
        var (configMock, httpMock) = CreateMocks();

        var vm = new LoginViewModel(mockPassword.Object, configMock.Object, httpMock.Object);

        Assert.True(vm.IsFirstLaunch);
    }

    [Fact]
    public void StatusText_ShowsStopped_WhenHttpNotRunning()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.IsDefaultPassword()).Returns(false);
        var configMock = new Mock<IConfigService>();
        configMock.Setup(c => c.Load()).Returns(new AppConfig());
        var httpMock = new Mock<IHttpServerService>();
        httpMock.Setup(h => h.IsRunning).Returns(false);

        var vm = new LoginViewModel(mockPassword.Object, configMock.Object, httpMock.Object);

        Assert.False(vm.IsHttpRunning);
        Assert.Equal("Stopped", vm.HttpStatusText);
    }

    [Fact]
    public void StatusText_ShowsPort_WhenHttpRunning()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.IsDefaultPassword()).Returns(false);
        var configMock = new Mock<IConfigService>();
        configMock.Setup(c => c.Load()).Returns(new AppConfig { HttpPort = 8080 });
        var httpMock = new Mock<IHttpServerService>();
        httpMock.Setup(h => h.IsRunning).Returns(true);

        var vm = new LoginViewModel(mockPassword.Object, configMock.Object, httpMock.Object);

        Assert.True(vm.IsHttpRunning);
        Assert.Equal("localhost:8080", vm.HttpStatusText);
    }

    [Fact]
    public void StatusText_Updates_WhenHttpServerChanges()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.IsDefaultPassword()).Returns(false);
        var configMock = new Mock<IConfigService>();
        configMock.Setup(c => c.Load()).Returns(new AppConfig { HttpPort = 8080 });
        var httpMock = new Mock<IHttpServerService>();
        httpMock.Setup(h => h.IsRunning).Returns(false);

        var vm = new LoginViewModel(mockPassword.Object, configMock.Object, httpMock.Object);

        Assert.False(vm.IsHttpRunning);
        Assert.Equal("Stopped", vm.HttpStatusText);

        httpMock.Raise(h => h.RunningChanged += null, httpMock.Object, true);
        Assert.True(vm.IsHttpRunning);
        Assert.Equal("localhost:8080", vm.HttpStatusText);

        httpMock.Raise(h => h.RunningChanged += null, httpMock.Object, false);
        Assert.False(vm.IsHttpRunning);
        Assert.Equal("Stopped", vm.HttpStatusText);
    }

    [Fact]
    public void HttpToggleText_ReturnsStop_WhenHttpRunning()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.IsDefaultPassword()).Returns(false);
        var configMock = new Mock<IConfigService>();
        configMock.Setup(c => c.Load()).Returns(new AppConfig());
        var httpMock = new Mock<IHttpServerService>();
        httpMock.Setup(h => h.IsRunning).Returns(true);

        var vm = new LoginViewModel(mockPassword.Object, configMock.Object, httpMock.Object);

        Assert.Equal("Stop", vm.HttpToggleText);
    }

    [Fact]
    public void HttpToggleText_ReturnsStart_WhenHttpNotRunning()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.IsDefaultPassword()).Returns(false);
        var configMock = new Mock<IConfigService>();
        configMock.Setup(c => c.Load()).Returns(new AppConfig());
        var httpMock = new Mock<IHttpServerService>();
        httpMock.Setup(h => h.IsRunning).Returns(false);

        var vm = new LoginViewModel(mockPassword.Object, configMock.Object, httpMock.Object);

        Assert.Equal("Start", vm.HttpToggleText);
    }

    [Fact]
    public void ToggleApiCommand_StopsServer_WhenHttpRunning()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.IsDefaultPassword()).Returns(false);
        var configMock = new Mock<IConfigService>();
        configMock.Setup(c => c.Load()).Returns(new AppConfig());
        var httpMock = new Mock<IHttpServerService>();
        httpMock.Setup(h => h.IsRunning).Returns(true);

        var vm = new LoginViewModel(mockPassword.Object, configMock.Object, httpMock.Object);

        vm.ToggleApiCommand.Execute(null);

        httpMock.Verify(h => h.Stop(), Times.Once);
    }

    [Fact]
    public void ToggleApiCommand_StartsServer_WhenHttpNotRunning()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.IsDefaultPassword()).Returns(false);
        var configMock = new Mock<IConfigService>();
        configMock.Setup(c => c.Load()).Returns(new AppConfig { HttpPort = 8080 });
        var httpMock = new Mock<IHttpServerService>();
        httpMock.Setup(h => h.IsRunning).Returns(false);

        var vm = new LoginViewModel(mockPassword.Object, configMock.Object, httpMock.Object);

        vm.ToggleApiCommand.Execute(null);

        httpMock.Verify(h => h.Start(8080), Times.Once);
    }
}
