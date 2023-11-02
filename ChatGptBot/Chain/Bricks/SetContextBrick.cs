using System.Collections.Concurrent;
using Azure.AI.OpenAI;
using ChatGptBot.Chain.Dto;
using ChatGptBot.Ioc;
using ChatGptBot.Repositories;
using ChatGptBot.Services;
using ChatGptBot.Settings;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Options;
using SharpToken;
using ICosineProximityService = ChatGptBot.Services.ICosineProximityService;
using IEmbeddingServiceCore = ChatGptBot.Services.IEmbeddingServiceCore;

namespace ChatGptBot.Chain.Bricks;

public class SetContextBrick : LangChainBrickBase, ILangChainBrick, ISingletonScope
{
    private readonly ICosineProximityService _cosineProximityService;
    private readonly IEmbeddingRepositoryCache _embeddingRepositoryCache;
    private readonly IEmbeddingServiceCore _embeddingServiceCore;
    private readonly GptEncoding _gptEncoding;
    private readonly TelemetryClient _telemetryClient;
    private readonly ChatGptSettings _chatGptSettings;

       public SetContextBrick(ICosineProximityService cosineProximityService
           , IEmbeddingRepositoryCache embeddingRepositoryCache
    , IOptions<ChatGptSettings> openAi, IEmbeddingServiceCore embeddingServiceCore
    , GptEncoding gptEncoding, TelemetryClient telemetryClient)
    {
        _chatGptSettings= openAi.Value; 
        _cosineProximityService = cosineProximityService;
        _embeddingRepositoryCache = embeddingRepositoryCache;
        _embeddingServiceCore = embeddingServiceCore;
        _gptEncoding = gptEncoding;
        _telemetryClient = telemetryClient;
    }

       public override async Task<Answer> Ask(Question question)
       {
           using var op = _telemetryClient.StartOperation<DependencyTelemetry>(nameof(SetContextBrick));
           try
           {
               if (Next == null)
               {
                   throw new Exception($"{GetType().Name} cannot be the last item of the chain");
               }

               var maxItems = question.EmbeddingMatchMaxItems ?? _chatGptSettings.DefaultEmbeddingMatchMaxItems;
            
               var set = await _embeddingRepositoryCache.LoadSet(question.EmbeddingSetCode ??
                                                                 _chatGptSettings.DefaultEmbeddingSetCode);
            
               var contextMessagesBag = new ConcurrentBag<ContextMessage>();
               // add matching documents from the history
               if (!question.UserHasChangedTopic)
               {
                   foreach (var item in question.ConversationHistoryMessages
                                .Where(c => c.ChatRole == ChatRole.User && (c.ProvideContext ?? false))
                                .SelectMany(ci => ci.ContextMessages))
                   {
                       contextMessagesBag.Add(new ContextMessage
                       {
                           EmbeddingId = item.EmbeddingId,
                           Text = item.Text, 
                           Tokens = item.Tokens,
                           Proximity = item.Proximity,
                           QuestionSequenceNumberInConversation = item.QuestionSequenceNumberInConversation, 
                           ConversationItemId = item.ConversationItemId});
                   }
               }

               var userQuestionEmbeddingsMatch = _cosineProximityService.GetClosestMatches(new ProximityRequest
               {
                   SimilarityThreshold = _chatGptSettings.SimilarityThreshold,
                   InputEmbedding = await _embeddingServiceCore.GetTextEmbeddings(question.UserQuestion.Text),
                   EmbeddingSet = set,
                   MaxItems = maxItems
               }).ToList();
               var nextConversationIndex = contextMessagesBag.Count==0? 0 : contextMessagesBag.Max(t => t.QuestionSequenceNumberInConversation)+1;  
               userQuestionEmbeddingsMatch.ForEach(item =>
               {
                   contextMessagesBag.Add(new ContextMessage
                   {
                       EmbeddingId = item.Embedding.Id,
                       Text = item.Embedding.Text,
                       Tokens = item.Embedding.Tokens,
                       Proximity = item.Proximity,
                       QuestionSequenceNumberInConversation = nextConversationIndex,
                       ConversationItemId = Guid.Empty
                   });
               });
               
                
               var betterEmbeddings = ExtractBetterMatches(contextMessagesBag);
               if (betterEmbeddings.Any())
               {
                   question.ContextIntroMessages.Add(new TextWIthTokenCount
                   {
                       Text = "Answer to the question using the following context:",
                       Tokens = _gptEncoding.Encode("Answer to the question using the following context:").Count
                   });
                   betterEmbeddings.ForEach(t =>
                       question.ContextMessages.Add(new ContextMessage
                       {
                           ConversationItemId=t.ConversationItemId,
                           QuestionSequenceNumberInConversation = t.QuestionSequenceNumberInConversation,
                           Proximity = t.Proximity,
                           Text = t.Text, Tokens = t.Tokens,
                           EmbeddingId = t.EmbeddingId
                       }));
               }

               return await Next.Ask(question);
           }
           catch (Exception)
           {
               op.Telemetry.Success = false;
               throw;
           }
       }

       private List<ContextMessage> ExtractBetterMatches(
           ConcurrentBag<ContextMessage> allCandidates)
       {
        // sort descending by ConversationIndex to have on top matches for the last user question,
        // and then by Proximity to have on top most relevant matches 
        return allCandidates.OrderByDescending(i => i.QuestionSequenceNumberInConversation)
               .ThenByDescending(i => i.Proximity)
               .DistinctBy(i => i.EmbeddingId)
               .Take(_chatGptSettings.DefaultTotalEmbeddingMatchMaxItems).ToList();
    }

}


