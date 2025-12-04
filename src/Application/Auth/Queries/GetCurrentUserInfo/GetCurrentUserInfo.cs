using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Auth.Queries.GetCurrentUserInfo;

public class GetCurrentUserInfoQuery : IRequest<Result<UserProfileDto>>
{
}

public class GetCurrentUserInfoQueryHandler(ICurrentUserService currentUserService, IUserService userService)
    : IRequestHandler<GetCurrentUserInfoQuery, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(GetCurrentUserInfoQuery request, CancellationToken ct)
    {
        string userId = currentUserService.Id!;
        var userResult = await userService.GetUserInformationAsync(userId);

        return userResult.IsSuccess && userResult.Value != null
            ? Result<UserProfileDto>.Success(userResult.Value)
            : Result<UserProfileDto>.Failure(userResult.Error!, userResult.Code);
    }
}
