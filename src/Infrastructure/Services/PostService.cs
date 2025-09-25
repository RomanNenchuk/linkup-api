using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Posts.Commands.CreatePost;
using Application.Posts.Queries.GetPosts;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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

    public async Task<Result<PagedResult<PostResponseDto>>> GetTopPostsAsync(GetPostsQuery query, CancellationToken ct)
    {
        int offset = 0;
        if (!string.IsNullOrEmpty(query.Cursor) && int.TryParse(query.Cursor, out var parsed))
            offset = parsed;


        // var cacheKey = $"best-posts:{query.PageSize}:{offset}";

        // if (cache.TryGetValue(cacheKey, out PagedResult<PostResponseDto>? cached))
        //     return Result<PagedResult<PostResponseDto>>.Success(cached);

        var oneWeekAgo = DateTime.UtcNow.AddDays(-7);

        var postsQuery = dbContext.Posts
            .Include(p => p.PostPhotos)
            .Include(p => p.PostReactions)
            .Where(p => p.CreatedAt >= oneWeekAgo)
            .OrderByDescending(p => p.PostReactions.Count)
            .ThenByDescending(p => p.CreatedAt);

        var posts = await postsQuery
            .Skip(offset)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var result = await ConvertToPagedResult(posts, query, ct);

        result.NextCursor = posts.Count == query.PageSize
            ? (offset + query.PageSize).ToString()
            : null;

        // cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        // {
        //     AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        // });

        return Result<PagedResult<PostResponseDto>>.Success(result);
    }

    public async Task<Result<PagedResult<PostResponseDto>>> GetFollowingPostsAsync(GetPostsQuery query, CancellationToken ct)
    {
        if (currentUser?.Id is null)
            return Result<PagedResult<PostResponseDto>>.Success(new PagedResult<PostResponseDto> { Items = [] });

        var followeeIds = dbContext.UserFollows
            .Where(f => f.FollowerId == currentUser.Id)
            .Select(f => f.FolloweeId);

        var postsQuery = dbContext.Posts
            .Include(p => p.PostPhotos)
            .Include(p => p.PostReactions)
            .Where(p => followeeIds.Contains(p.AuthorId))
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id).AsQueryable(); ;

        if (!string.IsNullOrEmpty(query.Cursor))
            postsQuery = ApplyCursorPaging(query.Cursor, postsQuery);


        var posts = await postsQuery
            .Take(query.PageSize)
            .ToListAsync(ct);

        var result = await ConvertToPagedResult(posts, query, ct);

        result.NextCursor = posts.Count == query.PageSize && query.Filter.Type != PostFilterType.Top
            ? $"{posts.Last().CreatedAt:o}|{posts.Last().Id}"
            : null;

        return Result<PagedResult<PostResponseDto>>.Success(result);
    }

    public async Task<Result<PagedResult<PostResponseDto>>> GetRecentPostsAsync(GetPostsQuery query, CancellationToken ct)
    {
        var postsQuery = dbContext.Posts
            .Include(p => p.PostPhotos)
            .Include(p => p.PostReactions)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id).AsQueryable();

        if (!string.IsNullOrEmpty(query.Cursor))
            postsQuery = ApplyCursorPaging(query.Cursor, postsQuery);

        var posts = await postsQuery
            .Take(query.PageSize)
            .ToListAsync(ct);

        var result = await ConvertToPagedResult(posts, query, ct);
        result.NextCursor = posts.Count == query.PageSize && query.Filter.Type != PostFilterType.Top
            ? $"{posts.Last().CreatedAt:o}|{posts.Last().Id}"
            : null;

        return Result<PagedResult<PostResponseDto>>.Success(result);
    }

    private async Task<PagedResult<PostResponseDto>> ConvertToPagedResult(List<Post> posts, GetPostsQuery query, CancellationToken ct)
    {
        var authorIds = posts.Select(p => p.AuthorId).Distinct().ToList();
        var authors = await dbContext.Users
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

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

        return new PagedResult<PostResponseDto>
        {
            Items = convertedPosts,
        };
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

    private static IQueryable<Post> ApplyCursorPaging(string cursor, IQueryable<Post> postsQuery)
    {
        var cursorParts = cursor.Split('|');
        if (cursorParts.Length == 2 &&
            DateTime.TryParse(cursorParts[0], null,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var cursorDate))
        {
            var cursorId = cursorParts[1];

            postsQuery = postsQuery.Where(p =>
                p.CreatedAt < cursorDate ||
                (p.CreatedAt == cursorDate && string.Compare(p.Id, cursorId) < 0));
        }

        return postsQuery;
    }

}