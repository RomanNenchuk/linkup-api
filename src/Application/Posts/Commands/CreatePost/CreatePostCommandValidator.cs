using Application.Posts.Commands.CreatePost;
using FluentValidation;

namespace Application.Post.Commands.CreatePost;

public class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MinimumLength(5).WithMessage("Content is too short")
            .MaximumLength(300).WithMessage("Content is too long");
    }
}