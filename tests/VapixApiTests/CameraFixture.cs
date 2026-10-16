using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace VapixApiTests;

// Gives every test an HttpClient.
// If Camera:BaseUrl is empty -> tests run against the in-memory MockCamera.
// If it is set (testsettings.json or env var CAMERA__BASEURL) -> tests run against that real device.
public class CameraFixture : IDisposable
{
    private readonly WebApplicationFactory<Program>? _factory;
    public HttpClient Client { get; }
    public IConfiguration Settings { get; }
    public bool UsingRealCamera { get; }

    public CameraFixture()
    {
        Settings = new ConfigurationBuilder()
            .AddJsonFile("testsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var baseUrl = Settings["Camera:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            UsingRealCamera = true;
            var handler = new HttpClientHandler
            {
                // Real devices use Digest auth; HttpClientHandler handles it with NetworkCredential.
                Credentials = new NetworkCredential(Settings["Camera:Username"], Settings["Camera:Password"])
            };
            Client = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
        }
        else
        {
            _factory = CreateMock("1.0.0");
            Client = _factory.CreateClient();
        }
    }

    public static WebApplicationFactory<Program> CreateMock(string firmware) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.UseSetting("MockCamera:Firmware", firmware));

    public void Dispose()
    {
        Client.Dispose();
        _factory?.Dispose();
    }
}
