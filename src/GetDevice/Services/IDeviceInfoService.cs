using GetDevice.Models;

namespace GetDevice.Services;

public interface IDeviceInfoService
{
    DeviceInfo Gather();
}
