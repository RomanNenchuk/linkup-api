using Application.Posts.Commands.EditPost;
using FluentValidation.TestHelper;

namespace Tests.Application.Posts.Commands.EditPost;

public class EditPostCommandValidatorTests
{
    private readonly EditPostCommandValidator _validator = new();
    [Fact]
    public void Should_HaveError_WhenContentIsEmpty()
    {
        var command = new EditPostCommand { Content = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Should_HaveError_WhenContentTooShort()
    {
        var command = new EditPostCommand { Content = "1234" }; // 4 символи

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Should_HaveError_WhenContentTooLong()
    {
        var longText = new string('A', 301);
        var command = new EditPostCommand { Content = longText };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Should_NotHaveError_WhenContentIsValid()
    {
        var validText = new string('A', 100);
        var command = new EditPostCommand { Content = validText };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Content);
    }
}
