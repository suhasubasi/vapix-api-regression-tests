using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace VapixApiTests;

// Contract tests: does the API answer in the shape we expect?
public class ContractTests : IClassFixture<CameraFixture>
{
    private readonly CameraFixture _fx;
    public ContractTests(CameraFixture fx) => _fx = fx;

    [Fact]
    public async Task BasicDeviceInfo_ReturnsRequiredProperties()
    {
        var response = await _fx.Client.PostAsJsonAsync("/axis-cgi/basicdeviceinfo.cgi",
            new { apiVersion = "1.0", method = "getAllProperties" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var props = doc.RootElement.GetProperty("data").GetProperty("propertyList");

        foreach (var key in new[] { "Brand", "ProdNbr", "SerialNumber", "Version" })
        {
            Assert.True(props.TryGetProperty(key, out var value), $"Missing property: {key}");
            Assert.False(string.IsNullOrWhiteSpace(value.GetString()), $"Empty property: {key}");
        }
    }

    [Fact]
    public async Task ParamList_FirmwareVersion_IsKeyValueText()
    {
        var text = await _fx.Client.GetStringAsync(
            "/axis-cgi/param.cgi?action=list&group=Properties.Firmware.Version");

        Assert.Contains("Properties.Firmware.Version=", text);
    }

    [Fact]
    public async Task Snapshot_IsJpeg()
    {
        var response = await _fx.Client.GetAsync("/axis-cgi/jpg/image.cgi");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 4);
        Assert.Equal(0xFF, bytes[0]); // JPEG files start with FF D8
        Assert.Equal(0xD8, bytes[1]);
    }

    [Fact]
    public async Task Snapshot_RespondsWithinLimit()
    {
        int maxMs = int.TryParse(_fx.Settings["Camera:MaxSnapshotMs"], out var v) ? v : 1000;

        var sw = Stopwatch.StartNew();
        var response = await _fx.Client.GetAsync("/axis-cgi/jpg/image.cgi");
        sw.Stop();

        response.EnsureSuccessStatusCode();
        Assert.True(sw.ElapsedMilliseconds < maxMs, $"Snapshot took {sw.ElapsedMilliseconds} ms (limit {maxMs} ms)");
    }
}
