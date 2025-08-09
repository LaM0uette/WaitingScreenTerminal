using System.Text.Json;
using Models;

namespace Services;

public class StreamConfigService : IStreamConfigService
{
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    public async Task<StreamConfig> LoadAsync()
    {
        await using Stream s = await FileSystem.OpenAppPackageFileAsync("wwwroot/data/config.json");
        using var r = new StreamReader(s);
        string json = await r.ReadToEndAsync();
        return JsonSerializer.Deserialize<StreamConfig>(json, _options) ?? new StreamConfig();
    }
}