namespace Domain.Entities;

public class PostCommentReaction
{
    public string UserId { get; set; } = null!;
    public string PostCommentId { get; set; } = null!;
    public PostComment PostComment { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}