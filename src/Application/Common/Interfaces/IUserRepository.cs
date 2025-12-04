using Application.Common.Models;

namespace Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<Dictionary<string, User>> GetUsersByIdsAsync(List<string> ids, CancellationToken ct = default);
    Task<List<User>> SearchByDisplayNameAsync(string pattern, int offset, int pageSize, CancellationToken ct = default);
    Task<bool> ExistsAsync(string id, CancellationToken ct = default);
    Task<int> GetFollowersCountAsync(string userId, CancellationToken ct = default);
    Task<User?> GetUserWithFollowRelationsAsync(string userId, CancellationToken ct = default);
    Task<List<User>> GetTopRecommendedAsync(string? excludeUserId, List<string> followingIds,
        HashSet<string> excludedIds, int take, CancellationToken ct = default);
}
