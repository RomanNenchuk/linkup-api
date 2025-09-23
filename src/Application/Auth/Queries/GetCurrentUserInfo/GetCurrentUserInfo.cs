using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Auth.Queries.GetCurrentUserInfo;

public class GetCurrentUserInfoQuery : IRequest<Result<UserProfieDto>>
{
}

public class GetCurrentUserInfoQueryHandler(ICurrentUserService currentUserService, IAccountService accountService)
    : IRequestHandler<GetCurrentUserInfoQuery, Result<UserProfieDto>>
{
    public async Task<Result<UserProfieDto>> Handle(GetCurrentUserInfoQuery request, CancellationToken ct)
    {
        string userId = currentUserService.Id!;
        var userResult = await accountService.GetUserInformationAsync(userId);
        if (userResult.IsSuccess && userResult.Value != null)
            return Result<UserProfieDto>.Success(userResult.Value);

        return Result<UserProfieDto>.Failure(userResult.Error!, userResult.Code);
    }
}
