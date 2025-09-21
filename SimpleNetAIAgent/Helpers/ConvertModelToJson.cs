using SimpleNetAIAgent.Models;
using System.Text.Json;

namespace SimpleNetAIAgent.Helpers
{
    public static class ConvertModelToJson
    {
        public static string ToJson(object obj)
        {
            return JsonSerializer.Serialize(obj);
        }
        public static string ResponseModelToJson()
        {
            return JsonSerializer.Serialize(new ResponseModel(), CustomJsonSerializeProperties.Default.ResponseModel);
        }
        public static string EvaluatorModelToJson()
        {
            return JsonSerializer.Serialize(new ResponseModel(), CustomJsonSerializeProperties.Default.EvaluatorModel);
        }
    }
}