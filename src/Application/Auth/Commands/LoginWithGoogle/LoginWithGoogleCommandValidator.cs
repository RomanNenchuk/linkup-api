using FluentValidation;

namespace Application.Auth.Commands.LoginWithGoogle;

public class LoginWithGoogleCommandValidator : AbstractValidator<LoginWithGoogleCommand>
{

    public LoginWithGoogleCommandValidator()
    {
        RuleFor(v => v.ClaimsPrincipal)
            .NotNull()
            .WithMessage("ClaimsPrincipal is required.");
    }
}