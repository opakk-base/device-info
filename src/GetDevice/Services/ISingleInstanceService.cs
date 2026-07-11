namespace GetDevice.Services;

public interface ISingleInstanceService
{
    bool TryAcquire();
    event Action? Activated;
    void Release();
}
