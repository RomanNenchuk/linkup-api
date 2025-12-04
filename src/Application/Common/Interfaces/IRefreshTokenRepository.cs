using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct);
    Task MarkAsRevokedAsync(RefreshToken token, CancellationToken ct);
}