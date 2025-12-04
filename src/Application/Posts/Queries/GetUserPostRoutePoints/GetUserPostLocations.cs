using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Posts.Queries.GetUserPostRoutePoints;

public class GetUserPostLocationsQuery : IRequest<Result<List<TimestampedPostLocationDto>>>
{
    public string UserId { get; set; } = null!;
}

public class GetUserPostLocationsQueryHandler(IGeoRepository geoRepository)
    : IRequestHandler<GetUserPostLocationsQuery, Result<List<TimestampedPostLocationDto>>>
{
    public async Task<Result<List<TimestampedPostLocationDto>>> Handle(GetUserPostLocationsQuery request, CancellationToken ct)
    {
        var points = await geoRepository.GetUserPostLocationsAsync(request.UserId, ct);
        return Result<List<TimestampedPostLocationDto>>.Success(points);
    }
}
