using Application.Common;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Geo.Queries.GetDefaultLocation;

public class GetDefaultLocationQuery : IRequest<Result<LocationDto>>
{
}

public class GetDefaultLocationQueryHandler(ILocationIqService locationService)
    : IRequestHandler<GetDefaultLocationQuery, Result<LocationDto>>
{
    public async Task<Result<LocationDto>> Handle(GetDefaultLocationQuery request, CancellationToken ct)
    {
        return await locationService.GetDefaultLocation();
    }
}
