using Application.Common.Models;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface ITokenService
{
    public string GenerateAccessToken(User user);
    public string GenerateRefreshToken(User user);
    Task<Result<string>> IssueRefreshToken(User user);
    public string GenerateVerificationToken(User user);
    Task<Result<string>> GenerateEmailConfirmationTokenAsync(User user);
    Task<Result<string>> GeneratePasswordResetTokenAsync(User user);
    Task<Result> SaveVerificationTokenAsync(string token, string userId, VerificationTokenType type);
    Task<int> GetCooldownRemainingSecondsAsync(string userId, VerificationTokenType type);
}