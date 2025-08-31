using System.Security.Claims;
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
}