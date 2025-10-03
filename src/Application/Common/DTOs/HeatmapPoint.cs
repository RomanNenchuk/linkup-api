using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Application.Common.DTOs;

[Keyless]
public class HeatmapPoint
{
    public Point Geom { get; set; } = default!;
    public int PointCount { get; set; }
}
