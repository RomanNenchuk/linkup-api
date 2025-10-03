using System.Globalization;
using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Posts.Commands.CreatePost;
using Application.Posts.Commands.EditPost;
using Application.Posts.Queries.GetHeatmapPoints;
using Application.Posts.Queries.GetPosts;
using AutoMapper;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Infrastructure.Services;

public class PostService(ApplicationDbContext dbContext, IMapper mapper, UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser, ICloudinaryService cloudinaryService) : IPostService
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
            .ThenByDescending(p => p.CreatedAt).AsQueryable();

        if (query.Filter.Latitude.HasValue && query.Filter.Longitude.HasValue && query.Filter.RadiusKm.HasValue)
            postsQuery = ApplyLocationFilter(query.Filter.Latitude.Value, query.Filter.Longitude.Value,
                query.Filter.RadiusKm.Value, postsQuery);

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

        if (query.Filter.Latitude.HasValue && query.Filter.Longitude.HasValue && query.Filter.RadiusKm.HasValue)
            postsQuery = ApplyLocationFilter(query.Filter.Latitude.Value, query.Filter.Longitude.Value,
                query.Filter.RadiusKm.Value, postsQuery);

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

        if (query.Filter.Latitude.HasValue && query.Filter.Longitude.HasValue && query.Filter.RadiusKm.HasValue)
            postsQuery = ApplyLocationFilter(query.Filter.Latitude.Value, query.Filter.Longitude.Value,
                query.Filter.RadiusKm.Value, postsQuery);


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

    private static IQueryable<Post> ApplyLocationFilter(double latitude, double longitude, double radiusKm, IQueryable<Post> postsQuery)
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var center = geometryFactory.CreatePoint(
            new Coordinate(longitude, latitude));

        postsQuery = postsQuery.Where(p => p.Location != null &&
                                        p.Location.IsWithinDistance(center, radiusKm * 1000));

        return postsQuery;
    }

    public async Task<Result> EditPostAsync(EditPostDto dto)
    {
        try
        {
            var userId = currentUser.Id!;
            var existingPost = await dbContext.Posts
                .Include(p => p.PostPhotos)
                .FirstOrDefaultAsync(p => p.Id == dto.PostId);

            if (existingPost == null)
                return Result.Failure("Post not found");

            if (userId != existingPost.AuthorId)
                return Result.Failure("Access denied");

            // --- Text data ---
            if (!string.IsNullOrWhiteSpace(dto.Title))
                existingPost.Title = dto.Title;

            if (!string.IsNullOrWhiteSpace(dto.Content))
                existingPost.Content = dto.Content;

            if (!string.IsNullOrWhiteSpace(dto.Address))
                existingPost.Address = dto.Address;

            // --- Location ---
            if (dto.Latitude.HasValue && dto.Longitude.HasValue)
            {
                var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
                existingPost.Location = geometryFactory.CreatePoint(
                    new Coordinate(dto.Longitude.Value, dto.Latitude.Value)
                );
            }

            // --- Photo ---
            if (dto.PhotosToDelete is not null && dto.PhotosToDelete.Count > 0)
            {
                foreach (var publicId in dto.PhotosToDelete)
                    await cloudinaryService.DeleteImageAsync(publicId);

                existingPost.PostPhotos.RemoveAll(p => dto.PhotosToDelete.Contains(p.PublicId));
            }

            if (dto.PhotosToAdd is not null && dto.PhotosToAdd.Count > 0)
            {
                foreach (var photo in dto.PhotosToAdd)
                {
                    existingPost.PostPhotos.Add(new PostPhoto
                    {
                        PostId = dto.PostId,
                        Url = photo.Url,
                        PublicId = photo.PublicId
                    });
                }
            }

            existingPost.UpdatedAt = DateTime.UtcNow;

            var saved = await dbContext.SaveChangesAsync() > 0;
            if (!saved)
                return Result.Failure("Failed to update post");

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to update post: {ex.Message}");
        }
    }

    public async Task<Result<PostResponseDto>> GetPostByIdAsync(string postId, CancellationToken ct)
    {
        var post = await dbContext.Posts.Include(p => p.PostPhotos).FirstOrDefaultAsync(p => p.Id == postId, ct);
        if (post == null) return Result<PostResponseDto>.Failure("Post not found");

        return Result<PostResponseDto>.Success(mapper.Map<PostResponseDto>(post));
    }

    public async Task<Result> ValidatePhotoLimitAsync(string postId, int photosToAddCount, List<string>? photosToDeleteList, CancellationToken ct)
    {
        var post = await dbContext.Posts.FirstOrDefaultAsync(p => p.Id == postId, ct);
        if (post == null) return Result.Failure("Post not found");
        var uniquePhotosToDelete = photosToDeleteList?.Distinct().ToList() ?? [];

        if (uniquePhotosToDelete.Count > 0)
        {
            var missingPhotos = uniquePhotosToDelete.Where(publicId => !post.PostPhotos.Any(p => p.PublicId == publicId));
            if (missingPhotos.Any()) return Result.Failure("Invalid set of photos. Some photos not found in post.");
        }

        var resultingPhotoNumber = post.PostPhotos.Count + photosToAddCount - uniquePhotosToDelete?.Count ?? 0;

        if (resultingPhotoNumber > PostConstants.MaxPhotosPerPost)
            return Result.Failure($"You can't upload more that {PostConstants.MaxPhotosPerPost} photos.");

        return Result.Success();
    }

    public async Task<Result> DeletePostAsync(string postId)
    {
        var post = await dbContext.Posts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null) return Result.Failure("Post not found");
        if (post.AuthorId != currentUser.Id) return Result.Failure("Access denied");

        if (post.PostPhotos.Count > 0)
        {
            foreach (var photo in post.PostPhotos)
            {
                await cloudinaryService.DeleteImageAsync(photo.PublicId);
            }
        }

        dbContext.Remove(post);
        var result = await dbContext.SaveChangesAsync() > 0;

        return result ? Result.Success() : Result.Failure("Failed to delete post");

    }

    public async Task<Result<List<HeatmapPointDto>>> GetHeatmapPointsAsync(
        double minLon, double maxLon, double minLat, double maxLat, int zoom, CancellationToken ct)
    {
        double cellSize = zoom switch
        {
            <= 5 => 0.5,
            <= 8 => 0.2,
            <= 12 => 0.05,
            _ => 0.01
        };

        string sql;

        if (zoom >= 6)
        {
            sql = @"
            SELECT 
                ST_MakePoint(
                    AVG(ST_X(""Location""::geometry)),
                    AVG(ST_Y(""Location""::geometry))
                ) AS ""Geom"",
                COUNT(*) AS ""PointCount""
            FROM ""Posts""
            WHERE ""Location""::geometry && ST_MakeEnvelope(@minLon, @minLat, @maxLon, @maxLat, 4326)
            GROUP BY ST_SnapToGrid(""Location""::geometry, @cellSize, @cellSize)
             ";
        }
        else
        {
            sql = @"
            SELECT 
                ST_SnapToGrid(""Location""::geometry, @cellSize, @cellSize) AS ""Geom"",
                COUNT(*) AS ""PointCount""
            FROM ""Posts""
            WHERE ""Location""::geometry && ST_MakeEnvelope(@minLon, @minLat, @maxLon, @maxLat, 4326)
            GROUP BY ST_SnapToGrid(""Location""::geometry, @cellSize, @cellSize)
            ";
        }

        var parameters = new[]
        {
            new Npgsql.NpgsqlParameter("@minLon", minLon),
            new Npgsql.NpgsqlParameter("@minLat", minLat),
            new Npgsql.NpgsqlParameter("@maxLon", maxLon),
            new Npgsql.NpgsqlParameter("@maxLat", maxLat),
            new Npgsql.NpgsqlParameter("@cellSize", cellSize)
        };

        var points = await dbContext.HeatmapPoints
            .FromSqlRaw(sql, parameters)
            .Select(p => new HeatmapPointDto
            {
                Latitude = p.Geom.Y,
                Longitude = p.Geom.X,
                Count = p.PointCount
            })
            .ToListAsync(ct);

        return Result<List<HeatmapPointDto>>.Success(points);
    }


}
