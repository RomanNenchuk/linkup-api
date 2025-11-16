using Application.Common.DTOs;

namespace Application.Posts.Commands.CreatePost;

public class CreatePostDto
{
    public string AuthorId { get; set; } = null!;
    public string Content { get; set; } = null!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public List<CloudinaryUploadDto>? Photos { get; set; }
}
