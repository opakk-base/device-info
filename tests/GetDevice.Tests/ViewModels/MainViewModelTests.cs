using GetDevice.Models;
using GetDevice.Services;
using GetDevice.ViewModels;
using Moq;

namespace GetDevice.Tests.ViewModels;

public class MainViewModelTests
{
    private static Mock<IHttpServerService> CreateHttpMock(bool isRunning = false)
    {
        var mock = new Mock<IHttpServerService>();
        mock.Setup(x => x.IsRunning).Returns(isRunning);
        return mock;
    }

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
            Mock.Of<IPasswordService>(),
            CreateHttpMock().Object);

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
            Mock.Of<IPasswordService>(),
            CreateHttpMock().Object);

        vm.SelectNoneCommand.Execute(null);
        Assert.All(vm.Fields, f => Assert.False(f.IsChecked));

        vm.SelectAllCommand.Execute(null);
        Assert.All(vm.Fields, f => Assert.True(f.IsChecked));
    }

    private static (Mock<IDeviceInfoService> Device, Mock<IConfigService> Config) CreateBaseMocks()
    {
        var deviceMock = new Mock<IDeviceInfoService>();
        deviceMock.Setup(x => x.Gather()).Returns(new DeviceInfo
        {
            DeviceId = "test-id",
            DeviceName = "TestPC",
            Hostname = "test-pc",
            Os = "Windows 11",
            Arch = "x64",
            MacAddress = "00:11:22:33:44:55",
            IpAddress = "192.168.1.1",
            Timestamp = "2024-01-01T00:00:00Z"
        });

        var config = new AppConfig
        {
            HttpPort = 8080,
            CheckedFields = new List<string> { "device_id", "device_name", "hostname", "os", "arch", "mac_address", "ip_address", "timestamp", "client_key" }
        };
        var configMock = new Mock<IConfigService>();
        configMock.Setup(x => x.Load()).Returns(config);

        return (deviceMock, configMock);
    }

    [Fact]
    public void HttpStatusText_ShowsStopped_WhenHttpNotRunning()
    {
        var (deviceMock, configMock) = CreateBaseMocks();

        var vm = new MainViewModel(
            deviceMock.Object,
            Mock.Of<IExportService>(),
            configMock.Object,
            Mock.Of<IPasswordService>(),
            CreateHttpMock(false).Object);

        Assert.False(vm.IsHttpRunning);
        Assert.Equal("Stopped", vm.HttpStatusText);
    }

    [Fact]
    public void HttpStatusText_ShowsPort_WhenHttpRunning()
    {
        var (deviceMock, configMock) = CreateBaseMocks();

        var vm = new MainViewModel(
            deviceMock.Object,
            Mock.Of<IExportService>(),
            configMock.Object,
            Mock.Of<IPasswordService>(),
            CreateHttpMock(true).Object);

        Assert.True(vm.IsHttpRunning);
        Assert.Equal("localhost:8080", vm.HttpStatusText);
    }

    [Fact]
    public void HttpStatusText_Updates_WhenHttpServerChanges()
    {
        var (deviceMock, configMock) = CreateBaseMocks();
        var httpMock = CreateHttpMock(false);

        var vm = new MainViewModel(
            deviceMock.Object,
            Mock.Of<IExportService>(),
            configMock.Object,
            Mock.Of<IPasswordService>(),
            httpMock.Object);

        // Simulate server starting
        httpMock.Raise(x => x.RunningChanged += null, httpMock.Object, true);
        Assert.True(vm.IsHttpRunning);
        Assert.Equal("localhost:8080", vm.HttpStatusText);

        // Simulate server stopping
        httpMock.Raise(x => x.RunningChanged += null, httpMock.Object, false);
        Assert.False(vm.IsHttpRunning);
        Assert.Equal("Stopped", vm.HttpStatusText);
    }

    [Fact]
    public void StartStopApiCommands_ControlServer_AndReflectRunningState()
    {
        var (deviceMock, configMock) = CreateBaseMocks();
        // HttpEnabled defaults to false => no auto-start
        var httpMock = CreateStatefulHttpMock();

        var vm = new MainViewModel(
            deviceMock.Object,
            Mock.Of<IExportService>(),
            configMock.Object,
            Mock.Of<IPasswordService>(),
            httpMock.Object);

        // Initially stopped: Start enabled, Stop disabled
        Assert.True(vm.StartApiCommand.CanExecute(null));
        Assert.False(vm.StopApiCommand.CanExecute(null));

        vm.StartApiCommand.Execute(null);
        httpMock.Verify(x => x.Start(8080), Times.Once);

        // Simulate server starting
        httpMock.Raise(x => x.RunningChanged += null, httpMock.Object, true);
        Assert.False(vm.StartApiCommand.CanExecute(null));
        Assert.True(vm.StopApiCommand.CanExecute(null));

        vm.StopApiCommand.Execute(null);
        httpMock.Verify(x => x.Stop(), Times.Once);

        // Simulate server stopping
        httpMock.Raise(x => x.RunningChanged += null, httpMock.Object, false);
        Assert.True(vm.StartApiCommand.CanExecute(null));
        Assert.False(vm.StopApiCommand.CanExecute(null));
    }

    private static Mock<IHttpServerService> CreateStatefulHttpMock()
    {
        var state = new { IsRunning = false };
        var mock = new Mock<IHttpServerService>();
        var running = false;
        mock.SetupGet(x => x.IsRunning).Returns(() => running);
        mock.Setup(x => x.Start(It.IsAny<int>())).Callback<int>(_ => running = true);
        mock.Setup(x => x.Stop()).Callback(() => running = false);
        return mock;
    }

    [Fact]
    public void AutoStart_StartsServer_WhenHttpEnabled()
    {
        var (deviceMock, configMock) = CreateBaseMocks();
        configMock.Setup(x => x.Load()).Returns(new AppConfig
        {
            HttpEnabled = true,
            HttpPort = 9090,
            CheckedFields = new List<string>()
        });
        var httpMock = CreateHttpMock(false);

        _ = new MainViewModel(
            deviceMock.Object,
            Mock.Of<IExportService>(),
            configMock.Object,
            Mock.Of<IPasswordService>(),
            httpMock.Object);

        httpMock.Verify(x => x.Start(9090), Times.Once);
    }
}
