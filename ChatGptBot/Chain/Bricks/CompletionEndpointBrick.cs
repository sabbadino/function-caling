using Azure.AI.OpenAI;
using ChatGptBot.Chain.Dto;
using ChatGptBot.Ioc;
using ChatGptBot.Services.FunctionsCalling;
using ChatGptBot.Settings;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Options;

namespace ChatGptBot.Chain.Bricks;

public class CompletionEndpointBrick : LangChainBrickBase, ILangChainBrick, ISingletonScope
{
    private readonly OpenAIClient _openAiClient;
    private readonly IFunctionCaller _functionCaller;

    public CompletionEndpointBrick(OpenAIClient openAiClient, IFunctionCaller functionCaller)
    {
        _openAiClient = openAiClient;
        _functionCaller = functionCaller;
    }

    public override async Task<Answer> Ask(Question question)
    {
            if (string.IsNullOrEmpty(question.UserQuestion.Text))
            {
                throw new Exception($"{nameof(Question)} is null");
            }

            var chatCompletionsOptions = new ChatCompletionsOptions();
            question.SystemMessages.ForEach(systemMessage =>
            {
                if (!string.IsNullOrEmpty(systemMessage.Text))
                {
                    chatCompletionsOptions.Messages.Add(new
                        ChatMessage(ChatRole.System, systemMessage.Text));
                }
            });

            question.ContextIntroMessages.ForEach(contentIntroMessage => chatCompletionsOptions.Messages.Add(new
                ChatMessage(ChatRole.User, contentIntroMessage.Text)));

            question.ContextMessages.OrderBy(c => c.Proximity).ToList()
                .ForEach(contentMessage => chatCompletionsOptions.Messages.Add(new
                ChatMessage(ChatRole.User, contentMessage.Text)));


            AddChatHistory(chatCompletionsOptions, question.ConversationHistoryMessages);

            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, $"user prompt: {question.UserQuestion.Text}"));

            chatCompletionsOptions.Temperature = question.QuestionOptions.Temperature;

            chatCompletionsOptions.Functions = question.Functions.Select(f => f.Function).ToList();

        var response = await _openAiClient.GetChatCompletionsAsync(
                deploymentOrModelName: question.ModelName,
                chatCompletionsOptions);
            var choice = response.Value.Choices[0];


            if (choice.FinishReason == CompletionsFinishReason.FunctionCall && !string.IsNullOrEmpty(choice.Message.FunctionCall?.Name))
            {
                var ret = await _functionCaller.CallFunction(choice.Message.FunctionCall);
                chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.Function, ret.Result) { Name = choice.Message.FunctionCall.Name });
                chatCompletionsOptions.FunctionCall = FunctionDefinition.None;
                // i should check for token limit 
                // for the moment i avoid to have another reply as function setting Function_call4.None 
                response = await _openAiClient.GetChatCompletionsAsync(
                    deploymentOrModelName: question.ModelName,
                    chatCompletionsOptions);
                return new Answer { AnswerFromChatGpt= response.Value.Choices.ToList()[0].Message.Content ?? "" };
            }
            else
            {
                return new Answer { AnswerFromChatGpt = choice.Message.Content ?? "" };
            }

        
        
    }

    private static void AddChatHistory(ChatCompletionsOptions chatCompletionsOptions, List<ConversationHistoryMessage> conversationHistoryMessages)
    {

        foreach (var conversationHistoryMessage in conversationHistoryMessages)
        {
            chatCompletionsOptions.Messages.Add(new ChatMessage
            {
                Role = ChatMessageExtensions.ChatRoleFromString(conversationHistoryMessage.ChatRole),
                Content = conversationHistoryMessage.Text
            });
        }


    }

}

