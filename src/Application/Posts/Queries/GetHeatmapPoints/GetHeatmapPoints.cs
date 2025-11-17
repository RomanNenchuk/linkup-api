using Application.Common;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Posts.Queries.GetHeatmapPoints;


public class GetHeatmapPointsQuery : IRequest<Result<List<HeatmapPointDto>>>
{
    public double MinLon { get; set; }
    public double MaxLon { get; set; }
    public double MinLat { get; set; }
    public double MaxLat { get; set; }
    public int Zoom { get; set; }

}

public class GetHeatmapPointsQueryHandler(IGeoService geoService) : IRequestHandler<GetHeatmapPointsQuery, Result<List<HeatmapPointDto>>>
{
    public async Task<Result<List<HeatmapPointDto>>> Handle(GetHeatmapPointsQuery request, CancellationToken ct)
    {
        return await geoService.GetHeatmapPointsAsync(request.MinLon, request.MaxLon, request.MinLat,
            request.MaxLat, request.Zoom, ct);
    }
}
