using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class VerificationTokenRepository(ApplicationDbContext dbContext) : IVerificationTokenRepository
{
    public Task<VerificationToken?> GetTokenAsync(string token, VerificationTokenType type, CancellationToken ct)
    {
        return dbContext.VerificationTokens
            .FirstOrDefaultAsync(x => x.Type == type && x.Token == token, ct);
    }

    public async Task MarkAsUsedAsync(VerificationToken token, CancellationToken ct)
    {
        token.IsUsed = true;
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task AddAsync(VerificationToken token, CancellationToken ct = default)
    {
        dbContext.VerificationTokens.Add(token);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<VerificationToken?> GetLastTokenAsync(string userId, VerificationTokenType type)
    {
        return await dbContext.VerificationTokens
            .Where(t => t.UserId == userId && t.Type == type)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }
}