using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Models;
using AutoMapper;
using MediatR;

namespace Application.Auth.Queries.GetCurrentUserInfo;

public class GetCurrentUserInfoQuery : IRequest<Result<UserDto>>
{
}

public class GetCurrentUserInfoQueryHandler(IUserService userService, IAccountService accountService, IMapper mapper)
    : IRequestHandler<GetCurrentUserInfoQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetCurrentUserInfoQuery request, CancellationToken ct)
    {
        string userId = userService.Id!;
        var userResult = await accountService.GetUserByIdAsync(userId);
        if (userResult.IsSuccess && userResult.Value != null)
            return Result<UserDto>.Success(mapper.Map<UserDto>(userResult.Value));

        return Result<UserDto>.Failure(userResult.Error!, userResult.Code);
    }
}
