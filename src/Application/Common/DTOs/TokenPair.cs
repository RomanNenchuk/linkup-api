namespace Application.Common.DTOs;

public class TokenPair
{
    public string RefreshToken { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
}