using FluentValidation.TestHelper;
using Application.Posts.Commands.CreatePostComment;

namespace Tests.Application.Posts.Commands.CreatePostComment;

public class CreatePostCommentCommandValidatorTests
{
    private readonly CreatePostCommentCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenContentIsEmpty()
    {
        var command = new CreatePostCommentCommand { Content = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Should_HaveError_WhenContentTooShort()
    {
        var command = new CreatePostCommentCommand { Content = "12" }; // 2 символи

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Should_HaveError_WhenContentTooLong()
    {
        var longText = new string('A', 301);
        var command = new CreatePostCommentCommand { Content = longText };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Should_NotHaveError_WhenContentIsValid()
    {
        var validText = new string('A', 100);
        var command = new CreatePostCommentCommand { Content = validText };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Content);
    }
}
