namespace SimpleNetAIAgent.Helpers
{
    public static partial class TextNormalize
    {
        public static string NormalizeText(string text)
        {
            return CustomRegex.SentenceCandidate()
                .Replace(text.Replace('-', '-').Replace("\r\n", " ").Replace('\n', ' '), " ") //To replace unexcepted characters/format which code can't recognize
                .Trim()
                .ToLower();
        }
    }
}