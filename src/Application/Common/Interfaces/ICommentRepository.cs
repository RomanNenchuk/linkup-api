using Domain.Entities;

namespace Application.Common.Interfaces;

public interface ICommentRepository
{
    Task<PostComment?> GetByIdAsync(string commentId, CancellationToken ct);
    Task<List<PostComment>> GetPostCommentsAsync(string postId, CancellationToken ct);
    Task AddAsync(PostComment comment, CancellationToken ct);
    Task RemoveAsync(PostComment comment, CancellationToken ct);
}