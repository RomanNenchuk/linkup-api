using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Users.Queries.GetRecommendedUsers;
using AutoMapper;

namespace Infrastructure.Services;

public class RecommendationsService(
    IPostRepository postRepo,
    IUserFollowRepository userFollowRepo,
    IUserRepository userRepo,
    IMapper mapper) : IRecommendationsService
{
    private const int MaxCount = 20;
    private const double RadiusMeters = 5000;

    public async Task<Result<List<RecommendedUserDto>>> GetRecommendedUsersAsync(string? userId)
    {
        var followingIds = userId == null ? [] : await userFollowRepo.GetFollowingIdsAsync(userId, CancellationToken.None);
        var userLocations = await postRepo.GetUserLocationsAsync(userId, CancellationToken.None);

        var recommended = new List<RecommendedUserDto>();
        var alreadyAdded = new HashSet<string>();

        if (userLocations.Count > 0)
        {
            var locationCandidates = await postRepo.GetLocationCandidateAuthorsAsync(userId, followingIds,
                userLocations, RadiusMeters, MaxCount, CancellationToken.None);

            var ids = locationCandidates.Select(c => c.UserId).ToList();
            var usersDict = await userRepo.GetUsersByIdsAsync(ids);

            var locationBased = locationCandidates
                .Where(c => usersDict.ContainsKey(c.UserId))
                .Select(c => new RecommendedUserDto
                {
                    User = mapper.Map<User>(usersDict[c.UserId]),
                    SameLocationsCount = c.SameLocations,
                    FollowersCount = null
                })
                .ToList();

            recommended.AddRange(locationBased);
            foreach (var r in locationBased) alreadyAdded.Add(r.User.Id);
        }

        if (recommended.Count < MaxCount)
        {
            var need = MaxCount - recommended.Count;
            var defaults = await GetDefaultRecommendationsAsync(userId, followingIds, alreadyAdded, need);
            recommended.AddRange(defaults);
        }

        return Result<List<RecommendedUserDto>>.Success(recommended);
    }

    private async Task<List<RecommendedUserDto>> GetDefaultRecommendationsAsync(
        string? userId,
        List<string> followingIds,
        HashSet<string> alreadyAdded,
        int need)
    {
        var users = await userRepo.GetTopRecommendedAsync(
            userId,
            followingIds,
            alreadyAdded,
            need);

        return users
            .Select(u => new RecommendedUserDto
            {
                User = u,
                SameLocationsCount = null,
                FollowersCount = u.FollowersCount
            })
            .ToList();
    }
}
