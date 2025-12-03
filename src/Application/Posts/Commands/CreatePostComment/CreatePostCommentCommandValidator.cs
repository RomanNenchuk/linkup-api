using FluentValidation;

namespace Application.Posts.Commands.CreatePostComment;

public class CreatePostCommentCommandValidator : AbstractValidator<CreatePostCommentCommand>
{
    public CreatePostCommentCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MinimumLength(3).WithMessage("Content is too short")
            .MaximumLength(300).WithMessage("Content is too long");
    }
}