using ChatGptBot.Dtos.Embeddings;
using ChatGptBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatGptBot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmbeddingController : ControllerBase
    {
        private readonly IEmbeddingService _embeddingService;
    
        public EmbeddingController(IEmbeddingService embeddingService)
        {
            _embeddingService = embeddingService;
            
        }

        [HttpPost("from-file-embeddings-and-store", Name = "from-file-embeddings-and-store")]
        public async Task StoreFileResourceEmbeddings(EmbedFromFileRequest embedFromFileRequest)
        {
            await _embeddingService.CalculateFileEmbeddingsAndStore(embedFromFileRequest);

        }

        [HttpPost("from-directory-embeddings-and-store", Name = "from-directory-embeddings-and-store")]
        public async Task StoreDirectoryResourceEmbeddings(EmbedFromDirectoryRequest embedFromDirectoryRequest)
        {
            await _embeddingService.CalculateDirectoryEmbeddingsAndStore(embedFromDirectoryRequest);
        }

       

      

        [HttpPut("embeddings-set", Name = "embeddings-set")]
        public async Task Upsert(EmbeddingSet embeddingSet)
        {
            await _embeddingService.UpsertEmbeddingSet(embeddingSet);

        }

        [HttpGet("embeddings-set", Name = "embeddings-set")]
        public async Task<List<EmbeddingSet>> SearchEmbeddingSet()
        {
            return await _embeddingService.SearchEmbeddingSet();

        }


    }
}