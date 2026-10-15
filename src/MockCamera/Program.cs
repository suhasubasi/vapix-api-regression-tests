// MockCamera: a small fake "camera" that imitates a few public VAPIX-style endpoints.
// All data is FAKE and only for testing. It is not a copy of any real Axis firmware.
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// The "firmware" setting decides how the fake camera behaves.
// "1.0.0" = good behaviour. "1.1.0-broken" = simulated regression.
string Firmware() => app.Configuration["MockCamera:Firmware"] ?? "1.0.0";
bool IsBroken() => Firmware().Contains("broken", StringComparison.OrdinalIgnoreCase);

app.MapGet("/", () => Results.Text($"MockCamera is running. Fake firmware: {Firmware()}"));

// 1) Basic device information (POST + JSON, like /axis-cgi/basicdeviceinfo.cgi)
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
        ["Version"] = Firmware()
    };

    // Simulated regression: the broken firmware "forgets" to return SerialNumber.
    if (IsBroken()) properties.Remove("SerialNumber");

    return Results.Json(new { apiVersion, data = new { propertyList = properties } });
});

// 2) Parameter list (GET, plain text key=value, like /axis-cgi/param.cgi?action=list)
app.MapGet("/axis-cgi/param.cgi", (string? action, string? group) =>
{
    if (action != "list")
        return Results.Text("# Error: unsupported action (mock)", statusCode: 400);

    var all = new Dictionary<string, string>
    {
        ["root.Brand.Brand"] = "MOCK",
        ["root.Brand.ProdNbr"] = "MOCK-0001",
        ["root.Properties.Firmware.Version"] = Firmware(),
        ["root.Image.I0.Appearance.Resolution"] = "1920x1080"
    };

    var selected = string.IsNullOrEmpty(group)
        ? all
        : all.Where(kv => kv.Key.Contains(group, StringComparison.OrdinalIgnoreCase))
             .ToDictionary(kv => kv.Key, kv => kv.Value);

    var sb = new StringBuilder();
    foreach (var kv in selected) sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\n');
    return Results.Text(sb.ToString(), "text/plain");
});

// 3) JPEG snapshot (GET, like /axis-cgi/jpg/image.cgi)
app.MapGet("/axis-cgi/jpg/image.cgi", async () =>
{
    // Simulated regression: the broken firmware is slow.
    if (IsBroken()) await Task.Delay(1500);
    return Results.File(FakeJpeg.Bytes, "image/jpeg");
});

app.Run();

// Needed so WebApplicationFactory<Program> can find this class in the tests.
public partial class Program { }

static class FakeJpeg
{
    // A tiny valid 1x1 pixel JPEG (grey). Starts with FF D8 and ends with FF D9.
    public static readonly byte[] Bytes = Convert.FromBase64String(
        "/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////wgALCAABAAEBAREA/8QAFBABAAAAAAAAAAAAAAAAAAAAAP/aAAgBAQABPxA=");
}
