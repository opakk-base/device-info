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

        if (string.IsNullOrEmpty(config.DeviceId))
        {
            config.DeviceId = Guid.NewGuid().ToString("D");
        }

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
