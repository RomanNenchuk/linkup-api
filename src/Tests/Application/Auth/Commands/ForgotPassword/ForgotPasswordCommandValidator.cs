using Application.Auth.Commands.ForgotPassword;
using FluentValidation.TestHelper;

namespace Tests.Application.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator = new();

    [Fact]
    public void ShouldHaveError_WhenEmailIsEmpty()
    {
        var result = _validator.TestValidate(new ForgotPasswordCommand { Email = "" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ShouldHaveError_WhenEmailIsInvalid()
    {
        var result = _validator.TestValidate(new ForgotPasswordCommand { Email = "not-email" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ShouldHaveError_WhenEmailHasWhitespaceAround()
    {
        var result = _validator.TestValidate(
            new ForgotPasswordCommand { Email = "  test@example.com " });

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ShouldNotHaveError_WhenEmailIsValid()
    {
        var result = _validator.TestValidate(
            new ForgotPasswordCommand { Email = "test@example.com" });

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }
}
