using SimpleNetAIAgent.Helpers;
using SimpleNetAIAgent.Models;
using System.Text.Json;

namespace SimpleNetAIAgent.Services
{
    public interface ILlmService
    {
        ResponseModel? GetResponseFromAI(string note, string ragExamples, string claimId, CancellationToken cancellationToken);
    }
    public class LlmService(ILogger<ILlmService> logger, OpenAIService openAIClient) : ILlmService
    {
        public ResponseModel? GetResponseFromAI(string note, string ragExamples, string claimId, CancellationToken cancellationToken)
        {
            try
            {
                string userPrompt = PromptText.LlmPrompt
                    .Replace("__NOTE_TEXT__", note)
                    .Replace("__RAG_EXAMPLES__", ragExamples)
                    .Replace("__CLAIM_ID__", claimId);
                string? json = openAIClient.CompleteAsync(userPrompt, PromptText.LlmSystemPrompt, cancellationToken).GetAwaiter().GetResult()!;
                if (string.IsNullOrWhiteSpace(json))
                {
                    logger.LogError("LLM Service returned null or empty response");
                    return null!;
                }
                else
                {
                    return JsonSerializer.Deserialize(json, CustomJsonSerializeProperties.Default.ResponseModel)!;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in GetResponseFromAI of LLM Service");
                //throw; //if needed
            }
            return null!;
        }
    }
}