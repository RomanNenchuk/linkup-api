using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;

namespace Application.Users.Queries.GetRecommendedUsers;

public class GetRecommendedUsersQuery : IRequest<Result<List<User>>>
{
}

public class GetRecommendedUsersQueryHandler(IRecommendationsService recommendationsService, ICurrentUserService currentUserService)
    : IRequestHandler<GetRecommendedUsersQuery, Result<List<User>>>
{
    public async Task<Result<List<User>>> Handle(GetRecommendedUsersQuery request,
        CancellationToken ct)
    {
        var currentUserId = currentUserService.Id;
        return await recommendationsService.GetRecommendedUsersAsync(currentUserId);
    }
}
