using Application.Common.Models;
using Application.Users.Queries.GetRecommendedUsers;

namespace Application.Common.Interfaces;

public interface IRecommendationsService
{
    Task<Result<List<RecommendedUserDto>>> GetRecommendedUsersAsync(string? userId);
}
