using System.Security.Claims;
using Application.Auth.Commands.LoginWithGoogle;
using FluentValidation.TestHelper;

namespace Tests.Application.Auth.Commands.LoginWithGoogle;

public class LoginWithGoogleCommandValidatorTests
{
    private readonly LoginWithGoogleCommandValidator _validator = new();

    [Fact]
    public void ShouldHaveError_WhenClaimsPrincipalIsNull()
    {
        var result = _validator.TestValidate(new LoginWithGoogleCommand(null!));

        result.ShouldHaveValidationErrorFor(x => x.ClaimsPrincipal)
            .WithErrorMessage("ClaimsPrincipal is required.");
    }

    [Fact]
    public void ShouldNotHaveError_WhenClaimsPrincipalIsProvided()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var result = _validator.TestValidate(new LoginWithGoogleCommand(principal));

        result.ShouldNotHaveValidationErrorFor(x => x.ClaimsPrincipal);
    }
}
