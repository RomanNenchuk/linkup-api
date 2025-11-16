using Application.Common.DTOs;

namespace Application.Posts.Commands.EditPost;

public class EditPostDto
{
    public string PostId { get; set; } = null!;
    public string? Content { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public List<string>? PhotosToDelete { get; set; }
    public List<CloudinaryUploadDto>? PhotosToAdd { get; set; }
}
