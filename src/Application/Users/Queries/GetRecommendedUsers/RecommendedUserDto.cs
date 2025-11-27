using Application.Common.Models;

namespace Application.Users.Queries.GetRecommendedUsers;

public class RecommendedUserDto
{
    public User User { get; set; } = null!;
    public int? SameLocationsCount { get; set; }
    public int? FollowersCount { get; set; }

}
