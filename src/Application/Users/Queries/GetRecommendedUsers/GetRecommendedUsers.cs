using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;

namespace Application.Users.Queries.GetRecommendedUsers;

public class GetRecommendedUsersQuery : IRequest<Result<List<RecommendedUserDto>>>
{
}

public class GetRecommendedUsersQueryHandler(IRecommendationsService recommendationsService, ICurrentUserService currentUserService)
    : IRequestHandler<GetRecommendedUsersQuery, Result<List<RecommendedUserDto>>>
{
    public async Task<Result<List<RecommendedUserDto>>> Handle(GetRecommendedUsersQuery request,
        CancellationToken ct)
    {
        var currentUserId = currentUserService.Id;
        return await recommendationsService.GetRecommendedUsersAsync(currentUserId);
    }
}
