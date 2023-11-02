using Azure;
using Azure.AI.OpenAI;
using ChatGptBot.Ioc;
using ChatGptBot.Settings;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ChatGptBot.Services;
public interface IEmbeddingServiceCore
{
    Task<List<float>> GetTextEmbeddings(string text);
}
public class EmbeddingServiceCore : IEmbeddingServiceCore, ISingletonScope
{
    private readonly OpenAIClient _openAiClient;
    private readonly IMemoryCache _memoryCache;
    private readonly ChatGptSettings _chatGptSettings;

    public EmbeddingServiceCore(OpenAIClient openAiClient, 
        IOptions<ChatGptSettings> openAiSettings, IMemoryCache memoryCache)
    {
        _openAiClient = openAiClient;
        _memoryCache = memoryCache;
        _chatGptSettings = openAiSettings.Value;
    }

    public async Task<List<float>> GetTextEmbeddings(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
            var vector = _memoryCache.Get<List<float>>(text);
            if (vector != null)
            {
                return vector;
            }

            var embeddingsOptions = new EmbeddingsOptions(text);
            var response = await _openAiClient.GetEmbeddingsAsync(
                deploymentOrModelName: _chatGptSettings.EmbeddingsModel, embeddingsOptions);
            vector = response.Value.Data[0].Embedding.ToList();
            _memoryCache.Set(text, vector, DateTimeOffset.MaxValue);
            return vector;
    }
}