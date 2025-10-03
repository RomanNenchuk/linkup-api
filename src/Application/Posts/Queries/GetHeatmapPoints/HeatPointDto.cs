using NetTopologySuite.Geometries;

namespace Application.Posts.Queries.GetHeatmapPoints;

public class HeatmapPoint
{
    public Point Geom { get; set; } = default!;
    public int PointCount { get; set; }
}

public class HeatmapPointDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Count { get; set; }
}
