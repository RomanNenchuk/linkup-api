using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Posts.Commands.CreatePost;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Persistence;

namespace Infrastructure.Services;

public class PostService(ApplicationDbContext dbContext, IMapper mapper) : IPostService
{
    public async Task<Result<PostResponseDto>> CreatePostAsync(CreatePostDto dto)
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
                PostPhotos = dto.ImageRecords?
                    .Select(photo => new PostPhoto
                    {
                        Url = photo.Url,
                        PublicId = photo.PublicId
                    })
                    .ToList() ?? [],
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Posts.Add(post);
            var result = await dbContext.SaveChangesAsync() > 0;
            if (!result) return Result<PostResponseDto>.Failure("Failed to create post");

            var postDto = mapper.Map<PostResponseDto>(post);
            return Result<PostResponseDto>.Success(postDto);
        }
        catch (Exception ex)
        {
            return Result<PostResponseDto>.Failure($"Failed to create post: {ex.Message}");
        }
    }
}