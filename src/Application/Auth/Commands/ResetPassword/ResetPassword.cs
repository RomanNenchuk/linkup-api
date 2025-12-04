using Application.Common;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Auth.Commands.ResetPassword;

public class ResetPasswordCommand : IRequest<Result>
{
    public string VerificationToken { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

public class ResetPasswordCommandHandler(IAccountService accountService) : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        return await accountService.ResetPasswordAsync(request.VerificationToken, request.NewPassword);
    }
}