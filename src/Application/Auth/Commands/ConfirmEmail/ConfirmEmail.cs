using Application.Common;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Auth.Commands.ConfirmEmail;

public class ConfirmEmailCommand : IRequest<Result>
{
    public string VerificationToken { get; set; } = null!;
}

public class ConfirmEmailCommandHandler(IAccountService accountService) : IRequestHandler<ConfirmEmailCommand, Result>
{
    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        return await accountService.ConfirmEmailAsync(request.VerificationToken);
    }
}