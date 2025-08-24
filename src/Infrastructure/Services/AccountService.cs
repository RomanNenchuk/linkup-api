using System.Security.Claims;
using System.Text.Json;
using Application.Common;
using Application.Common.DTOs;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class AccountService(UserManager<ApplicationUser> userManager, ITokenService tokenService,
    ApplicationDbContext dbContext, IMapper mapper) : IAccountService
{

    public async Task<Result<string>> LoginWithGoogleAsync(ClaimsPrincipal claimsPrincipal)
    {
        var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email)
            ?? throw new ExternalLoginProviderException("Google", "Email is null");

        var provider = "Google";
        var providerKey = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new ExternalLoginProviderException("Google", "Provider key is null");

        var existingUserByLogin = await userManager.FindByLoginAsync(provider, providerKey);
        if (existingUserByLogin != null)
            return await tokenService.IssueRefreshToken(mapper.Map<User>(existingUserByLogin));

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = claimsPrincipal.FindFirstValue(ClaimTypes.GivenName) ?? "Unknown",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(newUser);
            if (!result.Succeeded)
            {
                return Result<string>.Failure(
                    $"Unable to create user: {string.Join(", ", result.Errors.Select(x => x.Description))}",
                    400);
            }
            user = newUser;
        }

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
        var tokenInfo = await dbContext.RefreshTokens.Select(x => new { x.UserId, x.Token })
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (tokenInfo?.UserId == null) return Result<string>.Failure("Failed to refresh token", 400);

        var user = await userManager.FindByIdAsync(tokenInfo.UserId);
        if (user == null) return Result<string>.Failure("Failed to refresh token", 400);

        var accessToken = tokenService.GenerateAccessToken(mapper.Map<User>(user));
        return Result<string>.Success(accessToken);
    }
}