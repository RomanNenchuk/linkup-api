using Application.Common;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommand : IRequest<Result>
{
    public string Email { get; set; } = null!;
}

public class ForgotPasswordCommandHandler(
    IUserService userService,
    IEmailService emailService,
    IVerificationLinkService linkService,
    ITokenService tokenService)
    : IRequestHandler<ForgotPasswordCommand, Result>
{

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var userResult = await userService.GetUserByEmailAsync(request.Email);
        if (!userResult.IsSuccess || userResult.Value == null)
            return Result.Failure(userResult.Error!, userResult.Code);

        var remainingSeconds = await tokenService
            .GetCooldownRemainingSecondsAsync(userResult.Value.Id, VerificationTokenType.PasswordReset);

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
        var emailBody = emailService.BuildEmailTemplate(
            title: "Reset your password",
            message:
                "We received a request to reset your account password. " +
                "Click the button below to choose a new password.",
            actionUrl: resetUrl,
            actionText: "Reset password",
            footerNote:
                "If you did not request a password reset, please ignore this email. " +
                "Your password will remain unchanged."
        );

        var emailResult = await emailService.SendEmailAsync(
            to: userResult.Value.Email,
            subject: "Password reset request",
            body: emailBody,
            isHtml: true
        );

        return emailResult.IsSuccess ? Result.Success() : Result.Failure("Failed to send reset email", 400);
    }
}