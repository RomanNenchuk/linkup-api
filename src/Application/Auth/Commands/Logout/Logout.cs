using Application.Common;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest<Result>;

public class LogoutCommandHandler(IAccountService accountService)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
    {

        return await accountService.LogoutAsync(request.RefreshToken);
    }
}