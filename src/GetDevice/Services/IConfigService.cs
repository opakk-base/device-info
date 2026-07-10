using GetDevice.Models;

namespace GetDevice.Services;

public interface IConfigService
{
    AppConfig Load();
    void Save(AppConfig config);
    string ConfigDirectory { get; }
    string ConfigFilePath { get; }
}
