using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace GetDevice.Services;

public class SingleInstanceService : ISingleInstanceService, IDisposable
{
    private const string MutexName = @"Global\GetDevice_SingleInstanceMutex";
    private const string PipeName = "GetDevice_SingleInstancePipe";

    private Mutex? _mutex;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;

    public event Action? Activated;

    public bool TryAcquire()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            _mutex.Dispose();
            _mutex = null;
            SignalExisting();
            return false;
        }

        _cts = new CancellationTokenSource();
        _listenTask = Task.Run(() => ListenLoop(_cts.Token));
        return true;
    }

    private void SignalExisting()
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var client = new NamedPipeClientStream(
                    ".", PipeName, PipeDirection.Out, PipeOptions.Asynchronous);
                client.Connect(1000);
                using var writer = new StreamWriter(client) { AutoFlush = true };
                writer.WriteLine("activate");
                return;
            }
            catch (TimeoutException)
            {
                Thread.Sleep(150);
            }
            catch
            {
                return;
            }
        }
    }

    private async Task ListenLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var server = NamedPipeServerStreamConstruct();
                await server.WaitForConnectionAsync(token).ConfigureAwait(false);

                using var reader = new StreamReader(server);
                await reader.ReadLineAsync().ConfigureAwait(false);

                Activated?.Invoke();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // pipe error or race during shutdown; keep listening
            }
        }
    }

    private static NamedPipeServerStream NamedPipeServerStreamConstruct()
    {
        return new NamedPipeServerStream(
            PipeName,
            PipeDirection.In,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);
    }

    public void Release()
    {
        if (_cts == null)
            return;

        _cts.Cancel();
        try
        {
            _listenTask?.Wait(2000);
        }
        catch
        {
            // ignore
        }

        _cts.Dispose();
        _cts = null;
        _listenTask = null;

        if (_mutex != null)
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
            _mutex = null;
        }
    }

    public void Dispose() => Release();
}
