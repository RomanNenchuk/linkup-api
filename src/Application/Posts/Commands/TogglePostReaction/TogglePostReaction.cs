using Application.Common;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Posts.Commands.TogglePostReaction;

public class TogglePostReactionCommand : IRequest<Result>
{
    public string PostId { get; set; } = null!;
    public bool IsLiked { get; set; }
}

public class TogglePostLikeRequest
{
    public bool IsLiked { get; set; }
}

public class TogglePostReactionCommandHandler(IPostService postService, ICurrentUserService currentUserService)
    : IRequestHandler<TogglePostReactionCommand, Result>
{
    public async Task<Result> Handle(TogglePostReactionCommand request, CancellationToken ct)
    {
        var userId = currentUserService.Id!;
        return await postService.TogglePostReactionAsync(request.PostId, userId, request.IsLiked);
    }
}
