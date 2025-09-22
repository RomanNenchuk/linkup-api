using Application.Common;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Auth.Queries.GetEmailVerificationCooldown;

public class GetEmailVerificationCooldownQuery : IRequest<Result<int>>
{
}

public class GetEmailVerificationCooldownQueryHandler(
    ICurrentUserService currentUserService,
    ITokenService tokenService)
    : IRequestHandler<GetEmailVerificationCooldownQuery, Result<int>>
{

    public async Task<Result<int>> Handle(GetEmailVerificationCooldownQuery request, CancellationToken ct)
    {
        var userId = currentUserService.Id;
        if (userId == null) return Result<int>.Failure("Anauthorized", 401);
        var remainingSeconds = await tokenService
            .GetCooldownRemainingSecondsAsync(userId, VerificationTokenType.EmailVerification);

        return Result<int>.Success(remainingSeconds);
    }
}
