using System.Collections.Concurrent;
using ChatGptBot.Ioc;
using ChatGptBot.Settings;
using Microsoft.Extensions.Options;

namespace ChatGptBot.Services
{
    public interface ICosineProximityService
    {
        IEnumerable<(Embedding Embedding, float Proximity)> GetClosestMatches(ProximityRequest proximityRequest);
    }


    public class ProximityRequest
    {
        public required IReadOnlyList<float> InputEmbedding { get; init; } = new List<float>().AsReadOnly();
        public required IReadOnlyList<Embedding> EmbeddingSet { get; init; } = new List<Embedding>().AsReadOnly();

        public required int MaxItems { get; init; } = 1;

        public required float SimilarityThreshold { get; init; } 
        

    }

    public class CosineProximityService : ICosineProximityService, ISingletonScope
    {
        


        public IEnumerable<(Embedding Embedding, float Proximity)> GetClosestMatches(ProximityRequest proximityRequest)
        {
            var result = new ConcurrentBag<(Embedding embedding, float cosine)>();

            Parallel.ForEach(proximityRequest.EmbeddingSet, embedding =>
            {
                var proximity = GetProximity(embedding.VectorValues, proximityRequest.InputEmbedding);
                result.Add((embedding, proximity));
            });
            var similarityThreshold = proximityRequest.SimilarityThreshold;
            var ret =  result.OrderByDescending(item => item.cosine)
                .Where(item => item.cosine > similarityThreshold)   
                .Take(proximityRequest.MaxItems)
                .Select(item => (item.embedding, item.cosine));
            return ret;
        }
       

        public float GetProximity(IReadOnlyList<float> item1, IReadOnlyList<float> item2)
        {
           
            var vectorLength = item1.Count;
            var sumCosine = 0.0f;
            var item1Length = 0.0f;
            var item2Length = 0.0f;
            for (var i = 0; i < vectorLength; i++)
            {
                item1Length += Convert.ToSingle(Math.Pow(item1[i], 2));
                item2Length += Convert.ToSingle(Math.Pow(item2[i], 2));
                sumCosine += item1[i] * item2[i];
            }
            var proximity = sumCosine / (Math.Pow(item1Length, 0.5) * Math.Pow(item2Length, 0.5));
            return Convert.ToSingle(proximity);
        }

    }

   

    
}
