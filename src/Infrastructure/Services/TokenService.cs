using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

public class TokenService(IOptions<JwtOptions> jwtOptions, ApplicationDbContext dbContext)
    : ITokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!),
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
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
        };

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
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshToken.ExpireDays),
            UserId = user.Id
        });

        var dbResult = await dbContext.SaveChangesAsync() > 0;
        return dbResult
            ? Result<string>.Success(refreshToken)
            : Result<string>.Failure("Failed to save refresh token", 400);
    }

    public string GenerateVerificationToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim("token-type", "verification")
        };

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
        dbContext.VerificationTokens.Add(new VerificationToken
        {
            Token = verificationToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.VerificationToken.ExpireMinutes),
            Type = type,
            UserId = user.Id
        });

        var dbResult = await dbContext.SaveChangesAsync() > 0;
        return dbResult
            ? Result<string>.Success(verificationToken)
            : Result<string>.Failure("Failed to save verification token", 400);
    }
}