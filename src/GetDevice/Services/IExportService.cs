using GetDevice.Models;

namespace GetDevice.Services;

public interface IExportService
{
    string ExportToJson(DeviceInfo info, IEnumerable<string>? onlyFields = null);
    Task ExportToFileAsync(DeviceInfo info, string filePath, IEnumerable<string>? onlyFields = null);
    string ExportToJsonFromCurrent();
}
