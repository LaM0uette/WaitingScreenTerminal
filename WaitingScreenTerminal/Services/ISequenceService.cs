using Models;

namespace Services;

public interface ISequenceService
{
    public Task<List<SequenceItem>> LoadAsync();
}