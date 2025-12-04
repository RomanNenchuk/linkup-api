using Application.Common.DTOs;
using Application.Posts.Queries.GetHeatmapPoints;
using Application.Posts.Queries.GetPostClusters;

namespace Application.Common.Interfaces;

public interface IGeoRepository
{
    Task<List<HeatmapPointDto>> GetHeatmapPointsAsync(
        double minLon, double maxLon, double minLat, double maxLat, int zoom, CancellationToken ct);
    Task<List<ClusterDto>> GetPostClustersAsync(CancellationToken ct);
    Task<List<TimestampedPostLocationDto>> GetUserPostLocationsAsync(string userId, CancellationToken ct);
}
