# SimpleNetAIAgent

SimpleNetAIAgent is a .NET 8 web API application designed to process healthcare claims using advanced AI models, including LLMs (Large Language Models), NER (Named Entity Recognition), and RAG (Retrieval-Augmented Generation). The system integrates with Azure OpenAI and provides robust logging, validation, and telemetry.

## Features

- **AI-Driven Claim Processing:** Uses LLMs and NER to analyze and summarize claim data.
- **RAG Service:** Manages claim-related data entries, supports moving entries between "good" and "bad" states, and loads relevant examples for AI inference.
- **Evaluator Service:** Evaluates AI responses for quality and accuracy.
- **OpenAI Integration:** Connects to Azure OpenAI for advanced language processing.
- **Health Checks:** Provides liveness and readiness endpoints for monitoring.
- **Serilog Logging:** Structured logging with OpenTelemetry support.
- **FluentValidation:** Request validation via endpoint filters.
- **Polly Policies:** Resilient HTTP client configuration with retry and timeout policies.
- **CORS Support:** Configurable cross-origin resource sharing.

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Azure OpenAI credentials (endpoint and API key)
- Properly configured `appsettings.json` with required sections:
  - `EvaluatorDetail`
  - `LlmDetail`
  - `NerDetail`
  - `RagDetails`
  - `ApiDetails:BaseUrl`

### Installation

1. Clone the repository:

2. Restore NuGet packages:

3. Update `appsettings.json` with your configuration.

### Running the Application

The API will be available at `https://localhost:5001` (or as configured).

## Usage

### Endpoints

- **Health Checks**
  - `/health/live` — Liveness probe
  - `/health/ready` — Readiness probe
  - `/health/empty` — No checks (for testing)

- **Main Claim Processing**
  - `GET /yourendpoint`
    - Accepts a `RequestModel` (see below)
    - Processes the claim using AI and returns a `ResponseModel`

#### RequestModel Example
{ "ClaimId": "12345", "IsThisFristRequestForClaim": true, "FeedbackPatientId": false, "FeedbackSummary": false }

#### ResponseModel Example
{ "ClaimId": "12345", "PatientId": "67890", "Summary": "Claim details should be sent to XXX for review." }


## Project Structure

- `Program.cs` — Application entry point and service configuration.
- `Services/` — Contains core services (`RagService`, `WorkerService`, etc.).
- `Models/` — Data models for requests, responses, and configuration.
- `Validators/` — FluentValidation validators for request models.
- `Helpers/` — Utility classes and constants.

## Configuration

Ensure the sections with 'change as per your requirement' are present in your `appsettings.json`:


## Telemetry & Logging

- **Serilog** is used for structured logging.
- **OpenTelemetry** exports traces and metrics to the configured endpoint.

## Extending

- Implement additional services by following the interface patterns in `Services/`.
- I have given the prompt and config for NER but haven't provided the plugin in code as it is same as LlmService.cs
- Though I have use the same models for evaluation and extraction, you can take in further and configure seperately as the plugins for config, services, prompt, etc. are already there
- Add new endpoints in `Program.cs` using minimal API syntax.

## Next Phases
- MCP Servers in .NET 10.