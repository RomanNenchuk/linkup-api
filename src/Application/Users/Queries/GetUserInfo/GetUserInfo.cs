using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Users.Queries.GetUserInfo;

public class GetUserInfoQuery : IRequest<Result<UserProfieDto>>
{
    public string UserId { get; set; } = null!;
}

public class GetUserInfoQueryHandler(IAccountService accountService, ICurrentUserService currentUserService)
    : IRequestHandler<GetUserInfoQuery, Result<UserProfieDto>>
{
    public async Task<Result<UserProfieDto>> Handle(GetUserInfoQuery request,
        CancellationToken ct)
    {
        var currentUserId = currentUserService.Id;
        var userResult = await accountService.GetUserInformationAsync(request.UserId, currentUserId);
        if (userResult.IsSuccess && userResult.Value != null)
            return Result<UserProfieDto>.Success(userResult.Value);

        return Result<UserProfieDto>.Failure(userResult.Error!, userResult.Code);
    }
}
