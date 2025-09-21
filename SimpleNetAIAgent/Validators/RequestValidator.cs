using FluentValidation;
using SimpleNetAIAgent.Helpers;
using SimpleNetAIAgent.Models;

namespace SimpleNetAIAgent.Validators
{
    public class RequestValidator : AbstractValidator<RequestModel>
    {
        public RequestValidator()
        {
            _ = RuleFor(x => x.ClaimId)
                .NotEmpty().WithMessage("ClaimId is required.")
                .Matches(CustomRegex.ClaimOrPatientIdStrict())
                .WithMessage("ClaimId must be alphanumeric with optional dashes.");
        }
    }
}