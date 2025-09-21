namespace SimpleNetAIAgent.Models
{
    public sealed record ResponseModel
    {
        public string? ClaimId { get; set; }
        public string? PatientId { get; set; }
        public string? Summary { get; set; }
    }
}