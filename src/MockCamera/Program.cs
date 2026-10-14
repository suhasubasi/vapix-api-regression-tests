// MockCamera: a small fake "camera" that imitates public VAPIX-style endpoints.
// All data is FAKE and only for testing. It is not a copy of any real Axis firmware.
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

const string Firmware = "1.0.0";

app.MapGet("/", () => Results.Text($"MockCamera is running. Fake firmware: {Firmware}"));

// Basic device information (POST + JSON, like /axis-cgi/basicdeviceinfo.cgi)
app.MapPost("/axis-cgi/basicdeviceinfo.cgi", async (HttpRequest request) =>
{
    using var doc = await JsonDocument.ParseAsync(request.Body);
    var root = doc.RootElement;
    string method = root.TryGetProperty("method", out var m) ? m.GetString() ?? "" : "";
    string apiVersion = root.TryGetProperty("apiVersion", out var v) ? v.GetString() ?? "1.0" : "1.0";

    if (method != "getAllProperties")
    {
        return Results.Json(new { apiVersion, error = new { code = 2002, message = "Method not supported (mock)" } });
    }

    var properties = new Dictionary<string, string>
    {
        ["Brand"] = "MOCK",
        ["ProdFullName"] = "MOCK Network Camera",
        ["ProdNbr"] = "MOCK-0001",
        ["ProdType"] = "Network Camera",
        ["SerialNumber"] = "000000MOCK0001",
        ["Version"] = Firmware
    };

    return Results.Json(new { apiVersion, data = new { propertyList = properties } });
});

app.Run();

// Needed later so WebApplicationFactory<Program> can find this class in the tests.
public partial class Program { }
