namespace SimpleNetAIAgent.Models
{
    public sealed record EvaluatorDetails(string Model);

    public sealed record LlmDetails(
        string ApiKey,
        string Endpoint,
        int MaxOutputTokens,
        string Model,
        string TelemetryEndpoint,
        int Temperature);

    public sealed record NerDetails(string Model);

    public sealed record RagDetails(
        string Location,
        int MaxGoodFilesPerField,
        int MaxRetrivedModels,
        int MaxWrongFilesPerField);
}