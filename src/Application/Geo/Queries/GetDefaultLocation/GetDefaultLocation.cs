using Application.Common;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Geo.Queries.GetDefaultLocation;

public class GetDefaultLocationQuery : IRequest<Result<LocationDto>>
{
}

public class GetDefaultLocationQueryHandler(IGeoService geoService)
    : IRequestHandler<GetDefaultLocationQuery, Result<LocationDto>>
{
    public async Task<Result<LocationDto>> Handle(GetDefaultLocationQuery request, CancellationToken ct)
    {
        return await geoService.GetDefaultLocation();
    }
}
