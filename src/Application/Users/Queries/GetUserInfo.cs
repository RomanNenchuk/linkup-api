using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using AutoMapper;
using MediatR;

namespace Application.Auth.Queries.GetUserInfo;

public class GetUserInfoQuery : IRequest<Result<UserDto>>
{
    public string UserId { get; set; } = null!;
}

public class GetUserInfoQueryHandler(ICurrentUserService currentUserService, IAccountService accountService, IMapper mapper)
    : IRequestHandler<GetUserInfoQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetUserInfoQuery request, CancellationToken ct)
    {
        string userId = currentUserService.Id!;
        var userResult = await accountService.GetUserByIdAsync(userId);
        if (userResult.IsSuccess && userResult.Value != null)
            return Result<UserDto>.Success(mapper.Map<UserDto>(userResult.Value));

        return Result<UserDto>.Failure(userResult.Error!, userResult.Code);
    }
}
