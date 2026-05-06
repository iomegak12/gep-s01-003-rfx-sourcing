using EventService.Services.Events;
using EventService.Services.LineItems;
using FluentValidation;

namespace EventService.Infrastructure.Validation;

public sealed class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .Length(3, 200);

        RuleFor(x => x.Category)
            .NotEmpty()
            .Length(2, 80);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(c => c.Trim().ToUpperInvariant() == "INR")
            .WithMessage("Currency must be 'INR'.");

        RuleFor(x => x.ResponseDeadlineUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Response deadline must be a future date.");
    }
}

public sealed class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventRequestValidator()
    {
        When(x => x.Title is not null, () =>
            RuleFor(x => x.Title!).Length(3, 200));

        When(x => x.Category is not null, () =>
            RuleFor(x => x.Category!).Length(2, 80));

        When(x => x.ResponseDeadlineUtc.HasValue, () =>
            RuleFor(x => x.ResponseDeadlineUtc!.Value)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Response deadline must be a future date."));
    }
}

public sealed class AddLineItemRequestValidator : AbstractValidator<AddLineItemRequest>
{
    public AddLineItemRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0)
            .WithMessage("Unit price must be greater than zero.");
    }
}
