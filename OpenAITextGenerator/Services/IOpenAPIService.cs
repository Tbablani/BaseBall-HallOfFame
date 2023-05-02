namespace OpenAITextGenerator.Services
{
    public interface IOpenAPIService
    {
        Task<string> GetResults(string instruction, string webSearchResultsString);

        Task<string> GetSematicFunctionResults(string instruction, string webSearchResultsString);
    }
}