namespace Domain.Entities;

public class Post
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public List<PostPhoto> Photos { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}