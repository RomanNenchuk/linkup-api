using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Posts.Commands.CreatePost;
using Application.Posts.Queries.GetPosts;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Infrastructure.Services;

public class PostService(ApplicationDbContext dbContext, IMapper mapper, UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser) : IPostService
{
    public async Task<Result<string>> CreatePostAsync(CreatePostDto dto)
    {
        try
        {
            var creator = await userManager.FindByIdAsync(dto.AuthorId);
            if (creator == null)
                return Result<string>.Failure("Author not found");

            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

            var post = new Post
            {
                AuthorId = dto.AuthorId,
                Title = dto.Title,
                Content = dto.Content,
                Location = (dto.Latitude.HasValue && dto.Longitude.HasValue)
                    ? geometryFactory.CreatePoint(new Coordinate(dto.Longitude.Value, dto.Latitude.Value))
                    : null,
                Address = dto.Address,
                PostPhotos = dto.Photos?
                    .Select(photo => new PostPhoto
                    {
                        Url = photo.Url,
                        PublicId = photo.PublicId
                    })
                    .ToList() ?? [],
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Posts.Add(post);
            var saved = await dbContext.SaveChangesAsync() > 0;
            if (!saved)
                return Result<string>.Failure("Failed to create post");

            return Result<string>.Success(post.Id);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Failed to create post: {ex.Message}");
        }
    }

    public async Task<Result<PagedResult<PostResponseDto>>> GetPostsAsync(GetPostsQuery query, CancellationToken ct)
    {
        var postsQuery = dbContext.Posts
            .Include(p => p.PostPhotos)
            .Include(p => p.PostReactions)
            .AsQueryable();

        // Sorting
        postsQuery = query.Ascending
            ? postsQuery.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id)
            : postsQuery.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id);

        // Cursor
        if (!string.IsNullOrEmpty(query.Cursor))
        {
            var cursorParts = query.Cursor.Split('|');
            if (cursorParts.Length == 2 &&
                DateTime.TryParse(cursorParts[0], null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var cursorDate))
            {
                var cursorId = cursorParts[1];
                if (query.Ascending)
                    postsQuery = postsQuery.Where(p =>
                        p.CreatedAt > cursorDate ||
                        (p.CreatedAt == cursorDate && string.Compare(p.Id, cursorId) > 0));
                else
                    postsQuery = postsQuery.Where(p =>
                        p.CreatedAt < cursorDate ||
                        (p.CreatedAt == cursorDate && string.Compare(p.Id, cursorId) < 0));
            }
        }


        var posts = await postsQuery
            .Take(query.PageSize)
            .ToListAsync(ct);

        // Authors sampling
        var authorIds = posts.Select(p => p.AuthorId).Distinct().ToList();
        var authors = await dbContext.Users
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);


        // Liked posts only if authorized
        List<string> likedPostIds = [];
        if (currentUser?.Id is not null)
        {
            likedPostIds = await dbContext.PostReactions
                .Where(r => r.UserId == currentUser.Id && posts.Select(p => p.Id).Contains(r.PostId))
                .Select(r => r.PostId)
                .ToListAsync(ct);
        }

        var convertedPosts = posts.Select(p =>
        {
            var dto = mapper.Map<PostResponseDto>(p);
            if (authors.TryGetValue(p.AuthorId, out var author))
            {
                dto.Author = new AuthorDto
                {
                    Id = author.Id,
                    DisplayName = author.DisplayName ?? "Unknown"
                };
            }
            dto.IsLikedByCurrentUser = likedPostIds.Contains(p.Id);
            return dto;
        }).ToList();

        return Result<PagedResult<PostResponseDto>>.Success(new PagedResult<PostResponseDto>
        {
            Items = convertedPosts,
            NextCursor = posts.Count == query.PageSize
                ? $"{posts.Last().CreatedAt:o}|{posts.Last().Id}"
                : null
        });

    }

    public async Task<Result> TogglePostReactionAsync(string postId, string userId, bool isLiked)
    {
        var post = await dbContext.Posts.FirstOrDefaultAsync(x => x.Id == postId);
        if (post == null) return Result.Failure("Post does not exist");

        var reaction = await dbContext.PostReactions.FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId);
        if (reaction == null) dbContext.Add(new PostReaction { UserId = userId, PostId = postId });
        else dbContext.Remove(reaction);

        var result = await dbContext.SaveChangesAsync() > 0;

        return result ? Result.Success() : Result.Failure("Failed to toggle reaction");

    }

}