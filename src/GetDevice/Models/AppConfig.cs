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
