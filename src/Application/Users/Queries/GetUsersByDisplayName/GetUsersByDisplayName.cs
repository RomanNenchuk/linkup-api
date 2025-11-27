using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Users.Queries.GetUsersByDisplayName;

public class GetUsersByDisplayNameQuery : IRequest<Result<PagedResult<SearchedUserDto>>>
{
    public string DisplayName { get; set; } = null!;
    public string? Cursor { get; set; }
    private int _pageSize;
    public int PageSize { get => _pageSize; set => _pageSize = value >= 50 ? 50 : value; }
}

public class GetUsersByDisplayNameQueryHandler(IUserService userService)
    : IRequestHandler<GetUsersByDisplayNameQuery, Result<PagedResult<SearchedUserDto>>>
{
    public async Task<Result<PagedResult<SearchedUserDto>>> Handle(GetUsersByDisplayNameQuery request, CancellationToken ct)
    {
        return await userService.GetUsersByDisplayNameAsync(request);
    }
}
