using Application.Common.Models;

namespace Application.Common.Interfaces;

public interface ITokenService
{
    public string GenerateAccessToken(User user);
    public string GenerateRefreshToken(User user);
    Task<Result<string>> IssueRefreshToken(User user);
}