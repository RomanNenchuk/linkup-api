using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Geo.Queries.GetDefaultLocation;
using Application.Posts.Queries.GetHeatmapPoints;
using Application.Posts.Queries.GetPostClusters;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;


namespace Infrastructure.Services;

public class GeoService(ApplicationDbContext dbContext, ILocationIqService locationService,
    IMemoryCache memoryCache) : IGeoService
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
        double cellSize = zoom switch
        {
            <= 5 => 0.5,
            <= 8 => 0.2,
            <= 12 => 0.05,
            _ => 0.01
        };

        string sql;

        if (zoom >= 6)
        {
            sql = @"
            SELECT 
                ST_MakePoint(
                    AVG(ST_X(""Location""::geometry)),
                    AVG(ST_Y(""Location""::geometry))
                ) AS ""Geom"",
                COUNT(*) AS ""PointCount""
            FROM ""Posts""
            WHERE ""Location""::geometry && ST_MakeEnvelope(@minLon, @minLat, @maxLon, @maxLat, 4326)
            GROUP BY ST_SnapToGrid(""Location""::geometry, @cellSize, @cellSize)
             ";
        }
        else
        {
            sql = @"
            SELECT 
                ST_SnapToGrid(""Location""::geometry, @cellSize, @cellSize) AS ""Geom"",
                COUNT(*) AS ""PointCount""
            FROM ""Posts""
            WHERE ""Location""::geometry && ST_MakeEnvelope(@minLon, @minLat, @maxLon, @maxLat, 4326)
            GROUP BY ST_SnapToGrid(""Location""::geometry, @cellSize, @cellSize)
            ";
        }

        var parameters = new[]
        {
            new Npgsql.NpgsqlParameter("@minLon", minLon),
            new Npgsql.NpgsqlParameter("@minLat", minLat),
            new Npgsql.NpgsqlParameter("@maxLon", maxLon),
            new Npgsql.NpgsqlParameter("@maxLat", maxLat),
            new Npgsql.NpgsqlParameter("@cellSize", cellSize)
        };

        var points = await dbContext.HeatmapPoints
            .FromSqlRaw(sql, parameters)
            .Select(p => new HeatmapPointDto
            {
                Latitude = p.Geom.Y,
                Longitude = p.Geom.X,
                Count = p.PointCount
            })
            .ToListAsync(ct);

        return Result<List<HeatmapPointDto>>.Success(points);
    }

    public async Task<Result<List<ClusterDto>>> GetPostClustersAsync(CancellationToken ct)
    {
        const string cacheKey = "PostClusters";
        if (memoryCache.TryGetValue(cacheKey, out List<ClusterDto>? cachedClusters))
        {
            return Result<List<ClusterDto>>.Success(cachedClusters!);
        }

        var sql = @"
            WITH pts AS (
                SELECT 
                    ST_ClusterKMeans(""Location""::geometry, 10) OVER () AS cluster_id,
                    ""Location""::geometry AS geom
                FROM ""Posts""
                WHERE ""Location"" IS NOT NULL
            ),
            cent AS (
                SELECT 
                    cluster_id,
                    ST_Centroid(ST_Collect(geom)) AS centroid,
                    COUNT(*) AS count
                FROM pts
                GROUP BY cluster_id
            ),
            med AS (
                SELECT DISTINCT ON (p.cluster_id)
                    p.cluster_id,
                    p.geom
                FROM pts p
                JOIN cent c ON p.cluster_id = c.cluster_id
                ORDER BY p.cluster_id, ST_Distance(p.geom, c.centroid)
            )

            SELECT
                c.cluster_id AS ""ClusterId"",
                ST_Y(c.centroid) AS ""CentroidLatitude"",
                ST_X(c.centroid) AS ""CentroidLongitude"",
                ST_Y(m.geom) AS ""Latitude"",
                ST_X(m.geom) AS ""Longitude"",
                c.count AS ""Count""
            FROM cent c
            JOIN med m ON c.cluster_id = m.cluster_id
            ORDER BY c.count DESC
            ";


        var clusters = await dbContext.Clusters
            .FromSqlRaw(sql)
            .Select(c => new ClusterDto
            {
                Id = c.ClusterId,
                Latitude = c.Latitude,
                Longitude = c.Longitude,
                Count = c.Count
            })
            .ToListAsync(ct);

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

    public async Task<Result<List<PostRoutePointDto>>> GetUserPostLocations(string userId)
    {
        var sql = @"
            SELECT 
                ST_Y(""Location""::geometry) AS ""Latitude"",
                ST_X(""Location""::geometry) AS ""Longitude""
            FROM ""Posts""
            WHERE ""AuthorId"" = {0} AND ""Location"" IS NOT NULL;
        ";

        var points = await dbContext.PostRoutePoints
            .FromSqlRaw(sql, userId)
            .ToListAsync();

        return Result<List<PostRoutePointDto>>.Success(points);
    }

}
