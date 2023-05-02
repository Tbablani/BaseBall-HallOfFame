using OpenAITextGenerator.Shared;

namespace OpenAITextGenerator.Services
{
    public interface IRedisServices
    {
        void AddNarrative(MLBBatterInfo mlBBatterInfo, string generatedNarrative);
        void AddWebSearchResults(MLBBatterInfo mLBBatterInfo, string searchString, List<WebSearchResult> webSearchResults);
        List<NarrativeResult> GetNarratives(MLBBatterInfo mlBBatterInfo);
        List<WebSearchResult> GetWebSearchResults(MLBBatterInfo mLBBatterInfo, string searchString);
        bool ShouldAddNarrativeCount(MLBBatterInfo mlBBatterInfo);

        bool IsConnected();
    }
}