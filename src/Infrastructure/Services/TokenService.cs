using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Services;

public class TokenService(
    IOptions<JwtOptions> jwtOptions,
    UserManager<ApplicationUser> userManager,
    IRefreshTokenRepository refreshTokenRepo,
    IVerificationTokenRepository verificationTokenRepo
    ) : ITokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("EmailConfirmed", user.EmailConfirmed.ToString().ToLower())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.AccessToken.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessToken.ExpireMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken(User user)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id) };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.RefreshToken.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_jwtOptions.RefreshToken.ExpireDays),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<Result<string>> IssueRefreshToken(User user)
    {
        var refreshToken = GenerateRefreshToken(user);
        var entity = new RefreshToken
        {
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshToken.ExpireDays),
            UserId = user.Id
        };

        await refreshTokenRepo.AddAsync(entity, CancellationToken.None);

        return Result<string>.Success(refreshToken);
    }

    public string GenerateVerificationToken(User user)
    {
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id), new Claim("token-type", "verification") };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.VerificationToken.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.VerificationToken.ExpireMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<Result<string>> IssueVerificationToken(User user, VerificationTokenType type)
    {
        var verificationToken = GenerateVerificationToken(user);

        var entity = new VerificationToken
        {
            Token = verificationToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.VerificationToken.ExpireMinutes),
            Type = type,
            UserId = user.Id
        };

        await verificationTokenRepo.AddAsync(entity);

        return Result<string>.Success(verificationToken);
    }

    public async Task<int> GetCooldownRemainingSecondsAsync(string userId, VerificationTokenType type)
    {
        var lastToken = await verificationTokenRepo.GetLastTokenAsync(userId, type);
        if (lastToken == null) return 0;
        var cooldownUntil = lastToken.CreatedAt.AddSeconds(_jwtOptions.VerificationToken.CooldownSeconds);
        var remaining = (int)(cooldownUntil - DateTime.UtcNow).TotalSeconds;
        return remaining > 0 ? remaining : 0;
    }

    public async Task<Result<string>> GenerateEmailConfirmationTokenAsync(User user)
    {
        var applicationUser = await userManager.FindByEmailAsync(user.Email);
        if (applicationUser == null) return Result<string>.Failure("User not found", 404);

        var token = await userManager.GenerateEmailConfirmationTokenAsync(applicationUser);
        if (string.IsNullOrEmpty(token)) return Result<string>.Failure("Failed to generate email confirmation token", 400);

        return Result<string>.Success(token);
    }

    public async Task<Result<string>> GeneratePasswordResetTokenAsync(User user)
    {
        var applicationUser = await userManager.FindByEmailAsync(user.Email);
        if (applicationUser == null) return Result<string>.Failure("User not found", 404);

        var token = await userManager.GeneratePasswordResetTokenAsync(applicationUser);
        if (string.IsNullOrEmpty(token)) return Result<string>.Failure("Failed to generate password reset token", 400);

        return Result<string>.Success(token);
    }

    public async Task<Result> SaveVerificationTokenAsync(string token, string userId, VerificationTokenType type)
    {
        var entity = new VerificationToken
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.VerificationToken.ExpireMinutes),
            Type = type,
            UserId = userId
        };

        await verificationTokenRepo.AddAsync(entity);
        return Result.Success();
    }
}