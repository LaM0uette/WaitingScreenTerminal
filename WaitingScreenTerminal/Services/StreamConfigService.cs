using System.Text.Json;
using Models;

namespace Services;

public class StreamConfigService : IStreamConfigService
{
    #region Statements

    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    #endregion

    #region Methods

    public async Task<StreamConfig> LoadAsync()
    {
        await using Stream stream = await FileSystem.OpenAppPackageFileAsync("wwwroot/data/config.json");
        using var reader = new StreamReader(stream);
        string json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<StreamConfig>(json, _options) ?? new StreamConfig();
    }

    #endregion
}