using Application.Common.Interfaces;
using Application.Common.Models;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserRepository(ApplicationDbContext dbContext) : IUserRepository
{
    public async Task<User?> FindByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await dbContext.Users
            .Include(u => u.Followers)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        return entity == null ? null : ToModel(entity);
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var entity = await dbContext.Users
            .Include(u => u.Followers)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        return entity == null ? null : ToModel(entity);
    }

    public async Task<Dictionary<string, User>> GetUsersByIdsAsync(List<string> ids, CancellationToken ct = default)
    {
        var entities = await dbContext.Users
            .Include(u => u.Followers)
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(ct);

        return entities.ToDictionary(e => e.Id, ToModel);
    }

    public async Task<List<User>> SearchByDisplayNameAsync(string pattern, int offset, int pageSize, CancellationToken ct = default)
    {
        var entities = await dbContext.Users
            .Include(u => u.Followers)
            .Where(u => EF.Functions.ILike(u.DisplayName, $"%{pattern}%"))
            .OrderByDescending(u => u.Followers.Count)
            .Skip(offset)
            .Take(pageSize)
            .ToListAsync(ct);

        return entities.Select(ToModel).ToList();
    }

    public Task<bool> ExistsAsync(string id, CancellationToken ct = default) =>
        dbContext.Users.AnyAsync(u => u.Id == id, ct);

    public async Task<int> GetFollowersCountAsync(string userId, CancellationToken ct = default)
    {
        var user = await dbContext.Users
            .Include(u => u.Followers)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        return user?.Followers.Count ?? 0;
    }

    public async Task<User?> GetUserWithFollowRelationsAsync(string userId, CancellationToken ct = default)
    {
        var entity = await dbContext.Users
            .Include(u => u.Followers)
            .Include(u => u.Followings)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        return entity == null ? null : ToModel(entity);
    }

    private static User ToModel(ApplicationUser u) =>
        new()
        {
            Id = u.Id,
            DisplayName = u.DisplayName,
            Email = u.Email!,
            FollowersCount = u.Followers.Count,
            FollowingCount = u.Followings.Count,
            FollowerIds = u.Followers.Select(f => f.FollowerId).ToList(),
            FollowingIds = u.Followings.Select(f => f.FolloweeId).ToList()
        };

    public async Task<List<User>> GetTopRecommendedAsync(
    string? excludeUserId,
    List<string> followingIds,
    HashSet<string> excludedIds,
    int take,
    CancellationToken ct = default)
    {
        var entities = await dbContext.Users
            .Include(u => u.Followers)
            .Where(u =>
                (excludeUserId == null || u.Id != excludeUserId) &&
                !followingIds.Contains(u.Id) &&
                !excludedIds.Contains(u.Id)
            )
            .OrderByDescending(u => u.Followers.Count)
            .Take(take)
            .ToListAsync(ct);

        return entities.Select(ToModel).ToList();
    }

}
