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
