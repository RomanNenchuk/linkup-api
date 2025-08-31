using FluentValidation;

namespace Application.Auth.Commands.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{

    public ResetPasswordCommandValidator()
    {
        RuleFor(v => v.VerificationToken)
            .NotEmpty()
            .WithMessage("Verification token is required.");

        RuleFor(v => v.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.");
    }
}