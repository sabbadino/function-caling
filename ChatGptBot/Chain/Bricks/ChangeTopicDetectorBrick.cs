using System.Diagnostics;
using System.Text;
using Azure.AI.OpenAI;
using ChatGptBot.Chain.Dto;
using ChatGptBot.Ioc;
using ChatGptBot.Settings;
using Microsoft.Extensions.Options;
using SharpToken;
using ChatGptBot.Chain;
namespace ChatGptBot.Chain.Bricks;

public class ChangeTopicDetectorBrick : LangChainBrickBase, ILangChainBrick, ISingletonScope
{
    private readonly OpenAIClient _openAiClient;
    private readonly GptEncoding _gptEncoding;

    private readonly ChatGptSettings _chatGptSettings;
    private const string NotPartOfOngoingConversation = "no";
    


    public ChangeTopicDetectorBrick(OpenAIClient openAIClient,
        IOptions<ChatGptSettings> chatGptSettings, GptEncoding gptEncoding)
    {
        _openAiClient = openAIClient;
        _gptEncoding = gptEncoding;
        _chatGptSettings = chatGptSettings.Value;
    }



    private static void AddChatHistory(ChatCompletionsOptions chatCompletionsOptions, List<ConversationHistoryMessage> conversationHistoryMessages)
    {
       
        foreach (var conversationHistoryMessage in conversationHistoryMessages)
        {
            chatCompletionsOptions.Messages.Add(new ChatMessage
            {
                Role = ChatMessageExtensions.ChatRoleFromString( conversationHistoryMessage.ChatRole), Content = conversationHistoryMessage.Text
            });
        }

      
    }

    public override async Task<Answer> Ask(Question question)
    {
        if (Next == null)
        {
            throw new Exception($"{GetType().Name} cannot be the last item of the chain");
        }

        if (question.ConversationHistoryMessages.Count != 0)
        {
          
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                Temperature = _chatGptSettings.CondensationTemperature
            };
            chatCompletionsOptions.Messages.Add(new
                ChatMessage(ChatRole.System, _chatGptSettings.TopicChangeDetectorSystemMessage));
            
            //chatCompletionsOptions.Messages.Add(new
            //    ChatMessage(ChatRole.User, "Beginning of Chat History: "));
            AddChatHistory(chatCompletionsOptions, question.ConversationHistoryMessages);
            //chatCompletionsOptions.Messages.Add(new
            //    ChatMessage(ChatRole.User, "End of Chat History"));

            chatCompletionsOptions.Messages.Add(new
                ChatMessage(ChatRole.User, $"{question.UserQuestion.Text}"));
            chatCompletionsOptions.Messages.Add(new
                ChatMessage(ChatRole.User, _chatGptSettings.TopicChangeDetectorQueryToAi
                    ));

            var response = await _openAiClient.GetChatCompletionsAsync(
                deploymentOrModelName: _chatGptSettings.ModelName,
                chatCompletionsOptions);
            var completion = response.Value.Choices[0].Message;
            Debug.WriteLine(completion.Content);
            if ((completion.Content??"").StartsWith( NotPartOfOngoingConversation,StringComparison.OrdinalIgnoreCase))
            {
                question.UserHasChangedTopic=true;
            }
        }
        return await Next.Ask(question);
    }
}