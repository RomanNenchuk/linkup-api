using Application.Common.Models;

namespace Application.Common.Interfaces;

public interface IPostRepository
{
    Task<List<Domain.Entities.Post>> GetTopPostsAsync(DateTime minCreatedAt, int offset, int pageSize, double? lat, double? lng, double? radiusKm, CancellationToken ct);
    Task<List<Domain.Entities.Post>> GetFollowingPostsAsync(IEnumerable<string> followeeIds, string? cursor, int pageSize, double? lat, double? lng, double? radiusKm, CancellationToken ct);
    Task<List<Domain.Entities.Post>> GetRecentPostsAsync(string? authorId, string? cursor, int pageSize, double? lat, double? lng, double? radiusKm, CancellationToken ct);
    Task<Dictionary<string, User>> GetAuthorsAsync(List<string> authorIds, CancellationToken ct);
    Task<Dictionary<string, int>> GetReactionCountsAsync(List<string> postIds, CancellationToken ct);
    Task<Dictionary<string, int>> GetCommentCountsAsync(List<string> postIds, CancellationToken ct);
    Task<List<string>> GetLikedPostIdsAsync(List<string> postIds, string? currentUserId, CancellationToken ct);
}