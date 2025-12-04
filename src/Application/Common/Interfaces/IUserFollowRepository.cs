using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IUserFollowRepository
{
    Task<List<string>> GetFolloweeIdsAsync(string followerId, CancellationToken ct);
    Task<List<string>> GetFollowingIdsAsync(string userId, CancellationToken ct);
    Task<List<string>> GetFolloweeIdsForUsersAsync(IEnumerable<string> userIds, CancellationToken ct);
    Task<UserFollow?> GetFollowRelationAsync(string followerId, string followeeId, CancellationToken ct);
    Task AddFollowAsync(UserFollow follow, CancellationToken ct);
    Task RemoveFollowAsync(UserFollow follow, CancellationToken ct);
}