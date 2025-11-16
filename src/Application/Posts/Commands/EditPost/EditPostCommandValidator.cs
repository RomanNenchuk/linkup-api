using FluentValidation;

namespace Application.Posts.Commands.EditPost;

public class EditPostCommandValidator : AbstractValidator<EditPostCommand>
{
    public EditPostCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MinimumLength(5).WithMessage("Content is too short")
            .MaximumLength(300).WithMessage("Content is too long");
    }
}