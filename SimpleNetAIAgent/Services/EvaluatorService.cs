using SimpleNetAIAgent.Helpers;
using SimpleNetAIAgent.Models;
using System.Text.Json;

namespace SimpleNetAIAgent.Services
{
    public interface IEvaluatorService
    {
        EvaluatorModel? GetResponseFromAI(string note, string ragExamples, ResponseModel responseModel, CancellationToken cancellationToken);
    }
    public sealed class EvaluatorService(ILogger<IEvaluatorService> logger, OpenAIService openAIClient) : IEvaluatorService
    {
        public EvaluatorModel? GetResponseFromAI(string note, string ragExamples, ResponseModel responseModel, CancellationToken cancellationToken)
        {
            try
            {
                string userPrompt = PromptText.EvaluationPrompt
                    .Replace("__NOTE_TEXT__", note)
                    .Replace("__RAG_EXAMPLES__", ragExamples)
                    .Replace("__EXTRACTION_RESULT_JSON__", JsonSerializer.Serialize(responseModel, CustomJsonSerializeProperties.Default.ResponseModel));
                string? json = openAIClient.CompleteAsync(userPrompt, PromptText.EvaluationSystemPrompt, cancellationToken).GetAwaiter().GetResult()!;
                if (string.IsNullOrWhiteSpace(json))
                {
                    logger.LogError("LLM Service returned null or empty response");
                    return null!;
                }
                else
                {
                    return JsonSerializer.Deserialize(json, CustomJsonSerializeProperties.Default.EvaluatorModel)!;
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