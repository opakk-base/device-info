using System.Net;
using System.Net.Http;
using GetDevice.Services;
using Moq;

namespace GetDevice.Tests.Services;

public class HttpServerServiceTests : IDisposable
{
    private readonly Mock<IExportService> _mockExport;
    private readonly HttpServerService _service;

    public HttpServerServiceTests()
    {
        _mockExport = new Mock<IExportService>();
        _mockExport.Setup(e => e.ExportToJsonFromCurrent())
                   .Returns("""{"success":true,"data":{"hostname":"test-pc","os":"Windows 11"}}""");

        _service = new HttpServerService(_mockExport.Object);
    }

    [Fact]
    public void StartAndStop_ChangesRunningState()
    {
        Assert.False(_service.IsRunning);

        _service.Start(9191);
        Thread.Sleep(200);
        Assert.True(_service.IsRunning);

        _service.Stop();
        Thread.Sleep(200);
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        _service.Start(9192);
        Thread.Sleep(200);

        using var client = new HttpClient();
        var response = await client.GetStringAsync("http://localhost:9192/health");

        Assert.Equal("""{"status":"ok"}""", response);

        _service.Stop();
    }

    [Fact]
    public async Task GetDevice_ReturnsExportJson()
    {
        _service.Start(9193);
        Thread.Sleep(200);

        using var client = new HttpClient();
        var response = await client.GetStringAsync("http://localhost:9193/getdevice");

        Assert.Contains("hostname", response);
        Assert.Contains("test-pc", response);

        _service.Stop();
    }

    [Fact]
    public async Task UnknownPath_Returns404()
    {
        _service.Start(9194);
        Thread.Sleep(200);

        using var client = new HttpClient();
        using var response = await client.GetAsync("http://localhost:9194/unknown");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("not found", body);
        Assert.Contains("success", body);
        Assert.Contains("false", body);

        _service.Stop();
    }

    [Fact]
    public async Task OptionsRequest_Returns204WithCorsAndPnaHeaders()
    {
        _service.Start(9195);
        Thread.Sleep(200);

        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "http://localhost:9195/getdevice");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("*", response.Headers.GetValues("Access-Control-Allow-Origin").First());
        Assert.True(response.Headers.Contains("Access-Control-Allow-Private-Network"));
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Private-Network").First());

        _service.Stop();
    }

    [Fact]
    public async Task GetRequest_ReturnsCorsAndPnaHeaders()
    {
        _service.Start(9196);
        Thread.Sleep(200);

        using var client = new HttpClient();
        using var response = await client.GetAsync("http://localhost:9196/getdevice");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("*", response.Headers.GetValues("Access-Control-Allow-Origin").First());
        Assert.True(response.Headers.Contains("Access-Control-Allow-Private-Network"));
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Private-Network").First());

        _service.Stop();
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}
