using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Auth.Commands.Login;

public class LoginCommand : IRequest<Result<TokenPair>>
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;

}

public class LoginCommandHandler(IAccountService accountService, ITokenService tokenService)
    : IRequestHandler<LoginCommand, Result<TokenPair>>
{
    public async Task<Result<TokenPair>> Handle(LoginCommand request, CancellationToken ct)
    {
        var loginResult = await accountService.LoginAsync(request.Email, request.Password);
        if (!loginResult.IsSuccess || loginResult.Value == null)
            return Result<TokenPair>.Failure(loginResult.Error!, loginResult.Code);

        var refreshTokenResult = await tokenService.IssueRefreshToken(loginResult.Value);
        if (!refreshTokenResult.IsSuccess || string.IsNullOrEmpty(refreshTokenResult.Value))
            return Result<TokenPair>.Failure("Failed to issue refresh token. Please try logging in again.", 400);

        string accessToken = tokenService.GenerateAccessToken(loginResult.Value);
        return Result<TokenPair>.Success(new TokenPair { AccessToken = accessToken, RefreshToken = refreshTokenResult.Value });
    }
}
