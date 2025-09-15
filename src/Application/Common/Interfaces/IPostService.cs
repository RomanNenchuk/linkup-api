using Application.Common.DTOs;
using Application.Posts.Commands.CreatePost;

namespace Application.Common.Interfaces;

public interface IPostService
{
    Task<Result<PostResponseDto>> CreatePostAsync(CreatePostDto dto);
}
