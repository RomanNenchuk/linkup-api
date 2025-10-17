namespace Domain.Entities;

public class PostComment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public string AuthorId { get; set; } = null!;
    public Post Post { get; set; } = null!;
    public string? RepliedTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}