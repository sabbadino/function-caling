using Azure.AI.Translation.Text;
using ChatGptBot.Chain.Dto;
using ChatGptBot.Ioc;

namespace ChatGptBot.Chain.Bricks;

public class AnswerTranslatorBrick : LangChainBrickBase, ILangChainBrick, ISingletonScope
{
    private readonly TextTranslationClient _textTranslationClient;



    public AnswerTranslatorBrick(TextTranslationClient textTranslationClient)
    {
        _textTranslationClient = textTranslationClient;
    }

    public override async Task<Answer> Ask(Question question)
    {
        if (Next == null)
        {
            throw new Exception($"{GetType().Name} cannot be the last item of the chain");
        }

        var ret = await Next.Ask(question);
        if (!string.IsNullOrEmpty(ret.AnswerFromChatGpt))
        {
            if (question.AnswerRequiresTranslation())
            {
                var translationResponse = await _textTranslationClient.TranslateAsync(
                    question.UserQuestion.DetectedUserQuestionLanguageCode
                    , ret.AnswerFromChatGpt, QuestionTranslatorBrick.TargetLanguageCode);
                var answerTranslatedToQuestionLanguage =
                    translationResponse?.Value.FirstOrDefault()?.Translations.FirstOrDefault();
                var translationResponseText = answerTranslatedToQuestionLanguage?.Text ?? ret.AnswerFromChatGpt;
                ret.TranslatedAnswerFromAi = translationResponseText;
            }
            else
            {
                ret.TranslatedAnswerFromAi = ret.AnswerFromChatGpt;
            }
        }
        return ret;
    }
}