using Application.Common.Constants;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Posts.Queries.GetHeatmapPoints;
using Application.Posts.Queries.GetPostClusters;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Persistence.Repositories;

public class GeoRepository(ApplicationDbContext dbContext) : IGeoRepository
{
    public async Task<List<HeatmapPointDto>> GetHeatmapPointsAsync(
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
            new NpgsqlParameter("@minLon", minLon),
            new NpgsqlParameter("@minLat", minLat),
            new NpgsqlParameter("@maxLon", maxLon),
            new NpgsqlParameter("@maxLat", maxLat),
            new NpgsqlParameter("@cellSize", cellSize),
        };

        return await dbContext.HeatmapPoints
            .FromSqlRaw(sql, parameters)
            .Select(p => new HeatmapPointDto
            {
                Latitude = p.Geom.Y,
                Longitude = p.Geom.X,
                Count = p.PointCount
            })
            .ToListAsync(ct);
    }

    public async Task<List<ClusterDto>> GetPostClustersAsync(CancellationToken ct)
    {
        var k = ClusterConstants.ClusterCount;

        var sql = $@"
            WITH pts AS (
                SELECT 
                    ST_ClusterKMeans(""Location""::geometry, {k}) OVER () AS cluster_id,
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

        return await dbContext.Clusters
            .FromSqlRaw(sql)
            .Select(c => new ClusterDto
            {
                Id = c.ClusterId,
                Latitude = c.Latitude,
                Longitude = c.Longitude,
                Count = c.Count
            })
            .ToListAsync(ct);
    }

    public async Task<List<TimestampedPostLocationDto>> GetUserPostLocationsAsync(string userId, CancellationToken ct)
    {
        return await dbContext.Posts
            .Where(p => p.AuthorId == userId && p.Location != null)
            .Select(p => new TimestampedPostLocationDto
            {
                PostId = p.Id,
                Latitude = p.Location!.Y,
                Longitude = p.Location.X,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);
    }
}
