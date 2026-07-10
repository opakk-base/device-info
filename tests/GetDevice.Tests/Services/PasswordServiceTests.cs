using GetDevice.Models;
using GetDevice.Services;
using Moq;

namespace GetDevice.Tests.Services;

public class PasswordServiceTests
{
    private readonly Mock<IConfigService> _mockConfig;
    private readonly AppConfig _config;
    private readonly PasswordService _service;

    public PasswordServiceTests()
    {
        _config = new AppConfig
        {
            PasswordHash = ConfigService.ComputeSha256("12345678"),
            DeviceId = "test-id",
            CheckedFields = new List<string> { "hostname" }
        };

        _mockConfig = new Mock<IConfigService>();
        _mockConfig.Setup(c => c.Load()).Returns(_config);
        _mockConfig.Setup(c => c.Save(It.IsAny<AppConfig>()))
                   .Callback<AppConfig>(c => _config.PasswordHash = c.PasswordHash);

        _service = new PasswordService(_mockConfig.Object);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        Assert.True(_service.Verify("12345678"));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        Assert.False(_service.Verify("wrongpassword"));
    }

    [Fact]
    public void Change_WithCorrectPassword_UpdatesHash()
    {
        var result = _service.Change("12345678", "newpass123");

        Assert.True(result);
        Assert.True(_service.Verify("newpass123"));
        Assert.False(_service.Verify("12345678"));
    }

    [Fact]
    public void Change_WithWrongCurrentPassword_ReturnsFalse()
    {
        var result = _service.Change("wrong", "newpass123");

        Assert.False(result);
        Assert.True(_service.Verify("12345678"));
    }

    [Fact]
    public void IsDefaultPassword_WithDefault_ReturnsTrue()
    {
        Assert.True(_service.IsDefaultPassword());
    }

    [Fact]
    public void ResetToDefault_RestoresDefaultHash()
    {
        _service.Change("12345678", "newpass123");
        Assert.False(_service.IsDefaultPassword());

        _service.ResetToDefault();
        Assert.True(_service.IsDefaultPassword());
    }
}
