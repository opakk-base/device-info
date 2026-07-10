using System.Net;
using System.Text;

namespace GetDevice.Services;

public class HttpServerService : IHttpServerService, IDisposable
{
    private readonly IExportService _exportService;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;

    public bool IsRunning => _listener?.IsListening ?? false;

    public event EventHandler<bool>? RunningChanged;

    public HttpServerService(IExportService exportService)
    {
        _exportService = exportService;
    }

    public void Start(int port = 8080)
    {
        if (IsRunning)
            return;

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();

        _cts = new CancellationTokenSource();
        _listenTask = Task.Run(() => ListenLoop(_cts.Token));

        RunningChanged?.Invoke(this, true);
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _listener?.Stop(); } catch { }
        _listener?.Close();
        _listener = null;

        RunningChanged?.Invoke(this, false);
    }

    private async Task ListenLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(token);
                _ = Task.Run(() => HandleRequest(context), token);
            }
            catch (OperationCanceledException) { break; }
            catch (HttpListenerException) { break; }
            catch (ObjectDisposedException) { break; }
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            if (request.Url?.AbsolutePath == "/health")
            {
                RespondJson(response, """{"status":"ok"}""");
            }
            else if (request.Url?.AbsolutePath == "/getdevice")
            {
                var json = _exportService.ExportToJsonFromCurrent();
                RespondJson(response, json);
            }
            else
            {
                response.StatusCode = 404;
                RespondJson(response, """{"error":"not found"}""");
            }
        }
        catch
        {
            try { context.Response.StatusCode = 500; } catch { }
        }
    }

    private static void RespondJson(HttpListenerResponse response, string json)
    {
        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json";
        response.ContentEncoding = Encoding.UTF8;
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
