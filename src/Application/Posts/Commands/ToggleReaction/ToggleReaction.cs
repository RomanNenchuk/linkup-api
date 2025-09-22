using Application.Common;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Posts.Commands.ToggleReaction;

public class ToggleReactionCommand : IRequest<Result>
{
    public string PostId { get; set; } = null!;
    public bool IsLiked { get; set; }
}

public class ToggleLikeRequest
{
    public bool IsLiked { get; set; }
}

public class ToggleReactionCommandHandler(IPostService postService, ICurrentUserService currentUserService)
    : IRequestHandler<ToggleReactionCommand, Result>
{
    public async Task<Result> Handle(ToggleReactionCommand request, CancellationToken ct)
    {
        var userId = currentUserService.Id!;
        return await postService.TogglePostReactionAsync(request.PostId, userId, request.IsLiked);
    }
}
