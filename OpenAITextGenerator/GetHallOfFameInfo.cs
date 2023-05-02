using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Bing.WebSearch;
using Microsoft.Extensions.Logging;
using OpenAITextGenerator.Services;
using OpenAITextGenerator.Shared;

namespace OpenAITextGenerator
{
    public class GetHallOfFameInfo
    {
        private readonly ILogger _logger;
        private readonly IRedisServices _redisServices;
        private readonly IOpenAPIService _openAPIService;

        public GetHallOfFameInfo(ILoggerFactory loggerFactory,IRedisServices redisService, IOpenAPIService openAPIService)
        {
            _logger = loggerFactory.CreateLogger<GetHallOfFameInfo>();
            _redisServices = redisService;
            _openAPIService = openAPIService;
        }

        [Function("GetHallOfFameInfo")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            var requestBodyString = await req.GetRawBodyAsync(Encoding.UTF8);
            _logger.LogInformation("GenerateHallOfFameText - Request Body: " + requestBodyString);

            if (requestBodyString.Length < 20)
            {
                _logger.LogInformation("GenerateHallOfFameText - Not Enough to process error");
                response.StatusCode = HttpStatusCode.BadRequest;
                response.WriteString("Message request does not have enough inofrmation in the body request. Need MLBBatterInfo contract.");
                return response;
            }

            var mlbBatterInfo = JsonSerializer.Deserialize<MLBBatterInfo>(requestBodyString) ?? new MLBBatterInfo();
            _logger.LogInformation("GenerateHallOfFameText - MLBBatterInfo Deserialized");

            var narratives = new List<NarrativeResult>();
            // Cache - Determine if should pull from cache
            if (Util.UseRedisCache(_redisServices.IsConnected()))
            {
                _logger.LogInformation("GenerateHallOfFameText - Random selection attempting Cache");
                narratives = _redisServices.GetNarratives(mlbBatterInfo);
            }

            // Cache - Return the Narrative from Cache
            if (narratives.Count > 0)
            {
                var random = new Random(DateTime.Now.Second).Next(0, narratives.Count - 1);
                var selectedNarrative = narratives[random].Text;
                _logger.LogInformation("GenerateHallOfFameText - MLBBatterInfo - Narrative FOUND in Cache");

                // Successful response (OK)
                response.StatusCode = HttpStatusCode.OK;
                response.WriteString(selectedNarrative);
            }
            else
            {   // ELSE PROCESS either BING and/or OPENAI

                // Bing - Web Search Components
                var searchString = string.Format("{0} baseball Hall of Fame", mlbBatterInfo?.FullPlayerName);
                var webSearchResultsString = "Web search results:\r\n\r\n";
                var footNotes = string.Empty;
                var bingSearchId = 0;

                var webSearchResults = _redisServices.GetWebSearchResults(mLBBatterInfo: mlbBatterInfo, searchString);

                if (webSearchResults.Count > 0)
                {
                    _logger.LogInformation("GenerateHallOfFameText - MLBBatterInfo - WebSearchResults FOUND in Cache");

                    // Itertate over the Bing Web Pages (Cache)
                    foreach (var bingWebPage in webSearchResults)
                    {
                        bingSearchId++;

                        webSearchResultsString += string.Format("[{0}]: \"{1}: {2}\"\r\nURL: {3}\r\n\r\n",
                            bingSearchId, bingWebPage.Name, bingWebPage.Snippet, bingWebPage.Url);

                        footNotes += string.Format("[{0}]: {1}: {2}  \r\n",
                            bingSearchId, bingWebPage.Name, bingWebPage.Url);
                    }
                }
                else
                {
                    _logger.LogInformation("GenerateHallOfFameText - MLBBatterInfo - WebSearchResults NOT FOUND in Cache");

                    var bingSearchKey = System.Environment.GetEnvironmentVariable("BING_SEARCH_KEY");
                    var bingSearchClient = new WebSearchClient(new ApiKeyServiceClientCredentials(bingSearchKey));
                    var bingWebData = await bingSearchClient.Web.SearchAsync(query: searchString, count: 8);


                    if (bingWebData?.WebPages?.Value?.Count > 0)
                    {
                        // Itertate over the Bing Web Pages (Non-Cache Results)
                        foreach (var bingWebPage in bingWebData.WebPages.Value)
                        {
                            bingSearchId++;

                            webSearchResultsString += string.Format("[{0}]: \"{1}: {2}\"\r\nURL: {3}\r\n\r\n",
                                bingSearchId, bingWebPage.Name, bingWebPage.Snippet, bingWebPage.Url);

                            footNotes += string.Format("[{0}]: {1}: {2}  \r\n",
                                bingSearchId, bingWebPage.Name, bingWebPage.Url);

                            webSearchResults.Add(new WebSearchResult
                            {
                                Id = bingSearchId,
                                Name = bingWebPage.Name,
                                Snippet = bingWebPage.Snippet,
                                Url = bingWebPage.Url
                            });
                        }

                        // Add to Cache - WebSearchResults
                        _redisServices.AddWebSearchResults(mlbBatterInfo, searchString, webSearchResults);
                    }
                    
                }

                // OpenAI - Text Generator Components
                var promptInstructions = string.Format("The current date is {5}. Using most of the provided Web search results and probability and statistics found in the given query, write a comprehensive reply to the given query. " +
                    "Make sure to cite results using [number] notation of each URL after the reference. " +
                    "If the provided search results refer to multiple subjects with the same name, write separate answers for each subject. " +
                    "Query: An AI model states the probability of baseball hall of fame induction for {0} as {1}. {0} has played baseball for {2} years. Provide a detailed case supporting or against {0} to be considered for the Hall of Fame.\r\n",
                    mlbBatterInfo?.FullPlayerName, mlbBatterInfo?.HallOfFameProbability.ToString("P", CultureInfo.InvariantCulture), mlbBatterInfo?.YearsPlayed,
                    mlbBatterInfo?.HR, mlbBatterInfo?.TotalPlayerAwards, DateTime.Now.ToString("M/d/yyyy"));

                string openAPIResults = await _openAPIService.GetSematicFunctionResults(promptInstructions, webSearchResultsString);
                
                
                var fullNarrativeResponse = openAPIResults + "\r\n\r\n" + footNotes;
                _logger.LogInformation("GenerateHallOfFameText - OpenAI Text: " + openAPIResults);

                // Cache - Add Response To Cache
                //_redisServices.AddNarrative(mlbBatterInfo, fullNarrativeResponse ?? string.Empty);

                // Successful response (OK)
                response.StatusCode = HttpStatusCode.OK;
                response.WriteString(fullNarrativeResponse);
            }
        return response;
        }
    }
}
