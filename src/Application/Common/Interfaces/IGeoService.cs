using Application.Common.DTOs;
using Application.Posts.Queries.GetHeatmapPoints;
using Application.Posts.Queries.GetPostClusters;

namespace Application.Common.Interfaces;

public interface IGeoService
{
    Task<Result<List<HeatmapPointDto>>> GetHeatmapPointsAsync(
        double minLon, double maxLon, double minLat, double maxLat, int zoom, CancellationToken ct);
    Task<Result<List<ClusterDto>>> GetPostClustersAsync(CancellationToken ct);
    Task<Result<List<TimestampedPostLocationDto>>> GetUserPostLocations(string userId);
    Task<Result<LocationDto>> GetDefaultLocation();
}
