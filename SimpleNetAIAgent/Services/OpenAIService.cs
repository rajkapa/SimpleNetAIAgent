using Azure.AI.OpenAI;
using SimpleNetAIAgent.Models;
using OpenAI.Chat;
using System.ClientModel;

namespace SimpleNetAIAgent.Services
{
    public interface IOpenAIService
    {
        Task<string?> CompleteAsync(string userPrompt, string systemPrompt, CancellationToken cancellationToken);
    }
    public sealed class OpenAIService : IOpenAIService
    {
        private readonly ILogger<IOpenAIService> logger;
        private readonly LlmDetails llmConfigDetails;
        private readonly ChatClient chatClient;
        public OpenAIService(ILogger<IOpenAIService> openAIServiceLogger, LlmDetails llmDetails, AzureOpenAIClient azureOpenAIClient)
        {
            logger = openAIServiceLogger;
            llmConfigDetails = llmDetails;
            chatClient = azureOpenAIClient.GetChatClient(llmConfigDetails.Model); // can be Azure deployment name/id
        }
        public async Task<string?> CompleteAsync(string userPrompt, string systemPrompt, CancellationToken cancellationToken)
        {
            List<ChatMessage> chatMessages =
                [
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt),
                ];

            ChatCompletionOptions completionOptions = new()
            {
                Temperature = llmConfigDetails.Temperature,
                MaxOutputTokenCount = llmConfigDetails.MaxOutputTokens
            };

            try
            {
                ClientResult<ChatCompletion> response = await chatClient.CompleteChatAsync(chatMessages, completionOptions, cancellationToken);
                string? result = response.Value.Content.FirstOrDefault()?.Text;
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while calling OpenAI Models in Azure");
            }
            return null;
        }
    }
}