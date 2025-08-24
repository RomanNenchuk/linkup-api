using Domain.Common;

namespace Domain.Entities;

public class RefreshToken : BaseToken
{
    public string UserId { get; set; } = null!;
    public DateTime? RevokedAt { get; set; }
    public bool IsRevoked => RevokedAt != null;
}