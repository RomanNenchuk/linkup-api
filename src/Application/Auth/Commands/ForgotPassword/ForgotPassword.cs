using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Options;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommand : IRequest<Result>
{
    public string Email { get; set; } = null!;
}

public class ForgotPasswordCommandHandler(
    IAccountService accountService,
    IEmailService emailService,
    IOptions<ClientOptions> clientOptions,
    IVerificationLinkService linkService,
    ITokenService tokenService)
    : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly ClientOptions _clientOptions = clientOptions.Value;

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var userResult = await accountService.GetUserByEmailAsync(request.Email);
        if (!userResult.IsSuccess || userResult.Value == null)
            return Result.Failure(userResult.Error!, userResult.Code);

        var remainingSeconds = await tokenService
            .GetCooldownRemainingSecondsAsync(userResult.Value.Id, VerificationTokenType.EmailVerification);

        if (remainingSeconds > 0)
            return Result.Failure($"Please wait {remainingSeconds} seconds before requesting another email.", 429);

        var verificationTokenResult = await tokenService.GeneratePasswordResetTokenAsync(userResult.Value);
        if (!verificationTokenResult.IsSuccess || verificationTokenResult.Value == null)
            return Result.Failure(verificationTokenResult.Error!, verificationTokenResult.Code);

        var saveTokenResult = await tokenService.SaveVerificationTokenAsync(verificationTokenResult.Value,
            userResult.Value.Id, VerificationTokenType.PasswordReset);
        if (!saveTokenResult.IsSuccess)
            return Result.Failure(saveTokenResult.Error!, saveTokenResult.Code);

        string resetUrl = linkService.BuildPasswordResetLink(verificationTokenResult.Value);
        var emailResult = await emailService.SendEmailAsync(userResult.Value.Email, "Email reset", resetUrl);

        return emailResult.IsSuccess ? Result.Success() : Result.Failure("Failed to send reset email", 400);
    }
}