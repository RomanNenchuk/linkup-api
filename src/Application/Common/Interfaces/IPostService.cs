using Application.Posts.Commands.CreatePost;

namespace Application.Common.Interfaces;

public interface IPostService
{
    Task<Result<Domain.Entities.Post>> CreatePostAsync(CreatePostDto dto);
}
