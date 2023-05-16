using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.Bing.WebSearch;
using OpenAITextGenerator.Shared;

namespace OpenAITextGenerator.Services
{
    

    
    public class SearchSkill
    {
        string bingSearchKey = System.Environment.GetEnvironmentVariable("BING_SEARCH_KEY");


        
        
        [SKFunction("Get Search Results Based On Player Name")]
        [SKFunctionContextParameter(Name = "NoOfResults", Description = "Count Of results to be returned")]
        [SKFunctionContextParameter(Name = "InputSearch", Description = "String to be searched")] 
        public async  Task<string> SearchResults(SKContext context)
        {
            var bingSearchClient = new WebSearchClient(new ApiKeyServiceClientCredentials(bingSearchKey));
            var bingWebData = await bingSearchClient.Web.SearchAsync(query: context.Variables["InputSearch"],count: int.Parse(context.Variables["NoOfResults"]));
            var webSearchResultsString = "Web search results:\r\n\r\n";
            var footNotes = string.Empty;
            var bingSearchId = 0;
            List<WebSearchResult> webSearchResults = new List<WebSearchResult>();
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
            }
            return Newtonsoft.Json.JsonConvert.SerializeObject(webSearchResults); 
        }


    }


}
