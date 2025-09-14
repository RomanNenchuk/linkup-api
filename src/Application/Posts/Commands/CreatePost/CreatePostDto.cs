namespace Application.Posts.Commands.CreatePost;

public class CreatePostDto
{
    public string AuthorId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Content { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public List<string>? PhotoUrls { get; set; }
}
