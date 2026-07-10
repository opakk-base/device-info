using GetDevice.Models;
using GetDevice.Services;
using Moq;

namespace GetDevice.Tests.Services;

public class DeviceInfoServiceTests
{
    [Fact]
    public void Gather_ReturnsDeviceInfoWithAllFields()
    {
        var config = new AppConfig
        {
            PasswordHash = ConfigService.ComputeSha256("12345678"),
            DeviceId = "",
            ClientKey = "",
            CheckedFields = new List<string> { "hostname" }
        };

        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(config);
        mockConfig.Setup(c => c.Save(It.IsAny<AppConfig>()));

        var service = new DeviceInfoService(mockConfig.Object);
        var info = service.Gather();

        Assert.NotNull(info);
        Assert.NotEmpty(info.DeviceId);
        Assert.NotEmpty(info.ClientKey);
        Assert.NotEmpty(info.Hostname);
        Assert.NotEmpty(info.Os);
        Assert.NotEmpty(info.Arch);
        Assert.NotEmpty(info.MacAddress);
        Assert.NotEmpty(info.IpAddress);
        Assert.NotEmpty(info.Timestamp);
    }

    [Fact]
    public void Gather_ReusesExistingDeviceId()
    {
        var existingId = "existing-device-id";
        var config = new AppConfig
        {
            PasswordHash = ConfigService.ComputeSha256("12345678"),
            DeviceId = existingId,
            ClientKey = "existing-key",
            CheckedFields = new List<string> { "hostname" }
        };

        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(config);

        var service = new DeviceInfoService(mockConfig.Object);
        var info = service.Gather();

        Assert.Equal(existingId, info.DeviceId);
    }

    [Fact]
    public void ToDictionary_FiltersByFieldList()
    {
        var info = new DeviceInfo
        {
            DeviceId = "id1",
            Hostname = "pc1",
            Os = "Windows 11",
            IpAddress = "1.2.3.4"
        };

        var dict = info.ToDictionary(new[] { "hostname", "os" });

        Assert.Equal(2, dict.Count);
        Assert.True(dict.ContainsKey("hostname"));
        Assert.True(dict.ContainsKey("os"));
        Assert.False(dict.ContainsKey("device_id"));
        Assert.False(dict.ContainsKey("ip_address"));
    }

    [Fact]
    public void ToDictionary_WithNullFields_ReturnsAll()
    {
        var info = new DeviceInfo
        {
            DeviceId = "id1",
            Hostname = "pc1"
        };

        var dict = info.ToDictionary(null);

        Assert.Equal(9, dict.Count);
    }
}
