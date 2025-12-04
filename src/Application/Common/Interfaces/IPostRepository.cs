using Application.Common.Models;
using Domain.Entities;
using NetTopologySuite.Geometries;

namespace Application.Common.Interfaces;

public interface IPostRepository
{
    Task<List<Post>> GetTopPostsAsync(DateTime minCreatedAt, int offset, int pageSize, double? lat, double? lng, double? radiusKm, CancellationToken ct);
    Task<List<Post>> GetFollowingPostsAsync(IEnumerable<string> followeeIds, string? cursor, int pageSize, double? lat, double? lng, double? radiusKm, CancellationToken ct);
    Task<List<Post>> GetRecentPostsAsync(string? authorId, string? cursor, int pageSize, double? lat, double? lng, double? radiusKm, CancellationToken ct);
    Task<Dictionary<string, User>> GetAuthorsAsync(List<string> authorIds, CancellationToken ct);
    Task<Dictionary<string, int>> GetReactionCountsAsync(List<string> postIds, CancellationToken ct);
    Task<Dictionary<string, int>> GetCommentCountsAsync(List<string> postIds, CancellationToken ct);
    Task<int> GetCommentCountAsync(string postId, CancellationToken ct);
    Task<int> GetReactionCountAsync(string postId, CancellationToken ct);
    Task<bool> IsPostLikedByUserAsync(string postId, string userId, CancellationToken ct);
    Task<List<string>> GetLikedPostIdsAsync(List<string> postIds, string? currentUserId, CancellationToken ct);
    Task<Post?> GetPostWithPhotosAsync(string postId, CancellationToken ct);
    Task<Post?> GetPostByIdAsync(string postId, CancellationToken ct);
    Task<Result> DeletePostAsync(Post post, CancellationToken ct);
    Task AddPostAsync(Post post, CancellationToken ct);
    Task<List<Point>> GetUserLocationsAsync(string? userId, CancellationToken ct);
    Task<List<(string UserId, int SameLocations)>> GetLocationCandidateAuthorsAsync(string? userId,
        List<string> followingIds, List<Point> userLocations, double radiusMeters, int limit, CancellationToken ct);
    Task<bool> SaveChangesAsync(CancellationToken ct);
}