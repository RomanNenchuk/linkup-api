using Application.Common.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Posts.Queries.GetPostComments;

public class PostCommentResponseDto
{
    public string Id { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public AuthorDto Author { get; set; } = null!;
    public string? RepliedTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class Mapping : Profile
{
    public Mapping()
    {
        CreateMap<PostComment, PostCommentResponseDto>();
    }
}