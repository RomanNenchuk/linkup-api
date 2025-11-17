using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Posts.Queries.GetUserPostRoutePoints;

public class GetUserPostLocationsQuery : IRequest<Result<List<PostRoutePointDto>>>
{
    public string UserId { get; set; } = null!;
}

public class GetUserPostLocationsQueryHandler(IGeoService geoService)
    : IRequestHandler<GetUserPostLocationsQuery, Result<List<PostRoutePointDto>>>
{
    public async Task<Result<List<PostRoutePointDto>>> Handle(GetUserPostLocationsQuery request, CancellationToken ct)
    {
        return await geoService.GetUserPostLocations(request.UserId);
    }
}
