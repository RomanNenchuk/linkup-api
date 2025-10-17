using Application.Common.DTOs;
using Application.Posts.Commands.CreatePost;
using Application.Posts.Commands.CreatePostComment;
using Application.Posts.Commands.EditPost;
using Application.Posts.Queries.GetHeatmapPoints;
using Application.Posts.Queries.GetPostClusters;
using Application.Posts.Queries.GetPostComments;
using Application.Posts.Queries.GetPosts;

namespace Application.Common.Interfaces;

public interface IPostService
{
    Task<Result<string>> CreatePostAsync(CreatePostDto dto);
    Task<Result> EditPostAsync(EditPostDto dto);
    Task<Result> DeletePostAsync(string postId);
    Task<Result> TogglePostReactionAsync(string postId, string userId, bool isLiked);
    Task<Result<PagedResult<PostResponseDto>>> GetTopPostsAsync(GetPostsQuery query, CancellationToken ct);
    Task<Result<List<HeatmapPointDto>>> GetHeatmapPointsAsync(
     double minLon, double maxLon, double minLat, double maxLat, int zoom, CancellationToken ct);
    Task<Result<List<ClusterDto>>> GetPostClustersAsync(CancellationToken ct);
    Task<Result<PagedResult<PostResponseDto>>> GetFollowingPostsAsync(GetPostsQuery query, CancellationToken ct);
    Task<Result<PagedResult<PostResponseDto>>> GetRecentPostsAsync(GetPostsQuery query, CancellationToken ct);
    Task<Result<PostResponseDto>> GetPostByIdAsync(string postId, CancellationToken ct);
    Task<Result> ValidatePhotoLimitAsync(string postId, int photosToAddCount, List<string>? photosToDeleteList,
        CancellationToken ct);
    Task<Result<string>> CreatePostCommentAsync(CreatePostCommentDto dto);
    Task<Result<List<PostCommentResponseDto>>> GetPostCommentsAsync(string postId);
}
