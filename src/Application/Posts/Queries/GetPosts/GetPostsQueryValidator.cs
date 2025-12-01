using FluentValidation;

namespace Application.Posts.Queries.GetPosts;

public class GetPostsQueryValidator : AbstractValidator<GetPostsQuery>
{
    public GetPostsQueryValidator()
    {
        RuleFor(x => x.Params.RadiusKm)
            .GreaterThan(0)
            .When(x => x.Params.RadiusKm.HasValue)
            .WithMessage("Radius must be greater than 0");
    }
}