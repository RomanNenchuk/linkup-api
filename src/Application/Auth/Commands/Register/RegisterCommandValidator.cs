using Application.Auth.Commands.Register;
using FluentValidation;

namespace FantasyGolf.Application.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Please specify your name")
            .MinimumLength(2).WithMessage("Please enter a valid name")
            .MaximumLength(50).WithMessage("Please enter a valid name")
            .Must(name => name == name.Trim()).WithMessage("Please enter a valid name");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Please enter a valid email address")
            .Must(email => email == email.Trim()).WithMessage("Please enter a valid email address");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$")
            .WithMessage("Password must have at least one uppercase letter, one lowercase letter and one number");
    }
}