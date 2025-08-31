using System.Net;
using Application.Common.Interfaces;
using Application.Common.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

public class VerificationLinkService(IOptions<ClientOptions> clientOptions) : IVerificationLinkService
{
    private readonly ClientOptions _options = clientOptions.Value;

    public string BuildEmailConfirmationLink(string token)
    {
        var encodedToken = Base64UrlEncoder.Encode(token);
        return $"{_options.Url}/confirm-email?verificationToken={encodedToken}";
    }

    public string BuildPasswordResetLink(string token)
    {
        var encodedToken = Base64UrlEncoder.Encode(token);
        return $"{_options.Url}/reset-password?verificationToken={encodedToken}";
    }
}
