using ChatGptBot.Ioc;
using ChatGptBot.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Caching.Memory;

namespace ChatGptBot.Repositories;
public interface IEmbeddingRepositoryCache
{
    Task<List<Embedding>> LoadSet(string code);
    Task<List<(Embedding Embedding, float Proximity)>> GetRelevantDocuments(string embeddingSetCode, List<float> userQuestionEmbedding, float threshold, int maxItems);

}
public class EmbeddingRepositoryCache : IEmbeddingRepositoryCache,ISingletonScope
{
    private readonly IEmbeddingRepository _embeddingRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly TelemetryClient _telemetryClient;

    public EmbeddingRepositoryCache(IEmbeddingRepository embeddingRepository,IMemoryCache memoryCache, TelemetryClient telemetryClient)
    {
        _embeddingRepository = embeddingRepository;
        _memoryCache = memoryCache;
        _telemetryClient = telemetryClient;
    }
    public async Task<List<Embedding>> LoadSet(string code)
    {
        using var op = _telemetryClient.StartOperation<DependencyTelemetry>(nameof(EmbeddingRepositoryCache));
        var response = _memoryCache.Get<List<Embedding>>(code);
        if (response != null)
        {
            return response;
        }
        try
        {
            response = await _embeddingRepository.LoadSet(code);
            _memoryCache.Set(code, response, DateTimeOffset.MaxValue);
        }
        catch (Exception)
        {
            op.Telemetry.Success = false;
            throw;
        }
        return response;
    }

    public async Task<List<(Embedding Embedding, float Proximity)>> GetRelevantDocuments(
        string embeddingSetCode, List<float> userQuestionEmbedding, float threshold, int maxItems)
    {
        var ret =  await _embeddingRepository.GetRelevantDocuments(embeddingSetCode, userQuestionEmbedding, threshold, maxItems);
        var set = await LoadSet(embeddingSetCode);

        var response = new List<(Embedding Embedding, float Proximity)>();
        ret.ForEach(item =>
        {
            var embedding = set.FirstOrDefault(c => c.Id == item.EmbeddingId);
            if (embedding != null)
            {
                response.Add((embedding, item.Proximity));
            }
        });
        return response;
    }
}