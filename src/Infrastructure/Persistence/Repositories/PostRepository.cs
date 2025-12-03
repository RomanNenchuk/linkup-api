using Application.Common.Interfaces;
using Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Entities;
using Infrastructure.Identity;
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
}