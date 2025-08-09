using System.Text.Json;
using Models;

namespace Services;

public class SequenceService : ISequenceService
{
    #region Statements

    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    #endregion

    #region ISequenceService

    public async Task<List<SequenceItem>> LoadAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("wwwroot/data/sequence.json");
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<List<SequenceItem>>(json, _options) ?? new();
    }

    #endregion
}