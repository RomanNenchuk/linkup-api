using AutoMapper;
using Domain.Entities;

namespace Application.Common.DTOs;

public class PostResponseDto
{
    public string Id { get; set; } = null!;
    public string Content { get; set; } = null!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public int ReactionCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<PostPhotoDto> Photos { get; set; } = new();

    public AuthorDto Author { get; set; } = null!;
}

public class PostPhotoDto
{
    public string Id { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string PublicId { get; set; } = null!;
}

public class PagedPostsDto
{
    public List<PostResponseDto> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}

public class Mapping : Profile
{
    public Mapping()
    {
        CreateMap<Domain.Entities.Post, PostResponseDto>()
            .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => src.PostPhotos))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Location != null ? src.Location.Y : (double?)null))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Location != null ? src.Location.X : (double?)null));

        CreateMap<PostPhoto, PostPhotoDto>();
    }
}