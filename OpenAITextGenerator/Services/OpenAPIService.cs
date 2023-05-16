using OpenAITextGenerator.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Reliability;
using Microsoft.SemanticKernel.Orchestration;

namespace OpenAITextGenerator.Services
{
    public class OpenAPIService : IOpenAPIService
    {
        // OpenAI - Retrieve Keys
        string azureOpenAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
        string azureOpenAIDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
        string openAPIKey = Environment.GetEnvironmentVariable("OPENAI_KEY");

        public OpenAPIService()
        {
        }

        public async Task<string> GetSematicFunctionResults(string instruction, string webSearchResultsString, ContextVariables myContext)
        {
           
            IKernel kernel = Kernel.Builder
            .Configure(c => c.SetDefaultHttpRetryConfig(new HttpRetryConfig
            {
                MaxRetryCount = 3,
                UseExponentialBackoff = true,
                //  MinRetryDelay = TimeSpan.FromSeconds(2),
                //  MaxRetryDelay = TimeSpan.FromSeconds(8),
                //  MaxTotalRetryTime = TimeSpan.FromSeconds(30),
                //  RetryableStatusCodes = new[] { HttpStatusCode.TooManyRequests, HttpStatusCode.RequestTimeout },
                //  RetryableExceptions = new[] { typeof(HttpRequestException) }
            }))
            .Build();


            kernel.Config.AddOpenAITextCompletionService("text-davinci-003", "text-davinci-003", openAPIKey);
            var resultsAndInstructions = webSearchResultsString + instruction;
            var QuestionFunction = kernel.CreateSemanticFunction(resultsAndInstructions, maxTokens: 500);

            var result = await kernel.RunAsync(myContext, QuestionFunction);

            var openAICompletionResponseGeneratedText = result.ToString().Trim();


            return openAICompletionResponseGeneratedText;
        }
            public async Task<string> GetResults(string instruction, string webSearchResultsString)
        {

            using var client = new HttpClient();
            client.BaseAddress = new Uri(string.Format("https://{0}.openai.azure.com/openai/deployments/text-davinci-003-demo/completions?api-version=2022-12-01", azureOpenAIDeployment));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
            client.DefaultRequestHeaders.Add("api-key", azureOpenAIKey);

            // OpenAI - Body
            var resultsAndInstructions = webSearchResultsString + instruction;
            // OpenAI - Calculate the max tokens
            var resultsAndInstructionsLength = resultsAndInstructions.Length;
            var resultsAndInstructionsTokensEstimate = resultsAndInstructionsLength / 4;
            int maxTokens = Convert.ToInt32(resultsAndInstructionsTokensEstimate * 1.8) > 2000 ? 2000 : Convert.ToInt32(resultsAndInstructionsTokensEstimate * 1.6);

            // OpenAI - Completions Settings
            var openAICompletions = new OpenAICompletions()
            {
                prompt = resultsAndInstructions,
                max_tokens = maxTokens,
                temperature = 0.26f,
                top_p = 0.84f,
                frequency_penalty = 0.12f,
                presence_penalty = 0.12f,
                stop = string.Empty
            };
            var openAICompletionsJsonString = JsonSerializer.Serialize(openAICompletions);

            // OpenAI - Post Request
            var openAIRequestBody = new StringContent(openAICompletionsJsonString, Encoding.UTF8, "application/json");
            var opeanAIResponse = await client.PostAsync(client.BaseAddress, openAIRequestBody);
            var openAICompletionResponseBody = await opeanAIResponse.Content.ReadAsStringAsync();
            var openAICompletionResponse = JsonSerializer.Deserialize<OpenAICompletionsResponse>(openAICompletionResponseBody);

            var openAICompletionResponseGeneratedText = openAICompletionResponse?.choices[0].text.Trim();


            return openAICompletionResponseGeneratedText;
        }

    }
}
