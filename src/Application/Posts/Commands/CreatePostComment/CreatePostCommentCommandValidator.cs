using FluentValidation;

namespace Application.Posts.Commands.CreatePostComment;

public class CreatePostCommentCommandValidator : AbstractValidator<CreatePostCommentCommand>
{
    public CreatePostCommentCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(300).WithMessage("Content is too long");
    }
}