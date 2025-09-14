using Application.Posts.Commands.CreatePost;
using FluentValidation;

namespace Application.Post.Commands.CreatePost;

public class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MinimumLength(5).WithMessage("Title is too short")
            .MaximumLength(300).WithMessage("Title is too long");

        RuleFor(x => x.Content)
            .MaximumLength(300).WithMessage("Content is too long");
    }
}