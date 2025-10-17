using Application.Common;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Posts.Queries.GetPostComments;

public class GetPostCommentsQuery : IRequest<Result<List<PostCommentResponseDto>>>
{
    public string PostId { get; set; } = null!;
}

public class GetPostCommentsQueryHandler(IPostService postService)
    : IRequestHandler<GetPostCommentsQuery, Result<List<PostCommentResponseDto>>>
{
    public async Task<Result<List<PostCommentResponseDto>>> Handle(GetPostCommentsQuery request, CancellationToken ct)
    {
        return await postService.GetPostCommentsAsync(request.PostId);
    }
}
