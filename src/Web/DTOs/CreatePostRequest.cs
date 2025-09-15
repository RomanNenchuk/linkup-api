using Microsoft.AspNetCore.Mvc;

namespace Web.DTOs;

public class CreatePostRequest
{
    public string Title { get; set; } = null!;
    public string? Content { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public IFormFileCollection? PostPhotos { get; set; }
}