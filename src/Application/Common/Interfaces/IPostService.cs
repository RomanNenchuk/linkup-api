using Application.Common.DTOs;
using Application.Posts.Commands.CreatePost;
using Application.Posts.Queries.GetPosts;

namespace Application.Common.Interfaces;

public interface IPostService
{
    Task<Result<string>> CreatePostAsync(CreatePostDto dto);
    Task<Result> TogglePostReactionAsync(string postId, string userId, bool isLiked);
    Task<Result<PagedResult<PostResponseDto>>> GetTopPostsAsync(GetPostsQuery query, CancellationToken ct);
    Task<Result<PagedResult<PostResponseDto>>> GetFollowingPostsAsync(GetPostsQuery query, CancellationToken ct);
    Task<Result<PagedResult<PostResponseDto>>> GetRecentPostsAsync(GetPostsQuery query, CancellationToken ct);
}
