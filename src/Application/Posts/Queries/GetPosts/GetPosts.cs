using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Posts.Queries.GetPosts;

public class PostFilter
{
    public PostFilterType Type { get; set; } = PostFilterType.Recent;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? RadiusKm { get; set; }
}

public class GetPostsQuery : IRequest<Result<PagedResult<PostResponseDto>>>
{
    public PostFilter Filter { get; set; } = new();
    public string? Cursor { get; set; }
    private int _pageSize;
    public int PageSize { get => _pageSize; set => _pageSize = value >= 50 ? 50 : value; }
}

public class GetPostsQueryHandler(IPostService postService)
    : IRequestHandler<GetPostsQuery, Result<PagedResult<PostResponseDto>>>
{
    public async Task<Result<PagedResult<PostResponseDto>>> Handle(GetPostsQuery request, CancellationToken ct)
    {
        return request.Filter.Type switch
        {
            PostFilterType.Top => await postService.GetTopPostsAsync(request, ct),
            PostFilterType.Following => await postService.GetFollowingPostsAsync(request, ct),
            _ => await postService.GetRecentPostsAsync(request, ct)
        };
    }
}
