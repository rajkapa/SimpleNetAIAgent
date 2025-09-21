namespace SimpleNetAIAgent.Models
{
    public sealed record ClaimModel
    {
        public string ClaimId { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}