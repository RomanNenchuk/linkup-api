using System.Security.Claims;
using Application.Common;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using AutoMapper;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class AccountService(UserManager<ApplicationUser> userManager, ITokenService tokenService,
    ApplicationDbContext dbContext, IMapper mapper) : IAccountService
{
    public async Task<Result<User>> CreateUserAsync(string email, string displayName, string password)
    {
        var existinguserUser = await userManager.FindByEmailAsync(email);
        if (existinguserUser != null) return Result<User>.Failure("User with this email already exists", 400);

        var user = new ApplicationUser
        {
            DisplayName = displayName,
            UserName = email,
            Email = email,
            EmailConfirmed = false
        };

        var result = await userManager.CreateAsync(user, password);
        return result.Succeeded ? Result<User>.Success(mapper.Map<User>(user))
            : Result<User>.Failure("Failed to create user", 400);
    }

    public async Task<Result<string>> LoginWithGoogleAsync(ClaimsPrincipal claimsPrincipal)
    {
        var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email)
            ?? throw new ExternalLoginProviderException("Google", "Email is null");
        var displayName = claimsPrincipal.FindFirstValue(ClaimTypes.GivenName) ?? "Unknown";
        var provider = "Google";
        var providerKey = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new ExternalLoginProviderException("Google", "Provider key is null");

        var existingUserByLogin = await userManager.FindByLoginAsync(provider, providerKey);
        if (existingUserByLogin != null)
            return await tokenService.IssueRefreshToken(mapper.Map<User>(existingUserByLogin));

        var user = await FindOrCreateUserAsync(email, displayName);

        var info = new UserLoginInfo(provider, providerKey, provider);
        var loginResult = await userManager.AddLoginAsync(user, info);
        if (!loginResult.Succeeded &&
            loginResult.Errors.All(e => e.Code != "LoginAlreadyAssociated"))
        {
            return Result<string>.Failure(
                $"Unable to add login: {string.Join(", ", loginResult.Errors.Select(x => x.Description))}",
                400);
        }

        return await tokenService.IssueRefreshToken(mapper.Map<User>(user));
    }

    public async Task<Result<string>> RefreshTokenAsync(string refreshToken)
    {
        var tokenInfo = await dbContext.RefreshTokens.Select(x => new { x.UserId, x.Token, x.IsExpired })
            .FirstOrDefaultAsync(x => x.Token == refreshToken && !x.IsExpired);

        if (tokenInfo?.UserId == null) return Result<string>.Failure("Failed to refresh token", 400);

        var user = await userManager.FindByIdAsync(tokenInfo.UserId);
        if (user == null) return Result<string>.Failure("Failed to refresh token", 400);

        var accessToken = tokenService.GenerateAccessToken(mapper.Map<User>(user));
        return Result<string>.Success(accessToken);
    }

    private async Task<ApplicationUser> FindOrCreateUserAsync(string email, string displayName)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
            return user;

        var newUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(newUser);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Unable to create user: {string.Join(", ", result.Errors.Select(x => x.Description))}");
        }

        return newUser;
    }
}