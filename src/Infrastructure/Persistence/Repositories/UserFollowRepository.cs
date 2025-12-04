using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserFollowRepository(ApplicationDbContext dbContext) : IUserFollowRepository
{
    public async Task<List<string>> GetFolloweeIdsAsync(string followerId, CancellationToken ct)
    {
        return await dbContext.UserFollows
            .Where(f => f.FollowerId == followerId)
            .Select(f => f.FolloweeId)
            .ToListAsync(ct);
    }

    public Task<List<string>> GetFollowingIdsAsync(string userId, CancellationToken ct = default) =>
    dbContext.UserFollows.Where(f => f.FollowerId == userId).Select(f => f.FolloweeId).ToListAsync(ct);

    public Task<List<string>> GetFolloweeIdsForUsersAsync(IEnumerable<string> userIds, CancellationToken ct = default) =>
        dbContext.UserFollows.Where(f => userIds.Contains(f.FollowerId)).Select(f => f.FolloweeId).ToListAsync(ct);

    public Task<UserFollow?> GetFollowRelationAsync(string followerId, string followeeId, CancellationToken ct = default) =>
        dbContext.UserFollows.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId, ct);

    public async Task AddFollowAsync(UserFollow follow, CancellationToken ct = default)
    {
        dbContext.UserFollows.Add(follow);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task RemoveFollowAsync(UserFollow follow, CancellationToken ct = default)
    {
        dbContext.UserFollows.Remove(follow);
        await dbContext.SaveChangesAsync(ct);
    }
}