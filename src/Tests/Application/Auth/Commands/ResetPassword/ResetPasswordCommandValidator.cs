using Application.Auth.Commands.ResetPassword;
using FluentValidation.TestHelper;

namespace Tests.Application.Auth.Commands.ResetPassword;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Fact]
    public void ShouldHaveError_WhenTokenIsEmpty()
    {
        var result = _validator.TestValidate(
            new ResetPasswordCommand
            {
                VerificationToken = "",
                NewPassword = "Pass1234"
            });

        result.ShouldHaveValidationErrorFor(x => x.VerificationToken);
    }

    [Fact]
    public void ShouldHaveError_WhenNewPasswordIsEmpty()
    {
        var result = _validator.TestValidate(
            new ResetPasswordCommand
            {
                VerificationToken = "some-token",
                NewPassword = ""
            });

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void ShouldNotHaveErrors_WhenCommandIsValid()
    {
        var result = _validator.TestValidate(
            new ResetPasswordCommand
            {
                VerificationToken = "valid-token",
                NewPassword = "StrongPass1"
            });

        result.ShouldNotHaveValidationErrorFor(x => x.VerificationToken);
        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }
}
