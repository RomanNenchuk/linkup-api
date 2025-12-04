using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IVerificationTokenRepository
{
    Task<VerificationToken?> GetTokenAsync(string token, VerificationTokenType type, CancellationToken ct);
    Task MarkAsUsedAsync(VerificationToken token, CancellationToken ct);
}