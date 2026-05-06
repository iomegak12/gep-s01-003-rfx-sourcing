using EventService.Services.Invitations;
using FluentValidation;

namespace EventService.Infrastructure.Validation;

public sealed class InviteSupplierRequestValidator : AbstractValidator<InviteSupplierRequest>
{
    public InviteSupplierRequestValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty()
            .WithMessage("SupplierId is required.");
    }
}
