using Application.Common;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Posts.Commands.TogglePostCommentReaction;

public class TogglePostCommentReactionCommand : IRequest<Result>
{
    public string PostId { get; set; } = null!;
    public bool IsLiked { get; set; }
}

public class TogglePostCommentLikeRequest
{
    public bool IsLiked { get; set; }
}

public class TogglePostCommentReactionCommandHandler(IPostService postService, ICurrentUserService currentUserService)
    : IRequestHandler<TogglePostCommentReactionCommand, Result>
{
    public async Task<Result> Handle(TogglePostCommentReactionCommand request, CancellationToken ct)
    {
        var userId = currentUserService.Id!;
        return await postService.TogglePostReactionAsync(request.PostId, userId, request.IsLiked);
    }
}
