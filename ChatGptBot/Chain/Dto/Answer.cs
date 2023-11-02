namespace ChatGptBot.Chain.Dto;

public class Answer
{
    public string TranslatedAnswerFromAi { get; set; } = "";
    public string AnswerFromChatGpt { get; set; } = "";
    public Guid ConversationId { get; set; } = Guid.Empty;
    public string QuestionLanguageCode { get; set; } = "";
}