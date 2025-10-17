using NetTopologySuite.Geometries;

namespace Domain.Entities;

public class Post
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AuthorId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Content { get; set; }
    public Point? Location { get; set; }
    public string? Address { get; set; }
    public List<PostComment> PostComments { get; set; } = [];
    public List<PostPhoto> PostPhotos { get; set; } = [];
    public List<PostReaction> PostReactions { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}