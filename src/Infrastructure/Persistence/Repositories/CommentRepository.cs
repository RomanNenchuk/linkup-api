using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CommentRepository(ApplicationDbContext dbContext) : ICommentRepository
{
    public Task<PostComment?> GetByIdAsync(string commentId, CancellationToken ct)
    {
        return dbContext.PostComments.FirstOrDefaultAsync(c => c.Id == commentId, ct);
    }

    public Task<List<PostComment>> GetPostCommentsAsync(string postId, CancellationToken ct)
    {
        return dbContext.PostComments
            .Where(c => c.PostId == postId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(PostComment comment, CancellationToken ct)
    {
        dbContext.PostComments.Add(comment);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(PostComment comment, CancellationToken ct)
    {
        dbContext.PostComments.Remove(comment);
        await dbContext.SaveChangesAsync(ct);
    }
}
