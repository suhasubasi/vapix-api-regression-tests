using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace VapixApiTests;

// Regression tests: compare a "good" firmware with a "new" one.
// Here we use the mock's firmware switch to PROVE that our tests catch a regression.
public class RegressionTests
{
    private static async Task<JsonElement> GetProperties(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/axis-cgi/basicdeviceinfo.cgi",
            new { apiVersion = "1.0", method = "getAllProperties" });
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetProperty("propertyList").Clone();
    }

    [Fact]
    public async Task GoodFirmware_HasSerialNumber()
    {
        using var factory = CameraFixture.CreateMock("1.0.0");
        var props = await GetProperties(factory.CreateClient());
        Assert.True(props.TryGetProperty("SerialNumber", out _));
    }

    [Fact]
    public async Task BrokenFirmware_RegressionIsDetected_MissingField()
    {
        using var factory = CameraFixture.CreateMock("1.1.0-broken");
        var props = await GetProperties(factory.CreateClient());

        // The contract says SerialNumber must exist. The broken firmware drops it,
        // so this check is what would turn a real CI run red.
        Assert.False(props.TryGetProperty("SerialNumber", out _),
            "Expected the simulated regression (missing SerialNumber) to be visible");
    }

    [Fact]
    public async Task BrokenFirmware_RegressionIsDetected_SlowSnapshot()
    {
        using var factory = CameraFixture.CreateMock("1.1.0-broken");
        var client = factory.CreateClient();

        var sw = Stopwatch.StartNew();
        (await client.GetAsync("/axis-cgi/jpg/image.cgi")).EnsureSuccessStatusCode();
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds >= 1000, "Expected the simulated slow snapshot");
    }
}
