# GetDevice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a WPF desktop app that collects Windows device info, provides a checklist to filter JSON export, protects settings with a password, and runs an optional HTTP endpoint. Packaged as MSI.

**Architecture:** .NET 8 WPF with MVVM. Services layer handles device data (WMI), password hashing (SHA-256), JSON export, and HTTP (HttpListener). Three windows: Login, Main, Settings. System tray for minimize.

**Tech Stack:** .NET 8, WPF, MVVM, System.Management (WMI), System.Text.Json, WiX Toolset v4, xUnit + Moq for tests.

## Global Constraints

- .NET 8-only, no .NET Framework
- No external HTTP libraries — use `System.Net.HttpListener`
- No external JSON libraries — use `System.Text.Json`
- Password hashed with SHA-256
- Config stored at `%LOCALAPPDATA%\GetDevice\appsettings.json`
- Target: Windows x64 only
- Default password: `12345678`

---

### Task 1: Project Scaffolding

**Files:**
- Create: `GetDevice.sln`
- Create: `src/GetDevice/GetDevice.csproj`
- Create: `tests/GetDevice.Tests/GetDevice.Tests.csproj`
- Create: `src/GetDevice/Models/.gitkeep`
- Create: `src/GetDevice/Services/.gitkeep`
- Create: `src/GetDevice/ViewModels/.gitkeep`
- Create: `src/GetDevice/Views/.gitkeep`
- Create: `src/GetDevice/Converters/.gitkeep`

- [ ] **Step 1: Create solution and projects**

```bash
mkdir -p src/GetDevice/Models src/GetDevice/Services src/GetDevice/ViewModels src/GetDevice/Views src/GetDevice/Converters
mkdir -p tests/GetDevice.Tests
mkdir -p installer
```

- [ ] **Step 2: Create `src/GetDevice/GetDevice.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PlatformTarget>x64</PlatformTarget>
    <ApplicationIcon>Resources\icon.ico</ApplicationIcon>
    <AssemblyName>GetDevice</AssemblyName>
    <RootNamespace>GetDevice</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Management" Version="8.0.0" />
    <PackageReference Include="Hardware.Info" Version="99.0.0" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="appsettings.json" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create `tests/GetDevice.Tests/GetDevice.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <RootNamespace>GetDevice.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="xunit" Version="2.7.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7" />
    <PackageReference Include="Moq" Version="4.20.70" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\GetDevice\GetDevice.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Create default `src/GetDevice/appsettings.json`**

```json
{
  "password_hash": "ef797c8118f02dfb649607dd5d3f8c7623048c9c063d532cc95c5ed7a898a64d",
  "device_id": "",
  "client_key": "",
  "http_enabled": false,
  "http_port": 8080,
  "checked_fields": [
    "device_id", "device_name", "hostname", "os",
    "arch", "mac_address", "ip_address", "client_key", "timestamp"
  ]
}
```

- [ ] **Step 5: Create `src/GetDevice/Resources/` with placeholder icon**

```bash
mkdir -p src/GetDevice/Resources
```

(icon.ico can be generated or user provides one)

---

### Task 2: Models + ConfigService

**Files:**
- Create: `src/GetDevice/Models/DeviceInfo.cs`
- Create: `src/GetDevice/Models/AppConfig.cs`
- Create: `src/GetDevice/Services/ConfigService.cs`
- Create: `tests/GetDevice.Tests/Services/ConfigServiceTests.cs`

**Interfaces:**
- Consumes: nothing
- Produces: `DeviceInfo` (DTO), `AppConfig` (serializable), `IConfigService`

- [ ] **Step 1: Create `DeviceInfo.cs`**

```csharp
using System.Text.Json.Serialization;

namespace GetDevice.Models;

public class DeviceInfo
{
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("device_name")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("client_key")]
    public string ClientKey { get; set; } = string.Empty;

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;

    [JsonPropertyName("os")]
    public string Os { get; set; } = string.Empty;

    [JsonPropertyName("arch")]
    public string Arch { get; set; } = string.Empty;

    [JsonPropertyName("mac_address")]
    public string MacAddress { get; set; } = string.Empty;

    [JsonPropertyName("ip_address")]
    public string IpAddress { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    public Dictionary<string, object?> ToDictionary(IEnumerable<string>? onlyFields = null)
    {
        var all = new Dictionary<string, object?>
        {
            ["device_id"] = DeviceId,
            ["device_name"] = DeviceName,
            ["client_key"] = ClientKey,
            ["hostname"] = Hostname,
            ["os"] = Os,
            ["arch"] = Arch,
            ["mac_address"] = MacAddress,
            ["ip_address"] = IpAddress,
            ["timestamp"] = Timestamp,
        };

        if (onlyFields == null)
            return all;

        return all.Where(kv => onlyFields.Contains(kv.Key))
                  .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
```

- [ ] **Step 2: Create `AppConfig.cs`**

```csharp
using System.Text.Json.Serialization;

namespace GetDevice.Models;

public class AppConfig
{
    [JsonPropertyName("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("client_key")]
    public string ClientKey { get; set; } = string.Empty;

    [JsonPropertyName("http_enabled")]
    public bool HttpEnabled { get; set; }

    [JsonPropertyName("http_port")]
    public int HttpPort { get; set; } = 8080;

    [JsonPropertyName("checked_fields")]
    public List<string> CheckedFields { get; set; } = new();
}
```

- [ ] **Step 3: Create `IConfigService` and `ConfigService.cs`**

```csharp
using GetDevice.Models;

namespace GetDevice.Services;

public interface IConfigService
{
    AppConfig Load();
    void Save(AppConfig config);
    string ConfigDirectory { get; }
    string ConfigFilePath { get; }
}
```

```csharp
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

    private AppConfig CreateDefault()
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

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
```

Wait — SHA256 is in `System.Security.Cryptography`. Need the right using.

Let me fix that. `SHA256.HashData` is in `System.Security.Cryptography`.

Fixed:

```csharp
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
```

- [ ] **Step 4: Write tests**

```csharp
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
        // Known SHA-256 of "12345678"
        Assert.Equal("ef797c8118f02dfb649607dd5d3f8c7623048c9c063d532cc95c5ed7a898a64d", hash);
    }
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test tests/GetDevice.Tests/GetDevice.Tests.csproj
```

Expected: 3 tests pass

---

### Task 3: PasswordService

**Files:**
- Create: `src/GetDevice/Services/PasswordService.cs`
- Create: `tests/GetDevice.Tests/Services/PasswordServiceTests.cs`

**Interfaces:**
- Consumes: `IConfigService` (from Task 2)
- Produces: `IPasswordService`

- [ ] **Step 1: Create `IPasswordService` and `PasswordService.cs`**

```csharp
using GetDevice.Models;

namespace GetDevice.Services;

public interface IPasswordService
{
    bool Verify(string password);
    bool Change(string currentPassword, string newPassword);
    bool IsDefaultPassword();
    void ResetToDefault();
}
```

```csharp
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
```

- [ ] **Step 2: Write tests**

```csharp
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
```

- [ ] **Step 3: Run tests**

```bash
dotnet test tests/GetDevice.Tests/GetDevice.Tests.csproj --filter "FullyQualifiedName~PasswordServiceTests"
```

Expected: 6 tests pass

---

### Task 4: DeviceInfoService

**Files:**
- Create: `src/GetDevice/Services/DeviceInfoService.cs`
- Create: `tests/GetDevice.Tests/Services/DeviceInfoServiceTests.cs`

**Interfaces:**
- Consumes: `IConfigService` (for device_id and client_key)
- Produces: `IDeviceInfoService`

- [ ] **Step 1: Create `IDeviceInfoService` and `DeviceInfoService.cs`**

```csharp
using GetDevice.Models;

namespace GetDevice.Services;

public interface IDeviceInfoService
{
    DeviceInfo Gather();
}
```

```csharp
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using GetDevice.Models;

namespace GetDevice.Services;

public class DeviceInfoService : IDeviceInfoService
{
    private readonly IConfigService _configService;

    public DeviceInfoService(IConfigService configService)
    {
        _configService = configService;
    }

    public DeviceInfo Gather()
    {
        var config = _configService.Load();
        var now = DateTime.UtcNow;

        // Ensure device_id exists
        if (string.IsNullOrEmpty(config.DeviceId))
        {
            config.DeviceId = Guid.NewGuid().ToString("D");
        }

        // Ensure client_key exists
        if (string.IsNullOrEmpty(config.ClientKey))
        {
            config.ClientKey = GenerateClientKey(config.DeviceId);
        }

        _configService.Save(config);

        var (mac, ip) = GetPrimaryNetworkInfo();

        return new DeviceInfo
        {
            DeviceId = config.DeviceId,
            DeviceName = Environment.MachineName,
            ClientKey = config.ClientKey,
            Hostname = Dns.GetHostName(),
            Os = GetOsVersionString(),
            Arch = Environment.Is64BitOperatingSystem ? "x64" : "ARM64",
            MacAddress = mac,
            IpAddress = ip,
            Timestamp = now.ToString("o")
        };
    }

    private static string GetOsVersionString()
    {
        var os = Environment.OSVersion;
        var version = os.VersionString;
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key != null)
            {
                var productName = key.GetValue("ProductName")?.ToString() ?? "";
                var displayVersion = key.GetValue("DisplayVersion")?.ToString() ?? "";
                if (!string.IsNullOrEmpty(productName))
                {
                    version = string.IsNullOrEmpty(displayVersion)
                        ? productName
                        : $"{productName} {displayVersion}";
                }
            }
        }
        catch { }

        return version;
    }

    private static (string mac, string ip) GetPrimaryNetworkInfo()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up
                          && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToList();

            foreach (var ni in interfaces)
            {
                var ipProps = ni.GetIPProperties();
                var ipv4 = ipProps.UnicastAddresses
                    .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork
                                      && !IPAddress.IsLoopback(u.Address));

                if (ipv4 != null)
                {
                    var macStr = string.Join(":",
                        ni.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
                    return (macStr, ipv4.Address.ToString());
                }
            }
        }
        catch { }

        return ("00:00:00:00:00:00", "0.0.0.0");
    }

    private static string GenerateClientKey(string deviceId)
    {
        var machineSid = GetMachineSid();
        var input = $"{deviceId}:{machineSid}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static string GetMachineSid()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Cryptography");
            if (key?.GetValue("MachineGuid") is string guid)
                return guid;
        }
        catch { }

        return "UNKNOWN";
    }
}
```

- [ ] **Step 2: Write tests**

```csharp
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
```

- [ ] **Step 3: Run tests**

```bash
dotnet test tests/GetDevice.Tests/GetDevice.Tests.csproj --filter "FullyQualifiedName~DeviceInfoServiceTests"
```

Expected: 4 tests pass

---

### Task 5: ExportService

**Files:**
- Create: `src/GetDevice/Services/ExportService.cs`
- Create: `tests/GetDevice.Tests/Services/ExportServiceTests.cs`

**Interfaces:**
- Consumes: `IDeviceInfoService`, `IConfigService`
- Produces: `IExportService`

- [ ] **Step 1: Create `IExportService` and `ExportService.cs`**

```csharp
using System.Text.Json;
using GetDevice.Models;

namespace GetDevice.Services;

public interface IExportService
{
    string ExportToJson(DeviceInfo info, IEnumerable<string>? onlyFields = null);
    Task ExportToFileAsync(DeviceInfo info, string filePath, IEnumerable<string>? onlyFields = null);
    string ExportToJsonFromCurrent();
}
```

```csharp
using System.Text.Json;
using GetDevice.Models;

namespace GetDevice.Services;

public class ExportService : IExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly IDeviceInfoService _deviceInfoService;
    private readonly IConfigService _configService;

    public ExportService(IDeviceInfoService deviceInfoService, IConfigService configService)
    {
        _deviceInfoService = deviceInfoService;
        _configService = configService;
    }

    public string ExportToJson(DeviceInfo info, IEnumerable<string>? onlyFields = null)
    {
        var dict = info.ToDictionary(onlyFields);
        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    public async Task ExportToFileAsync(DeviceInfo info, string filePath, IEnumerable<string>? onlyFields = null)
    {
        var json = ExportToJson(info, onlyFields);
        await File.WriteAllTextAsync(filePath, json);
    }

    public string ExportToJsonFromCurrent()
    {
        var config = _configService.Load();
        var info = _deviceInfoService.Gather();
        return ExportToJson(info, config.CheckedFields);
    }
}
```

- [ ] **Step 2: Write tests**

```csharp
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
```

- [ ] **Step 3: Run tests**

```bash
dotnet test tests/GetDevice.Tests/GetDevice.Tests.csproj --filter "FullyQualifiedName~ExportServiceTests"
```

Expected: 4 tests pass

---

### Task 6: HttpServerService

**Files:**
- Create: `src/GetDevice/Services/HttpServerService.cs`
- Create: `tests/GetDevice.Tests/Services/HttpServerServiceTests.cs`

**Interfaces:**
- Consumes: `IExportService`
- Produces: `IHttpServerService`

- [ ] **Step 1: Create `IHttpServerService` and `HttpServerService.cs`**

```csharp
namespace GetDevice.Services;

public interface IHttpServerService
{
    bool IsRunning { get; }
    event EventHandler<bool>? RunningChanged;
    void Start(int port = 8080);
    void Stop();
}
```

```csharp
using System.Net;
using System.Text;

namespace GetDevice.Services;

public class HttpServerService : IHttpServerService, IDisposable
{
    private readonly IExportService _exportService;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;

    public bool IsRunning => _listener?.IsListening ?? false;

    public event EventHandler<bool>? RunningChanged;

    public HttpServerService(IExportService exportService)
    {
        _exportService = exportService;
    }

    public void Start(int port = 8080)
    {
        if (IsRunning)
            return;

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();

        _cts = new CancellationTokenSource();
        _listenTask = Task.Run(() => ListenLoop(_cts.Token));

        RunningChanged?.Invoke(this, true);
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _listener?.Stop(); } catch { }
        _listener?.Close();
        _listener = null;

        RunningChanged?.Invoke(this, false);
    }

    private async Task ListenLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(token);
                _ = Task.Run(() => HandleRequest(context), token);
            }
            catch (OperationCanceledException) { break; }
            catch (HttpListenerException) { break; }
            catch (ObjectDisposedException) { break; }
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            if (request.Url?.AbsolutePath == "/health")
            {
                RespondJson(response, """{"status":"ok"}""");
            }
            else if (request.Url?.AbsolutePath == "/getdevice")
            {
                var json = _exportService.ExportToJsonFromCurrent();
                RespondJson(response, json);
            }
            else
            {
                response.StatusCode = 404;
                RespondJson(response, """{"error":"not found"}""");
            }
        }
        catch
        {
            try { context.Response.StatusCode = 500; } catch { }
        }
    }

    private static void RespondJson(HttpListenerResponse response, string json)
    {
        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json";
        response.ContentEncoding = Encoding.UTF8;
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
```

- [ ] **Step 2: Write tests**

```csharp
using System.Net;
using System.Text;
using GetDevice.Services;
using Moq;

namespace GetDevice.Tests.Services;

public class HttpServerServiceTests : IDisposable
{
    private readonly Mock<IExportService> _mockExport;
    private readonly HttpServerService _service;

    public HttpServerServiceTests()
    {
        _mockExport = new Mock<IExportService>();
        _mockExport.Setup(e => e.ExportToJsonFromCurrent())
                   .Returns("""{"hostname":"test-pc","os":"Windows 11"}""");

        _service = new HttpServerService(_mockExport.Object);
    }

    [Fact]
    public void StartAndStop_ChangesRunningState()
    {
        Assert.False(_service.IsRunning);

        _service.Start(9191);
        Thread.Sleep(200); // Let listener start
        Assert.True(_service.IsRunning);

        _service.Stop();
        Thread.Sleep(200);
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        _service.Start(9192);
        Thread.Sleep(200);

        using var client = new HttpClient();
        var response = await client.GetStringAsync("http://localhost:9192/health");

        Assert.Equal("""{"status":"ok"}""", response);

        _service.Stop();
    }

    [Fact]
    public async Task GetDevice_ReturnsExportJson()
    {
        _service.Start(9193);
        Thread.Sleep(200);

        using var client = new HttpClient();
        var response = await client.GetStringAsync("http://localhost:9193/getdevice");

        Assert.Contains("hostname", response);
        Assert.Contains("test-pc", response);

        _service.Stop();
    }

    [Fact]
    public async Task UnknownPath_Returns404()
    {
        _service.Start(9194);
        Thread.Sleep(200);

        using var client = new HttpClient();
        var response = await client.GetStringAsync("http://localhost:9194/unknown");

        Assert.Contains("not found", response);

        _service.Stop();
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}
```

- [ ] **Step 3: Run tests**

```bash
dotnet test tests/GetDevice.Tests/GetDevice.Tests.csproj --filter "FullyQualifiedName~HttpServerServiceTests"
```

Expected: 4 tests pass

---

### Task 7: MVVM Infrastructure (BaseViewModel + Converters)

**Files:**
- Create: `src/GetDevice/ViewModels/BaseViewModel.cs`
- Create: `src/GetDevice/ViewModels/RelayCommand.cs`
- Create: `src/GetDevice/Converters/BoolToVisibilityConverter.cs`

**Interfaces:**
- Consumes: nothing
- Produces: `BaseViewModel`, `RelayCommand`, `BoolToVisibilityConverter`

- [ ] **Step 1: Create `BaseViewModel.cs`**

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GetDevice.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

- [ ] **Step 2: Create `RelayCommand.cs`**

```csharp
using System.Windows.Input;

namespace GetDevice.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is not null ? _ => canExecute() : null) { }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);
}
```

- [ ] **Step 3: Create `BoolToVisibilityConverter.cs`**

```csharp
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GetDevice.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
            return v == Visibility.Visible;
        return false;
    }
}

public class InvertedBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
            return v != Visibility.Visible;
        return false;
    }
}
```

---

### Task 8: LoginWindow

**Files:**
- Create: `src/GetDevice/ViewModels/LoginViewModel.cs`
- Create: `src/GetDevice/Views/LoginWindow.xaml`
- Create: `src/GetDevice/Views/LoginWindow.xaml.cs`
- Create: `tests/GetDevice.Tests/ViewModels/LoginViewModelTests.cs`

**Interfaces:**
- Consumes: `IPasswordService`
- Produces: `LoginViewModel` (with event/action on success)

- [ ] **Step 1: Create `LoginViewModel.cs`**

```csharp
using System.Windows.Input;
using GetDevice.Services;

namespace GetDevice.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IPasswordService _passwordService;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isFirstLaunch;

    public event Action? LoginSucceeded;

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsFirstLaunch
    {
        get => _isFirstLaunch;
        set => SetProperty(ref _isFirstLaunch, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand SkipChangePasswordCommand { get; }

    public LoginViewModel(IPasswordService passwordService)
    {
        _passwordService = passwordService;
        LoginCommand = new RelayCommand(ExecuteLogin);
        SkipChangePasswordCommand = new RelayCommand(_ => LoginSucceeded?.Invoke());
        IsFirstLaunch = _passwordService.IsDefaultPassword();
    }

    private void ExecuteLogin(object? parameter)
    {
        if (string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Please enter a password.";
            return;
        }

        if (_passwordService.Verify(Password))
        {
            ErrorMessage = string.Empty;
            LoginSucceeded?.Invoke();
        }
        else
        {
            ErrorMessage = "Incorrect password.";
        }
    }
}
```

- [ ] **Step 2: Create `LoginWindow.xaml`**

```xml
<Window x:Class="GetDevice.Views.LoginWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:GetDevice.ViewModels"
        Title="GetDevice - Login"
        Width="380" Height="280"
        WindowStartupLocation="CenterScreen"
        ResizeMode="NoResize"
        WindowStyle="ToolWindow">
    <Window.Resources>
        <vm:LoginViewModel x:Key="DesignViewModel" />
    </Window.Resources>
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Text="GetDevice" FontSize="24" FontWeight="Bold"
                   HorizontalAlignment="Center" Margin="0,0,0,10"/>

        <TextBlock Grid.Row="1" Text="Enter password to access device settings"
                   HorizontalAlignment="Center" Margin="0,0,0,15"/>

        <StackPanel Grid.Row="2" Margin="0,0,0,10">
            <TextBlock Text="Password:" Margin="0,0,0,5"/>
            <PasswordBox x:Name="PasswordBox"
                         KeyDown="PasswordBox_KeyDown"
                         Height="30"
                         FontSize="14"/>
        </StackPanel>

        <Button Grid.Row="3" Content="Unlock"
                Command="{Binding LoginCommand}"
                CommandParameter="{Binding ElementName=PasswordBox, Path=Password}"
                Height="35"
                FontSize="14"
                Margin="0,0,0,10"/>

        <TextBlock Grid.Row="4" Text="{Binding ErrorMessage}"
                   Foreground="Red"
                   HorizontalAlignment="Center"
                   Visibility="{Binding ErrorMessage, Converter={StaticResource StringNotEmptyConverter}}"/>

        <TextBlock Grid.Row="5" Text="Forgot password? Delete appsettings.json in %LOCALAPPDATA%\GetDevice"
                   Foreground="Gray" FontSize="11"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Bottom"
                   TextWrapping="Wrap"/>
    </Grid>
</Window>
```

- [ ] **Step 3: Create `LoginWindow.xaml.cs`**

```csharp
using System.Windows;
using System.Windows.Controls;
using GetDevice.ViewModels;

namespace GetDevice.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.LoginSucceeded += OnLoginSucceeded;

        if (viewModel.IsFirstLaunch)
        {
            Title = "GetDevice - Change Default Password";
        }
    }

    private void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && sender is PasswordBox pb)
        {
            _viewModel.Password = pb.Password;
            if (_viewModel.LoginCommand.CanExecute(pb.Password))
                _viewModel.LoginCommand.Execute(pb.Password);
        }
    }

    private void OnLoginSucceeded()
    {
        _viewModel.LoginSucceeded -= OnLoginSucceeded;
        DialogResult = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _viewModel.LoginSucceeded -= OnLoginSucceeded;
    }
}
```

- [ ] **Step 4: Write tests**

```csharp
using GetDevice.Services;
using GetDevice.ViewModels;
using Moq;

namespace GetDevice.Tests.ViewModels;

public class LoginViewModelTests
{
    [Fact]
    public void Login_WithCorrectPassword_FiresLoginSucceeded()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.Verify("correct")).Returns(true);
        mockPassword.Setup(p => p.IsDefaultPassword()).Returns(false);

        var vm = new LoginViewModel(mockPassword.Object);
        var succeeded = false;
        vm.LoginSucceeded += () => succeeded = true;

        vm.Password = "correct";
        vm.LoginCommand.Execute(null);

        Assert.True(succeeded);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void Login_WithWrongPassword_ShowsError()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.Verify("wrong")).Returns(false);
        mockPassword.Setup(p => p.IsDefaultPassword()).Returns(false);

        var vm = new LoginViewModel(mockPassword.Object);
        var succeeded = false;
        vm.LoginSucceeded += () => succeeded = true;

        vm.Password = "wrong";
        vm.LoginCommand.Execute(null);

        Assert.False(succeeded);
        Assert.NotEmpty(vm.ErrorMessage);
    }

    [Fact]
    public void Login_WithEmptyPassword_ShowsError()
    {
        var mockPassword = new Mock<IPasswordService>();

        var vm = new LoginViewModel(mockPassword.Object);

        vm.Password = "";
        vm.LoginCommand.Execute(null);

        Assert.NotEmpty(vm.ErrorMessage);
    }

    [Fact]
    public void IsFirstLaunch_WhenDefaultPassword_ReturnsTrue()
    {
        var mockPassword = new Mock<IPasswordService>();
        mockPassword.Setup(p => p.IsDefaultPassword()).Returns(true);

        var vm = new LoginViewModel(mockPassword.Object);

        Assert.True(vm.IsFirstLaunch);
    }
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test tests/GetDevice.Tests/GetDevice.Tests.csproj --filter "FullyQualifiedName~LoginViewModelTests"
```

Expected: 4 tests pass

---

### Task 9: MainWindow

**Files:**
- Create: `src/GetDevice/ViewModels/MainViewModel.cs`
- Create: `src/GetDevice/Views/MainWindow.xaml`
- Create: `src/GetDevice/Views/MainWindow.xaml.cs`
- Create: `tests/GetDevice.Tests/ViewModels/MainViewModelTests.cs`

**Interfaces:**
- Consumes: `IDeviceInfoService`, `IExportService`, `IConfigService`
- Produces: `MainViewModel`

- [ ] **Step 1: Create `MainViewModel.cs`**

```csharp
using System.Collections.ObjectModel;
using System.Windows.Input;
using GetDevice.Models;
using GetDevice.Services;

namespace GetDevice.ViewModels;

public class DeviceFieldItem : BaseViewModel
{
    private bool _isChecked;

    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }
}

public class MainViewModel : BaseViewModel
{
    private readonly IDeviceInfoService _deviceInfoService;
    private readonly IExportService _exportService;
    private readonly IConfigService _configService;
    private readonly IPasswordService _passwordService;

    private DeviceInfo? _deviceInfo;
    private string _statusText = string.Empty;

    public ObservableCollection<DeviceFieldItem> Fields { get; } = new();
    public ICommand ExportCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand RefreshCommand { get; }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public event Action? OpenSettingsRequested;

    public MainViewModel(
        IDeviceInfoService deviceInfoService,
        IExportService exportService,
        IConfigService configService,
        IPasswordService passwordService)
    {
        _deviceInfoService = deviceInfoService;
        _exportService = exportService;
        _configService = configService;
        _passwordService = passwordService;

        ExportCommand = new RelayCommand(ExecuteExport);
        SelectAllCommand = new RelayCommand(_ => SetAllChecked(true));
        SelectNoneCommand = new RelayCommand(_ => SetAllChecked(false));
        OpenSettingsCommand = new RelayCommand(_ => OpenSettingsRequested?.Invoke());
        RefreshCommand = new RelayCommand(_ => LoadDeviceInfo());

        LoadDeviceInfo();
    }

    public void LoadDeviceInfo()
    {
        _deviceInfo = _deviceInfoService.Gather();
        var config = _configService.Load();

        var fieldDefs = new Dictionary<string, (string Label, Func<DeviceInfo, string>)>
        {
            ["device_id"] = ("Device ID", i => i.DeviceId),
            ["device_name"] = ("Device Name", i => i.DeviceName),
            ["client_key"] = ("Client Key", i => i.ClientKey),
            ["hostname"] = ("Hostname", i => i.Hostname),
            ["os"] = ("OS", i => i.Os),
            ["arch"] = ("Architecture", i => i.Arch),
            ["mac_address"] = ("MAC Address", i => i.MacAddress),
            ["ip_address"] = ("IP Address", i => i.IpAddress),
            ["timestamp"] = ("Timestamp", i => i.Timestamp),
        };

        Fields.Clear();
        foreach (var (key, (label, valueSelector)) in fieldDefs)
        {
            Fields.Add(new DeviceFieldItem
            {
                Key = key,
                Label = label,
                Value = valueSelector(_deviceInfo),
                IsChecked = config.CheckedFields.Contains(key)
            });
        }
    }

    private void SetAllChecked(bool isChecked)
    {
        foreach (var field in Fields)
            field.IsChecked = isChecked;
    }

    private async void ExecuteExport(object? parameter)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Device Info",
            Filter = "JSON files (*.json)|*.json",
            DefaultExt = "json",
            FileName = "GetDevice-export.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        var checkedFields = Fields.Where(f => f.IsChecked).Select(f => f.Key).ToList();

        var config = _configService.Load();
        config.CheckedFields = checkedFields;
        _configService.Save(config);

        if (_deviceInfo == null)
            _deviceInfo = _deviceInfoService.Gather();

        await _exportService.ExportToFileAsync(_deviceInfo, dialog.FileName, checkedFields);

        StatusText = $"Exported to {dialog.FileName}";
    }
}
```

- [ ] **Step 2: Create `MainWindow.xaml`**

```xml
<Window x:Class="GetDevice.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:cvt="clr-namespace:GetDevice.Converters"
        Title="GetDevice - Device Information"
        Width="600" Height="500"
        WindowStartupLocation="CenterScreen">
    <Window.Resources>
        <cvt:BoolToVisibilityConverter x:Key="BoolToVis"/>
    </Window.Resources>
    <DockPanel Margin="10">
        <!-- Toolbar -->
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,10">
            <Button Content="Refresh" Command="{Binding RefreshCommand}" Width="80" Margin="0,0,5,0"/>
            <Button Content="Select All" Command="{Binding SelectAllCommand}" Width="80" Margin="0,0,5,0"/>
            <Button Content="Select None" Command="{Binding SelectNoneCommand}" Width="80" Margin="0,0,5,0"/>
            <Button Content="Settings" Command="{Binding OpenSettingsCommand}" Width="80" Margin="0,0,5,0"/>
            <Button Content="Export JSON" Command="{Binding ExportCommand}" Width="100" FontWeight="Bold"/>
        </StackPanel>

        <!-- Device Info List -->
        <ListBox ItemsSource="{Binding Fields}"
                 ScrollViewer.HorizontalScrollBarVisibility="Disabled">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <Grid Margin="0,2">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="150"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <CheckBox Grid.Column="0"
                                  IsChecked="{Binding IsChecked}"
                                  VerticalAlignment="Center"
                                  Margin="0,0,8,0"/>
                        <TextBlock Grid.Column="1"
                                   Text="{Binding Label}"
                                   FontWeight="SemiBold"
                                   VerticalAlignment="Center"/>
                        <TextBlock Grid.Column="2"
                                   Text="{Binding Value}"
                                   TextWrapping="Wrap"
                                   VerticalAlignment="Center"/>
                    </Grid>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>

        <!-- Status Bar -->
        <StatusBar DockPanel.Dock="Bottom" Margin="0,10,0,0">
            <TextBlock Text="{Binding StatusText}" />
        </StatusBar>
    </DockPanel>
</Window>
```

- [ ] **Step 3: Create `MainWindow.xaml.cs`**

```csharp
using System.Windows;
using GetDevice.ViewModels;

namespace GetDevice.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.OpenSettingsRequested += () =>
        {
            var settingsVm = App.ServiceProvider.GetService<SettingsViewModel>();
            if (settingsVm != null)
            {
                var settingsWindow = new SettingsWindow(settingsVm);
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();
            }
        };
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
            Hide();

        base.OnStateChanged(e);
    }
}
```

- [ ] **Step 4: Write tests**

```csharp
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
```

- [ ] **Step 5: Run tests**

```bash
dotnet test tests/GetDevice.Tests/GetDevice.Tests.csproj --filter "FullyQualifiedName~MainViewModelTests"
```

Expected: 2 tests pass

---

### Task 10: SettingsWindow

**Files:**
- Create: `src/GetDevice/ViewModels/SettingsViewModel.cs`
- Create: `src/GetDevice/Views/SettingsWindow.xaml`
- Create: `src/GetDevice/Views/SettingsWindow.xaml.cs`
- Create: `tests/GetDevice.Tests/ViewModels/SettingsViewModelTests.cs`

**Interfaces:**
- Consumes: `IPasswordService`, `IConfigService`, `IHttpServerService`
- Produces: `SettingsViewModel`

- [ ] **Step 1: Create `SettingsViewModel.cs`**

```csharp
using System.Windows.Input;
using GetDevice.Models;
using GetDevice.Services;

namespace GetDevice.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly IPasswordService _passwordService;
    private readonly IConfigService _configService;
    private readonly IHttpServerService _httpServerService;

    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _passwordMessage = string.Empty;
    private bool _passwordMessageIsError;
    private bool _isHttpEnabled;

    public string CurrentPassword
    {
        get => _currentPassword;
        set => SetProperty(ref _currentPassword, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public string PasswordMessage
    {
        get => _passwordMessage;
        set => SetProperty(ref _passwordMessage, value);
    }

    public bool PasswordMessageIsError
    {
        get => _passwordMessageIsError;
        set => SetProperty(ref _passwordMessageIsError, value);
    }

    public bool IsHttpEnabled
    {
        get => _isHttpEnabled;
        set
        {
            if (SetProperty(ref _isHttpEnabled, value))
            {
                var config = _configService.Load();
                config.HttpEnabled = value;
                _configService.Save(config);

                if (value)
                    _httpServerService.Start(config.HttpPort);
                else
                    _httpServerService.Stop();
            }
        }
    }

    public bool IsHttpRunning => _httpServerService.IsRunning;

    public ICommand ChangePasswordCommand { get; }
    public ICommand FactoryResetCommand { get; }
    public ICommand CloseCommand { get; }

    public event Action? CloseRequested;

    public SettingsViewModel(
        IPasswordService passwordService,
        IConfigService configService,
        IHttpServerService httpServerService)
    {
        _passwordService = passwordService;
        _configService = configService;
        _httpServerService = httpServerService;

        ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
        FactoryResetCommand = new RelayCommand(ExecuteFactoryReset);
        CloseCommand = new RelayCommand(_ => CloseRequested?.Invoke());

        var config = _configService.Load();
        _isHttpEnabled = config.HttpEnabled;
    }

    private void ExecuteChangePassword(object? parameter)
    {
        if (string.IsNullOrEmpty(CurrentPassword))
        {
            PasswordMessage = "Enter current password.";
            PasswordMessageIsError = true;
            return;
        }

        if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 4)
        {
            PasswordMessage = "New password must be at least 4 characters.";
            PasswordMessageIsError = true;
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            PasswordMessage = "Passwords do not match.";
            PasswordMessageIsError = true;
            return;
        }

        if (_passwordService.Change(CurrentPassword, NewPassword))
        {
            PasswordMessage = "Password changed successfully.";
            PasswordMessageIsError = false;
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }
        else
        {
            PasswordMessage = "Current password is incorrect.";
            PasswordMessageIsError = true;
        }
    }

    private void ExecuteFactoryReset(object? parameter)
    {
        _passwordService.ResetToDefault();
        PasswordMessage = "Password reset to default. Restart app to use '12345678'.";
        PasswordMessageIsError = false;
    }
}
```

- [ ] **Step 2: Create `SettingsWindow.xaml`**

```xml
<Window x:Class="GetDevice.Views.SettingsWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="GetDevice - Settings"
        Width="400" Height="400"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize"
        WindowStyle="ToolWindow">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="20" Grid.IsSharedSizeScope="True">

            <!-- Password Change Section -->
            <TextBlock Text="Change Password" FontSize="18" FontWeight="Bold" Margin="0,0,0,10"/>

            <Grid Margin="0,0,0,5">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="120" SharedSizeGroup="Labels"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>

                <TextBlock Grid.Row="0" Grid.Column="0" Text="Current:" VerticalAlignment="Center"/>
                <PasswordBox Grid.Row="0" Grid.Column="1" x:Name="CurrentPasswordBox" Height="28"/>

                <TextBlock Grid.Row="1" Grid.Column="0" Text="New:" VerticalAlignment="Center" Margin="0,5"/>
                <PasswordBox Grid.Row="1" Grid.Column="1" x:Name="NewPasswordBox" Height="28" Margin="0,5"/>

                <TextBlock Grid.Row="2" Grid.Column="0" Text="Confirm:" VerticalAlignment="Center"/>
                <PasswordBox Grid.Row="2" Grid.Column="1" x:Name="ConfirmPasswordBox" Height="28"/>
            </Grid>

            <Button Content="Change Password"
                    Command="{Binding ChangePasswordCommand}"
                    CommandParameter="{Binding ElementName=CurrentPasswordBox}"
                    Height="32"
                    Margin="0,5,0,10"/>

            <TextBlock Text="{Binding PasswordMessage}"
                       Foreground="{Binding PasswordMessageIsError, Converter={StaticResource BoolToErrorBrush}}"
                       Margin="0,0,0,10"
                       TextWrapping="Wrap"/>

            <Separator Margin="0,0,0,10"/>

            <!-- HTTP Server Section -->
            <TextBlock Text="HTTP Server" FontSize="18" FontWeight="Bold" Margin="0,0,0,10"/>

            <CheckBox Content="Enable HTTP endpoint (localhost:8080)"
                      IsChecked="{Binding IsHttpEnabled}"
                      Margin="0,0,0,5"/>

            <TextBlock Foreground="Gray" FontSize="11" TextWrapping="Wrap">
                When enabled, GET /getdevice on localhost:8080 returns filtered device info as JSON.
            </TextBlock>

            <Separator Margin="0,10"/>

            <!-- Factory Reset -->
            <TextBlock Text="Factory Reset" FontSize="18" FontWeight="Bold" Margin="0,0,0,10"/>
            <TextBlock Foreground="Gray" FontSize="11" TextWrapping="Wrap" Margin="0,0,0,5">
                Resets password to default '12345678'. Device ID and client key are preserved.
            </TextBlock>
            <Button Content="Reset to Default Password"
                    Command="{Binding FactoryResetCommand}"
                    Height="32"
                    Foreground="DarkRed"/>

            <Button Content="Close"
                    Command="{Binding CloseCommand}"
                    Height="32"
                    Margin="0,15,0,0"
                    HorizontalAlignment="Right"
                    Width="100"/>
        </StackPanel>
    </ScrollViewer>
</Window>
```

- [ ] **Step 3: Create `SettingsWindow.xaml.cs`**

```csharp
using System.Windows;
using System.Windows.Media;
using GetDevice.ViewModels;

namespace GetDevice.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Resources["BoolToErrorBrush"] = new ConverterErrorMessageConverter();

        viewModel.CloseRequested += () =>
        {
            DialogResult = true;
            Close();
        };
    }
}

public class ConverterErrorMessageConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isError && isError)
            return new SolidColorBrush(Colors.Red);
        return new SolidColorBrush(Colors.Green);
    }

    public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new System.NotImplementedException();
    }
}
```

- [ ] **Step 4: Write tests**

```csharp
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
```

- [ ] **Step 5: Run tests**

```bash
dotnet test tests/GetDevice.Tests/GetDevice.Tests.csproj --filter "FullyQualifiedName~SettingsViewModelTests"
```

Expected: 4 tests pass

---

### Task 11: App Entry Point, Tray Icon, and Composition Root

**Files:**
- Create: `src/GetDevice/App.xaml`
- Create: `src/GetDevice/App.xaml.cs`
- Create: `src/GetDevice/Program.cs`

**Interfaces:**
- Consumes: All services and viewmodels above
- Produces: Runnable application

- [ ] **Step 1: Create `App.xaml`**

```xml
<Application x:Class="GetDevice.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             ShutdownMode="OnExplicitShutdown">
    <Application.Resources>
    </Application.Resources>
</Application>
```

- [ ] **Step 2: Create `App.xaml.cs`**

```csharp
using System.Windows;
using GetDevice.Services;
using GetDevice.ViewModels;
using GetDevice.Views;

namespace GetDevice;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private LoginWindow? _loginWindow;
    private System.Windows.Forms.NotifyIcon? _trayIcon;

    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Build service provider
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        // Show login
        ShowLogin();
    }

    private static void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        // Services
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<IDeviceInfoService, DeviceInfoService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IHttpServerService, HttpServerService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    private void ShowLogin()
    {
        var loginVm = ServiceProvider.GetRequiredService<LoginViewModel>();
        _loginWindow = new LoginWindow(loginVm);
        loginVm.LoginSucceeded += ShowMainWindow;

        _loginWindow.ShowDialog();
    }

    private void ShowMainWindow()
    {
        _loginWindow?.Close();
        _loginWindow = null;

        var mainVm = ServiceProvider.GetRequiredService<MainViewModel>();
        _mainWindow = new MainWindow(mainVm);
        _mainWindow.Show();

        // Check if first launch - prompt to change default password
        var passwordService = ServiceProvider.GetRequiredService<IPasswordService>();
        if (passwordService.IsDefaultPassword())
        {
            MessageBox.Show(
                "You are using the default password '12345678'. Please change it in Settings.",
                "Security Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        SetupTrayIcon();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                System.Reflection.Assembly.GetExecutingAssembly().Location),
            Text = "GetDevice",
            Visible = true
        };

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (_, _) =>
        {
            _mainWindow?.Show();
            _mainWindow?.WindowState = WindowState.Normal;
            _mainWindow?.Activate();
        });
        contextMenu.Items.Add("Exit", null, (_, _) =>
        {
            _trayIcon?.Dispose();
            Shutdown();
        });
        _trayIcon.ContextMenuStrip = contextMenu;

        _trayIcon.DoubleClick += (_, _) =>
        {
            _mainWindow?.Show();
            _mainWindow?.WindowState = WindowState.Normal;
            _mainWindow?.Activate();
        };

        // Update tray text when HTTP status changes
        var httpService = ServiceProvider.GetRequiredService<IHttpServerService>();
        httpService.RunningChanged += (_, running) =>
        {
            _trayIcon.Text = running
                ? "GetDevice — HTTP: Running"
                : "GetDevice — HTTP: Stopped";
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        var httpService = ServiceProvider.GetService<IHttpServerService>();
        httpService?.Stop();
        base.OnExit(e);
    }
}
```

- [ ] **Step 3: Create `Program.cs`**

```csharp
using System;

namespace GetDevice;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var app = new App();
        app.Run();
    }
}
```

- [ ] **Step 4: Update `GetDevice.csproj` to add Windows.Compatibility and DI packages**

Replace the csproj with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PlatformTarget>x64</PlatformTarget>
    <AssemblyName>GetDevice</AssemblyName>
    <RootNamespace>GetDevice</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Management" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="appsettings.json" />
  </ItemGroup>

</Project>
```

---

### Task 12: WiX Installer

**Files:**
- Create: `installer/Product.wxs`
- Create: `installer/GetDevice.wixproj`

- [ ] **Step 1: Create `installer/GetDevice.wixproj`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="WixToolset.Sdk/4.0.0">
  <PropertyGroup>
    <OutputType>Package</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <OutputName>GetDevice</OutputName>
    <Platform>x64</Platform>
    <SuppressValidation>false</SuppressValidation>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\src\GetDevice\GetDevice.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create `installer/Product.wxs`**

```xml
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Package Name="GetDevice"
           Language="1033"
           Version="1.0.0.0"
           Manufacturer="GetDevice"
           UpgradeCode="PUT-GUID-HERE">

    <MajorUpgrade DowngradeErrorMessage="A newer version of GetDevice is already installed." />

    <Media Id="1" Cabinet="Setup.cab" EmbedCab="yes" />

    <Property Id="ARPPRODUCTICON" Value="icon.ico" />

    <!-- Install directory -->
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFiles64Folder">
        <Directory Id="INSTALLFOLDER" Name="GetDevice" />
      </Directory>
      <Directory Id="ProgramMenuFolder">
        <Directory Id="ApplicationProgramsFolder" Name="GetDevice" />
      </Directory>
    </Directory>

    <!-- Files -->
    <DirectoryRef Id="INSTALLFOLDER">
      <Component Id="GetDevice.exe" Bitness="always64" Guid="PUT-GUID-HERE">
        <File Id="GetDevice.exe" Source="$(var.GetDevice.TargetDir)\GetDevice.exe" KeyPath="yes" />
      </Component>
      <Component Id="appsettings.json" Bitness="always64" Guid="PUT-GUID-HERE">
        <File Id="appsettings.json" Source="$(var.GetDevice.TargetDir)\appsettings.json" KeyPath="yes" />
      </Component>
    </DirectoryRef>

    <!-- Start Menu shortcut -->
    <DirectoryRef Id="ApplicationProgramsFolder">
      <Component Id="Shortcut" Guid="PUT-GUID-HERE">
        <Shortcut Id="ApplicationStartMenuShortcut"
                  Name="GetDevice"
                  Description="GetDevice - Device Information Exporter"
                  Target="[INSTALLFOLDER]GetDevice.exe"
                  WorkingDirectory="INSTALLFOLDER" />
        <RemoveFolder Id="RemoveApplicationProgramsFolder" Directory="ApplicationProgramsFolder" On="uninstall" />
        <RegistryValue Root="HKCU"
                       Key="Software\GetDevice"
                       Name="installed"
                       Type="integer"
                       Value="1"
                       KeyPath="yes" />
      </Component>
    </DirectoryRef>

    <!-- Features -->
    <Feature Id="MainFeature" Title="GetDevice" Level="1">
      <ComponentRef Id="GetDevice.exe" />
      <ComponentRef Id="appsettings.json" />
      <ComponentRef Id="Shortcut" />
    </Feature>
  </Package>
</Wix>
```

---

### Full Test Suite

- [ ] **Run all tests**

```bash
dotnet test tests/GetDevice.Tests/GetDevice.Tests.csproj
```

Expected: All 27+ tests pass

---

### Build and Package

- [ ] **Build the application**

```bash
dotnet build src/GetDevice/GetDevice.csproj -c Release
```

- [ ] **Build the MSI installer**

```bash
dotnet build installer/GetDevice.wixproj -c Release
```

Expected: `installer/bin/Release/GetDevice.msi` is created
