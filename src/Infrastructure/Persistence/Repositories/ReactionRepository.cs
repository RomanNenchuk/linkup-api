using System;
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class PostReactionRepository(ApplicationDbContext dbContext) : IPostReactionRepository
{
    public Task<PostReaction?> GetReactionAsync(string postId, string userId, CancellationToken ct = default)
    {
        return dbContext.PostReactions
            .FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId, ct);
    }

    public async Task AddReactionAsync(PostReaction reaction, CancellationToken ct = default)
    {
        dbContext.PostReactions.Add(reaction);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task RemoveReactionAsync(PostReaction reaction, CancellationToken ct = default)
    {
        dbContext.PostReactions.Remove(reaction);
        await dbContext.SaveChangesAsync(ct);
    }
}
