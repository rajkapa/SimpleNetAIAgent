using Azure.AI.OpenAI;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using Polly.Timeout;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using SimpleNetAIAgent.Models;
using SimpleNetAIAgent.Services;
using SimpleNetAIAgent.Validators;
using System.ClientModel.Primitives;
using System.Net.Http.Headers;
using System.Text.Json;

//Serilog binding for logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    //Get appsettings details
    ConfigurationManager cfg = builder.Configuration;
    EvaluatorDetails evaluatorDetails = cfg.GetSection("EvaluatorDetail").Get<EvaluatorDetails>()!;
    LlmDetails llmDetails = cfg.GetSection("LlmDetail").Get<LlmDetails>()!;
    NerDetails nerDetails = cfg.GetSection("NerDetail").Get<NerDetails>()!;
    RagDetails ragDetails = cfg.GetSection("RagDetails").Get<RagDetails>()!;

    //Define Http Policies
    AsyncRetryPolicy<HttpResponseMessage> RetryPolicy(
        PolicyBuilder<HttpResponseMessage> policyBuilder) =>
            policyBuilder
            .WaitAndRetryAsync(
                Backoff.DecorrelatedJitterBackoffV2(
                    medianFirstRetryDelay: TimeSpan.FromMilliseconds(100)
                    , retryCount: 3
                    , fastFirst: false)
        );

    AsyncTimeoutPolicy<HttpResponseMessage> TimeoutPolicy() =>
            Policy.TimeoutAsync<HttpResponseMessage>(
                timeout: TimeSpan.FromSeconds(4000)
                , timeoutStrategy: TimeoutStrategy.Optimistic
                );


    //------------------- Add services to the container. -------------------

    //Bind Configuration to models
    _ = builder.Services.AddSingleton(evaluatorDetails);
    _ = builder.Services.AddSingleton(llmDetails);
    _ = builder.Services.AddSingleton(nerDetails);
    _ = builder.Services.AddSingleton(ragDetails);

    //register healthchecks
    _ = builder.Services.AddHealthChecks();
    _ = builder.Services.AddProblemDetails();

    // Add CORS policy (allow all)
    _ = builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", policy =>
        {
            _ = policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });

    //Adding Swagger generation
    _ = builder.Services.AddEndpointsApiExplorer();
    _ = builder.Services.AddSwaggerGen();

    //Serilog binding for logging
    _ = builder.Host.UseSerilog((ctx, services, configuration) =>
    {
        _ = configuration
            .ReadFrom.Configuration(ctx.Configuration)           // read settings from appsettings.json
            .ReadFrom.Services(services)                         // allow DI-based enrichers
            .Enrich.FromLogContext()
            .WriteTo.OpenTelemetry(cfg =>
            {
                cfg.Endpoint = llmDetails.TelemetryEndpoint;
                cfg.Protocol = OtlpProtocol.HttpProtobuf;
            });
    });

    // ===== Fleunt Validations =====
    _ = builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton);

    // ===== OpenTelemetry Tracing & Metrics =====
    _ = builder.Services.AddOpenTelemetry()
        .WithMetrics(metricsBuilder =>
        {
            _ = metricsBuilder
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(oltp =>
                {
                    oltp.Endpoint = new Uri(llmDetails.TelemetryEndpoint);
                });
        })
        .WithTracing(tracerProviderBuilder =>
        {
            _ = tracerProviderBuilder.ConfigureResource(resource => resource
                    .AddService(serviceName: builder.Environment.ApplicationName, serviceVersion: "1.0.0")
                    )
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("OpenAIClient")
            .AddOtlpExporter(oltp =>
            {
                oltp.Endpoint = new Uri(llmDetails.TelemetryEndpoint);
            });
        });

    // ===== Add HTTP Clients =====
    _ = builder.Services.AddHttpClient<IYourApiService, YourApiService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ApiDetails:BaseUrl"]!);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    })
        .AddTransientHttpErrorPolicy(RetryPolicy); // not adding TimeoutPolicy here

    _ = builder.Services.AddHttpClient("OpenAIClient")
        .AddTransientHttpErrorPolicy(RetryPolicy)
        .AddPolicyHandler(TimeoutPolicy());


    // Register a factory that builds AzureOpenAIClient using the named HttpClient
    _ = builder.Services.AddSingleton(provider =>
    {
        IHttpClientFactory httpFactory = provider.GetRequiredService<IHttpClientFactory>();
        HttpClient httpClient = httpFactory.CreateClient("OpenAIClient");

        // Create AzureOpenAIClientOptions (adjust per your SDK version)
        AzureOpenAIClientOptions options = new()
        {
            Transport = new HttpClientPipelineTransport(httpClient)
        };

        // The Azure OpenAI SDK class name might be OpenAIClient or AzureOpenAIClient depending on package version.
        // This client is lightweight when compared to others
        // You can use just HttpClient directly, but this allows you to have openAI telemetry 
        return new AzureOpenAIClient(endpoint: new Uri(llmDetails.Endpoint), credential: new System.ClientModel.ApiKeyCredential(llmDetails.ApiKey), options: options);
    });

    // ===== Add Application Services =====
    _ = builder.Services.AddSingleton<IEvaluatorService, EvaluatorService>();
    _ = builder.Services.AddSingleton<ILlmService, LlmService>();
    _ = builder.Services.AddSingleton<RagService>();
    _ = builder.Services.AddSingleton<IOpenAIService, OpenAIService>();
    _ = builder.Services.AddScoped<IWorkerService, WorkerService>();


    WebApplication app = builder.Build();

    // Use CORS
    _ = app.UseCors("CorsPolicy");
    _ = app.UseHttpsRedirection();

    //Adding Swagger generation only for Development
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Global exception handler
    _ = app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            IExceptionHandlerFeature? feature = context.Features.Get<IExceptionHandlerFeature>();
            Exception? ex = feature?.Error;

            // Log the exception
            app.Logger.LogError(ex, "Unhandled exception");

            var problem = new
            {
                Title = "An unexpected error occurred.",
                Status = 500,
                Detail = ex?.Message // optional: remove in production to avoid leaking details
            };

            string json = JsonSerializer.Serialize(problem);
            await context.Response.WriteAsync(json);
        });
    });

    //To capture more logging like request details, etc. in Serilog
    _ = app.UseSerilogRequestLogging();

    // Map liveness (quick) and readiness (comprehensive) endpoints
    _ = app.MapHealthChecks("/health/live", new()
    {
        Predicate = hc => hc.Tags.Contains("liveness"),
        AllowCachingResponses = false
    });

    _ = app.MapHealthChecks("/health/ready", new()
    {
        Predicate = hc => hc.Tags.Contains("readiness") || !hc.Tags.Any(),
        AllowCachingResponses = false
    });

    _ = app.MapHealthChecks("/health/empty", new()
    {
        Predicate = _ => false, // include no registered checks
        AllowCachingResponses = false
    });

    _ = app.MapGet("/yourendpoint", async ([AsParameters] RequestModel requestModel, IWorkerService workerService, CancellationToken cancellationToken) =>
    {
        return Results.Ok(await workerService.MainLogic(requestModel, cancellationToken));
    })
        .AddEndpointFilter<ValidationHandler<RequestModel>>()
        .WithOpenApi();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}