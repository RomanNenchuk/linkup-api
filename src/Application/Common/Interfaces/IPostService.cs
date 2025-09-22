using Application.Common.DTOs;
using Application.Posts.Commands.CreatePost;
using Application.Posts.Queries.GetPosts;

namespace Application.Common.Interfaces;

public interface IPostService
{
    Task<Result<string>> CreatePostAsync(CreatePostDto dto);
    Task<Result> TogglePostReactionAsync(string postId, string userId, bool isLiked);
    Task<Result<PagedResult<PostResponseDto>>> GetPostsAsync(GetPostsQuery query, CancellationToken ct);
}
