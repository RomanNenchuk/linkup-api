using Application.Auth.Commands.Register;
using FluentValidation.TestHelper;

namespace Tests.Application.Auth.Commands.Register;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void ShouldHaveError_WhenDisplayNameEmpty()
    {
        var result = _validator.TestValidate(new RegisterCommand { DisplayName = "" });
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void ShouldHaveError_WhenDisplayNameHasSpacesAround()
    {
        var result = _validator.TestValidate(new RegisterCommand { DisplayName = "  John  " });
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void ShouldNotHaveError_WhenDisplayNameValid()
    {
        var result = _validator.TestValidate(new RegisterCommand { DisplayName = "John" });
        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void ShouldHaveError_WhenEmailInvalid()
    {
        var result = _validator.TestValidate(new RegisterCommand { Email = "invalid" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ShouldNotHaveError_WhenEmailValid()
    {
        var result = _validator.TestValidate(new RegisterCommand { Email = "test@example.com" });
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ShouldHaveError_WhenPasswordWeak()
    {
        var result = _validator.TestValidate(new RegisterCommand { Password = "weak" });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ShouldNotHaveError_WhenPasswordStrong()
    {
        var result = _validator.TestValidate(new RegisterCommand { Password = "Strong123" });
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
