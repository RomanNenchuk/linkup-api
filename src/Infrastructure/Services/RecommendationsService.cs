using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Users.Queries.GetRecommendedUsers;
using AutoMapper;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Infrastructure.Services;

public class RecommendationsService(ApplicationDbContext dbContext, IMapper mapper)
    : IRecommendationsService
{
    private const int MaxCount = 20;
    private const double Radius = 1000;

    public async Task<Result<List<RecommendedUserDto>>> GetRecommendedUsersAsync(string? userId)
    {
        var followingIds = await GetFollowingIdsAsync(userId);
        var userLocations = await GetUserLocationsAsync(userId);

        var recommended = new List<RecommendedUserDto>();
        var alreadyAdded = new HashSet<string>();

        if (userLocations.Count > 0)
        {
            // locations based recommendations
            var locationBased = await GetLocationBasedRecommendationsAsync(userId, followingIds, userLocations);

            recommended.AddRange(locationBased);
            foreach (var r in locationBased)
                alreadyAdded.Add(r.User.Id);
        }

        // default recommendations (fallback)
        if (recommended.Count < MaxCount)
        {
            int need = MaxCount - recommended.Count;

            var defaultRecommendations = await GetDefaultRecommendationsAsync(
                userId,
                followingIds,
                alreadyAdded,
                need
            );

            recommended.AddRange(defaultRecommendations);
        }

        return Result<List<RecommendedUserDto>>.Success(recommended);
    }


    private async Task<List<string>> GetFollowingIdsAsync(string? userId)
    {
        if (userId == null)
            return [];

        return await dbContext.UserFollows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FolloweeId)
            .ToListAsync();
    }

    private async Task<List<Point>> GetUserLocationsAsync(string? userId)
    {
        if (userId == null)
            return [];

        return await dbContext.Posts
            .Where(p => p.AuthorId == userId && p.Location != null)
            .Select(p => p.Location!)
            .ToListAsync();
    }

    private async Task<List<RecommendedUserDto>> GetLocationBasedRecommendationsAsync(
        string? userId,
        List<string> followingIds,
        List<Point> userLocations
    )
    {
        var candidates = await dbContext.Posts
            .Where(p =>
                p.AuthorId != userId &&
                !followingIds.Contains(p.AuthorId) &&
                p.Location != null &&
                userLocations.Any(loc => loc.Distance(p.Location!) <= Radius)
            )
            .GroupBy(p => p.AuthorId)
            .Select(g => new
            {
                UserId = g.Key,
                SameLocations = g.Count()
            })
            .OrderByDescending(x => x.SameLocations)
            .Take(MaxCount)
            .ToListAsync();

        var ids = candidates.Select(c => c.UserId).ToList();

        var users = await dbContext.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        return candidates
            .Select(c => new RecommendedUserDto
            {
                User = mapper.Map<User>(users[c.UserId]),
                SameLocationsCount = c.SameLocations,
                FollowersCount = null
            })
            .ToList();
    }

    private async Task<List<RecommendedUserDto>> GetDefaultRecommendationsAsync(
        string? userId,
        List<string> followingIds,
        HashSet<string> alreadyAdded,
        int need
    )
    {
        var candidates = await dbContext.Users
            .Where(u =>
                u.Id != userId &&
                !followingIds.Contains(u.Id) &&
                !alreadyAdded.Contains(u.Id)
            )
            .OrderByDescending(u => u.Followers.Count)
            .Take(need)
            .Select(u => new { u, Followers = u.Followers.Count })
            .ToListAsync();

        return candidates
            .Select(c => new RecommendedUserDto
            {
                User = mapper.Map<User>(c.u),
                SameLocationsCount = null,
                FollowersCount = c.Followers
            })
            .ToList();
    }
}
