using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class VerificationToken : BaseToken
{
    public VerificationTokenType Type { get; set; }
    public string UserId { get; set; } = null!;
}