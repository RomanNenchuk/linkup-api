using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CommentReactionRepository(ApplicationDbContext dbContext) : ICommentReactionRepository
{

    public Task<PostCommentReaction?> GetReactionAsync(string commentId, string userId, CancellationToken ct)
    {
        return dbContext.PostCommentReactions
            .FirstOrDefaultAsync(r => r.PostCommentId == commentId && r.UserId == userId, ct);
    }

    public async Task AddReactionAsync(PostCommentReaction reaction, CancellationToken ct)
    {
        dbContext.PostCommentReactions.Add(reaction);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task RemoveReactionAsync(PostCommentReaction reaction, CancellationToken ct)
    {
        dbContext.PostCommentReactions.Remove(reaction);
        await dbContext.SaveChangesAsync(ct);
    }

    public Task<List<string>> GetLikedCommentIdsAsync(string userId, List<string> commentIds, CancellationToken ct)
    {
        return dbContext.PostCommentReactions
            .Where(r => r.UserId == userId && commentIds.Contains(r.PostCommentId))
            .Select(r => r.PostCommentId)
            .ToListAsync(ct);
    }
}
