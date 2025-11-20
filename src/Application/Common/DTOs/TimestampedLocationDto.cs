namespace Application.Common.DTOs;

public class TimestampedPostLocationDto
{
    public string PostId { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
}
