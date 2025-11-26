using System.Security.Claims;
using Application.Common.DTOs;
using Application.Common.Models;
using Application.Users.Queries.GetUsersByDisplayName;

namespace Application.Common.Interfaces;

public interface IUserService
{
    Task<Result<User>> GetUserByIdAsync(string id);
    Task<Result<User>> GetUserByEmailAsync(string email);
    Task<Result> ToggleFollowAsync(string followerId, string followeeId, bool IsFollowed);
    Task<Result<UserProfileDto>> GetUserInformationAsync(string userId, string? currentUserId = null);
    Task<Result<PagedResult<User>>> GetUsersByDisplayNameAsync(GetUsersByDisplayNameQuery query);
}