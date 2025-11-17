using Application.Posts.Commands.CreatePostComment;
using Application.Posts.Queries.GetPostComments;

namespace Application.Common.Interfaces;

public interface ICommentService
{
    Task<Result<string>> CreatePostCommentAsync(CreatePostCommentDto dto);
    Task<Result<List<PostCommentResponseDto>>> GetPostCommentsAsync(string postId);

    Task<Result> DeletePostCommentAsync(string commentId);
    Task<Result> TogglePostCommentReactionAsync(string commentId, string userId, bool isLiked);
}
