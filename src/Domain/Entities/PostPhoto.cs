namespace Domain.Entities;

public class PostPhoto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PostId { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string? Description { get; set; }
    public Post Post { get; set; } = null!;
}
