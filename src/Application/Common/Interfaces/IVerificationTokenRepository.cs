using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IVerificationTokenRepository
{
    Task<VerificationToken?> GetTokenAsync(string token, VerificationTokenType type, CancellationToken ct);
    Task MarkAsUsedAsync(VerificationToken token, CancellationToken ct);
    Task AddAsync(VerificationToken token, CancellationToken ct = default);
    Task<VerificationToken?> GetLastTokenAsync(string userId, VerificationTokenType type);
}