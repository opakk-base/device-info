using GetDevice.Models;
using GetDevice.Services;

namespace GetDevice.Tests.Services;

public class ConfigServiceTests
{
    [Fact]
    public void Load_WhenFileMissing_ReturnsDefaultConfig()
    {
        var tempPath = Path.GetTempFileName();
        File.Delete(tempPath);
        var service = new ConfigService(tempPath);

        var config = service.Load();

        Assert.NotNull(config);
        Assert.Equal(8080, config.HttpPort);
        Assert.False(config.HttpEnabled);
        Assert.NotEmpty(config.DeviceId);
        Assert.NotEmpty(config.PasswordHash);
        Assert.Equal(9, config.CheckedFields.Count);
    }

    [Fact]
    public void SaveAndLoad_RoundtripsCorrectly()
    {
        var tempPath = Path.GetTempFileName();
        File.Delete(tempPath);
        var service = new ConfigService(tempPath);

        var original = new AppConfig
        {
            PasswordHash = "abc123",
            DeviceId = "test-device-id",
            HttpEnabled = true,
            HttpPort = 9090,
            CheckedFields = new List<string> { "hostname", "os" }
        };

        service.Save(original);

        var loaded = service.Load();

        Assert.Equal(original.PasswordHash, loaded.PasswordHash);
        Assert.Equal(original.DeviceId, loaded.DeviceId);
        Assert.Equal(original.HttpEnabled, loaded.HttpEnabled);
        Assert.Equal(original.HttpPort, loaded.HttpPort);
        Assert.Equal(original.CheckedFields, loaded.CheckedFields);
    }

    [Fact]
    public void ComputeSha256_ReturnsExpectedHash()
    {
        var hash = ConfigService.ComputeSha256("12345678");
        Assert.Equal("ef797c8118f02dfb649607dd5d3f8c7623048c9c063d532cc95c5ed7a898a64d", hash);
    }
}
