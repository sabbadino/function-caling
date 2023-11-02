using ChatGptBot.Chain.Bricks;
using ChatGptBot.Chain.Dto;

namespace ChatGptBot.Chain;

public static class QuestionExtensions
{
    public static bool AnswerRequiresTranslation(this Question question)
    {
        return question.UserQuestion.DetectedUserQuestionLanguageCode != QuestionTranslatorBrick.TargetLanguageCode;
    }

    public static void RemoveLeastSignificantContextItem(this Question question)
    {
        var toBeRemoved = question.ContextMessages
            .OrderBy(c => c.QuestionSequenceNumberInConversation)
            .ThenBy(c => c.Proximity).FirstOrDefault();
        if (toBeRemoved != null)
        {
            question.ContextMessages.Remove(toBeRemoved);
        }
    }

    public static void RemoveOldestConversationEntryPair(this Question question, List<ContextMessage> contentMessages)
    {
        if (question.ConversationHistoryMessages.Count > 0)
        {
            //remove also content message elated to history message
            var id = question.ConversationHistoryMessages[0].Id;
            contentMessages.RemoveAll(c => c.ConversationItemId == id); 
            question.ConversationHistoryMessages.RemoveAt(0);
        }
        if (question.ConversationHistoryMessages.Count > 0)
        {
            var id = question.ConversationHistoryMessages[0].Id;
            contentMessages.RemoveAll(c => c.ConversationItemId == id);
            question.ConversationHistoryMessages.RemoveAt(0);
        }
    }

    
}