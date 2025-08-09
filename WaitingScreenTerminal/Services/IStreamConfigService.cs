using Models;

namespace Services;

public interface IStreamConfigService
{
    public Task<StreamConfig> LoadAsync();
}