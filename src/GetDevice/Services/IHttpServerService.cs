namespace GetDevice.Services;

public interface IHttpServerService
{
    bool IsRunning { get; }
    event EventHandler<bool>? RunningChanged;
    void Start(int port = 8080);
    void Stop();
}
