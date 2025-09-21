using SimpleNetAIAgent.Helpers;
using SimpleNetAIAgent.Models;
using System.Text.Json;

namespace SimpleNetAIAgent.Services
{
    public interface IYourApiService
    {
        Task<ClaimModel?> YourApiMethod(string claimId, CancellationToken cancellationToken);
    }
    public class YourApiService(ILogger<YourApiService> logger, HttpClient httpClient) : IYourApiService
    {
        public async Task<ClaimModel?> YourApiMethod(string claimId, CancellationToken cancellationToken)
        {
            logger.LogInformation("YourApiMethod called");
            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync(requestUri: $"/claims/{claimId}", cancellationToken: cancellationToken);
                return JsonSerializer.Deserialize(json: await response.Content.ReadAsStringAsync(cancellationToken), jsonTypeInfo: CustomJsonSerializeProperties.Default.ClaimModel)!;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error calling YourApiMethod");
                //throw; //if you want to propagate the exception uncomment this line
            }
            return null!;
        }
    }
}