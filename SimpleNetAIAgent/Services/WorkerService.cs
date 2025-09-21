using SimpleNetAIAgent.Helpers;
using SimpleNetAIAgent.Models;

namespace SimpleNetAIAgent.Services
{
    public interface IWorkerService
    {
        Task<ResponseModel> MainLogic(RequestModel request, CancellationToken cancellationToken);
    }
    public class WorkerService(ILogger<IWorkerService> logger, IYourApiService yourApiService, ILlmService llmService, IEvaluatorService evaluatorService, RagService ragService) : IWorkerService
    {
        public async Task<ResponseModel> MainLogic(RequestModel request, CancellationToken cancellationToken)
        {
            // Main logic to process the request and populate the responseModel
            ResponseModel responseModel = new();
            try
            {
                //Here I used API to show standards on api. But the main purpose is to get a string which contains your request just like a prompt you give to chatgpt
                ClaimModel? claimModel = await yourApiService.YourApiMethod(request.ClaimId!, cancellationToken);
                if (claimModel == null)
                {
                    logger.LogError("ClaimModel is null for ClaimId: {ClaimId}", request.ClaimId);
                    return responseModel;
                }

                //Normalize the notes text and assigning same value to claimid of response that we got in request
                claimModel.Notes = TextNormalize.NormalizeText(claimModel.Notes?.Trim()!);
                responseModel.ClaimId = request.ClaimId;

                // Upon the user feedback, move the RAG entry from good to bad list.
                // You can enhance this logic as per your requirement to include unique id
                if (!request.IsThisFristRequestForClaim &&
                    !(request.FeedbackPatientId || request.FeedbackSummary))
                {
                    ragService.MoveRagFromGoodToBad(request.ClaimId!);
                }

                string ragExamples = ragService.LoadRagEntries() ?? string.Empty;
                responseModel = llmService.GetResponseFromAI(claimModel.Notes!, ragExamples, request.ClaimId!, cancellationToken)!;

                if (responseModel == null)
                {
                    logger.LogError("LLM response is null for ClaimId: {ClaimId}", request.ClaimId);
                    return responseModel!;
                }

                EvaluatorModel? evaluationResponse = evaluatorService.GetResponseFromAI(claimModel.Notes!, ragExamples, responseModel, cancellationToken);
                if (evaluationResponse == null)
                {
                    logger.LogError("Evaluator response is null for ClaimId: {ClaimId}", request.ClaimId);
                }

                logger.LogInformation("Evaluator's evaluation: {str}", evaluationResponse?.AiEvaluation);

                if (evaluationResponse!.FeedbackSummary && evaluationResponse!.FeedbackPatientId)
                {
                    ragService.SaveRagEntry(claimModel.Notes!, responseModel, isGood: true);
                }
                else
                {
                    ragService.SaveRagEntry(claimModel.Notes!, responseModel, isGood: false);
                }

                return responseModel;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in MainLogic of WorkerService");
                // Handle exceptions as needed
            }
            return responseModel;
        }
    }
}