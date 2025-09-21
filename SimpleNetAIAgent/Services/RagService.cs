using SimpleNetAIAgent.Helpers;
using SimpleNetAIAgent.Models;
using System.Text.Json;

namespace SimpleNetAIAgent.Services
{
    public sealed class RagService
    {
        private readonly ILogger<RagService> logger;
        private readonly RagDetails ragConfigDetails;
        private readonly object gate = new();
        public RagService(ILogger<RagService> ragServiceLogger, RagDetails ragDetails)
        {
            logger = ragServiceLogger;
            ragConfigDetails = ragDetails;
            _ = Directory.CreateDirectory(ragConfigDetails.Location);
            EnsureDirs();
        }
        private void EnsureDirs()
        {
            string goodFilesDir = Path.Combine(ragConfigDetails.Location, RagLabels.GoodEntry);
            string wrongFilesDir = Path.Combine(ragConfigDetails.Location, RagLabels.BadEntry);
            _ = Directory.CreateDirectory(goodFilesDir);
            _ = Directory.CreateDirectory(wrongFilesDir);
        }
        public void MoveRagFromGoodToBad(string claimId)
        {
            lock (gate) // Ensure thread safety
            {
                try
                {
                    string goodFilesDir = Path.Combine(ragConfigDetails.Location, RagLabels.GoodEntry);
                    string wrongFilesDir = Path.Combine(ragConfigDetails.Location, RagLabels.BadEntry);
                    string[] files = Directory.GetFiles(goodFilesDir, $"{claimId}_*.json");
                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        if (fileName is not null)
                        {
                            string destFile = Path.Combine(wrongFilesDir, fileName);
                            if (File.Exists(destFile))
                            {
                                File.Delete(destFile);
                            }
                            File.Move(file, destFile);

                            //Confidence Score will still be noting as good, change it accordingly
                            string content = File.ReadAllText(destFile);
                            RagModel? ragEntry = JsonSerializer.Deserialize(content, CustomJsonSerializeProperties.Default.RagModel);

                            // This is where Records shine with shallow copy and non-destructive mutation
                            RagModel? updatedRagEntry = ragEntry! with { Score = 0.4 };
                            string jsonContent = JsonSerializer.Serialize(value: updatedRagEntry, jsonTypeInfo: CustomJsonSerializeProperties.Default.RagModel);
                            File.WriteAllText(destFile, jsonContent);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error moving RAG files from Good to Bad for ClaimId: {ClaimId}", claimId);
                }
            }
        }
        public void SaveRagEntry(string text, ResponseModel response, bool isGood)
        {
            lock (gate) // Ensure thread safety
            {
                try
                {
                    string subDir;
                    double confidenceScore;
                    if (isGood)
                    {
                        subDir = RagLabels.GoodEntry;
                        confidenceScore = 0.8;
                    }
                    else
                    {
                        subDir = RagLabels.BadEntry;
                        confidenceScore = 0.4;
                    }
                    string targetDir = Path.Combine(ragConfigDetails.Location, subDir);
                    string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                    string fileName = $"{response.ClaimId}_{timestamp}.json";
                    string filePath = Path.Combine(targetDir, fileName);
                    RagModel ragEntry = new(
                        Text: text,
                        Entities:
                        [
                            new(RagLabels.ClaimLabel, response.ClaimId ?? string.Empty),
                            new(RagLabels.PatientLabel, response.PatientId ?? string.Empty),
                            new(RagLabels.SummaryLabel, response.Summary ?? string.Empty)
                        ],
                        Score: confidenceScore
                    );
                    string jsonContent = JsonSerializer.Serialize(value: ragEntry, jsonTypeInfo: CustomJsonSerializeProperties.Default.RagModel);
                    File.WriteAllText(filePath, jsonContent);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error saving RAG entry for ClaimId: {ClaimId}", response.ClaimId);
                }
            }
        }
        public string? LoadRagEntries()
        {
            try
            {
                string goodFilesDir = Path.Combine(ragConfigDetails.Location, RagLabels.GoodEntry);
                string wrongFilesDir = Path.Combine(ragConfigDetails.Location, RagLabels.BadEntry);
                List<string> picks = new(ragConfigDetails.MaxRetrivedModels);
                picks.AddRange(GetFilesRecursively(goodFilesDir, Math.Min(ragConfigDetails.MaxGoodFilesPerField, ragConfigDetails.MaxRetrivedModels)));
                if(picks.Count < ragConfigDetails.MaxRetrivedModels)
                {
                    picks.AddRange(GetFilesRecursively(wrongFilesDir, ragConfigDetails.MaxRetrivedModels - picks.Count));
                }
                return string.Join(", ", picks);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading RAG entries");
            }
            return null!;
        }
        private static IEnumerable<string> GetFilesRecursively(string rootPath, int n)
        {
            if (!Directory.Exists(rootPath))
            {
                yield break;
            }
            foreach (string file in Directory.EnumerateFiles(rootPath, "*.json").OrderByDescending(File.GetCreationTimeUtc).Take(n))
            {
                yield return File.ReadAllText(file);
            }
        }
    }
}