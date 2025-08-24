using Application.Common;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<string>>;

public class RefreshTokenCommandHandler(IAccountService accountService) : IRequestHandler<RefreshTokenCommand, Result<string>>
{
    public async Task<Result<string>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        return await accountService.RefreshTokenAsync(request.RefreshToken);
    }
}