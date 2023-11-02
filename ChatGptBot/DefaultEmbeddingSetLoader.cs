using ChatGptBot.Repositories;
using ChatGptBot.Settings;
using Microsoft.Extensions.Options;

namespace ChatGptBot;

public class DefaultEmbeddingSetLoader : BackgroundService
{
    private readonly ChatGptSettings _chatGptSettings;
    private readonly IEmbeddingRepositoryCache _embeddingRepositoryCache;

    public DefaultEmbeddingSetLoader(IOptions<ChatGptSettings> chatGptSettings, IEmbeddingRepositoryCache embeddingRepositoryCache)
    {
        _chatGptSettings = chatGptSettings.Value;
        _embeddingRepositoryCache = embeddingRepositoryCache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _embeddingRepositoryCache.LoadSet(_chatGptSettings.DefaultEmbeddingSetCode);
     }
}