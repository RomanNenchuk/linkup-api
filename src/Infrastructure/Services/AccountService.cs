using System.Security.Claims;
using Application.Common;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using AutoMapper;
using Domain.Enums;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class AccountService(UserManager<ApplicationUser> userManager, ITokenService tokenService,
    ApplicationDbContext dbContext, IMapper mapper) : IAccountService
{
    public async Task<Result> ConfirmEmailAsync(string verificationToken)
    {
        var token = await dbContext.VerificationTokens.FirstOrDefaultAsync(x =>
            x.Type == VerificationTokenType.EmailVerification && x.Token == verificationToken);

        if (token == null) return Result.Failure("Failed to verify email", 400);

        var user = await userManager.FindByIdAsync(token.UserId);
        if (user == null) return Result.Failure("Failed to verify email", 400);
        if (user.EmailConfirmed) return Result.Failure("Email is already confirmed", 400);

        if (token.IsUsed) return Result.Failure("Token is already used", 400);
        token.IsUsed = true;
        await dbContext.SaveChangesAsync();

        var result = await userManager.ConfirmEmailAsync(user, verificationToken);
        if (result.Succeeded)
            return Result.Success();

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return Result.Failure(errors, 400);
    }

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

    public async Task<Result<User>> GetUserByIdAsync(string id)
    {
        var applicationUser = await userManager.FindByIdAsync(id);
        if (applicationUser == null) return Result<User>.Failure("User not found", 404);
        return Result<User>.Success(mapper.Map<User>(applicationUser));
    }

    public async Task<Result<User>> GetUserByEmailAsync(string email)
    {
        var applicationUser = await userManager.FindByEmailAsync(email);
        if (applicationUser == null) return Result<User>.Failure("User not found", 404);
        return Result<User>.Success(mapper.Map<User>(applicationUser));
    }

    public async Task<Result<User>> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return Result<User>.Failure("Invalid email or password", 401);

        if (!await userManager.HasPasswordAsync(user))
            return Result<User>.Failure("This account was created with Google. Please login with Google.", 400);

        var isPasswordValid = await userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
            return Result<User>.Failure("Invalid email or password", 401);

        var mappedUser = mapper.Map<User>(user);

        return Result<User>.Success(mappedUser);
    }

    public async Task<Result<User>> LoginWithGoogleAsync(ClaimsPrincipal claimsPrincipal)
    {
        var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email)
            ?? throw new ExternalLoginProviderException("Google", "Email is null");
        var displayName = claimsPrincipal.FindFirstValue(ClaimTypes.GivenName) ?? "Unknown";
        var provider = "Google";
        var providerKey = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new ExternalLoginProviderException("Google", "Provider key is null");

        var existingUserByLogin = await userManager.FindByLoginAsync(provider, providerKey);
        if (existingUserByLogin != null)
            return Result<User>.Success(mapper.Map<User>(existingUserByLogin));

        var user = await FindOrCreateUserAsync(email, displayName);

        var info = new UserLoginInfo(provider, providerKey, provider);
        var loginResult = await userManager.AddLoginAsync(user, info);
        if (!loginResult.Succeeded &&
            loginResult.Errors.All(e => e.Code != "LoginAlreadyAssociated"))
        {
            return Result<User>.Failure(
                $"Unable to add login: {string.Join(", ", loginResult.Errors.Select(x => x.Description))}",
                400);
        }

        return Result<User>.Success(mapper.Map<User>(user));
    }

    public async Task<Result> LogoutAsync(string refreshToken)
    {
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);
        if (token == null) return Result.Failure("Refresh token not found", 400);
        token.RevokedAt = DateTime.UtcNow;
        var result = await dbContext.SaveChangesAsync() > 0;
        return result ? Result.Success() : Result.Failure("Failed to revoke refresh token", 400);
    }

    public async Task<Result<string>> RefreshTokenAsync(string refreshToken)
    {
        var tokenInfo = await dbContext.RefreshTokens.Select(x => new { x.UserId, x.Token, x.ExpiresAt })
            .FirstOrDefaultAsync(r => r.Token == refreshToken && r.ExpiresAt > DateTime.UtcNow);

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

    public async Task<Result> ResetPasswordAsync(string verificationToken, string newPassword)
    {
        var token = await dbContext.VerificationTokens.FirstOrDefaultAsync(x =>
            x.Type == VerificationTokenType.PasswordReset && x.Token == verificationToken);

        if (token == null) return Result.Failure("Failed to reset password", 400);

        if (token.IsUsed) return Result.Failure("Token is already used", 400);
        token.IsUsed = true;
        await dbContext.SaveChangesAsync();

        var user = await userManager.FindByIdAsync(token.UserId);
        if (user == null) return Result.Failure("User not found", 404);

        var result = await userManager.ResetPasswordAsync(user, verificationToken, newPassword);
        if (result.Succeeded)
            return Result.Success();

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return Result.Failure(errors, 400);
    }

    public async Task<Result<string>> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return Result<string>.Failure("User not found", 404);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        return string.IsNullOrEmpty(token)
            ? Result<string>.Failure("Failed to generate token", 400)
            : Result<string>.Success(token);
    }
}