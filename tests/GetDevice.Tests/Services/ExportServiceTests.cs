using System.IO;
using System.Text.Json;
using GetDevice.Models;
using GetDevice.Services;
using Moq;

namespace GetDevice.Tests.Services;

public class ExportServiceTests
{
    private readonly DeviceInfo _testInfo;

    public ExportServiceTests()
    {
        _testInfo = new DeviceInfo
        {
            DeviceId = "test-id",
            DeviceName = "PC-1",
            Hostname = "pc-1.local",
            Os = "Windows 11 Pro",
            Arch = "x64",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            IpAddress = "192.168.1.100",
            ClientKey = "client-key-hash",
            Timestamp = "2026-01-01T00:00:00Z"
        };
    }

    [Fact]
    public void ExportToJson_ReturnsValidJsonWithAllFields()
    {
        var mockDevice = new Mock<IDeviceInfoService>();
        var mockConfig = new Mock<IConfigService>();
        var service = new ExportService(mockDevice.Object, mockConfig.Object);

        var json = service.ExportToJson(_testInfo);
        var doc = JsonDocument.Parse(json);

        Assert.Equal(9, doc.RootElement.EnumerateObject().Count());
        Assert.Equal("test-id", doc.RootElement.GetProperty("device_id").GetString());
        Assert.Equal("192.168.1.100", doc.RootElement.GetProperty("ip_address").GetString());
    }

    [Fact]
    public void ExportToJson_FiltersFields()
    {
        var mockDevice = new Mock<IDeviceInfoService>();
        var mockConfig = new Mock<IConfigService>();
        var service = new ExportService(mockDevice.Object, mockConfig.Object);

        var json = service.ExportToJson(_testInfo, new[] { "hostname", "os" });
        var doc = JsonDocument.Parse(json);

        Assert.Equal(2, doc.RootElement.EnumerateObject().Count());
        Assert.True(doc.RootElement.TryGetProperty("hostname", out _));
        Assert.True(doc.RootElement.TryGetProperty("os", out _));
    }

    [Fact]
    public async Task ExportToFileAsync_WritesJsonFile()
    {
        var mockDevice = new Mock<IDeviceInfoService>();
        var mockConfig = new Mock<IConfigService>();
        var service = new ExportService(mockDevice.Object, mockConfig.Object);

        var tempFile = Path.GetTempFileName();
        try
        {
            await service.ExportToFileAsync(_testInfo, tempFile);
            var content = await File.ReadAllTextAsync(tempFile);
            var doc = JsonDocument.Parse(content);
            Assert.Equal("PC-1", doc.RootElement.GetProperty("device_name").GetString());
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ExportToJsonFromCurrent_UsesCheckedFields()
    {
        var mockDevice = new Mock<IDeviceInfoService>();
        mockDevice.Setup(d => d.Gather()).Returns(_testInfo);

        var config = new AppConfig
        {
            CheckedFields = new List<string> { "device_name", "ip_address" }
        };
        var mockConfig = new Mock<IConfigService>();
        mockConfig.Setup(c => c.Load()).Returns(config);

        var service = new ExportService(mockDevice.Object, mockConfig.Object);
        var json = service.ExportToJsonFromCurrent();
        var doc = JsonDocument.Parse(json);

        Assert.Equal(2, doc.RootElement.EnumerateObject().Count());
    }
}
