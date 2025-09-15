using AutoMapper;
using Domain.Entities;

namespace Application.Common.DTOs;

public class PostResponseDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Content { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public string AuthorId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<PostPhotoDto> Photos { get; set; } = new();
}

public class PostPhotoDto
{
    public string Id { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string PublicId { get; set; } = null!;
}

public class Mapping : Profile
{
    public Mapping()
    {
        CreateMap<Domain.Entities.Post, PostResponseDto>()
            .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => src.PostPhotos));
        CreateMap<PostPhoto, PostPhotoDto>();
    }
}