using System.Security.Claims;
using System.Text.Json;
using Application.Common;
using Application.Common.DTOs;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using AutoMapper;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Services;

public class AccountService(UserManager<ApplicationUser> userManager,
    ITokenService tokenService, IMapper mapper) : IAccountService
{

    public async Task<Result<string>> LoginWithGoogleAsync(ClaimsPrincipal claimsPrincipal)
    {
        var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email)
            ?? throw new ExternalLoginProviderException("Google", "Email is null");

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

            if (!result.Succeeded) return Result<string>.Failure(
                $"Unable to create user: {string.Join(", ", result.Errors.Select(x => x.Description))}", 400);

            user = newUser;
        }

        var info = new UserLoginInfo("Google",
            claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            "Google");

        var loginResult = await userManager.AddLoginAsync(user, info);

        if (!loginResult.Succeeded) return Result<string>.Failure(
                $"Unable to create user: {string.Join(", ", loginResult.Errors.Select(x => x.Description))}", 400);

        var refreshToken = tokenService.GenerateRefreshToken(mapper.Map<User>(user));
        return Result<string>.Success(refreshToken);
    }

}