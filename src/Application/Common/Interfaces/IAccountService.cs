using System.Security.Claims;
using Application.Common.Models;

namespace Application.Common.Interfaces;

public interface IAccountService
{
    Task<Result<string>> LoginWithGoogleAsync(ClaimsPrincipal claimsPrincipal);
    Task<Result<string>> RefreshTokenAsync(string refreshToken);
    Task<Result<User>> CreateUserAsync(string email, string displayName, string password);
}