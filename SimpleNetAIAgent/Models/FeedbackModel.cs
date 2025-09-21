namespace SimpleNetAIAgent.Models
{
    public record FeedbackModel
    {
        public bool FeedbackPatientId { get; set; }
        public bool FeedbackSummary { get; set; }
    }
}