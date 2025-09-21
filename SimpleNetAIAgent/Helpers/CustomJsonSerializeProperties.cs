using SimpleNetAIAgent.Models;
using System.Text.Json.Serialization;

namespace SimpleNetAIAgent.Helpers
{
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(ClaimModel))]
    [JsonSerializable(typeof(EvaluatorModel))]
    [JsonSerializable(typeof(RagModel))]
    [JsonSerializable(typeof(ResponseModel))]
    public sealed partial class CustomJsonSerializeProperties : JsonSerializerContext
    {
    }
}