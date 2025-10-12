using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Application.Common.DTOs;

[Keyless]
public class Cluster
{
    public int ClusterId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Count { get; set; }
}

