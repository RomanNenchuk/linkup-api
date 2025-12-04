using Application.Auth.Commands.Login;
using FluentValidation.TestHelper;

namespace Tests.Application.Auth.Commands.Login;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void ShouldHaveError_WhenEmailIsEmpty()
    {
        var result = _validator.TestValidate(new LoginCommand { Email = "", Password = "SomePass1" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ShouldHaveError_WhenEmailIsInvalid()
    {
        var result = _validator.TestValidate(new LoginCommand { Email = "not-email", Password = "SomePass1" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ShouldHaveError_WhenEmailHasWhitespaceAround()
    {
        var result = _validator.TestValidate(
            new LoginCommand { Email = "  test@example.com  ", Password = "SomePass1" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ShouldHaveError_WhenPasswordIsEmpty()
    {
        var result = _validator.TestValidate(new LoginCommand { Email = "test@example.com", Password = "" });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("short1A")]             // too short
    [InlineData("alllowercase1")]       // no uppercase
    [InlineData("ALLUPPERCASE1")]       // no lowercase
    [InlineData("NoNumberPass")]        // no digit
    public void ShouldHaveError_WhenPasswordDoesNotMeetComplexity(string pwd)
    {
        var result = _validator.TestValidate(new LoginCommand { Email = "test@example.com", Password = pwd });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ShouldNotHaveError_WhenEmailAndPasswordAreValid()
    {
        var result = _validator.TestValidate(
            new LoginCommand { Email = "test@example.com", Password = "GoodPass123" });
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
