using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(ApplicationDbContext dbContext) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct)
    {
        return dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token, ct);
    }

    public async Task MarkAsRevokedAsync(RefreshToken token, CancellationToken ct)
    {
        token.RevokedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(ct);
    }
}