using Application.Common;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Auth.Commands.ResendEmailVerification;

public class ResendEmailVerificationCommand : IRequest<Result>
{
}

public class ResendEmailVerificationCommandHandler(
    ICurrentUserService currentUserService,
    IEmailService emailService,
    IUserService userService,
    IVerificationLinkService linkService,
    ITokenService tokenService)
    : IRequestHandler<ResendEmailVerificationCommand, Result>
{

    public async Task<Result> Handle(ResendEmailVerificationCommand request, CancellationToken ct)
    {
        var userId = currentUserService.Id;
        if (userId == null) return Result.Failure("Anauthorized", 401);
        var currentUserResult = await userService.GetUserByIdAsync(userId);
        if (!currentUserResult.IsSuccess || currentUserResult.Value == null)
            return Result.Failure(currentUserResult.Error!, currentUserResult.Code);

        var remainingSeconds = await tokenService
            .GetCooldownRemainingSecondsAsync(userId, VerificationTokenType.EmailVerification);
        if (remainingSeconds > 0)
            return Result.Failure($"Please wait {remainingSeconds} seconds before requesting another email.", 429);

        var verificationTokenResult = await tokenService.GenerateEmailConfirmationTokenAsync(currentUserResult.Value);
        if (!verificationTokenResult.IsSuccess || verificationTokenResult.Value == null)
            return Result.Failure(verificationTokenResult.Error!, verificationTokenResult.Code);

        var saveTokenResult = await tokenService.SaveVerificationTokenAsync(verificationTokenResult.Value,
            currentUserResult.Value.Id, VerificationTokenType.EmailVerification);
        if (!saveTokenResult.IsSuccess)
            return Result.Failure(saveTokenResult.Error!, saveTokenResult.Code);

        string confirmationUrl = linkService.BuildEmailConfirmationLink(verificationTokenResult.Value);
        var emailBody = emailService.BuildEmailTemplate(
            title: "Verify your email address",
            message:
                "You recently requested to verify your email address. " +
                "Please click the button below to complete the verification process.",
            actionUrl: confirmationUrl,
            actionText: "Verify email address");

        await emailService.SendEmailAsync(
            to: currentUserResult.Value.Email,
            subject: "Confirm your email address",
            body: emailBody,
            isHtml: true
        );

        return Result.Success();
    }
}
