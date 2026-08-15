using FluentValidation;
using Meterly.Application.Auth;

namespace Meterly.Application.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(v => v.TenantName)
            .NotEmpty().WithMessage("Tenant Name is required.")
            .MaximumLength(100).WithMessage("Tenant Name must not exceed 100 characters.");

        RuleFor(v => v.FirstName)
            .NotEmpty().WithMessage("First Name is required.")
            .MaximumLength(50).WithMessage("First Name must not exceed 50 characters.");

        RuleFor(v => v.LastName)
            .NotEmpty().WithMessage("Last Name is required.")
            .MaximumLength(50).WithMessage("Last Name must not exceed 50 characters.");

        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
    }
}
