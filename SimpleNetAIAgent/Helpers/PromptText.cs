using SimpleNetAIAgent.Models;

namespace SimpleNetAIAgent.Helpers
{
    public static class PromptText
    {
        public static readonly string EvaluationPrompt = $@"Return STRICT JSON that matches the structure:
                                                {ConvertModelToJson.EvaluatorModelToJson()}

                                                Rules:
                                                - Feedback for {RagLabels.SummaryLabel}: 'true' if matches 80% of your summary, else 'false'
                                                - Feedback for {RagLabels.PatientLabel}: 'true' if matches {CustomRegex.ClaimOrPatientIdStrict()} and should not be the value of {RagLabels.ClaimLabel}. Or couldn't extract, else 'false'
                                                - AiEvaluatoin: Give your reason in one sentence on why you choose the value for each field. This field is string data type.

                                                NOTE:
                                                __NOTE_TEXT__

                                                EXTRACTION_RESULT_JSON:
                                                __EXTRACTION_RESULT_JSON__

                                                RAG Examples (may be empty). Examples contains texts and their extracted entities, along with a confidence score
                                                __RAG_EXAMPLES__";

        public static readonly string LlmPrompt = $@"Return STRICT JSON that matches the structure:
                                                {ConvertModelToJson.ResponseModelToJson()}

                                                Rules:
                                                - {RagLabels.ClaimLabel}: Return the same as you received in this field
                                                - {RagLabels.SummaryLabel}: Summarize the text. Maximum 2-3 sentences
                                                - {RagLabels.PatientLabel}: include only if it matches {CustomRegex.ClaimOrPatientIdStrict()}, else null. If it matches multiple, return the first match and should not be the value of {RagLabels.ClaimLabel}.

                                                {RagLabels.ClaimLabel}: __CLAIM_ID__

                                                NOTE:
                                                __NOTE_TEXT__

                                                RAG Examples (may be empty). Examples contains texts and their extracted entities, along with a confidence score:
                                                __RAG_EXAMPLES__";

        public static readonly string NerPrompt = $@"Return STRICT JSON that matches the structure:
                                                {ConvertModelToJson.ResponseModelToJson()}

                                                Rules:
                                                - {RagLabels.SummaryLabel}: Summarize the text. Maximum 2-3 sentences
                                                - {RagLabels.PatientLabel}: include only if it matches {CustomRegex.ClaimOrPatientIdStrict()}, else null. If it matches multiple, return the first match and should not be the value of {RagLabels.ClaimLabel}.

                                                NOTE:
                                                __NOTE_TEXT__

                                                RAG Examples (may be empty). Examples contains texts and their extracted entities, along with a confidence score
                                                __RAG_EXAMPLES__";
        
        public static readonly string EvaluationSystemPrompt = $@"You are a senior healthcare claim processor and compliance evaluator.
                                                You must evaluate the provided extraction JSON against domain rules, RAG hints.
                                                Feedback for each field have true which denotes good and false for bad";                
        
        public static readonly string LlmSystemPrompt = $@"You are astructured extraction assistant.
                                                Extract a list of entities from the provided text.";
        
        //Open AI models doesn't have NER seperately, using the same as LLM and hence different system prompt. For other NERs this will be different.
        public static readonly string NerSystemPrompt = $@"You are an expert at Named Entity Recognition.
                                                Extract a list of entities from the provided text.";
    }
}