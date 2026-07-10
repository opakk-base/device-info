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
