using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Posts.Queries.GetHeatmapPoints;
using Application.Posts.Queries.GetPostClusters;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;


namespace Infrastructure.Services;

public class GeoService(ApplicationDbContext dbContext, ILocationIqService locationService,
    IMemoryCache memoryCache, IGeoRepository geoRepository) : IGeoService
{
    public async Task<Result<LocationDto>> GetDefaultLocation()
    {
        var query = dbContext.Posts
            .Where(p => p.Location != null && p.PostPhotos.Count > 0);

        var total = await query.CountAsync();
        if (total == 0) return Result<LocationDto>.Failure("Failed to get a default location");
        var random = Random.Shared.Next(total);

        var post = await query
            .Skip(random)
            .FirstOrDefaultAsync();

        if (post == null) return Result<LocationDto>.Failure("Failed to get a default location");

        var postLocation = new LocationDto
        {
            Latitude = post.Location!.Y,
            Longitude = post.Location.X
        };

        return Result<LocationDto>.Success(postLocation);
    }

    public async Task<Result<List<HeatmapPointDto>>> GetHeatmapPointsAsync(
        double minLon, double maxLon, double minLat, double maxLat, int zoom, CancellationToken ct)
    {
        var points = await geoRepository.GetHeatmapPointsAsync(minLon, maxLon, minLat, maxLat, zoom, ct);
        return Result<List<HeatmapPointDto>>.Success(points);
    }

    public async Task<Result<List<ClusterDto>>> GetPostClustersAsync(CancellationToken ct)
    {
        const string cacheKey = "PostClusters";
        if (memoryCache.TryGetValue(cacheKey, out List<ClusterDto>? cachedClusters))
            return Result<List<ClusterDto>>.Success(cachedClusters!);

        var clusters = await geoRepository.GetPostClustersAsync(ct);

        // --- Reverse geocoding ---
        foreach (var cluster in clusters)
        {
            var result = await locationService.ReverseGeocodePlace(cluster.Latitude, cluster.Longitude);
            if (result.IsSuccess && !string.IsNullOrEmpty(result.Value))
                cluster.Name = result.Value;
            else
                cluster.Name = $"Cluster #{cluster.Id}";
            await Task.Delay(1000, ct);
        }
        var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
        memoryCache.Set(cacheKey, clusters, cacheOptions);
        return Result<List<ClusterDto>>.Success(clusters);
    }
}
