using System.Globalization;
using Azure.AI.Translation.Text;
using ChatGptBot.Chain.Dto;
using ChatGptBot.Ioc;
using ChatGptBot.Settings;
using Microsoft.Extensions.Options;
using SharpToken;

namespace ChatGptBot.Chain.Bricks;

public class QuestionTranslatorBrick : LangChainBrickBase, ILangChainBrick, ISingletonScope
{
    private readonly TextTranslationClient _textTranslationClient;
    private readonly GptEncoding _gptEncoding;

    public static readonly string TargetLanguageCode = "en";
    public static readonly string TargetLanguageName = "english";
    private readonly CultureInfo[] _allCultures;
    public QuestionTranslatorBrick(IOptions<ChatGptSettings> openAi
        , TextTranslationClient textTranslationClient, GptEncoding gptEncoding)
    {
        _textTranslationClient = textTranslationClient;
        _gptEncoding = gptEncoding;
        _allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
    }

    public override async Task<Answer> Ask(Question question)
    {
        if (Next == null)
        {
            throw new Exception($"{GetType().Name} cannot be the last item of the chain");
        }

        if (string.IsNullOrEmpty(question.UserQuestion.Text))
        {
            return await Next.Ask(question);
        }
        var translationResponse = await _textTranslationClient.TranslateAsync(TargetLanguageCode, question.UserQuestion.Text);
        var translationToEnglish =
            translationResponse?.Value.FirstOrDefault()?.Translations.FirstOrDefault();
        var translationToEnglishText  = translationToEnglish?.Text ?? question.UserQuestion.Text;
        // Rewrite user question to english
        question.UserQuestion = question.UserQuestion  with
        {
            Text = translationToEnglishText
            , Tokens= _gptEncoding.Encode(translationToEnglishText).Count 
            , DetectedUserQuestionLanguageCode = translationResponse?.Value.FirstOrDefault()?.DetectedLanguage?.Language ?? TargetLanguageCode
            , DetectedUserQuestionLanguageDisplayName = _allCultures.FirstOrDefault(c => 
                c.Name == (translationResponse?.Value.FirstOrDefault()?.DetectedLanguage?.Language ?? TargetLanguageCode))?
                        .DisplayName ?? TargetLanguageName
        };


        var ret = await Next.Ask(question);
        ret.QuestionLanguageCode = question.UserQuestion.DetectedUserQuestionLanguageCode;
        return ret;
    }
}