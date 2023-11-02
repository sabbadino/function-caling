using Azure.AI.OpenAI;
using ChatGptBot.Chain.Dto;
using ChatGptBot.Ioc;
using ChatGptBot.Repositories;
using ChatGptBot.Repositories.Entities;
using ChatGptBot.Settings;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Options;
using SharpToken;
using ContextMessage = ChatGptBot.Repositories.Entities.ContextMessage;
using ChatGptBot.Chain;
namespace ChatGptBot.Chain.Bricks;

public class ConversationManagerBrick : LangChainBrickBase, ILangChainBrick, ISingletonScope
{
    private readonly IConversationRepository _conversationRepository;
    private readonly GptEncoding _gptEncoding;
    private readonly TelemetryClient _telemetryClient;
    private readonly ChatGptSettings _chatGptSettings;
    private readonly IEmbeddingRepositoryCache _embeddingRepositoryCache;

    public ConversationManagerBrick(IConversationRepository conversationRepository, GptEncoding gptEncoding, TelemetryClient telemetryClient,
        IEmbeddingRepositoryCache embeddingRepositoryCache, IOptions<ChatGptSettings> chatGptSettings)
    {
        _conversationRepository = conversationRepository;
        _gptEncoding = gptEncoding;
        _telemetryClient = telemetryClient;
        _chatGptSettings = chatGptSettings.Value;
        _embeddingRepositoryCache = embeddingRepositoryCache;
    }

    public override async Task<Answer> Ask(Question question)
    {

        if (Next == null)
        {
            throw new Exception($"{GetType().Name} cannot be the last item of the chain");
        }
        using var op = _telemetryClient.StartOperation<DependencyTelemetry>(nameof(ConversationManagerBrick));
        try
        {
            var questionConversationItem = new ConversationItem
            {
                OriginalTextLanguageCode = question.UserQuestion.DetectedUserQuestionLanguageCode,
                OriginalText = question.UserQuestion.OriginalLanguageQuestionText,
                EnglishText = question.UserQuestion.Text,
                ChatRole = ChatMessageExtensions.ChatRoleToString(ChatRole.User),
                ConversationId = question.ConversationId,
                Tokens = question.UserQuestion.Tokens
            };


            if (question.ConversationId != Guid.Empty)
            {
                var items = await _conversationRepository.LoadConversation(question.ConversationId);

                var set = await _embeddingRepositoryCache.LoadSet(question.EmbeddingSetCode ??
                                                                  _chatGptSettings.DefaultEmbeddingSetCode);

                question.ConversationHistoryMessages.AddRange(items.Select((item,counter) => new ConversationHistoryMessage
                {
                    Id = item.Id, ChatRole = item.ChatRole,
                    Text = item.EnglishText, Tokens = item.Tokens,
                    ProvideContext = item.ProvideContext, ContextMessages = ToContextMessages(set, item.ContextMessages
                        , counter)
                }));
            }

            var ret = await Next.Ask(question);
            if (question.ConversationId != Guid.Empty)
            {
                ret.ConversationId = question.ConversationId;
                if (question.ConversationId != Guid.Empty)
                {
                    //.RelatedConversationHistoryItem == Guid.Empty is pretty fragile  
                    await _conversationRepository.StoreUserConversationItem(questionConversationItem, question.ContextMessages
                        .Where(c=> c.ConversationItemId == Guid.Empty).ToList(), question.UserHasChangedTopic);
                    // the assistant conversation item
                    await _conversationRepository.StoreAssistantConversationItem(
                        new ConversationItem
                        {
                            OriginalTextLanguageCode = question.UserQuestion.DetectedUserQuestionLanguageCode,
                            ConversationId = question.ConversationId,
                            EnglishText = ret.AnswerFromChatGpt,
                            OriginalText = ret.TranslatedAnswerFromAi,
                            ChatRole = ChatMessageExtensions.ChatRoleToString(ChatRole.Assistant),
                            At = DateTimeOffset.UtcNow,
                            Tokens = _gptEncoding.Encode(ret.AnswerFromChatGpt).Count,
                            ProvideContext = null
                        });
                }
            }

            return ret;
        }
        catch (Exception)

        {
            op.Telemetry.Success = false;
            throw;
        }
    }

    List<Dto.ContextMessage> ToContextMessages(List<Services.Embedding> set,List<ContextMessage> contextMessageDbs,int questionSequenceNumberInConversation)
    {
        var ret = new List<Dto.ContextMessage>();
        foreach (var contextMessageDb in contextMessageDbs)
        {
            var embedding = set.SingleOrDefault(e => e.Id == contextMessageDb.EmbeddingId);
            if (embedding != null)
            {
                var contextMessage = new Dto.ContextMessage
                {
                    EmbeddingId = contextMessageDb.EmbeddingId,
                    Proximity = contextMessageDb.Proximity,
                    ConversationItemId = contextMessageDb.RelatedConversationHistoryItem,
                    Text = embedding.Text,
                    Tokens = embedding.Tokens,
                    QuestionSequenceNumberInConversation = questionSequenceNumberInConversation
                };
                ret.Add(contextMessage);
            }
        }
        return ret;
    }
}