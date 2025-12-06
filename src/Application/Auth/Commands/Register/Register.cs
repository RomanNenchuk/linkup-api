using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Auth.Commands.Register;

public class RegisterCommand : IRequest<Result<TokenPair>>
{
    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set => _email = value?.Trim() ?? string.Empty;
    }
    public string DisplayName { get; set; } = null!;
    public string Password { get; set; } = null!;

}

public class RegisterCommandHandler(
    IAccountService accountService,
    IEmailService emailService,
    IVerificationLinkService linkService,
    ITokenService tokenService)
    : IRequestHandler<RegisterCommand, Result<TokenPair>>
{

    public async Task<Result<TokenPair>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var creationResult = await accountService.CreateUserAsync(request.Email, request.DisplayName, request.Password);
        if (!creationResult.IsSuccess || creationResult.Value == null)
            return Result<TokenPair>.Failure(creationResult.Error!, creationResult.Code);

        var verificationTokenResult = await tokenService.GenerateEmailConfirmationTokenAsync(creationResult.Value);
        if (!verificationTokenResult.IsSuccess || verificationTokenResult.Value == null)
            return Result<TokenPair>.Failure(verificationTokenResult.Error!, verificationTokenResult.Code);

        var saveTokenResult = await tokenService.SaveVerificationTokenAsync(verificationTokenResult.Value,
            creationResult.Value.Id, VerificationTokenType.EmailVerification);
        if (!saveTokenResult.IsSuccess)
            return Result<TokenPair>.Failure(saveTokenResult.Error!, saveTokenResult.Code);

        string confirmationUrl = linkService.BuildEmailConfirmationLink(verificationTokenResult.Value);
        // Fire-and-forget
        _ = Task.Run(async () =>
        {
            try
            {
                var emailBody = emailService.BuildEmailTemplate(
                title: "Verify your email address",
                message: "Thank you for signing up. Please confirm your email address by clicking the button below.",
                actionUrl: confirmationUrl,
                actionText: "Verify email"
                );
                await emailService.SendEmailAsync(to: request.Email,
                    subject: "Email verification",
                    body: emailBody,
                    isHtml: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email send background error: " + ex);
            }
        }, ct);

        var refreshTokenResult = await tokenService.IssueRefreshToken(creationResult.Value);
        if (!refreshTokenResult.IsSuccess || string.IsNullOrEmpty(refreshTokenResult.Value))
            return Result<TokenPair>.Failure("Failed to issue refresh token. Please, try to login", 400);

        string accessToken = tokenService.GenerateAccessToken(creationResult.Value);
        return Result<TokenPair>.Success(new TokenPair { AccessToken = accessToken, RefreshToken = refreshTokenResult.Value });
    }
}