using System.Security.Claims;
using Application.Common;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using AutoMapper;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Services;

public class AccountService(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IVerificationTokenRepository verificationTokenRepo,
    IRefreshTokenRepository refreshTokenRepo,
    IMapper mapper) : IAccountService
{
    private readonly CancellationToken ct = CancellationToken.None;

    public async Task<Result> ConfirmEmailAsync(string verificationToken)
    {
        var token = await verificationTokenRepo.GetTokenAsync(
            verificationToken,
            VerificationTokenType.EmailVerification,
            ct
        );

        if (token == null)
            return Result.Failure("Failed to verify email", 400);

        if (token.IsUsed)
            return Result.Failure("Token is already used", 400);

        var user = await userManager.FindByIdAsync(token.UserId);
        if (user == null)
            return Result.Failure("Failed to verify email", 400);

        if (user.EmailConfirmed)
            return Result.Failure("Email is already confirmed", 400);

        await verificationTokenRepo.MarkAsUsedAsync(token, ct);

        var result = await userManager.ConfirmEmailAsync(user, verificationToken);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)), 400);
    }

    public async Task<Result<User>> CreateUserAsync(string email, string displayName, string password)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null)
            return Result<User>.Failure("User with this email already exists", 400);

        var user = new ApplicationUser
        {
            DisplayName = displayName,
            UserName = email,
            Email = email,
            EmailConfirmed = false
        };

        var result = await userManager.CreateAsync(user, password);

        return result.Succeeded
            ? Result<User>.Success(mapper.Map<User>(user))
            : Result<User>.Failure("Failed to create user", 400);
    }

    public async Task<Result<User>> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return Result<User>.Failure("Invalid email or password", 401);

        if (!await userManager.HasPasswordAsync(user))
            return Result<User>.Failure("This account was created with Google. Please login with Google.", 400);

        if (!await userManager.CheckPasswordAsync(user, password))
            return Result<User>.Failure("Invalid email or password", 401);

        return Result<User>.Success(mapper.Map<User>(user));
    }

    public async Task<Result<User>> LoginWithGoogleAsync(ClaimsPrincipal claimsPrincipal)
    {
        var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email)
            ?? throw new ExternalLoginProviderException("Google", "Email is null");

        var displayName = claimsPrincipal.FindFirstValue(ClaimTypes.GivenName) ?? "Unknown";
        var providerKey = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new ExternalLoginProviderException("Google", "Provider key is null");

        const string provider = "Google";

        var existingLoginUser = await userManager.FindByLoginAsync(provider, providerKey);
        if (existingLoginUser != null)
            return Result<User>.Success(mapper.Map<User>(existingLoginUser));

        var user = await FindOrCreateUserAsync(email, displayName);
        await EnsureEmailConfirmedAsync(user);

        var loginInfo = new UserLoginInfo(provider, providerKey, provider);
        var loginResult = await userManager.AddLoginAsync(user, loginInfo);

        if (!loginResult.Succeeded &&
            loginResult.Errors.All(e => e.Code != "LoginAlreadyAssociated"))
        {
            return Result<User>.Failure(
                "Unable to add login: " +
                string.Join(", ", loginResult.Errors.Select(e => e.Description)));
        }

        return Result<User>.Success(mapper.Map<User>(user));
    }

    public async Task<Result> LogoutAsync(string refreshToken)
    {
        var token = await refreshTokenRepo.GetByTokenAsync(refreshToken, ct);
        if (token == null)
            return Result.Failure("Refresh token not found", 400);

        await refreshTokenRepo.MarkAsRevokedAsync(token, ct);

        return Result.Success();
    }

    public async Task<Result<string>> RefreshTokenAsync(string refreshToken)
    {
        var token = await refreshTokenRepo.GetByTokenAsync(refreshToken, ct);

        if (token == null || token.IsExpired)
            return Result<string>.Failure("Invalid or expired token");

        if (token.IsRevoked)
            return Result<string>.Failure("Token was revoked");

        var user = await userManager.FindByIdAsync(token.UserId);
        if (user == null)
            return Result<string>.Failure("Failed to refresh token", 400);

        var newAccessToken = tokenService.GenerateAccessToken(mapper.Map<User>(user));

        return Result<string>.Success(newAccessToken);
    }

    public async Task<Result> ResetPasswordAsync(string verificationToken, string newPassword)
    {
        var token = await verificationTokenRepo.GetTokenAsync(
            verificationToken,
            VerificationTokenType.PasswordReset,
            ct
        );

        if (token == null)
            return Result.Failure("Failed to reset password", 400);

        if (token.IsUsed)
            return Result.Failure("Token is already used", 400);

        await verificationTokenRepo.MarkAsUsedAsync(token, ct);

        var user = await userManager.FindByIdAsync(token.UserId);
        if (user == null)
            return Result.Failure("User not found", 404);

        var result = await userManager.ResetPasswordAsync(user, verificationToken, newPassword);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)), 400);
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
                "Unable to create user: " +
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return newUser;
    }

    private async Task EnsureEmailConfirmedAsync(ApplicationUser user)
    {
        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
        }
    }
}
