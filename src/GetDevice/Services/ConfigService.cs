using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GetDevice.Models;

namespace GetDevice.Services;

public class ConfigService : IConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _configDir;
    private readonly string _configPath;

    public ConfigService()
    {
        _configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GetDevice");
        _configPath = Path.Combine(_configDir, "appsettings.json");
    }

    public ConfigService(string customPath)
    {
        _configDir = Path.GetDirectoryName(customPath) ?? ".";
        _configPath = customPath;
    }

    public string ConfigDirectory => _configDir;
    public string ConfigFilePath => _configPath;

    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(_configPath))
                return CreateDefault();

            var json = File.ReadAllText(_configPath, Encoding.UTF8);
            var config = JsonSerializer.Deserialize<AppConfig>(json);

            if (config == null || string.IsNullOrEmpty(config.PasswordHash))
                return CreateDefault();

            return config;
        }
        catch
        {
            return CreateDefault();
        }
    }

    public void Save(AppConfig config)
    {
        if (!Directory.Exists(_configDir))
            Directory.CreateDirectory(_configDir);

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configPath, json, Encoding.UTF8);
    }

    private static AppConfig CreateDefault()
    {
        return new AppConfig
        {
            PasswordHash = ComputeSha256("12345678"),
            DeviceId = Guid.NewGuid().ToString("D"),
            ClientKey = "",
            HttpEnabled = false,
            HttpPort = 8080,
            CheckedFields = new List<string>
            {
                "device_id", "device_name", "hostname", "os",
                "arch", "mac_address", "ip_address", "client_key", "timestamp"
            }
        };
    }

    internal static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
