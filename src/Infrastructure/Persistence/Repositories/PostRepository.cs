using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Infrastructure.Persistence.Repositories;

public class PostRepository(ApplicationDbContext dbContext, IMapper mapper) : IPostRepository
{
    public async Task<List<Post>> GetTopPostsAsync(DateTime minCreatedAt, int offset, int pageSize, double? lat, double? lng, double? radiusKm, CancellationToken ct)
    {
        var query = dbContext.Posts
            .Include(p => p.PostPhotos)
            .Where(p => p.CreatedAt >= minCreatedAt)
            .OrderByDescending(p => dbContext.PostReactions.Count(r => r.PostId == p.Id))
            .ThenByDescending(p => dbContext.PostComments.Count(c => c.PostId == p.Id))
            .ThenByDescending(p => p.CreatedAt)
            .AsQueryable();

        if (lat.HasValue && lng.HasValue && radiusKm.HasValue)
            query = ApplyLocationFilter(lat.Value, lng.Value, radiusKm.Value, query);

        return await query
            .Skip(offset)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<Dictionary<string, User>> GetAuthorsAsync(List<string> authorIds, CancellationToken ct)
    {
        var users = await dbContext.Users
            .Where(u => authorIds.Contains(u.Id))
            .ProjectTo<User>(mapper.ConfigurationProvider)
            .ToDictionaryAsync(u => u.Id, ct);

        return users;
    }

    public async Task<List<Post>> GetFollowingPostsAsync(IEnumerable<string> followeeIds, string? cursor, int pageSize, double? lat, double? lng, double? radiusKm, CancellationToken ct)
    {
        var query = dbContext.Posts
            .Include(p => p.PostPhotos)
            .Where(p => followeeIds.Contains(p.AuthorId))
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .AsQueryable();

        if (!string.IsNullOrEmpty(cursor))
            query = ApplyCursorPaging(cursor, query);

        if (lat.HasValue && lng.HasValue && radiusKm.HasValue)
            query = ApplyLocationFilter(lat.Value, lng.Value, radiusKm.Value, query);

        return await query
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<List<Post>> GetRecentPostsAsync(string? authorId, string? cursor, int pageSize, double? lat, double? lng, double? radiusKm, CancellationToken ct)
    {
        var query = dbContext.Posts
            .Include(p => p.PostPhotos)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .AsQueryable();

        if (authorId != null)
            query = query.Where(p => p.AuthorId == authorId);

        if (!string.IsNullOrEmpty(cursor))
            query = ApplyCursorPaging(cursor, query);

        if (lat.HasValue && lng.HasValue && radiusKm.HasValue)
            query = ApplyLocationFilter(lat.Value, lng.Value, radiusKm.Value, query);

        return await query
            .Take(pageSize)
            .ToListAsync(ct);
    }
    public async Task<int> GetReactionCountAsync(string postId, CancellationToken ct)
    {
        var dict = await GetReactionCountsAsync([postId], ct);
        return dict.TryGetValue(postId, out var count) ? count : 0;
    }

    public async Task<int> GetCommentCountAsync(string postId, CancellationToken ct)
    {
        var dict = await GetCommentCountsAsync([postId], ct);
        return dict.TryGetValue(postId, out var count) ? count : 0;
    }

    public Task<bool> IsPostLikedByUserAsync(string postId, string userId, CancellationToken ct)
    {
        return dbContext.PostReactions.AnyAsync(r => r.PostId == postId && r.UserId == userId, ct);
    }

    public async Task<Dictionary<string, int>> GetReactionCountsAsync(List<string> postIds, CancellationToken ct)
    {
        return await dbContext.PostReactions
            .Where(r => postIds.Contains(r.PostId))
            .GroupBy(r => r.PostId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
    }

    public async Task<Dictionary<string, int>> GetCommentCountsAsync(List<string> postIds, CancellationToken ct)
    {
        return await dbContext.PostComments
            .Where(c => postIds.Contains(c.PostId))
            .GroupBy(c => c.PostId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
    }

    public async Task<List<string>> GetLikedPostIdsAsync(List<string> postIds, string? currentUserId, CancellationToken ct)
    {
        if (currentUserId == null)
            return [];

        return await dbContext.PostReactions
            .Where(r => r.UserId == currentUserId && postIds.Contains(r.PostId))
            .Select(r => r.PostId)
            .ToListAsync(ct);
    }

    private static IQueryable<Post> ApplyCursorPaging(string cursor, IQueryable<Post> postsQuery)
    {
        var parts = cursor.Split('|');
        if (parts.Length == 2 &&
            DateTime.TryParse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
        {
            var id = parts[1];

            postsQuery = postsQuery.Where(p =>
                p.CreatedAt < dt ||
                (p.CreatedAt == dt && string.Compare(p.Id, id) < 0));
        }

        return postsQuery;
    }

    private static IQueryable<Post> ApplyLocationFilter(double lat, double lng, double radiusKm, IQueryable<Post> postsQuery)
    {
        var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var center = factory.CreatePoint(new Coordinate(lng, lat));

        return postsQuery
            .Where(p => p.Location != null &&
                        p.Location.IsWithinDistance(center, radiusKm * 1000));
    }

    public async Task<Post?> GetPostWithPhotosAsync(string postId, CancellationToken ct = default)
    {
        return await dbContext.Posts
            .Include(p => p.PostPhotos)
            .FirstOrDefaultAsync(p => p.Id == postId, ct);
    }

    public async Task<Post?> GetPostByIdAsync(string postId, CancellationToken ct = default)
    {
        return await dbContext.Posts.FirstOrDefaultAsync(p => p.Id == postId, ct);
    }

    public async Task<Result> DeletePostAsync(Post post, CancellationToken ct)
    {
        dbContext.Remove(post);
        var result = await dbContext.SaveChangesAsync(ct) > 0;

        return result ? Result.Success() : Result.Failure("Failed to delete post");

    }

    public async Task AddPostAsync(Post post, CancellationToken ct)
    {
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync(ct);
    }

    public Task<bool> SaveChangesAsync(CancellationToken ct)
    {
        return Task.FromResult(dbContext.SaveChangesAsync(ct).Result > 0);
    }

    public Task<List<Point>> GetUserLocationsAsync(string? userId, CancellationToken ct)
    {
        if (userId == null) return Task.FromResult(new List<Point>());
        return dbContext.Posts.Where(p => p.AuthorId == userId && p.Location != null).Select(p => p.Location!).ToListAsync(ct);
    }

    public async Task<List<(string UserId, int SameLocations)>> GetLocationCandidateAuthorsAsync(
        string? userId, List<string> followingIds, List<Point> userLocations, double radiusMeters, int limit, CancellationToken ct)
    {
        if (userLocations.Count == 0) return [];

        var candidates = await dbContext.Posts
            .Where(p =>
                p.AuthorId != userId &&
                !followingIds.Contains(p.AuthorId) &&
                p.Location != null &&
                userLocations.Any(loc => loc.Distance(p.Location!) <= radiusMeters))
            .GroupBy(p => p.AuthorId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync(ct);

        return candidates.Select(c => (c.UserId, c.Count)).ToList();
    }
}