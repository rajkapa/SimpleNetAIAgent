using System.Text.RegularExpressions;

namespace SimpleNetAIAgent.Helpers
{
    public static partial class CustomRegex
    {
        [GeneratedRegex(@"^[A-Za-z0-9-]{10}$")]
        public static partial Regex ClaimOrPatientIdStrict();
        [GeneratedRegex(@"\b([A-Za-z0-9-]{10})\b")]
        public static partial Regex PatientIdCandidate(); //use this in data extrcted in regex and evaluate with LLM on strict regex to see the difference
        [GeneratedRegex(@"\s+")]
        public static partial Regex SentenceCandidate();
    }
}