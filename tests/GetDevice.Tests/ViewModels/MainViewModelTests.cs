using GetDevice.Models;
using GetDevice.Services;
using GetDevice.ViewModels;
using Moq;

namespace GetDevice.Tests.ViewModels;

public class MainViewModelTests
{
    [Fact]
    public void LoadDeviceInfo_PopulatesFields()
    {
        var info = new DeviceInfo { Hostname = "test-pc", Os = "Windows 11" };
        var config = new AppConfig
        {
            CheckedFields = new List<string> { "hostname", "os", "device_id" }
        };

        var mockDevice = new Mock<IDeviceInfoService>();
        mockDevice.Setup(d => d.Gather()).Returns(info);

        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(config);

        var vm = new MainViewModel(
            mockDevice.Object,
            Mock.Of<IExportService>(),
            mockConfig.Object,
            Mock.Of<IPasswordService>());

        Assert.Equal(9, vm.Fields.Count);
        Assert.Contains(vm.Fields, f => f.Key == "hostname" && f.Value == "test-pc");
        Assert.Contains(vm.Fields, f => f.Key == "os" && f.Value == "Windows 11");
    }

    [Fact]
    public void SelectAll_SetsAllFieldsChecked()
    {
        var mockDevice = new Mock<IDeviceInfoService>();
        mockDevice.Setup(d => d.Gather()).Returns(new DeviceInfo());

        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(new AppConfig());

        var vm = new MainViewModel(
            mockDevice.Object,
            Mock.Of<IExportService>(),
            mockConfig.Object,
            Mock.Of<IPasswordService>());

        vm.SelectNoneCommand.Execute(null);
        Assert.All(vm.Fields, f => Assert.False(f.IsChecked));

        vm.SelectAllCommand.Execute(null);
        Assert.All(vm.Fields, f => Assert.True(f.IsChecked));
    }
}
