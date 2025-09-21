using FluentValidation;

namespace SimpleNetAIAgent.Validators
{
    //This class is optional.
    //Here I have written to log the fluent details through serilog
    //And you can also use it to send the bad request in a specific format.
    public sealed class ValidationHandler<T>(ILogger<ValidationHandler<T>> logger) : IEndpointFilter where T : class
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            IValidator<T>? validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
            if (validator is not null)
            {
                var argument = context.Arguments.SingleOrDefault(argument => argument?.GetType() == typeof(T));
                if (argument is not T request)
                {
                    // This case should not be hit if the endpoint is correctly typed.
                    return Results.BadRequest("Invalid request");
                }
                FluentValidation.Results.ValidationResult? validationResult = await validator.ValidateAsync(instance: request, cancellation: context.HttpContext.RequestAborted);
                if(!validationResult.IsValid)
                {
                    logger.LogWarning("Validation failed for endpoint {EndpointName}. Errors: {@Errors}"
                        , context.HttpContext.Request.Path
                        , validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage}));
                    return Results.BadRequest("Validation Errors");
                }
            }
            return await next(context);
        }
    }
}