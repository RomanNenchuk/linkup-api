namespace Domain.Entities;

public class PostReaction
{
    public string UserId { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public Post Post { get; set; } = null!;
}