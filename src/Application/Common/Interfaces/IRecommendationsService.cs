using Application.Common.Models;

namespace Application.Common.Interfaces;

public interface IRecommendationsService
{
    Task<Result<List<User>>> GetRecommendedUsersAsync(string? userId);
}
