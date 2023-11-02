using System.Text;
using System.Text.Json;
using ChatGptBot.Dtos.Embeddings;
using ChatGptBot.Ioc;
using ChatGptBot.Repositories;
using ChatGptBot.Repositories.Entities;
using ChatGptBot.Settings;
using Microsoft.Extensions.Options;
using SharpToken;

namespace ChatGptBot.Services
{
    public interface IEmbeddingService
    {
        Task CalculateFileEmbeddingsAndStore(EmbedFromFileRequest embedFromFileRequest);

        Task CalculateDirectoryEmbeddingsAndStore(EmbedFromDirectoryRequest embedFromDirectoryRequest);
       
        Task<EmbeddingForDb> GetResourceEmbeddings(EmbedFromFileRequest embedFromFileRequest);
        Task UpsertEmbeddingSet(EmbeddingSet embeddingSet);
        Task<List<EmbeddingSet>> SearchEmbeddingSet();
    
    }

    public class EmbeddingService : IEmbeddingService, ISingletonScope
    {
       
        private readonly ChatGptSettings _chatGptSettings;

        private readonly IEmbeddingRepository _embeddingRepository;
        private readonly GptEncoding _gptEncoding;
        private readonly IEmbeddingServiceCore _embeddingServiceCore;

        public EmbeddingService( IOptions<ChatGptSettings> openAiSettings
            , IEmbeddingRepository embeddingRepository, ICosineProximityService cosineProximityService,
            GptEncoding gptEncoding, IEmbeddingRepositoryCache embeddingRepositoryCache, IEmbeddingServiceCore embeddingServiceCore)
        {
            _embeddingRepository = embeddingRepository;
            _gptEncoding = gptEncoding;
            _embeddingServiceCore = embeddingServiceCore;
            _chatGptSettings = openAiSettings.Value;
        }

     
        public async Task CalculateDirectoryEmbeddingsAndStore(EmbedFromDirectoryRequest embedFromDirectoryRequest)
        {
            ArgumentException.ThrowIfNullOrEmpty(embedFromDirectoryRequest.DirectoryPath);
            if (embedFromDirectoryRequest.SetId==Guid.Empty)
            {
                throw new ArgumentException($"embedFromDirectoryRequest.SetId==Guid.Empty");
            }
            // if not found throw exception
            var _ = _embeddingRepository.EmbeddingSetById(embedFromDirectoryRequest.SetId); 

            if (!Directory.Exists(embedFromDirectoryRequest.DirectoryPath))
            {
                throw new ArgumentException($"directory {embedFromDirectoryRequest.DirectoryPath} not found");
            }
            var files = Directory.GetFiles(embedFromDirectoryRequest.DirectoryPath, embedFromDirectoryRequest.SearchPattern
            , new EnumerationOptions { RecurseSubdirectories = embedFromDirectoryRequest.Recursive });
            foreach (var file in files)
            {
                await CalculateFileEmbeddingsAndStore(new EmbedFromFileRequest
                {
                    SetId = embedFromDirectoryRequest.SetId,
                    FilePath = file,IgnoreIfStartsWith = embedFromDirectoryRequest.IgnoreIfStartsWith
                });
            }
        }

      

        public async Task CalculateFileEmbeddingsAndStore(EmbedFromFileRequest embedFromFileRequest)
        {
            var embedding = await GetResourceEmbeddings(embedFromFileRequest);
#if DEBUG
            var ret = JsonSerializer.Serialize(embedding);
#endif
            await _embeddingRepository.AddEmbedding(embedding);
        }


        public async Task<EmbeddingForDb> GetResourceEmbeddings(EmbedFromFileRequest embedFromFileRequest)
        {
          
                ArgumentException.ThrowIfNullOrEmpty(embedFromFileRequest.FilePath);
                if (!File.Exists(embedFromFileRequest.FilePath))
                {
                    throw new ArgumentException($"file {embedFromFileRequest.FilePath} not found");
                }

                var text = await LoadResource(embedFromFileRequest);
                var ret = await _embeddingServiceCore.GetTextEmbeddings(text);
                return new EmbeddingForDb
                {
                    SetId = embedFromFileRequest.SetId,
                    Text = text,
                    Tokens = _gptEncoding.Encode(text).Count,
                    VectorValues = ret.Select((item, index) => (item, index)).ToList()
                };
          
            
        }

        public async Task UpsertEmbeddingSet(EmbeddingSet embeddingSet)
        {
            await _embeddingRepository.UpsertEmbeddingSet(embeddingSet);
        }

        public async Task<List<EmbeddingSet>> SearchEmbeddingSet()
        {
            return await _embeddingRepository.SearchEmbeddingSet();
        }


        private async Task<string> LoadResource(EmbedFromFileRequest embedFromFileRequest)
        {
            var lines = await File.ReadAllLinesAsync(embedFromFileRequest.FilePath);
            var sb = new StringBuilder();
            return lines.Aggregate( sb, (acc, line) =>
                {
                    if (!embedFromFileRequest.IgnoreIfStartsWith.Any(token => line.StartsWith(token)))
                    {
                        acc.AppendLine(line);
                    }
                    return acc;
                },

                acc => acc.ToString());
        }
    }

   
}
