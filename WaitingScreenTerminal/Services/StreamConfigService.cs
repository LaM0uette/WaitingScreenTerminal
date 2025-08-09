using System.Text.Json;
using Models;

namespace Services;

public class StreamConfigService : IStreamConfigService
{
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public async Task<StreamConfig> LoadAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("wwwroot/data/config.json");
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<StreamConfig>(json, _options) ?? new();
    }
}