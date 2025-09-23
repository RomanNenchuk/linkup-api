using System.Security.Claims;
using Application.Common.DTOs;
using Application.Common.Models;

namespace Application.Common.Interfaces;

public interface IAccountService
{
    Task<Result<User>> LoginWithGoogleAsync(ClaimsPrincipal claimsPrincipal);
    Task<Result<string>> RefreshTokenAsync(string refreshToken);
    Task<Result> ConfirmEmailAsync(string verificationToken);
    Task<Result> ResetPasswordAsync(string verificationToken, string newPassword);
    Task<Result<User>> CreateUserAsync(string email, string displayName, string password);
    Task<Result<User>> LoginAsync(string email, string password);
    Task<Result<User>> GetUserByIdAsync(string id);
    Task<Result<User>> GetUserByEmailAsync(string email);
    Task<Result> LogoutAsync(string refreshToken);
    Task<Result> ToggleFollowAsync(string followerId, string followeeId, bool IsFollowed);
    Task<Result<UserProfieDto>> GetUserInformationAsync(string userId, string? currentUserId = null);
}