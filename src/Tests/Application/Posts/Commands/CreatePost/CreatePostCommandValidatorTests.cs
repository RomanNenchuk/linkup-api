using Application.Posts.Commands.CreatePost;
using FluentValidation.TestHelper;
using Xunit;

namespace Tests.Posts;

public class CreatePostCommandValidatorTests
{
    private readonly CreatePostCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenContentIsEmpty()
    {
        // arrange
        var command = new CreatePostCommand
        {
            Content = ""
        };

        // act
        var result = _validator.TestValidate(command);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Should_HaveError_WhenContentTooShort()
    {
        var command = new CreatePostCommand
        {
            Content = "hi" // менше 5 символів
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Should_NotHaveError_WhenContentValid()
    {
        var command = new CreatePostCommand
        {
            Content = "This is valid content"
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Should_HaveError_WhenContentTooLong()
    {
        var longContent = new string('A', 301);

        var command = new CreatePostCommand
        {
            Content = longContent
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content);
    }
}
