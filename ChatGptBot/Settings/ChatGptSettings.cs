namespace ChatGptBot.Settings
{
    public class ChatGptSettings
    {
        public bool IsAzureChatGpt { get; init; } = true;
        public string OpenAiApiKey { get; init; } = "";
        

        public string AzureChatGptEndPoint { get; init; } = "";

        public string AzureOpenAiApiKey { get; init; } = "";

        public string ModelName { get; init; } = "";

        public string EmbeddingsModel { get; init; } ="";

        public float SimilarityThreshold { get; init; } = 0.75f;
        
        public string SystemMessage { get; init; } = "";
        public float Temperature { get; init; } = 0.2f;

        public float CondensationTemperature { get; init; } = 0.0f;
        public string DefaultEmbeddingSetCode { get; init; } = "mymsc";
        public int DefaultEmbeddingMatchMaxItems { get; init; } = 5;

        public int DefaultTotalEmbeddingMatchMaxItems { get; init; } = 10;

        public int MaxTokens { get; init; } = 4096;


        public int MinimumAvailableTokensForTokenForAnswer => 50;

        public float MaxAllowedTokenRatioForUserQuestion => Convert.ToInt32(MaxTokens * 0.05f);
        public int MaxConversationHistoryPairsToLoad { get; init; } = 6;

        public string TikToken { get; init; } = "cl100k_base";

        public string TopicChangeDetectorSystemMessage { get; set; } = "Topic Change Detection";

        public string TopicChangeDetectorQueryToAi { get; set; } = "Is the last question of the user about the same topic of the previous part of the conversation?";

    }
}
