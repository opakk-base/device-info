using GetDevice.Models;
using GetDevice.Services;
using GetDevice.ViewModels;
using Moq;

namespace GetDevice.Tests.ViewModels;

public class SettingsViewModelTests
{
    [Fact]
    public void ChangePassword_WithMismatchedNewPasswords_ShowsError()
    {
        var mockPassword = new Mock<IPasswordService>();
        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(new AppConfig());
        var vm = new SettingsViewModel(
            mockPassword.Object,
            mockConfig.Object,
            Mock.Of<IHttpServerService>());

        vm.CurrentPassword = "old";
        vm.NewPassword = "new1";
        vm.ConfirmPassword = "new2";
        vm.ChangePasswordCommand.Execute(null);

        Assert.True(vm.PasswordMessageIsError);
        Assert.Contains("do not match", vm.PasswordMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ChangePassword_WithCorrectInput_ChangesPassword()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.Change("old", "newpass123")).Returns(true);
        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(new AppConfig());

        var vm = new SettingsViewModel(
            mockPassword.Object,
            mockConfig.Object,
            Mock.Of<IHttpServerService>());

        vm.CurrentPassword = "old";
        vm.NewPassword = "newpass123";
        vm.ConfirmPassword = "newpass123";
        vm.ChangePasswordCommand.Execute(null);

        Assert.False(vm.PasswordMessageIsError);
        Assert.Contains("successfully", vm.PasswordMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FactoryReset_CallsResetToDefault()
    {
        var mockPassword = new Mock<IPasswordService>();
        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(new AppConfig());

        var vm = new SettingsViewModel(
            mockPassword.Object,
            mockConfig.Object,
            Mock.Of<IHttpServerService>());

        vm.FactoryResetCommand.Execute(null);

        mockPassword.Verify(p => p.ResetToDefault(), Times.Once);
    }

    [Fact]
    public void HttpPort_DefaultsToConfigValue()
    {
        var config = new AppConfig { HttpPort = 9090 };
        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(config);

        var vm = new SettingsViewModel(
            Mock.Of<IPasswordService>(),
            mockConfig.Object,
            Mock.Of<IHttpServerService>());

        Assert.Equal(9090, vm.HttpPort);
    }

    [Fact]
    public void HttpPort_SavesToConfig_OnChange()
    {
        var config = new AppConfig { HttpPort = 8080 };
        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(config);
        var mockHttp = new Mock<IHttpServerService>();

        var vm = new SettingsViewModel(
            Mock.Of<IPasswordService>(),
            mockConfig.Object,
            mockHttp.Object);

        vm.HttpPort = 9090;

        mockConfig.Verify(c => c.Save(It.Is<AppConfig>(cfg => cfg.HttpPort == 9090)), Times.Once);
    }

    [Fact]
    public void ChangingPort_RestartsServer_WhenRunning()
    {
        var config = new AppConfig { HttpPort = 8080 };
        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(config);
        var mockHttp = new Mock<IHttpServerService>();
        mockHttp.Setup(x => x.IsRunning).Returns(true);

        var vm = new SettingsViewModel(
            Mock.Of<IPasswordService>(),
            mockConfig.Object,
            mockHttp.Object);

        vm.HttpPort = 9090;

        mockHttp.Verify(x => x.Stop(), Times.Once);
        mockHttp.Verify(x => x.Start(9090), Times.Once);
    }

    [Fact]
    public void HttpToggleText_ReflectsServerState()
    {
        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(new AppConfig());
        var mockHttp = new Mock<IHttpServerService>();
        mockHttp.Setup(x => x.IsRunning).Returns(false);

        var vm = new SettingsViewModel(
            Mock.Of<IPasswordService>(),
            mockConfig.Object,
            mockHttp.Object);

        Assert.Equal("Start Server", vm.HttpToggleText);

        mockHttp.Raise(x => x.RunningChanged += null, mockHttp.Object, true);
        Assert.Equal("Stop Server", vm.HttpToggleText);

        mockHttp.Raise(x => x.RunningChanged += null, mockHttp.Object, false);
        Assert.Equal("Start Server", vm.HttpToggleText);
    }

    [Fact]
    public void ToggleServerCommand_StartsAndStopsServer()
    {
        var config = new AppConfig { HttpPort = 8080 };
        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(config);
        var mockHttp = new Mock<IHttpServerService>();
        mockHttp.Setup(x => x.IsRunning).Returns(false);

        var vm = new SettingsViewModel(
            Mock.Of<IPasswordService>(),
            mockConfig.Object,
            mockHttp.Object);

        vm.ToggleServerCommand.Execute(null);
        mockHttp.Verify(x => x.Start(8080), Times.Once);

        mockHttp.Raise(x => x.RunningChanged += null, mockHttp.Object, true);
        vm.ToggleServerCommand.Execute(null);
        mockHttp.Verify(x => x.Stop(), Times.Once);
    }

    [Fact]
    public void HttpPortText_BindsToString()
    {
        var config = new AppConfig { HttpPort = 8080 };
        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(config);

        var vm = new SettingsViewModel(
            Mock.Of<IPasswordService>(),
            mockConfig.Object,
            Mock.Of<IHttpServerService>());

        Assert.Equal("8080", vm.HttpPortText);

        vm.HttpPortText = "9090";
        Assert.Equal(9090, vm.HttpPort);
        mockConfig.Verify(c => c.Save(It.Is<AppConfig>(cfg => cfg.HttpPort == 9090)), Times.Once);
    }

    [Fact]
    public void HttpPort_RejectsOutOfRangeValues()
    {
        var config = new AppConfig { HttpPort = 8080 };
        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(config);

        var vm = new SettingsViewModel(
            Mock.Of<IPasswordService>(),
            mockConfig.Object,
            Mock.Of<IHttpServerService>());

        vm.HttpPort = 80;
        Assert.Equal(8080, vm.HttpPort);

        vm.HttpPort = 70000;
        Assert.Equal(8080, vm.HttpPort);
    }

    [Fact]
    public void ToggleHttp_StartsAndStopsServer()
    {
        var mockHttp = new Mock<IHttpServerService>();
        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(new AppConfig());

        var vm = new SettingsViewModel(
            Mock.Of<IPasswordService>(),
            mockConfig.Object,
            mockHttp.Object);

        vm.IsHttpEnabled = true;
        mockHttp.Verify(h => h.Start(8080), Times.Once);

        vm.IsHttpEnabled = false;
        mockHttp.Verify(h => h.Stop(), Times.Once);
    }
}
