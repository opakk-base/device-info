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
        var vm = new SettingsViewModel(
            mockPassword.Object,
            Mock.Of<IConfigService>(),
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

        var vm = new SettingsViewModel(
            mockPassword.Object,
            Mock.Of<IConfigService>(),
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

        var vm = new SettingsViewModel(
            mockPassword.Object,
            Mock.Of<IConfigService>(),
            Mock.Of<IHttpServerService>());

        vm.FactoryResetCommand.Execute(null);

        mockPassword.Verify(p => p.ResetToDefault(), Times.Once);
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
