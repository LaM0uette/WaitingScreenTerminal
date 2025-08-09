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
        await using Stream s = await FileSystem.OpenAppPackageFileAsync("wwwroot/data/sequence.json");
        using var r = new StreamReader(s);
        string json = await r.ReadToEndAsync();
        List<SequenceItem> list = JsonSerializer.Deserialize<List<SequenceItem>>(json, _options) ?? [];
        return list;
    }

    #endregion
}