using Application.Common;
using Application.Common.Interfaces;
using Application.Posts.Commands.CreatePost;
using Domain.Entities;
using Infrastructure.Persistence;

namespace Infrastructure.Services;

public class PostService(ApplicationDbContext dbContext) : IPostService
{
    public async Task<Result<Post>> CreatePostAsync(CreatePostDto dto)
    {
        try
        {
            var post = new Post
            {
                AuthorId = dto.AuthorId,
                Title = dto.Title,
                Content = dto.Content,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Address = dto.Address,
                PostPhotos = dto.PhotoUrls?
                    .Select(url => new PostPhoto
                    {
                        Url = url
                    })
                    .ToList() ?? [],
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Posts.Add(post);
            var result = await dbContext.SaveChangesAsync() > 0;

            return result ? Result<Post>.Success(post) : Result<Post>.Failure("Failed to create post");
        }
        catch (Exception ex)
        {
            return Result<Post>.Failure($"Failed to create post: {ex.Message}");
        }
    }
}