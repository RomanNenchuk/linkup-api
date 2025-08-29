using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Options;
using AutoMapper;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Auth.Commands.Register;

public class RegisterCommand : IRequest<Result<TokenPair>>
{
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Password { get; set; } = null!;

}

public class RegisterCommandHandler(
    IAccountService accountService,
    IEmailService emailService,
    IOptions<ClientOptions> clientOptions,
    ITokenService tokenService)
    : IRequestHandler<RegisterCommand, Result<TokenPair>>
{
    private readonly ClientOptions _clientOptions = clientOptions.Value;

    public async Task<Result<TokenPair>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var creationResult = await accountService.CreateUserAsync(request.Email, request.DisplayName, request.Password);
        if (!creationResult.IsSuccess || creationResult.Value == null)
            return Result<TokenPair>.Failure(creationResult.Error!, creationResult.Code);

        var verificationTokenResult = await tokenService.IssueVerificationToken(creationResult.Value, VerificationTokenType.EmailVerification);
        if (!verificationTokenResult.IsSuccess || verificationTokenResult.Value == null)
            return Result<TokenPair>.Failure(verificationTokenResult.Error!, verificationTokenResult.Code);

        var confirmationUrl = $"{_clientOptions.Url}/verify-email?verificationToken={verificationTokenResult.Value}";
        await emailService.SendEmailAsync(creationResult.Value.Email, "Email confirmation", confirmationUrl);

        var refreshTokenResult = await tokenService.IssueRefreshToken(creationResult.Value);
        if (!refreshTokenResult.IsSuccess || string.IsNullOrEmpty(refreshTokenResult.Value))
            return Result<TokenPair>.Failure("Failed to issue refresh token. Please, try to login", 400);

        string accessToken = tokenService.GenerateAccessToken(creationResult.Value);
        return Result<TokenPair>.Success(new TokenPair { AccessToken = accessToken, RefreshToken = refreshTokenResult.Value });
    }
}