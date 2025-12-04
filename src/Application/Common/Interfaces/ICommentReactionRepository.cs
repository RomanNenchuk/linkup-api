using Domain.Entities;

namespace Application.Common.Interfaces;

public interface ICommentReactionRepository
{
    Task<PostCommentReaction?> GetReactionAsync(string commentId, string userId, CancellationToken ct);
    Task AddReactionAsync(PostCommentReaction reaction, CancellationToken ct);
    Task RemoveReactionAsync(PostCommentReaction reaction, CancellationToken ct);
    Task<List<string>> GetLikedCommentIdsAsync(string userId, List<string> commentIds, CancellationToken ct);
}