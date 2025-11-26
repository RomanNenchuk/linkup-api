using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using AutoMapper;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Infrastructure.Services;

public class RecommendationsService(ApplicationDbContext dbContext, IMapper mapper) : IRecommendationsService
{
    public async Task<Result<List<User>>> GetRecommendedUsersAsync(string? userId)
    {
        try
        {
            var followingIds = new List<string>();
            var userLocations = new List<Point>();

            if (userId != null)
            {
                followingIds = await dbContext.UserFollows
                    .Where(f => f.FollowerId == userId)
                    .Select(f => f.FolloweeId)
                    .ToListAsync();

                userLocations = await dbContext.Posts
                    .Where(p => p.AuthorId == userId && p.Location != null)
                    .Select(p => p.Location!)
                    .ToListAsync();
            }


            var recommended = new List<string>();

            if (userLocations.Count > 0)
            {
                const double radius = 1000;

                var localCandidates = await dbContext.Posts
                    .Where(p =>
                        p.AuthorId != userId &&
                        !followingIds.Contains(p.AuthorId) &&
                        p.Location != null &&
                        userLocations.Any(loc => loc.Distance(p.Location!) <= radius)
                    )
                    .GroupBy(p => p.AuthorId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        Score = g.Count()
                    })
                    .OrderByDescending(x => x.Score)
                    .Take(20)
                    .ToListAsync();

                recommended.AddRange(localCandidates.Select(x => x.UserId));
            }

            // if there are less than 20 then add some extra default candidates
            if (recommended.Count < 20)
            {
                int need = 20 - recommended.Count;

                var defaultCandidates = await dbContext.Users
                    .Where(u =>
                        u.Id != userId &&
                        !followingIds.Contains(u.Id) &&
                        !recommended.Contains(u.Id)
                    )
                    .OrderByDescending(u => u.Followers.Count)
                    .Take(need)
                    .Select(u => u.Id)
                    .ToListAsync();

                recommended.AddRange(defaultCandidates);
            }

            var users = await dbContext.Users
                .Where(u => recommended.Contains(u.Id))
                .ToListAsync();

            var dtos = mapper.Map<List<User>>(users);
            return Result<List<User>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<List<User>>.Failure($"Failed to get recommendations: {ex.Message}");
        }
    }

}
