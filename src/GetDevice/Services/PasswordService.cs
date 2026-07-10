using GetDevice.Models;

namespace GetDevice.Services;

public class PasswordService : IPasswordService
{
    private readonly IConfigService _configService;
    private AppConfig _config;

    public PasswordService(IConfigService configService)
    {
        _configService = configService;
        _config = configService.Load();
    }

    public bool Verify(string password)
    {
        var hash = ConfigService.ComputeSha256(password);
        return string.Equals(hash, _config.PasswordHash, StringComparison.OrdinalIgnoreCase);
    }

    public bool Change(string currentPassword, string newPassword)
    {
        if (!Verify(currentPassword))
            return false;

        _config.PasswordHash = ConfigService.ComputeSha256(newPassword);
        _configService.Save(_config);
        return true;
    }

    public bool IsDefaultPassword()
    {
        var defaultHash = ConfigService.ComputeSha256("12345678");
        return string.Equals(_config.PasswordHash, defaultHash, StringComparison.OrdinalIgnoreCase);
    }

    public void ResetToDefault()
    {
        _config.PasswordHash = ConfigService.ComputeSha256("12345678");
        _configService.Save(_config);
    }
}
