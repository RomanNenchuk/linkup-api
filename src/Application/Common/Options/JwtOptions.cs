namespace Application.Common.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public AccessTokenOptions AccessToken { get; set; } = null!;
    public RefreshTokenOptions RefreshToken { get; set; } = null!;
    public VerificationTokenOptions VerificationToken { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;

    public class AccessTokenOptions
    {
        public string Key { get; set; } = null!;
        public int ExpireMinutes { get; set; }
    }

    public class RefreshTokenOptions
    {
        public string Key { get; set; } = null!;
        public int ExpireDays { get; set; }
    }

    public class VerificationTokenOptions
    {
        public string Key { get; set; } = null!;
        public int ExpireMinutes { get; set; }
    }
}
