using System.Security.Claims;
using Application.Common.DTOs;

namespace Application.Common.Interfaces;

public interface IAccountService
{
    Task<Result<string>> LoginWithGoogleAsync(ClaimsPrincipal claimsPrincipal);
    Task<Result<string>> RefreshTokenAsync(string refreshToken);
}