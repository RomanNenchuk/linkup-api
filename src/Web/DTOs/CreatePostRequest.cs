using Microsoft.AspNetCore.Mvc;

namespace Web.DTOs;

public class CreatePostRequest
{
    [FromForm] public string Title { get; set; } = null!;
    [FromForm] public string? Content { get; set; }
    [FromForm] public double? Latitude { get; set; }
    [FromForm] public double? Longitude { get; set; }
    [FromForm] public string? Address { get; set; }
    [FromForm] public List<IFormFile>? PostPhotos { get; set; }
}