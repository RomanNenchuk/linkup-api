namespace Web.DTOs;

public class EditPostRequest
{
    public string Content { get; set; } = null!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public IFormFileCollection? PhotosToAdd { get; set; }
    public List<string>? PhotosToDelete { get; set; }
}

